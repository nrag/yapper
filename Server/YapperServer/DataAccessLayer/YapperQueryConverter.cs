using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    public class YapperQueryConverter : QueryStringConverter
    {
        public override bool CanConvert(Type type)
        {
            return (type == typeof(RecipientCollection)) || (type == typeof(PhoneCollection)) || base.CanConvert(type);
        }

        public override object ConvertStringToValue(string parameter, Type parameterType)
        {
            if (parameterType == typeof(PhoneCollection))
            {
                return this.ConvertPhoneCollectionFromString(parameter);
            }

            if (parameterType == typeof(RecipientCollection))
            {
                return this.ConvertRecipientCollectionFromString(parameter);
            }

            return base.ConvertStringToValue(parameter, parameterType);
        }

        private object ConvertPhoneCollectionToPhoneString(object value)
        {
            PhoneCollection phones = (PhoneCollection)value;
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < phones.Count; i++)
            {
                if (i == 0)
                {
                    builder.Append(phones[i]);
                }
                else
                {
                    builder.AppendFormat(";{0}", phones[i]);
                }
            }

            return builder.ToString();
        }

        private object ConvertPhoneCollectionFromString(object value)
        {
            string phoneString = (string)value;

            string[] phones = phoneString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            PhoneCollection phoneCollection = new PhoneCollection();
            for (int i = 0; i < phones.Length; i++)
            {
                string normalizedPhone = PhoneNumberUtils.ValidatePhoneNumber(phones[i]);
                phoneCollection.Add(normalizedPhone);
            }

            return phoneCollection;
        }

        private object ConvertRecipientCollectionToString(object value)
        {
            RecipientCollection users = (RecipientCollection)value;
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < users.Count; i++)
            {
                if (i == 0)
                {
                    builder.Append(users[i].ToString());
                }
                else
                {
                    builder.AppendFormat(";{0}", users[i]);
                }
            }

            return builder.ToString();
        }

        private object ConvertRecipientCollectionFromString(object value)
        {
            string recipientString = (string)value;

            string[] recipients = recipientString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            RecipientCollection recipientCollection = new RecipientCollection();
            for (int i = 0; i < recipients.Length; i++)
            {
                recipientCollection.Add(Int32.Parse(recipients[i]));
            }

            return recipientCollection;
        }
    }
    
}
