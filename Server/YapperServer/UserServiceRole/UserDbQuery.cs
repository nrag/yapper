    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

namespace UserServiceRole
{
    /// <summary>
    /// UserService is a cache of the user objects.
    /// All operations and query on users should go through the userservice.
    /// </summary>
    public class UserDbQuery
    {
        private static string UserPhoneNumberQueryString = "SELECT ID, PhoneNumber, Name, Secret, LastSyncTime, PublicKey from dbo.UserTable WHERE PhoneNumber = @phoneNumber AND UserType = @userType ORDER BY ID ASC;";

        private static string UserIdQueryString =
            "SELECT ID, PhoneNumber, Name, Secret, UserType, LastSyncTime, PublicKey, GroupOwner from dbo.UserTable WHERE ID = @id ORDER BY ID ASC;";

        private static string InsertUserCommandString = "INSERT into dbo.UserTable (PhoneNumber, Name, Secret, UserType) VALUES (@phoneNumber, @name, @secret, @userType); SELECT scope_identity();";

        private static string UpdateUserCommandString = "UPDATE dbo.UserTable SET Name = @name WHERE ID = @userId AND UserType = @userType";

        private static string UpdateLastSyncTimeCommand = "UPDATE dbo.UserTable SET LastSyncTime = @lastSyncTime WHERE ID = @userId";

        private static string UpdateUserPublicKeyCommand = "UPDATE dbo.UserTable SET PublicKey = @publicKey WHERE ID = @userId";

        private static string UsersQueryString = "SELECT ID, PhoneNumber, Name, Secret, UserType, PublicKey, GroupOwner from dbo.UserTable {0} ORDER BY Name ASC;";

        private static string QueryMembersForGroup = "Select UserId FROM dbo.GroupTable Where GroupId = @groupId";

        private static string WhereClause = "WHERE";

        private static string IdClause = " ID = {0} ";

        private static string PhoneNumberClause = " PhoneNumber = '{0}' ";

        private ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        private Dictionary<int, User> userCache = new Dictionary<int, User>();

        private Dictionary<string, User> phoneUserCache = new Dictionary<string, User>();

        private Dictionary<object, object> userLockObjects = new Dictionary<object, object>();

        public static UserDbQuery Instance = new UserDbQuery();

        public User GetUserFromId(int id)
        {
            try
            {
                this.readerWriterLock.EnterReadLock();

                if (this.userCache.ContainsKey(id))
                {
                    return this.userCache[id];
                }
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }

            // We don't use a global lock while reading the user from the table
            // We lock only the 'id' that we are trying to retrieve. For each id,
            // we use a dummy lock object. This lock object is used to synchronize
            // the threads attempting to read the same user object.
            // The list of lock objects are stored in a dictionary.
            // We first lock the dictionary and get/create a lock object for this id.
            // Then we lock the lock object and check if another thread added the user to
            // the cache already. If so, we return.
            // Else we read it from the database, add the user to the user cache and exit.
            object lockObject = null;

            try
            {
                lock (this.userLockObjects)
                {
                    if (!this.userLockObjects.ContainsKey(id))
                    {
                        this.userLockObjects.Add(id, new object());
                    }

                    lockObject = this.userLockObjects[id];
                }

                Monitor.Enter(lockObject);

                if (this.userCache.ContainsKey(id))
                {
                    return this.userCache[id];
                }

                User user = UserDbQuery.InternalGetUserFromId(id);

                if (user == null)
                {
                    return null;
                }

                this.userCache.Add(id, user);
                if (user.UserData.UserType != UserType.Group)
                {
                    this.phoneUserCache.Add(user.UserData.PhoneNumber, user);
                }

                return user;
            }
            finally
            {
                Monitor.Exit(lockObject);

                lock (this.userLockObjects)
                {
                    if (this.userLockObjects.ContainsKey(id))
                    {
                        this.userLockObjects.Remove(id);
                    }
                }
            }
        }

        public User GetUserFromPhone(string phoneNumber)
        {
            try
            {
                this.readerWriterLock.EnterReadLock();

                if (this.phoneUserCache.ContainsKey(phoneNumber))
                {
                    return this.phoneUserCache[phoneNumber];
                }
            }
            finally
            {
                this.readerWriterLock.ExitReadLock();
            }


            // We don't use a global lock while reading the user from the table
            // We lock only the 'id' that we are trying to retrieve. For each id,
            // we use a dummy lock object. This lock object is used to synchronize
            // the threads attempting to read the same user object.
            // The list of lock objects are stored in a dictionary.
            // We first lock the dictionary and get/create a lock object for this id.
            // Then we lock the lock object and check if another thread added the user to
            // the cache already. If so, we return.
            // Else we read it from the database, add the user to the user cache and exit.
            object lockObject = null;

            try
            {
                lock (this.userLockObjects)
                {
                    if (!this.userLockObjects.ContainsKey(phoneNumber))
                    {
                        this.userLockObjects.Add(phoneNumber, new object());
                    }

                    lockObject = this.userLockObjects[phoneNumber];
                }

                Monitor.Enter(lockObject);

                if (this.phoneUserCache.ContainsKey(phoneNumber))
                {
                    return this.phoneUserCache[phoneNumber];
                }

                User user = UserDbQuery.InternalGetUserFromPhone(phoneNumber);

                if (user != null)
                {

                    this.userCache.Add(user.UserData.Id, user);
                    if (user.UserData.UserType != UserType.Group)
                    {
                        this.phoneUserCache.Add(user.UserData.PhoneNumber, user);
                    }
                }

                return user;
            }
            finally
            {
                Monitor.Exit(lockObject);

                lock (this.userLockObjects)
                {
                    if (this.userLockObjects.ContainsKey(phoneNumber))
                    {
                        this.userLockObjects.Remove(phoneNumber);
                    }
                }
            }
        }

        public void RemoveUserFromCache(User user)
        {
            this.RemoveUserFromCache(user.UserData.Id);
        }

        public void RemoveUserFromCache(int userId)
        {
            this.readerWriterLock.EnterReadLock();

            if (!this.userCache.ContainsKey(userId))
            {
                return;
            }

            object lockObject = null;

            try
            {
                lock (this.userLockObjects)
                {
                    if (!this.userLockObjects.ContainsKey(userId))
                    {
                        this.userLockObjects.Add(userId, new object());
                    }

                    lockObject = this.userLockObjects[userId];

                    Monitor.Enter(lockObject);
                }

                if (!this.userCache.ContainsKey(userId))
                {
                    return;
                }

                User user = this.userCache[userId];
                this.userCache.Remove(userId);
                this.phoneUserCache.Remove(user.UserData.PhoneNumber);
            }
            finally
            {
                Monitor.Exit(lockObject);

                lock (this.userLockObjects)
                {
                    if (this.userLockObjects.ContainsKey(userId))
                    {
                        this.userLockObjects.Remove(userId);
                    }
                }

                this.readerWriterLock.ExitReadLock();
            }
        }

        public void ReloadUserSubscriptions(int userId)
        {
            this.readerWriterLock.EnterReadLock();

            if (!this.userCache.ContainsKey(userId))
            {
                return;
            }

            object lockObject = null;

            try
            {
                lock (this.userLockObjects)
                {
                    if (!this.userLockObjects.ContainsKey(userId))
                    {
                        this.userLockObjects.Add(userId, new object());
                    }

                    lockObject = this.userLockObjects[userId];

                    Monitor.Enter(lockObject);
                }

                if (!this.userCache.ContainsKey(userId))
                {
                    return;
                }

                User user = this.userCache[userId];
                user.UserData.SubscriptionUrls = Subscription.GetSubscriptionsForUser(userId);
            }
            finally
            {
                Monitor.Exit(lockObject);

                lock (this.userLockObjects)
                {
                    if (this.userLockObjects.ContainsKey(userId))
                    {
                        this.userLockObjects.Remove(userId);
                    }
                }

                this.readerWriterLock.ExitReadLock();
            }
        }

        public List<User> GetUsersFromPhones(List<string> phoneNumbers)
        {
            List<User> users = new List<User>();
            List<string> notFoundPhoneNumbers = new List<string>();
            Dictionary<int, User> addedUsers = new Dictionary<int, User>();

            foreach (string phoneNumber in phoneNumbers)
            {
                try
                {
                    this.readerWriterLock.EnterReadLock();

                    if (this.phoneUserCache.ContainsKey(phoneNumber))
                    {
                        User user = this.phoneUserCache[phoneNumber];
                        if (!addedUsers.ContainsKey(user.UserData.Id))
                        {
                            users.Add(this.phoneUserCache[phoneNumber]);
                            addedUsers.Add(user.UserData.Id, user);
                        }
                    }
                    else
                    {
                        notFoundPhoneNumbers.Add(phoneNumber);
                    }
                }
                finally
                {
                    this.readerWriterLock.ExitReadLock();
                }
            }

            List<User> notFoundUsers = UserDbQuery.InternalGetUsersFromPhones(notFoundPhoneNumbers);
            foreach (User u in notFoundUsers)
            {
                if (!addedUsers.ContainsKey(u.UserData.Id))
                {
                    users.Add(u);
                    addedUsers.Add(u.UserData.Id, u);
                }
            }

            foreach (User user in notFoundUsers)
            {
                object lockObject = null;

                try
                {
                    lock (this.userLockObjects)
                    {
                        if (!this.userLockObjects.ContainsKey(user.UserData.Id))
                        {
                            this.userLockObjects.Add(user.UserData.Id, new object());
                        }

                        lockObject = this.userLockObjects[user.UserData.Id];
                    }

                    Monitor.Enter(lockObject);

                    if (this.userCache.ContainsKey(user.UserData.Id))
                    {
                        continue;
                    }

                    this.userCache.Add(user.UserData.Id, user);
                    this.phoneUserCache.Add(user.UserData.PhoneNumber, user);
                }
                finally
                {
                    Monitor.Exit(lockObject);

                    lock (this.userLockObjects)
                    {
                        if (this.userLockObjects.ContainsKey(user.UserData.Id))
                        {
                            this.userLockObjects.Remove(user.UserData.Id);
                        }
                    }
                }
            }

            return users;
        }

        /// <summary>
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private static User InternalGetUserFromPhone(string phoneNumber)
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.UserPhoneNumberQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                            command.Parameters.AddWithValue("@userType", UserType.User);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            if (dataset.Tables[0].Rows.Count != 1)
                            {
                                return null;
                            }

                            DateTime lastSyncTime = DateTime.MinValue;

                            if (!DBNull.Value.Equals(dataset.Tables[0].Rows[0][4]))
                            {
                                lastSyncTime = (DateTime)dataset.Tables[0].Rows[0][4];
                            }

                            byte[] publicKey = null;
                            if (!DBNull.Value.Equals(dataset.Tables[0].Rows[0][5]))
                            {
                                publicKey = (byte[])dataset.Tables[0].Rows[0][5];
                            }

                            User user = new User();
                            user.UserData = new UserData();
                            user.UserData.Id = (int)dataset.Tables[0].Rows[0][0];
                            user.UserData.PhoneNumber = (string)dataset.Tables[0].Rows[0][1];
                            user.UserData.Name = (string)dataset.Tables[0].Rows[0][2];
                            user.UserData.Secret = (string)dataset.Tables[0].Rows[0][3];
                            user.UserData.LastSyncTimeTicks = lastSyncTime.Ticks;
                            user.UserData.PublicKey = publicKey;

                            return user;
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private static User InternalGetUserFromId(int id)
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.UserIdQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@id", id);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            if (dataset.Tables[0].Rows.Count != 1)
                            {
                                return null;
                            }

                            DateTime lastSyncTime = DateTime.MinValue;
                            byte[] publicKey = null;
                            if ((UserType)dataset.Tables[0].Rows[0][4] == UserType.User)
                            {
                                if (!DBNull.Value.Equals(dataset.Tables[0].Rows[0][5]))
                                {
                                    lastSyncTime = (DateTime)dataset.Tables[0].Rows[0][5];
                                }

                                if (!DBNull.Value.Equals(dataset.Tables[0].Rows[0][6]))
                                {
                                    publicKey = (byte[])dataset.Tables[0].Rows[0][6];
                                }
                            }

                            User user = new User();
                            user.UserData = new UserData();
                            user.UserData.Id = (int)dataset.Tables[0].Rows[0][0];
                            user.UserData.PhoneNumber = (string)dataset.Tables[0].Rows[0][1];
                            user.UserData.Name = (string)dataset.Tables[0].Rows[0][2];
                            user.UserData.Secret = (string)dataset.Tables[0].Rows[0][3];
                            user.UserData.LastSyncTimeTicks = lastSyncTime.Ticks;
                            user.UserData.UserType = (UserType)dataset.Tables[0].Rows[0][4];
                            user.UserData.PublicKey = publicKey;

                            if ((UserType)dataset.Tables[0].Rows[0][4] == UserType.Group)
                            {
                                user.GroupData.Owner = UserDbQuery.Instance.GetUserFromId((int)dataset.Tables[0].Rows[0][7]).UserData;
                                user.GroupData.Members = UserDbQuery.Instance.GetGroupMembers(user.UserData.Id);
                            }

                            return user;
                        }
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        private static List<User> InternalGetUsersFromPhones(List<string> phoneNumbers)
        {
            if (phoneNumbers == null || phoneNumbers.Count == 0)
            {
                return new List<User>();
            }

            StringBuilder whereClauses = new StringBuilder();

            whereClauses.Append(UserDbQuery.WhereClause);

            for (int i = 0; i < phoneNumbers.Count; i++)
            {
                if (i != 0)
                {
                    whereClauses.Append("OR");
                }

                whereClauses.AppendFormat(UserDbQuery.PhoneNumberClause, phoneNumbers[i]);
            }

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
                        string commandString = string.Format(UserDbQuery.UsersQueryString, whereClauses);
                        using (SqlCommand command = new SqlCommand(commandString, connection, sqlTransaction))
                        {
                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<User> users = new List<User>();

                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                User u = new User();
                                u.UserData = new UserData();
                                u.UserData.Id = (int)dataset.Tables[0].Rows[i][0];
                                u.UserData.PhoneNumber = (string)dataset.Tables[0].Rows[i][1];
                                u.UserData.Name = (string)dataset.Tables[0].Rows[i][2];
                                u.UserData.Secret = (string)dataset.Tables[0].Rows[i][3];
                                if (dataset.Tables[0].Rows[i][5] != DBNull.Value)
                                {
                                    u.UserData.PublicKey = (byte[])dataset.Tables[0].Rows[i][5];
                                }

                                users.Add(u);
                            }

                            return users;
                        }
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
        internal static User Register(string phoneNumber, string name)
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.InsertUserCommandString, connection, sqlTransaction))
                        {
                            string secret = Guid.NewGuid().ToString();
                            command.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@secret", secret);
                            command.Parameters.AddWithValue("@userType", UserType.User);

                            SqlParameter outParameter = new SqlParameter("@UserId", SqlDbType.BigInt);
                            outParameter.Direction = ParameterDirection.Output;
                            command.Parameters.Add(outParameter);

                            int identity = (int)(decimal)command.ExecuteScalar();

                            sqlTransaction.Commit();
                            User user = new User();
                            user.UserData = new UserData();
                            user.UserData.Id = identity;
                            user.UserData.PhoneNumber = phoneNumber;
                            user.UserData.Name = name;
                            user.UserData.Secret = secret;

                            return user;
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

        public void Update(User user, string name)
        {
            Stopwatch watch = new Stopwatch();
            long connectionTime = 0;
            long transctionTime = 0;
            long executionTime = 0;
            long commitTime = 0;

            watch.Start();
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();
                connectionTime = watch.ElapsedMilliseconds;
                watch.Restart();
                using (SqlTransaction sqlTransaction = connection.BeginTransaction())
                {
                    transctionTime = watch.ElapsedMilliseconds;
                    watch.Restart();

                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(UserDbQuery.UpdateUserCommandString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@userId", user.UserData.Id);
                            command.Parameters.AddWithValue("@userType", UserType.User);

                            int result = command.ExecuteNonQuery();

                            executionTime = watch.ElapsedMilliseconds;
                            watch.Restart();

                            if (result > 0)
                            {
                                user.UserData.Name = name;
                                sqlTransaction.Commit();
                            }
                            else
                            {
                                sqlTransaction.Rollback();
                            }

                            commitTime = watch.ElapsedMilliseconds;
                            watch.Restart();
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
                }
            }
        }

        internal static List<User> GetUsers(List<int> participants)
        {
            StringBuilder whereClauses = new StringBuilder();

            whereClauses.Append(UserDbQuery.WhereClause);

            for (int i = 0; i < participants.Count; i++)
            {
                if (i != 0)
                {
                    whereClauses.Append("OR");
                }

                whereClauses.AppendFormat(UserDbQuery.IdClause, participants[i]);
            }

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
                        string commandString = string.Format(UserDbQuery.UsersQueryString, whereClauses);
                        using (SqlCommand command = new SqlCommand(commandString, connection, sqlTransaction))
                        {
                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<User> users = new List<User>();

                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                User user = new User();
                                user.UserData = new UserData();
                                user.UserData.Id = (int)dataset.Tables[0].Rows[i][0];
                                user.UserData.PhoneNumber = (string)dataset.Tables[0].Rows[i][1];
                                user.UserData.Name = (string)dataset.Tables[0].Rows[i][2];
                                user.UserData.Secret = (string)dataset.Tables[0].Rows[i][3];
                                users.Add(user);
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

        internal static void UpdateUserLastSyncTime(int UserId, DateTime lastSyncTime)
        {
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.UpdateLastSyncTimeCommand, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", UserId);
                            command.Parameters.AddWithValue("@lastSyncTime", lastSyncTime);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();
                            }
                            else
                            {
                                sqlTransaction.Rollback();
                            }
                        }

                        User user = UserDbQuery.Instance.GetUserFromId(UserId);
                        user.UserData.LastSyncTimeTicks = lastSyncTime.Ticks;
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        internal static void UpdateUserPublicKey(User user, byte[] publicKey)
        {
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.UpdateUserPublicKeyCommand, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", user.UserData.Id);
                            command.Parameters.AddWithValue("@publicKey", publicKey);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();
                            }
                            else
                            {
                                sqlTransaction.Rollback();
                            }
                        }

                        user.UserData.PublicKey = publicKey;
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        connection.Close();
                    }
                }
            }
        }

        private List<UserData> GetGroupMembers(int groupId)
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
                        using (SqlCommand command = new SqlCommand(UserDbQuery.QueryMembersForGroup, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@groupId", groupId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<UserData> users = new List<UserData>();
                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                User member = UserDbQuery.Instance.GetUserFromId((int)dataset.Tables[0].Rows[i][0]);
                                if (member != null)
                                {
                                    users.Add(member.UserData);
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
    }
}
