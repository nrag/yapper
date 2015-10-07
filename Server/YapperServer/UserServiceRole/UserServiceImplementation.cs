using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserServiceRole
{
    class UserServiceImplementation :UserService.Iface
    {
        public UserCookie ValidateUser(string phoneNumber, int oneTimePassword, string deviceId, string random)
        {
            string normalizedPhone = PhoneNumberUtils.ValidatePhoneNumber(phoneNumber);

            User existingUser = UserDbQuery.Instance.GetUserFromPhone(normalizedPhone);
            if (existingUser == null)
            {
                throw new Exception("User not registered");
            }

            Authenticator.TOTP oneTimePasswordValidator = new Authenticator.TOTP(existingUser.UserData.Secret, 30, 6);
            if (!oneTimePasswordValidator.Verify(oneTimePassword))
            {
                throw new Exception("Invalid one-time password");
            }

            UserCookie cookie = UserCookie.GetCookie(existingUser.UserData, deviceId);
            if (cookie == null)
            {
                cookie = UserCookie.CreateCookie(existingUser.UserData, deviceId);
            }
            else
            {
                cookie.Update();
            }

            return cookie;
        }

        public User RegisterUser(string phoneNumber, string name, string deviceId)
        {
            throw new NotImplementedException();
        }

        public User CreateGroup(User newGroup)
        {
            throw new NotImplementedException();
        }

        public bool AddUserToGroup(int groupId, string user)
        {
            throw new NotImplementedException();
        }

        public bool RemoveUserFromGroup(int groupId, string user)
        {
            throw new NotImplementedException();
        }

        public List<User> GetGroups()
        {
            throw new NotImplementedException();
        }

        public List<User> GetUsers(List<string> phoneNumbers)
        {
            throw new NotImplementedException();
        }
    }
}
