using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YapperChat.Models
{
    public class AppointmentDateTime
    {
        public DateTime ApptDate;
        public DateTime ApptTime;

        public AppointmentDateTime(long ticks)
        {
            ApptDate = new DateTime(ticks);
            ApptTime = new DateTime(ticks);
        }
    }
}
