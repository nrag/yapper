using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Runtime.Serialization;

namespace YapperChat.Common
{
    [DataContract(Namespace="http://schemas.datacontract.org/2004/07/Yapper")]
    public class Tuple<T1, T2>
    {
        private T1 item1;

        private T2 item2;

        public Tuple(T1 item1, T2 item2)
        {
            this.item1 = item1;
            this.item2 = item2;
        }

        [DataMember]
        public T1 m_Item1
        {
            get
            {
                return this.item1;
            }

            set
            {
                this.item1 = value;
            }
        }

        [DataMember]
        public T2 m_Item2
        {
            get
            {
                return this.item2;
            }

            set
            {
                this.item2 = value;
            }
        }
    }
}
