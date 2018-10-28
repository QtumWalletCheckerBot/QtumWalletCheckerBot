using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker_common
{
    public class Logger
    {
        static public string GetDateTime()
        {
            DateTime NowDate = DateTimeHandler.GetTimeZoneNow();

            return NowDate.ToString("yyyy-MM-dd HH:mm:ss") + ":" + NowDate.Millisecond.ToString("000");
        }

        private StreamWriter streamWriter = null;
        private static Logger logger = new Logger();

        private Logger()
        {
            string filePath = Directory.GetCurrentDirectory() + @"\Logs\" + DateTime.Today.ToString("yyyyMMdd") + ".log";
            string dirPath = Directory.GetCurrentDirectory() + @"\Logs";

            DirectoryInfo di = new DirectoryInfo(dirPath);

            FileInfo fi = new FileInfo(filePath);

            try
            {
                if (di.Exists != true)
                    Directory.CreateDirectory(dirPath);

                if (fi.Exists != true)
                {
                    streamWriter = new StreamWriter(filePath);
                }
                else
                {
                    streamWriter = File.AppendText(filePath);
                }

                streamWriter.AutoFlush = true;
            }
            catch (Exception)
            {
            }
        }

        ~Logger()
        {
        }

        private void WriteLog(String msg)
        {
            if (streamWriter == null)
                return;

            try
            {
                streamWriter.WriteLine(string.Format("[{0}] {1}", GetDateTime(), msg));
                streamWriter.Flush();
            }
            catch (Exception)
            {

            }
        }

        static public void Log(String format, params object[] args)
        {
            string log = strings.Format(format, args);
            System.Console.WriteLine(log);
            logger.WriteLog(log);
        }
    }
}
