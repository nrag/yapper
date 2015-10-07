using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Authenticator
{
    /// <summary>
    /// Implements the UnixTime in UTC
    /// </summary>
    class Unixtime
    {
        private DateTime date;

        public Unixtime()
        {
            this.date = DateTime.UtcNow;
        }

        public Unixtime(int timestamp)
        {
            DateTime converted = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            DateTime newDateTime = converted.AddSeconds(timestamp);
            this.date = newDateTime;
        }

        public DateTime ToDateTime()
        {
            return this.date;
        }

        public long ToTimeStamp()
        {
            TimeSpan span = (this.date - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            return Convert.ToInt64(span.TotalSeconds);
        }
    }
}
