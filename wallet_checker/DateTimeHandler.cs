using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker
{
    static public class DateTimeHandler
    {
        static public DateTime ToKoreaTime(DateTime srcTime)
        {
            try
            {
                TimeZoneInfo koreaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Korea Standard Time");

                return TimeZoneInfo.ConvertTimeFromUtc(srcTime, koreaTimeZone);
            }
            catch(Exception)
            {
                return srcTime;
            }
        }

        static public DateTime GetKoreaNow()
        {
            try
            {
                if (TimeZone.CurrentTimeZone.StandardName == "Korea Standard Time")
                    return DateTime.Now;

                return ToKoreaTime(DateTime.Now);
            }
            catch(Exception)
            {
                return DateTime.Now;
            }
            
        }
    }
}
