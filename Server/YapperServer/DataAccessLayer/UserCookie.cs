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
    public class UserCookie
    {
        private static int CurrentCookieVersion = 1;

        private static string CookieQueryString = "SELECT Cookie from dbo.CookieTable WHERE UserId = @userId AND DeviceId = @deviceId;";

        private static string InsertCookieCommandString = "INSERT into dbo.CookieTable (UserId, DeviceId, Cookie) VALUES (@userId, @deviceId, @cookie);";

        private static string UpdateCookieCommandString = "UPDATE dbo.CookieTable SET Cookie = @cookie WHERE UserId = @userId AND DeviceId = @deviceId";

        private static string CookieFormatWithoutHashString = "yasv={0}|yasu={1}|yasd={2}|yase={3}";

        private static string CookieFormatString = "yasv={0}|yasu={1}|yasd={2}|yase={3}|yash={4}";

        private static string CookieVersionPrefix = "yasv=";

        private static string CookieUserPrefix = "yasu=";

        private static string CookieDeviceIdPrefix = "yasd=";

        private static string ExpiryDatePrefix = "yase=";

        private static string SignedHashPrefix = "yash=";

        public UserCookie(User user, string deviceId)
            : this(user, deviceId, DateTime.UtcNow.AddYears(1).Ticks)
        {
        }

        protected UserCookie(User user, string deviceId, long expiryDate)
            : this(user, deviceId, expiryDate, null)
        {
        }

        protected UserCookie(User user, string deviceId, long expiryDate, string hash)
        {
            this.User = user;
            this.DeviceId = deviceId;
            this.ExpiryDate = expiryDate;
            this.SignedHash = hash ?? SecureSigningService.Instance.SignAuthCookieV1(this.CookieWithoutHash);
            this.AuthCookie = string.Format(UserCookie.CookieFormatString, UserCookie.CurrentCookieVersion, this.User.Id, this.DeviceId, this.ExpiryDate, this.SignedHash);
        }

        [DataMember]
        public User User
        {
            get;
            set;
        }

        [DataMember]
        public string AuthCookie
        {
            get;
            set;
        }

        public string CookieWithoutHash
        {
            get
            {
                return string.Format(UserCookie.CookieFormatWithoutHashString, UserCookie.CurrentCookieVersion, this.User.Id, this.DeviceId, this.ExpiryDate);
            }
        }

        public string SignedHash
        {
            get;
            set;
        }

        public string DeviceId
        {
            get;
            set;
        }

        public long ExpiryDate
        {
            get;
            set;
        }

        public bool IsValid()
        {
            if (this.User == null)
            {
                return false;
            }

            // Validate that only one deviceid can be registered
            if (0 != StringComparer.OrdinalIgnoreCase.Compare(this.DeviceId, this.User.RegisteredDevice))
            {
                return false;
            }

            if (this.ExpiryDate < DateTime.UtcNow.Ticks)
            {
                return false;
            }

            return SecureSigningService.Instance.VerifyAuthCookieV1(this.CookieWithoutHash, this.SignedHash);
        }

        public override bool Equals(object obj)
        {
            UserCookie otherCookie = obj as UserCookie;

            if (otherCookie == null)
            {
                return false;
            }

            if (this.User.Id == otherCookie.User.Id &&
                this.DeviceId.Equals(otherCookie.DeviceId) &&
                this.AuthCookie.Equals(otherCookie.AuthCookie))
            {
                return true;
            }

            return false;
        }

        public static UserCookie Parse(string cookieString)
        {
            string[] parts = cookieString.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 5)
            {
                return null;
            }

            int version;
            if (!parts[0].StartsWith(UserCookie.CookieVersionPrefix) ||
                !Int32.TryParse(parts[0].Substring(UserCookie.CookieVersionPrefix.Length), out version))
            {
                return null;
            }

            if (version == 1)
            {
                return UserCookie.ParseV1Cookie(parts);
            }

            return null;
        }

        public static UserCookie ParseV1Cookie(string[] parts)
        {
            int userId;
            if (!parts[1].StartsWith(UserCookie.CookieUserPrefix) ||
                !Int32.TryParse(parts[1].Substring(UserCookie.CookieUserPrefix.Length), out userId))
            {
                return null;
            }

            User user = UserService.Instance.GetUserFromId(userId);
            if (user == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(parts[2]) ||
                !parts[2].StartsWith(UserCookie.CookieDeviceIdPrefix) ||
                parts[2].Length > 70)
            {
                return null;
            }

            long expiryDate;
            if (!parts[3].StartsWith(UserCookie.ExpiryDatePrefix) ||
                !Int64.TryParse(parts[3].Substring(UserCookie.ExpiryDatePrefix.Length), out expiryDate))
            {
                return null;
            }

            if (string.IsNullOrEmpty(parts[4]) ||
                !parts[4].StartsWith(UserCookie.SignedHashPrefix))
            {
                return null;
            }

            return new UserCookie(
                user,
                parts[2].Substring(UserCookie.CookieDeviceIdPrefix.Length),
                expiryDate,
                parts[4].Substring(UserCookie.SignedHashPrefix.Length));
        }
    }
}
