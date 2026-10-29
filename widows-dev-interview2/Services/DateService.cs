using widows_dev_interview2.Models;

namespace widows_dev_interview2.Services
{
    public class DateService
    {
        public static readonly DateOnly FirstDate = new(1995, 6, 16);

        public DateOnly GetEasternToday(DateTimeOffset currentTime)
        {
            try
            {
                TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Eastern Standard Time");

                DateTimeOffset easternTime = TimeZoneInfo.ConvertTime(
                    currentTime, easternTimeZone);

                return DateOnly.FromDateTime(easternTime.DateTime);
            }
            catch (Exception)
            {
                throw new Exception("อ่านวันที่ไม่สำเร็จ");
            }
        }

        public DateRange GetMonthRange(int year, int month, DateOnly easternToday)
        {
            if (year < 1 || year > 9999)
            {
                throw new Exception("วันที่ไม่ถูกต้อง");
            }

            if (month < 1 || month > 12)
            {
                throw new Exception("วันที่ไม่ถูกต้อง");
            }

            DateOnly startDate = new DateOnly(year, month, 1);

            int daysInMonth = DateTime.DaysInMonth(year, month);
            DateOnly endDate = new DateOnly(year, month, daysInMonth);

            if (endDate < FirstDate)
            {
                throw new Exception("วันที่ไม่ถูกต้อง");
            }

            if (startDate > easternToday)
            {
                throw new Exception("วันที่ไม่ถูกต้อง");
            }

            if (startDate < FirstDate)
            {
                startDate = FirstDate;
            }

            if (endDate > easternToday)
            {
                endDate = easternToday;
            }

            return new DateRange(startDate, endDate);
        }
    }
}
