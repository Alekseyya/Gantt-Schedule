using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Core.Extensions
{
    public static class DataTimeExtensions
    {
        /// <summary>
        /// Список дней между двумя датами
        /// </summary>
        /// <param name="startDate">Дата начала</param>
        /// <param name="endDate">Дата окончания</param>
        /// <returns></returns>
        public static IEnumerable<DateTime> Range(this DateTime startDate, DateTime endDate)
        {
            return Enumerable.Range(0, (endDate - startDate).Days + 1).Select(d => startDate.AddDays(d));
        }

        public static DateTime FirstDayOfMonth(this DateTime value)
        {
            return new DateTime(value.Year, value.Month, 1);
        }

        public static DateTime LastDayOfMonth(this DateTime value)
        {
            return FirstDayOfMonth(value).AddMonths(1).AddDays(-1);
        }
        public static DateTime LastDayOfMonth(this DateTime value, int number)
        {
            return new DateTime(value.Year, value.AddMonths(number).Month, 1).AddMonths(1).AddDays(-1);
        }

        public static int DaysInMonth(this DateTime value)
        {
            return DateTime.DaysInMonth(value.Year, value.Month);
        }

        public static DateTime FirstDayOnNextMonth(this DateTime value)
        {
            return new DateTime(value.Year, value.AddMonths(1).Month, 1);
        }

        public static DateTime LastDayNextMonth(this DateTime value)
        {
            return FirstDayPlusTwoMonths(value).AddDays(-1);
        }

        public static DateTime FirstDayPlusTwoMonths(this DateTime value)
        {
            return FirstDayOnNextMonth(value).AddMonths(2);
        }

        public static double DifferenceBetweenTwoDatesAbs(this DateTime startDateTime, DateTime endDateTime)
        {
            return Math.Abs((endDateTime - startDateTime).TotalDays);
        }

        public static int DifferenceBetweenTwoDates(this DateTime startDateTime, DateTime endDateTime)
        {
            return (int)(endDateTime - startDateTime).TotalDays;
        }

        public static double DifferenceBetweenTwoDatesInHoursAbs(this DateTime staDateTime, DateTime endDateTime)
        {
            return Math.Abs((endDateTime - staDateTime).TotalHours);
        }

        public static int DifferenceBetweenTwoDatesInHours(this DateTime staDateTime, DateTime endDateTime)
        {
            return (int)(endDateTime - staDateTime).TotalHours;
        }


        /// <summary>
        /// Конец последнего дня в следующем месяце. Пример:31.01.2018 23.59.59
        /// </summary>
        public static DateTime EndOfLastDayNextMonth(this DateTime value)
        {
            return FirstDayPlusTwoMonths(value).AddTicks(-1);
        }

        /// <summary>
        /// Логика для страницы MyHours, переключение влево
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="dateEnd"></param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> SwitchingDateLeft(string dateStart, string dateEnd)
        {
            DateTime.TryParse(dateStart, out DateTime dateStartTime);
            DateTime.TryParse(dateEnd, out DateTime dateEndTime);
            if (dateStartTime != default && dateEndTime != default)
            {
                if (dateStartTime == dateStartTime.FirstDayOfMonth() && dateEndTime == dateEndTime.LastDayOfMonth())
                    return new Tuple<DateTime, DateTime>(dateStartTime.FirstDayOfMonth().AddDays(-1).FirstDayOfMonth(), dateStartTime.FirstDayOfMonth().AddDays(-1).LastDayOfMonth());
                var differenceTwoDates = dateStartTime.DifferenceBetweenTwoDatesAbs(dateEndTime) + 1;
                return new Tuple<DateTime, DateTime>(dateStartTime.AddDays(differenceTwoDates * -1), dateEndTime.AddDays(differenceTwoDates * -1));
            }
            return new Tuple<DateTime, DateTime>(dateStartTime, dateEndTime);
        }

        /// <summary>
        /// Логика для страницы MyHours, переключение вправо
        /// </summary>
        /// <param name="dateStart"></param>
        /// <param name="dateEnd"></param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> SwitchingDateRight(string dateStart, string dateEnd)
        {
            DateTime.TryParse(dateStart, out DateTime dateStartTime);
            DateTime.TryParse(dateEnd, out DateTime dateEndTime);
            if (dateStartTime != default && dateEndTime != default)
            {
                if (dateStartTime == dateStartTime.FirstDayOfMonth() && dateEndTime == dateEndTime.LastDayOfMonth())
                    return new Tuple<DateTime, DateTime>(dateStartTime.LastDayOfMonth().AddDays(1).FirstDayOfMonth(), dateStartTime.LastDayOfMonth().AddDays(1).LastDayOfMonth());
                var differenceTwoDates = dateStartTime.DifferenceBetweenTwoDatesAbs(dateEndTime) + 1;
                return new Tuple<DateTime, DateTime>(dateStartTime.AddDays(differenceTwoDates), dateEndTime.AddDays(differenceTwoDates));
            }
            return new Tuple<DateTime, DateTime>(dateStartTime, dateEndTime);
        }

        public static DateTime StartOfNextWeek(this DateTime dt)
        {
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)dt.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0)
                daysUntilMonday = 7;
            return dt.AddDays(daysUntilMonday);
        }
        public static DateTime StartOfNextWeek(this DateTime dt, int number)
        {
            var startOfNextWeek = dt;
            for (int i = 0; i < number; i++)
            {
                int daysUntilMonday = ((int)DayOfWeek.Monday - (int)startOfNextWeek.DayOfWeek + 7) % 7;
                if (daysUntilMonday == 0)
                    daysUntilMonday = 7;
                startOfNextWeek = startOfNextWeek.AddDays(daysUntilMonday);
            }

            return startOfNextWeek;
        }

        public static DateTime EndOfNextWeek(this DateTime dt)
        {
            return dt.StartOfNextWeek().EndOfWeek();
        }
        public static DateTime EndOfNextWeek(this DateTime dt, int number)
        {
            return dt.StartOfNextWeek(number).EndOfWeek();
        }


        public static DateTime StartOfWeek(this DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
        public static DateTime EndOfWeek(this DateTime dt)
        {
            return dt.StartOfWeek().AddDays(7).AddSeconds(-1).Date;
        }

        public static DateTime PreviousWeekStart(this DateTime dt)
        {
            while (dt.DayOfWeek != DayOfWeek.Monday)
                dt = dt.AddDays(-1);
            return dt.AddDays(-7);
        }

        public static DateTime PreviousWeekStart(this DateTime dt, int numberWeeks)
        {
            var previousStartWeekDay = dt;
            for (int i = 0; i < numberWeeks; i++)
            {
                while (previousStartWeekDay.DayOfWeek != DayOfWeek.Monday)
                    previousStartWeekDay = previousStartWeekDay.AddDays(-1);
                previousStartWeekDay = previousStartWeekDay.AddDays(-7);
            }
            return previousStartWeekDay;
        }

        public static DateTime PreviousWeekEnd(this DateTime dt)
        {
            while (dt.DayOfWeek != DayOfWeek.Monday)
                dt = dt.AddDays(-1);
            return dt.AddDays(-1);
        }
        public static DateTime PreviousWeekEnd(this DateTime dt, int numberWeeks)
        {
            var previousEndWeekDay = dt;
            for (int i = 0; i < numberWeeks; i++)
            {
                while (previousEndWeekDay.DayOfWeek != DayOfWeek.Monday)
                    previousEndWeekDay = previousEndWeekDay.AddDays(-1);
                previousEndWeekDay = previousEndWeekDay.AddDays(-1);
            }
            return previousEndWeekDay;
        }

        public static List<int> GetWeeksNumberOfMonth(this DateTime dateMonth)
        {
            return GetWeeksListNumberBetweenTwoDates(dateMonth.FirstDayOfMonth(), dateMonth.LastDayOfMonth());
        }

        public static List<int> GetWeeksListNumberBetweenTwoDates(DateTime startDate, DateTime endDate)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var weeks = new List<int>();
            for (var dt = startDate; dt < endDate; dt = dt.AddDays(1))
            {
                var weekNo = currentCulture.Calendar.GetWeekOfYear(dt, currentCulture.DateTimeFormat.CalendarWeekRule, currentCulture.DateTimeFormat.FirstDayOfWeek);
                if (!weeks.Contains(weekNo))
                    weeks.Add(weekNo);
            }
            return weeks;
        }

        public static int GetWeeksNumberBetweenTwoDates(DateTime startDate, DateTime endDate)
        {
            var currentCulture = CultureInfo.CurrentCulture;
            var weeks = new List<int>();
            for (var dt = startDate; dt < endDate; dt = dt.AddDays(1))
            {
                var weekNo = currentCulture.Calendar.GetWeekOfYear(dt, currentCulture.DateTimeFormat.CalendarWeekRule, currentCulture.DateTimeFormat.FirstDayOfWeek);
                if (!weeks.Contains(weekNo))
                    weeks.Add(weekNo);
            }
            return weeks.Count();
        }

        /// <summary>
        /// Получить номер недели по дате
        /// </summary>
        /// <param name="time">Дата</param>
        /// <returns></returns>
        public static int GetIso8601WeekOfYear(DateTime time)
        {
            // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
            // be the same week# as whatever Thursday, Friday or Saturday are,
            // and we always get those right
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                time = time.AddDays(3);
            }

            // Return the week of our adjusted day
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        public static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }

        public static DateTime LastDateOfWeekISO8601(int year, int weekOfYear)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Add 3 days from Thursday to get Wendsday, which is the first weekday in ISO8601
            return result.AddDays(+3);
        }

        public static DateTime StartOfDay(this DateTime theDate)
        {
            return theDate.Date;
        }

        public static DateTime EndOfDay(this DateTime theDate)
        {
            return theDate.Date.AddDays(1).AddTicks(-1);
        }

        public static string GetMonthName(this DateTime dateTime)
        {
            return dateTime.ToString("MMMM", CultureInfo.CreateSpecificCulture("ru"));
        }

        public static DateTime Trim(this DateTime date, long ticks)
        {
            return new DateTime(date.Ticks - (date.Ticks % ticks), date.Kind);
        }

        public static int BusinessDaysUntil(this DateTime firstDay, DateTime lastDay, params DateTime[] bankHolidays)
        {
            firstDay = firstDay.Date;
            lastDay = lastDay.Date;

            TimeSpan span = lastDay - firstDay;
            int businessDays = span.Days + 1;
            int fullWeekCount = businessDays / 7;
            // find out if there are weekends during the time exceedng the full weeks
            if (businessDays > fullWeekCount * 7)
            {
                // we are here to find out if there is a 1-day or 2-days weekend
                // in the time interval remaining after subtracting the complete weeks
                int firstDayOfWeek = firstDay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)firstDay.DayOfWeek;
                int lastDayOfWeek = lastDay.DayOfWeek == DayOfWeek.Sunday
                    ? 7 : (int)lastDay.DayOfWeek;
                if (lastDayOfWeek < firstDayOfWeek)
                    lastDayOfWeek += 7;
                if (firstDayOfWeek <= 6)
                {
                    if (lastDayOfWeek >= 7)// Both Saturday and Sunday are in the remaining time interval
                        businessDays -= 2;
                    else if (lastDayOfWeek >= 6)// Only Saturday is in the remaining time interval
                        businessDays -= 1;
                }
                else if (firstDayOfWeek <= 7 && lastDayOfWeek >= 7)// Only Sunday is in the remaining time interval
                    businessDays -= 1;
            }

            // subtract the weekends during the full weeks in the interval
            businessDays -= fullWeekCount + fullWeekCount;

            // subtract the number of bank holidays during the time interval
            foreach (DateTime bankHoliday in bankHolidays)
            {
                DateTime bh = bankHoliday.Date;
                if (firstDay <= bh && bh <= lastDay)
                    --businessDays;
            }

            return businessDays;
        }
    }
}
