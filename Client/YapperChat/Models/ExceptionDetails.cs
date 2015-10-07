using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace YapperChat.Models
{
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    public class ExceptionDetails
    {
        private static string InsertExceptionCommandString = "INSERT into dbo.ExceptionDetailsTable" +
                " (UserId, ExceptionString, ExceptionDate)" +
                " VALUES (@userId, @exceptionString, @exceptionDate; SELECT Scope_Identity();";

        public ExceptionDetails()
        {
            var nameHelper = new AssemblyName(Assembly.GetExecutingAssembly().FullName);
            var version = nameHelper.Version;
            this.Version = version.ToString(3);
            this.ExceptionDate = DateTime.UtcNow;
        }

        public ExceptionDetails(
            int userId,
            string exceptionString) : this()
        {
            this.UserId = userId;
            this.ExceptionString = exceptionString;
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
    }
}
