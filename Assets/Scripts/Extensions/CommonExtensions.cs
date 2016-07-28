using System;

namespace Assets.Scripts.Extensions
{
    public static class CommonExtensions
    {
        public static string ToOutputString(this TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours < 1)
                return string.Format("0:{0}:{1}", timeSpan.Minutes, timeSpan.Seconds);
            else
                return string.Format("{0}:{1}:{2}", timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
        }
    }
}
