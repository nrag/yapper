using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserServiceRole
{
    public partial class UserCookie
    {
        private static string CookieQueryString = "SELECT Cookie from dbo.CookieTable WHERE UserId = @userId AND DeviceId = @deviceId;";

        private static string InsertCookieCommandString = "INSERT into dbo.CookieTable (UserId, DeviceId, Cookie) VALUES (@userId, @deviceId, @cookie);";

        private static string UpdateCookieCommandString = "UPDATE dbo.CookieTable SET Cookie = @cookie WHERE UserId = @userId AND DeviceId = @deviceId";

        private static string CookieFormatString = "yasu={0}|yasd={1}|yasc={2}";

        private static string CookieUserPrefix = "yasu=";

        private static string CookieDeviceIdPrefix = "yasd=";

        private static string CookieGuidPrefix = "yasc=";

        public static string SqlConnectionString = "";

        public UserCookie(int userId, string deviceId, string cookie)
        {
            this.UserId = userId;
            this.DeviceId = deviceId;
            this.Cookie = cookie;
        }

        public static UserCookie Parse(string cookieString)
        {
            string[] parts = cookieString.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 3)
            {
                return null;
            }

            int userId;
            if (!parts[0].StartsWith(UserCookie.CookieUserPrefix) ||
                !Int32.TryParse(parts[0].Substring(UserCookie.CookieUserPrefix.Length), out userId))
            {
                return null;
            }

            if (string.IsNullOrEmpty(parts[1]) ||
                !parts[1].StartsWith(UserCookie.CookieDeviceIdPrefix) ||
                parts[1].Length > 70)
            {
                return null;
            }

            Guid cookieGuid;
            if (!parts[2].StartsWith(UserCookie.CookieGuidPrefix) ||
                !Guid.TryParse(parts[2].Substring(UserCookie.CookieGuidPrefix.Length), out cookieGuid))
            {
                return null;
            }

            return new UserCookie(
                userId,
                parts[1].Substring(UserCookie.CookieDeviceIdPrefix.Length),
                parts[2].Substring(UserCookie.CookieGuidPrefix.Length));
        }

        /// <summary>
        /// Creates a cookie for the user
        /// </summary>
        /// <param name="existingUser"></param>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public static UserCookie GetCookie(UserData existingUser, string deviceId)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(UserCookie.SqlConnectionString))
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
                        using (SqlCommand command = new SqlCommand(UserCookie.CookieQueryString, connection, sqlTransaction))
                        {
                            command.Parameters.AddWithValue("@userId", existingUser.Id);
                            command.Parameters.AddWithValue("@deviceId", deviceId);

                            var adapter = new SqlDataAdapter(command);
                            var dataset = new DataSet();

                            adapter.Fill(dataset);

                            if (dataset.Tables[0].Rows.Count != 1)
                            {
                                return null;
                            }

                            return new UserCookie(existingUser.Id, deviceId, (string)dataset.Tables[0].Rows[0][0]);
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

        public void Update()
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
                        using (SqlCommand command = new SqlCommand(UserCookie.UpdateCookieCommandString, connection, sqlTransaction))
                        {
                            string cookie = Guid.NewGuid().ToString();
                            command.Parameters.AddWithValue("@userId", this.UserId);
                            command.Parameters.AddWithValue("@deviceId", this.DeviceId);
                            command.Parameters.AddWithValue("@cookie", cookie);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();
                                this.Cookie = cookie;
                            }
                            else
                            {
                                sqlTransaction.Rollback();
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

        public static UserCookie CreateCookie(UserData user, string deviceId)
        {
            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(UserCookie.SqlConnectionString))
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
                        using (SqlCommand command = new SqlCommand(UserCookie.InsertCookieCommandString, connection, sqlTransaction))
                        {
                            string cookie = Guid.NewGuid().ToString();
                            command.Parameters.AddWithValue("@userId", user.Id);
                            command.Parameters.AddWithValue("@deviceId", deviceId);
                            command.Parameters.AddWithValue("@cookie", cookie);

                            int result = command.ExecuteNonQuery();

                            if (result > 0)
                            {
                                sqlTransaction.Commit();
                                return new UserCookie(user.Id, deviceId, cookie);
                            }
                            else
                            {
                                sqlTransaction.Rollback();
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

                    return null;
                }
            }

        }

        public static bool Validate(UserData user, UserCookie cookie)
        {
            UserCookie realCookie = UserCookie.GetCookie(user, cookie.DeviceId);

            return realCookie.Equals(cookie);
        }

    }
}
