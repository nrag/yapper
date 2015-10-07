using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DataAccessLayer
{
    public class PhoneNumberUtils
    {
        private static List<string> DebugPhoneNumbers = new List<string>(){ "+1 425-818-8207", "+1 425-898-0401"};

        public static string ValidatePhoneNumber(string phoneNumber)
        {
            try
            {
                PhoneNumbers.PhoneNumber phone = PhoneNumbers.PhoneNumberUtil.GetInstance().Parse(phoneNumber, "US");

                if (!PhoneNumbers.PhoneNumberUtil.GetInstance().IsValidNumber(phone))
                {
                    return null;
                }

                return PhoneNumbers.PhoneNumberUtil.GetInstance().Format(phone, PhoneNumbers.PhoneNumberFormat.INTERNATIONAL);
            }
            catch (PhoneNumbers.NumberParseException)
            {
                return null;
            }
        }

        public static bool IsDebugPhoneNumber(string phoneNumber)
        {
            try
            {
                if (DebugPhoneNumbers.Contains(phoneNumber))
                {
                    return true;
                }

                return false;
            }
            catch (PhoneNumbers.NumberParseException)
            {
                return false;
            }
        }
    }
}