using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    public class Subscription
    {
        private static string InsertSubscriptionCommandString = "INSERT into dbo.SubscriptionTable (DeviceId, SubscriptionType, PushUrl, UserId) VALUES (@deviceid, @subscriptionType, @pushUrl, @userId);";

        private static string UpdateSubscriptionCommandString = "UPDATE dbo.SubscriptionTable SET PushUrl = @pushUrl, UserId = @userId WHERE DeviceId = @deviceId AND SubscriptionType=@subscriptionType";

        private static string SubscriptionsQueryString =
            "SELECT UserId, PushUrl FROM dbo.SubscriptionTable WHERE {0}";

        private static string SubscriptionsUserQueryString =
            "SELECT PushUrl " +
            "FROM dbo.SubscriptionTable " +
            "WHERE UserId = @userId " +
            "AND DeviceId = @deviceId";

        private static string DeleteSubscriptionCommandString =
            "DELETE FROM dbo.SubscriptionTable WHERE dbo.SubscriptionTable.DeviceId = @deviceId AND dbo.SubscriptionTable.SubscriptionType = @subscriptionType";

        private static string UserIdFormatString = "UserId = {0}";

        private static string OrString = " OR ";

        private string deviceid;

        private SubscriptionType type;

        private string url;

        private int userId;

        public Subscription(string deviceid, SubscriptionType type, string url, int userId)
        {
            this.type = type;
            this.url = url;
            this.userId = userId;
            this.deviceid = deviceid;
        }

        [DataMember]
        public string DeviceId
        {
            get
            {
                return this.deviceid;
            }
        }

        [DataMember]
        public SubscriptionType SubscriptionType
        {
            get
            {
                return this.type;
            }
        }

        [DataMember]
        public string Url
        {
            get
            {
                return this.url;
            }
        }

        [DataMember]
        public int UserId
        {
            get
            {
                return this.userId;
            }
        }

        internal static Subscription Subscribe(
            string deviceid,
            SubscriptionType type,
            string url,
            int userId,
            SqlTransaction sqlTransaction,
            SqlConnection connection)
        {
            // Open the connection in a try/catch block.
            // Create and execute the DataReader, writing the result
            // set to the console window.
            try
            {
                // Create the Command and Parameter objects.
                using (SqlCommand command = new SqlCommand(Subscription.InsertSubscriptionCommandString, connection, sqlTransaction))
                {
                    command.Parameters.AddWithValue("@deviceid", deviceid);
                    command.Parameters.AddWithValue("@subscriptionType", type);
                    command.Parameters.AddWithValue("@pushUrl", url);
                    command.Parameters.AddWithValue("@userId", userId);

                    int result = command.ExecuteNonQuery();

                    if (result > 0)
                    {
                        sqlTransaction.Commit();

                        return new Subscription(deviceid, type, url, userId);
                    }
                    else
                    {
                        sqlTransaction.Rollback();
                    }

                    UserService.Instance.RemoveUserFromCache(userId);
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

        internal static Subscription UpdateOrInsertSubscription(string deviceid, SubscriptionType type, string url, int userId)
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
                        using (SqlCommand command = new SqlCommand(Subscription.UpdateSubscriptionCommandString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@deviceid", deviceid);
                            command.Parameters.AddWithValue("@subscriptionType", type);
                            command.Parameters.AddWithValue("@pushUrl", url);
                            command.Parameters.AddWithValue("@userId", userId);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();

                                return new Subscription(deviceid, type, url, userId);
                            }
                            else
                            {
                                return Subscription.Subscribe(deviceid, type, url, userId, sqlTransaction, connection);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        sqlTransaction.Rollback();
                    }
                    finally
                    {
                        UserService.Instance.RemoveUserFromCache(userId);
                        connection.Close();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        internal static List<Tuple<User, string>> GetSubscriptions(int recipientId, User sender)
        {
            User recipient = UserService.Instance.GetUserFromId(recipientId);
            List<Tuple<User, string>> urls = new List<Tuple<User, string>>();

            if (recipient.UserType == UserType.User)
            {
                if (recipient.SubscriptionUrls != null)
                {
                    for (int i = 0; i < recipient.SubscriptionUrls.Count; i++)
                    {
                        urls.Add(new Tuple<User, string>(recipient, recipient.SubscriptionUrls[i]));
                    }

                    return urls;
                }
            }

            if (recipient.UserType == UserType.Group)
            {
                Group group = (Group)recipient;

                return Subscription.GetSubscriptionsForUsers(group.Members, sender);
            }

            return null;
        }

        /// <summary>
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        internal static List<string> GetSubscriptionsForUser(int userId, string deviceId)
        {

            if (string.IsNullOrEmpty(deviceId))
            {
                return new List<string>();
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
                        using (SqlCommand command = new SqlCommand(Subscription.SubscriptionsUserQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", userId);
                            command.Parameters.AddWithValue("@deviceId", deviceId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<string> urls = new List<string>();

                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                urls.Add((string)dataset.Tables[0].Rows[i][0]);
                            }

                            return urls;
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
        /// Search for the phone based on the phone number
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        internal static List<Tuple<User, string>> GetSubscriptionsForUsers(List<User> users, User sender)
        {
            List<Tuple<User, string>> urls = new List<Tuple<User, string>>();

            for (int i = 0; i < users.Count; i++)
            {
                if (users[i].SubscriptionUrls != null)
                {
                    for (int subs = 0; subs < users[i].SubscriptionUrls.Count; subs++)
                    {
                        if (sender.Id != users[i].Id)
                        {
                            urls.Add(new Tuple<User, string>(users[i], users[i].SubscriptionUrls[subs]));
                        }
                    }
                }
            }

            return urls;
        }

        internal static void Unsubscribe(string deviceid, DataAccessLayer.SubscriptionType subscriptionType, int p)
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
                        using (SqlCommand command = new SqlCommand(Subscription.DeleteSubscriptionCommandString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@deviceid", deviceid);
                            command.Parameters.AddWithValue("@subscriptionType", subscriptionType);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();
                            }
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
    }
}

