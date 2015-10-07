using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer
{
    [DataContract(Name = "SubscriptionType", Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    public enum SubscriptionType
    {
        [EnumMember]
        WindowsPhoneToast,

        [EnumMember]
        WindowsPhoneTile
    }
}
