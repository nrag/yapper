using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UserServiceRole
{

    public class PhoneNumberUtils
    {
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
    }
}
