using LogReader.Helpers;

namespace LogReader.Models
{
    class SummaryHeader
    {
        public int TotalMessages { get; set; }
        public string PeriodText { get; set; }

        public SummaryHeader UpdateHeader()
        {
            TotalMessages = Variables.totalMessages;
            PeriodText = "";

            if (TotalMessages > 0)
            {
                var totalMinDt = Variables.totalMinDt;
                var totalMaxDt = Variables.totalMaxDt;
                var distinctDays = Variables.distinctDays;

                int totalDays = (totalMaxDt.Date - totalMinDt.Date).Days + 1;

                var dt = "1 day of them has log entries)";
                if (distinctDays > 1)
                {
                    dt = distinctDays.ToString() + " days of them have log entries)";
                }

                string dayText = " (" + totalDays.ToString() + " days; " + dt;
                string pt = totalMinDt.ToShortDateString() + " - " + totalMaxDt.Date.ToShortDateString();

                if (totalDays == 1)
                {
                    dayText = " (1 day)";
                    pt = totalMinDt.ToShortDateString();
                }
                PeriodText = " " + pt + dayText;
            }

            return this;
        }
    }
}
