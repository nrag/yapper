using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace DataAccessLayer
{
    /// <summary>
    /// 
    /// </summary>
    [DataContract(Name = "Group", Namespace = "http://schemas.datacontract.org/2004/07/Yapper")]
    public class Group : User
    {
        private static string InsertGroupCommand = "INSERT into dbo.UserTable (Name, PhoneNumber, UserType, GroupOwner) VALUES (@name, @phoneNumber, @userType, @groupOwner); SELECT Scope_Identity()";

        private static string InsertGroupMemberCommand = "INSERT into dbo.GroupTable (GroupId, UserId) VALUES (@groupId, @userId)";

        private static string RemoveGroupMemberCommand = "DELETE from dbo.GroupTable WHERE GroupId = @groupId AND UserId= @userId";

        private static string GroupIdQueryString = "SELECT ID, PhoneNumber, Name, Secret, UserType from dbo.UserTable WHERE ID = @id AND GroupType = @userType ORDER BY ID ASC;";

        private static string UpdateGroupCommandString = "UPDATE dbo.UserTable SET Name = @name WHERE ID = @userId AND GroupType = @userType";

        private static string QueryGroupsForUser = "Select dbo.GroupTable.GroupId, dbo.UserTable.Name, dbo.UserTable.GroupOwner FROM dbo.GroupTable JOIN dbo.UserTable on dbo.GroupTable.GroupId = dbo.UserTable.ID Where dbo.GroupTable.UserId = @userId";

        private static string QueryMembersForGroup = "Select UserId FROM dbo.GroupTable Where GroupId = @groupId";

        public Group(int id, string name, User groupOwner)
            : this(id, name, groupOwner, Group.GetMembers(id))
        {
        }

        public Group(int id, string name, User groupOwner, List<User> users) : base(id, string.Empty, name, string.Empty)
        {
            this.Members = users;
            this.Owner = groupOwner;
        }

        [DataMember]
        public List<User> Members
        {
            get;
            set;
        }

        [DataMember]
        public User Owner
        {
            get;
            set;
        }

        internal override bool CanSendMessage(User sender)
        {
            foreach (User m in this.Members)
            {
                if (m.Id == sender.Id)
                {
                    return true;
                }
            }

            return false;
        }

        internal static List<Group> GetGroupsForUser(int userId)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(Group.QueryGroupsForUser, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", userId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<Group> groups = new List<Group>();
                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                int ownerId = (int)dataset.Tables[0].Rows[i][2];
                                User owner = UserService.Instance.GetUserFromId(ownerId);
                                groups.Add(new Group((int)dataset.Tables[0].Rows[i][0], (string)dataset.Tables[0].Rows[i][1], owner));
                            }

                            return groups;
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        internal static List<int> GetGroupIdsForUser(int userId)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(Group.QueryGroupsForUser, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", userId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<int> groups = new List<int>();
                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                groups.Add((int)dataset.Tables[0].Rows[i][0]);
                            }

                            return groups;
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        private static List<User> GetMembers(int groupId)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(Group.QueryMembersForGroup, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@groupId", groupId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<User> users = new List<User>();
                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                User member = UserService.Instance.GetUserFromId((int)dataset.Tables[0].Rows[i][0]);
                                if (member != null)
                                {
                                    users.Add(member);
                                }
                            }

                            return users;
                        }
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="name"></param>
        /// <remarks>
        /// Note that because the table is not locked and phonenumber is not the primary key,
        /// another instance of the webapp could register with the same phone number.
        /// </remarks>
        /// <returns>the registered user</returns>
        internal static Group CreateGroup(User creator, string name, List<User> users)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction())
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(Group.InsertGroupCommand, connection, sqlTransaction))
                        {
                            // Create group
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@phoneNumber", "NoPhone");
                            command.Parameters.AddWithValue("@userType", UserType.Group);
                            command.Parameters.AddWithValue("@groupOwner", creator.Id);

                            SqlParameter outParameter = new SqlParameter("@UserId", SqlDbType.Int);
                            outParameter.Direction = ParameterDirection.Output;
                            command.Parameters.Add(outParameter);

                            int identity = (int)(decimal)command.ExecuteScalar();

                            // Add members
                            for (int i = 0; i < users.Count; i++)
                            {
                                Group.AddGroupMember(identity, users[i], connection, sqlTransaction);
                            }

                            // For each user in the cache, update the list of groups in the user object
                            for (int i = 0; i < users.Count; i++)
                            {
                                // Remove the user from cache so that
                                // the correct set of group will be loaded
                                // next time
                                UserService.Instance.RemoveUserFromCache(users[i]);
                            }

                            sqlTransaction.Commit();

                            return new Group(identity, name, creator, users);
                        }
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        connection.Close();
                    }

                    return null;
                }
            }
        }

        internal static bool AddGroupMember(User caller, Group group, User member)
        {
            if (group.Owner.Id != caller.Id)
            {
                return false;
            }

            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction())
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        using (SqlCommand command = new SqlCommand(Group.InsertGroupMemberCommand, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@groupId", group.Id);
                            command.Parameters.AddWithValue("@userId", member.Id);

                            command.ExecuteNonQuery();

                            // Remove the user from cache so that
                            // the correct set of group will be loaded
                            // next time
                            UserService.Instance.RemoveUserFromCache(member);
                            UserService.Instance.RemoveUserFromCache(group);
                            
                            sqlTransaction.Commit();

                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        connection.Close();
                    }

                    return false;
                }
            }
        }

        private static void AddGroupMember(int identity, User member, SqlConnection connection, SqlTransaction transaction)
        {
            using (SqlCommand command = new SqlCommand(Group.InsertGroupMemberCommand, connection, transaction))
            {
                command.Parameters.AddWithValue("@groupId", identity);
                command.Parameters.AddWithValue("@userId", member.Id);

                command.ExecuteNonQuery();
            }
        }

        internal static bool RemoveGroupMember(User caller, Group group, User member)
        {
            if (group.Owner.Id != caller.Id)
            {
                return false;
            }

            if (!group.Members.Contains(member))
            {
                return false;
            }

            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction())
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        using (SqlCommand command = new SqlCommand(Group.RemoveGroupMemberCommand, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@groupId", group.Id);
                            command.Parameters.AddWithValue("@userId", member.Id);

                            command.ExecuteNonQuery();

                            // Remove the user from cache so that
                            // the correct set of group will be loaded
                            // next time
                            UserService.Instance.RemoveUserFromCache(member);
                            UserService.Instance.RemoveUserFromCache(group);

                            sqlTransaction.Commit();

                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        connection.Close();
                    }

                    return false;
                }
            }
        }
    }
}
