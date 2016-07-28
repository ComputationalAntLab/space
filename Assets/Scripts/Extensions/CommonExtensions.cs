using System;

namespace Assets.Scripts.Extensions
{
    public static class CommonExtensions
    {
        public static string ToOutputString(this TimeSpan time)
        {
            return string.Format("{0:00}:{1:00}.{2:00}", time.TotalHours, time.Minutes, time.Seconds);
        }
    }
}
