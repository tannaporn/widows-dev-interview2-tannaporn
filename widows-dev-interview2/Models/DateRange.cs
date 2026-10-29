namespace widows_dev_interview2.Models
{
    public class DateRange
    {
        public DateOnly StartDate { get; }
        public DateOnly EndDate { get; }

        public DateRange(DateOnly startDate, DateOnly endDate)
        {
            StartDate = startDate;
            EndDate = endDate;
        }
    }
}
