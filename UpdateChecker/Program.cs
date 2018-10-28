using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace update_checker
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
                return;

            string hashStr = args[0];

            if (string.IsNullOrEmpty(hashStr))
                return;

            KillChecker();

            string tmpPath = "ipfsTmp";

            {
                string command = string.Format("ipfs get {0} --output={1}", hashStr, tmpPath);
                CommandLine.Run(command);
            }

            {
                string command = string.Format("xcopy \".\\{0}\\*.*\" \"{1}\" /d /c /y /i", tmpPath, System.IO.Directory.GetCurrentDirectory());
                CommandLine.Run(command);
            }

            DeleteFolder(tmpPath);

            {
                CommandLine.Run("wallet_checker.exe");
            }
        }

        static void KillChecker()
        {
            const string checkProcessName = "wallet_checker";

            Process[] processList = System.Diagnostics.Process.GetProcessesByName(checkProcessName);

            if (processList.Length > 0)
            {
                foreach (var process in processList)
                {
                    process.Kill();
                }
            }

            DateTime startTime = DateTime.Now;

            while (System.Diagnostics.Process.GetProcessesByName(checkProcessName).Length != 0)
            {
                Thread.Sleep(500);

                if ((DateTime.Now.Ticks - startTime.Ticks) * TimeSpan.TicksPerSecond > 30)
                {
                    processList = System.Diagnostics.Process.GetProcessesByName(checkProcessName);

                    foreach (var process in processList)
                    {
                        process.Kill();
                    }
                }
            }
        }

        static void DeleteFolder(string srcPath)
        {
            DirectoryInfo dir = new DirectoryInfo(srcPath);

            System.IO.FileInfo[] files = dir.GetFiles("*.*", SearchOption.AllDirectories);

            foreach (System.IO.FileInfo file in files)
            {
                file.Attributes = FileAttributes.Normal;
            }

            Directory.Delete(srcPath, true);
        }
    }
}
