using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace update_checker
{
    class CommandLine
    {
        static public string Run(string command)
        {
            if (string.IsNullOrEmpty(command))
                return null;

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @"cmd";
            startInfo.WindowStyle = ProcessWindowStyle.Normal;            // cmd창이 숨겨지도록 하기
            startInfo.CreateNoWindow = false;                              // cmd창을 띄우지 안도록 하기

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;                      // cmd창에서 데이터를 가져오기
            startInfo.RedirectStandardInput = true;                       // cmd창으로 데이터 보내기
            startInfo.RedirectStandardError = true;                       // cmd창에서 오류 내용 가져오기

            //startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();

            string result = "";

            try
            {
                using (Process process = new Process())
                {
                    process.EnableRaisingEvents = false;
                    process.StartInfo = startInfo;

                    process.Start();
                    process.StandardInput.Write(command + Environment.NewLine);
                    process.StandardInput.Close();

                    result = process.StandardOutput.ReadToEnd();

                    process.WaitForExit();
                    process.Close();
                }
            }
            catch (Exception e)
            {
                result = e.ToString();
                Console.WriteLine(result);                
            }

            return result;
        }
    }
}
