using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    /// <summary>
    /// UserService is a cache of the user objects.
    /// All operations and query on users should go through the userservice.
    /// </summary>
    public class UserService
    {
        /********************IMPORTANT*******************************
         * Make sure that all the queries that query the users have the
         * same columns in the same order. Because the users are cached
         * in memory, accidentally missing a few properties could be
         * catastrophic.
         ********************IMPORTANT*******************************/
        private static string UserPhoneNumberQueryString = "SELECT ID, PhoneNumber, Name, Secret, UserType, LastSyncTime, PublicKey, GroupOwner, RegisteredDevice, RegistrationDate from dbo.UserTable WHERE PhoneNumber = @phoneNumber AND UserType = @userType ORDER BY ID ASC;";

        private static string UserIdQueryString =
            "SELECT ID, PhoneNumber, Name, Secret, UserType, LastSyncTime, PublicKey, GroupOwner, RegisteredDevice, RegistrationDate from dbo.UserTable WHERE ID = @id ORDER BY ID ASC;";

        private static string InsertUserCommandString = "INSERT into dbo.UserTable (PhoneNumber, Name, Secret, UserType) VALUES (@phoneNumber, @name, @secret, @userType); SELECT scope_identity();";

        private static string UpdateUserNameCommandString = "UPDATE dbo.UserTable SET Name = @name WHERE ID = @userId AND UserType = @userType";

        private static string UpdateDeviceIdCommandString = "UPDATE dbo.UserTable SET RegisteredDevice = @device, RegistrationDate = @registrationdate WHERE ID = @userId AND UserType = @userType";

        private static string UpdateLastSyncTimeCommand = "UPDATE dbo.UserTable SET LastSyncTime = @lastSyncTime WHERE ID = @userId";

        private static string UpdateUserPublicKeyCommand = "UPDATE dbo.UserTable SET PublicKey = @publicKey WHERE ID = @userId";

        private static string UsersQueryString = "SELECT ID, PhoneNumber, Name, Secret, UserType, LastSyncTime, PublicKey, GroupOwner, RegisteredDevice, RegistrationDate from dbo.UserTable {0} ORDER BY Name ASC;";

        private static string WhereClause = "WHERE";

        private static string IdClause = " ID = {0} ";

        private static string PhoneNumberClause = " PhoneNumber = '{0}' ";

        private ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

        private Dictionary<int, User> userCache = new Dictionary<int, User>();

        private Dictionary<string, User> phoneUserCache = new Dictionary<string, User>();

        private Dictionary<object, object> userLockObjects = new Dictionary<object, object>();

        public static UserService Instance = new UserService();

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

                User user = UserService.InternalGetUserFromId(id);

                if (user == null)
                {
                    return null;
                }

                this.userCache.Add(id, user);
                if (user.UserType != UserType.Group)
                {
                    this.phoneUserCache.Add(user.PhoneNumber, user);
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

                User user = UserService.InternalGetUserFromPhone(phoneNumber);

                if (user != null)
                {

                    this.userCache.Add(user.Id, user);
                    if (user.UserType != UserType.Group)
                    {
                        this.phoneUserCache.Add(user.PhoneNumber, user);
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
            this.RemoveUserFromCache(user.Id);
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
                this.phoneUserCache.Remove(user.PhoneNumber);
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
                user.SubscriptionUrls = Subscription.GetSubscriptionsForUser(userId, user.RegisteredDevice);
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
                        if (!addedUsers.ContainsKey(user.Id))
                        {
                            users.Add(this.phoneUserCache[phoneNumber]);
                            addedUsers.Add(user.Id, user);
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

            List<User> notFoundUsers = UserService.InternalGetUsersFromPhones(notFoundPhoneNumbers);
            foreach (User u in notFoundUsers)
            {
                if (!addedUsers.ContainsKey(u.Id))
                {
                    users.Add(u);
                    addedUsers.Add(u.Id, u);
                }
            }

            foreach (User user in notFoundUsers)
            {
                object lockObject = null;

                try
                {
                    lock (this.userLockObjects)
                    {
                        if (!this.userLockObjects.ContainsKey(user.Id))
                        {
                            this.userLockObjects.Add(user.Id, new object());
                        }

                        lockObject = this.userLockObjects[user.Id];
                    }

                    Monitor.Enter(lockObject);

                    if (this.userCache.ContainsKey(user.Id))
                    {
                        continue;
                    }

                    this.userCache.Add(user.Id, user);
                    this.phoneUserCache.Add(user.PhoneNumber, user);
                }
                finally
                {
                    Monitor.Exit(lockObject);

                    lock (this.userLockObjects)
                    {
                        if (this.userLockObjects.ContainsKey(user.Id))
                        {
                            this.userLockObjects.Remove(user.Id);
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
                        using (SqlCommand command = new SqlCommand(UserService.UserPhoneNumberQueryString, connection, sqlTransaction))
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

                            return UserService.CreateUserFromRow(dataset.Tables[0].Rows[0]);
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
                        using (SqlCommand command = new SqlCommand(UserService.UserIdQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@id", id);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            if (dataset.Tables[0].Rows.Count != 1)
                            {
                                return null;
                            }

                            return UserService.CreateUserFromRow(dataset.Tables[0].Rows[0]);
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

            whereClauses.Append(UserService.WhereClause);

            for (int i = 0; i < phoneNumbers.Count; i++)
            {
                if (i != 0)
                {
                    whereClauses.Append("OR");
                }

                whereClauses.AppendFormat(UserService.PhoneNumberClause, phoneNumbers[i]);
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
                        string commandString = string.Format(UserService.UsersQueryString, whereClauses);
                        using (SqlCommand command = new SqlCommand(commandString, connection, sqlTransaction))
                        {
                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<User> users = new List<User>();

                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                User u = UserService.CreateUserFromRow(dataset.Tables[0].Rows[i]);

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

        private static User CreateUserFromRow(DataRow dataRow)
        {
            if ((UserType)dataRow[4] == UserType.User)
            {
                DateTime LastSyncTime = DateTime.MinValue;

                if (!DBNull.Value.Equals(dataRow[5]))
                {
                    LastSyncTime = (DateTime)dataRow[5];
                }

                byte[] publicKey = null;

                if (!DBNull.Value.Equals(dataRow[6]))
                {
                    publicKey = (byte[])dataRow[6];
                }

                return new User(
                    (int)dataRow[0],
                    (string)dataRow[1],
                    (string)dataRow[2],
                    (string)dataRow[3],
                    LastSyncTime,
                    publicKey,
                    dataRow[8] == DBNull.Value ? string.Empty : (string)dataRow[8],
                    dataRow[8] == DBNull.Value ? 0 : (long)dataRow[9]);
            }
            else
            {
                int groupOwnerId = (int)dataRow[7];
                User groupOwner = UserService.Instance.GetUserFromId(groupOwnerId);
                Group group = new Group((int)dataRow[0], (string)dataRow[2], groupOwner);

                return group;
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
                        using (SqlCommand command = new SqlCommand(UserService.InsertUserCommandString, connection, sqlTransaction))
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
                            return new User(identity, phoneNumber, name, secret);
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

        public void UpdateName(User user, string name)
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
                        using (SqlCommand command = new SqlCommand(UserService.UpdateUserNameCommandString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@name", name);
                            command.Parameters.AddWithValue("@userId", user.Id);
                            command.Parameters.AddWithValue("@userType", UserType.User);

                            int result = command.ExecuteNonQuery();

                            executionTime = watch.ElapsedMilliseconds;
                            watch.Restart();

                            if (result > 0)
                            {
                                user.Name = name;
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

        public void UpdateDeviceId(User user, string deviceId)
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
                        long registrationdate = DateTime.UtcNow.Ticks;
                        // Create the Command and Parameter objects.
                        using (SqlCommand command = new SqlCommand(UserService.UpdateDeviceIdCommandString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@device", deviceId);
                            command.Parameters.AddWithValue("@registrationdate", registrationdate);
                            command.Parameters.AddWithValue("@userId", user.Id);
                            command.Parameters.AddWithValue("@userType", UserType.User);

                            int result = command.ExecuteNonQuery();

                            executionTime = watch.ElapsedMilliseconds;
                            watch.Restart();

                            if (result > 0)
                            {
                                user.RegisteredDevice = deviceId;
                                user.RegistrationDate = registrationdate;
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

            whereClauses.Append(UserService.WhereClause);

            for (int i = 0; i < participants.Count; i++)
            {
                if (i != 0)
                {
                    whereClauses.Append("OR");
                }

                whereClauses.AppendFormat(UserService.IdClause, participants[i]);
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
                        string commandString = string.Format(UserService.UsersQueryString, whereClauses);
                        using (SqlCommand command = new SqlCommand(commandString, connection, sqlTransaction))
                        {
                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            List<User> users = new List<User>();

                            for (int i = 0; i < dataset.Tables[0].Rows.Count; i++)
                            {
                                users.Add(UserService.CreateUserFromRow(dataset.Tables[0].Rows[i]));
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
                        using (SqlCommand command = new SqlCommand(UserService.UpdateLastSyncTimeCommand, connection, sqlTransaction))
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

                        User user = UserService.Instance.GetUserFromId(UserId);
                        user.LastSyncTime = lastSyncTime;
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
                        using (SqlCommand command = new SqlCommand(UserService.UpdateUserPublicKeyCommand, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", user.Id);
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

                        user.PublicKey = publicKey;
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
