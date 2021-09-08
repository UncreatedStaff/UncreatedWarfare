using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatsAnalyzer
{
    public static class F
    {
        public static string GetTimeFromMinutes(this uint minutes)
        {
            if (minutes < 60) // < 1 hour
            {
                return minutes.ToString() + " min" + (minutes == 1 ? "" : "s");
            }
            else if (minutes < 1440) // < 1 day 
            {
                uint hours = DivideRemainder(minutes, 60, out uint minutesOverflow);
                return $"{hours} hr{hours.S()}{(minutesOverflow == 0 ? "" : $" and {minutesOverflow} min{minutesOverflow.S()}")}";
            }
            else if (minutes < 43800) // < 1 month (30.416 days)
            {
                uint days = DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out uint hoursOverflow);
                return $"{days} day{days.S()}{(hoursOverflow == 0 ? "" : $" and {hoursOverflow} hr{hoursOverflow.S()}")}";
            }
            else if (minutes < 525600) // < 1 year
            {
                uint months = DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out uint daysOverflow);
                return $"{months} mon{months.S()}{(daysOverflow == 0 ? "" : $" and {daysOverflow} day{daysOverflow.S()}")}";
            }
            else // > 1 year
            {
                uint years = DivideRemainder(DivideRemainder(DivideRemainder(DivideRemainder(minutes, 60, out _), 24, out _), 30.416m, out _), 12, out uint monthOverflow);
                return $"{years} yr{years.S()}{(monthOverflow == 0 ? "" : $" and {monthOverflow} mon{monthOverflow.S()}")}";
            }
        }
        public static int DivideRemainder(float divisor, float dividend, out int remainder)
        {
            float answer = divisor / dividend;
            remainder = (int)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (int)Math.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, uint dividend, out uint remainder)
        {
            decimal answer = (decimal)divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static uint DivideRemainder(uint divisor, decimal dividend, out uint remainder)
        {
            decimal answer = divisor / dividend;
            remainder = (uint)Math.Round((answer - Math.Floor(answer)) * dividend);
            return (uint)Math.Floor(answer);
        }
        public static string S(this int number) => number == 1 ? "" : "s";
        public static string S(this float number) => number == 1 ? "" : "s";
        public static string S(this uint number) => number == 1 ? "" : "s";

    }
}
