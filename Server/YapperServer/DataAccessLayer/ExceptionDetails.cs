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
    public class ExceptionDetails
    {
        private static string InsertExceptionCommandString = "INSERT into dbo.ExceptionDetailsTable" +
                " (UserId, ExceptionString, ExceptionDate, Version)" +
                " VALUES (@userId, @exceptionString, @exceptionDate, @version); SELECT Scope_Identity();";

        public ExceptionDetails()
        {
            this.Id = -1;
        }

        public ExceptionDetails(
            int userId,
            string exceptionString)
        {
            this.UserId = userId;
            this.ExceptionString = exceptionString;
            this.ExceptionDate = DateTime.UtcNow;
            this.Id = -1;
        }

        public int Id
        {
            get;
            set;
        }

        [DataMember]
        public int UserId
        {
            get;
            set;
        }

        [DataMember]
        public string ExceptionString
        {
            get;
            set;
        }

        [DataMember]
        public DateTime ExceptionDate
        {
            get;
            set;
        }

        [DataMember]
        public string Version
        {
            get;
            set;
        }

        public void Save(User user)
        {
            if (string.IsNullOrEmpty(this.ExceptionString) ||
                string.IsNullOrEmpty(this.Version))
            {
                return;
            }

            DateTime time = DateTime.UtcNow;

            // Create and open the connection in a using block. This
            // ensures that all resources will be closed and disposed
            // when the code exits.
            using (SqlConnection connection = new SqlConnection(Globals.SqlConnectionString))
            {
                connection.Open();

                // Create or update the conversation object
                using (SqlTransaction sqlTransaction = connection.BeginTransaction())
                {
                    // Open the connection in a try/catch block.
                    // Create and execute the DataReader, writing the result
                    // set to the console window.
                    try
                    {
                        if (user.Id != this.UserId)
                        {
                            throw new Exception("Invalid exception details");
                        }

                        // Create the Command and Parameter objects.
                        SqlCommand command = new SqlCommand(ExceptionDetails.InsertExceptionCommandString, connection, sqlTransaction);
                        command.Parameters.AddWithValue("@userId", user.Id);
                        command.Parameters.AddWithValue("@exceptionString", this.ExceptionString.Count<char>() > 2000 ? this.ExceptionString.Substring(0, 2000) : this.ExceptionString);
                        command.Parameters.AddWithValue("@exceptionDate", this.ExceptionDate);
                        command.Parameters.AddWithValue("@version", this.Version);

                        SqlParameter outParameter = new SqlParameter("@Id", SqlDbType.Int);
                        outParameter.Direction = ParameterDirection.Output;
                        command.Parameters.Add(outParameter);

                        int identity = (int)(decimal)command.ExecuteScalar();

                        sqlTransaction.Commit();
                        this.Id = identity;
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
