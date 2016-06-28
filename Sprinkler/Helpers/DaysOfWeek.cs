using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprinklerRPI.Helpers
{
    public sealed class DayOfWeek
    {
        #region Day of week
        private static string[] days = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        private static string[] minidays = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        public static string[] Days
        {
            get
            {
                return days;
            }
        }

        public static string[] Minidays
        {
            get
            {
                return minidays;
            }
        }

        public static int NumberDaysPerMonth(int Month, int Year)
        {
            if ((Month <= 0) || (Month >= 13))
                return 0;
            if ((Year % 4 == 0 && Year % 100 != 0) || Year % 400 == 0)
            {
                int[] NbDays = new int[] { 31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                return NbDays[Month - 1];
            }
            else
            {
                int[] NbDays = new int[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
                return NbDays[Month - 1];
            }
        }
        #endregion Day of week
    }
}
