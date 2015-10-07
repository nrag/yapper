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

namespace YapperChat.Models
{
    public static class DateTimeHelper
    {
        public static string SimpleDate(this DateTime time)
        {
            DateTime localTime = time;
            if (time.Kind == DateTimeKind.Utc || time.Kind == DateTimeKind.Unspecified)
            {
                localTime = DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime();
            }

            DateTime now = DateTime.Now;

            if ((now - localTime).TotalDays < 1 &&
                (localTime.Day == now.Day))
            {
                int twelveHourTime = localTime.Hour > 12 ? localTime.Hour - 12 : localTime.Hour;
                string amPM = localTime.Hour >= 12 ? "PM" : "AM";

                return string.Format("{0:D2}:{1:D2} {2}", twelveHourTime, localTime.Minute, amPM);
            }
            else if ((now - localTime).TotalDays < 2)
            {
                return DateTimeHelper.GetDay(localTime.DayOfWeek);
            }
            else if (now.Year == localTime.Year ||
                    (now - localTime).TotalDays < 100)
            {
                return string.Format("{0}/{1}", localTime.Month, localTime.Day);
            }
            else
            {
                return localTime.Year.ToString();
            }
        }

        public static string SimpleDateTime(this DateTime time)
        {
            DateTime now = DateTime.Now;
            DateTime localTime = time;
            if (time.Kind == DateTimeKind.Utc || time.Kind == DateTimeKind.Unspecified)
            {
                localTime = DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime();
            }

            if (now.Day == localTime.Day &&
                now.Year == localTime.Year &&
                now.Month == localTime.Month)
            {
                // Only time
                return localTime.ToString("t");
            }
            else if (((now > localTime) && (now - localTime).TotalDays < 2) ||
                ((localTime > now) && (localTime - now).TotalDays < 2))
            {
                return string.Format("{0}, {1}", DateTimeHelper.GetDay(localTime.DayOfWeek), localTime.ToString("t"));
            }
            else if (((now > localTime) && (now - localTime).TotalDays < 30) ||
                ((localTime > now) && (localTime - now).TotalDays < 30))
            {
                return string.Format("{0} {1}", localTime.ToString("M"), localTime.ToString("t"));
            }
            else
            {
                // Month and year
                return localTime.ToString("Y");
            }
        }

        public static string GetCalendarDate(this DateTime time)
        {
            DateTime now = DateTime.Now;
            DateTime localTime = time;
            if (time.Kind == DateTimeKind.Utc || time.Kind == DateTimeKind.Unspecified)
            {
                localTime = DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime();
            }

            return localTime.ToString("D");
        }

        public static string GetCalendarTime(this DateTime time)
        {
            DateTime now = DateTime.Now;
            DateTime localTime = time;
            if (time.Kind == DateTimeKind.Utc || time.Kind == DateTimeKind.Unspecified)
            {
                localTime = DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime();
            }

            return localTime.ToString("t");
        }

        private static string GetDay(DayOfWeek dayOfWeek)
        {
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    return Resources.Strings.Monday;
                case DayOfWeek.Tuesday:
                    return Resources.Strings.Tuesday;
                case DayOfWeek.Wednesday:
                    return Resources.Strings.Wednesday;
                case DayOfWeek.Thursday:
                    return Resources.Strings.Thursday;
                case DayOfWeek.Friday:
                    return Resources.Strings.Friday;
                case DayOfWeek.Saturday:
                    return Resources.Strings.Saturday;
                case DayOfWeek.Sunday:
                default:
                    return Resources.Strings.Sunday;
            }
        }

        private static string GetMonth(int month)
        {
            switch (month)
            {
                case 1:
                    return Resources.Strings.January;
                case 2:
                    return Resources.Strings.February;
                case 3:
                    return Resources.Strings.March;
                case 4:
                    return Resources.Strings.April;
                case 5:
                    return Resources.Strings.May;
                case 6:
                    return Resources.Strings.June;
                case 7:
                    return Resources.Strings.July;
                case 8:
                    return Resources.Strings.August;
                case 9:
                    return Resources.Strings.September;
                case 10:
                    return Resources.Strings.October;
                case 11:
                    return Resources.Strings.November;
                case 12:
                default:
                    return Resources.Strings.December;
            }
        }
    }
}
