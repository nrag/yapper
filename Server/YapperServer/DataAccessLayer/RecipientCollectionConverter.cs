using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class RecipientCollectionConverter : TypeConverter
    {
        public override object ConvertTo(
            ITypeDescriptorContext context,
            System.Globalization.CultureInfo culture,
            object value,
            Type destinationType)
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

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
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