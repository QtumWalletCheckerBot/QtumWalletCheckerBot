using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker_common
{
    static public class DateTimeHandler
    {
        static public DateTime ToLocalTime(DateTime srcTime)
        {
            try
            {
                TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(Config.TimeZoneName);
                return TimeZoneInfo.ConvertTimeFromUtc(TimeZoneInfo.ConvertTimeToUtc(srcTime), timeZoneInfo);
            }
            catch (Exception)
            {
            }

            return srcTime;
        }

        static public DateTime GetTimeZoneNow()
        {
            try
            {
                if (TimeZone.CurrentTimeZone.StandardName == Config.TimeZoneName)
                    return DateTime.Now;

                return ToLocalTime(DateTime.Now);
            }
            catch (Exception)
            {
                return DateTime.Now;
            }

        }
    }
}
