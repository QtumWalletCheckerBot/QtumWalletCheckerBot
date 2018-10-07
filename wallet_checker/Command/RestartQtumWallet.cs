using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    public class RestartQtumWallet : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        /// 

        public override eCommand GetCommandType()
        {
            return eCommand.RestartQtumWallet;
        }

        public override string GetCommandName()
        {
            return strings.GetString("재시작");
        }

        public override string GetCommandDesc()
        {
            return strings.GetString("Qtum Core Wallet 을 다시 실행합니다.");
        }
        
        ///--------------------------------------------------------------------------------------------------------
        /// 
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            await SendMessage(requesterId, strings.Format("{0} 지갑을 재기동합니다.", requesterName));
            
            bool success = Restart();

            string response = strings.Format("재기동 완료. 결과 : {0}", success ? "Success" : "Failed");

            await SendMessage(requesterId, response);

            Logger.Log("재기동 완료. {0}\n", success ? "Success" : "Failed");

            if(success && Config.StartupAutoStaking)
            {
                ICommand autoStaking = CommandFactory.CreateCommand(eCommand.StartStaking);
                if (autoStaking != null)
                    return await autoStaking.Process(requesterId, requesterName, DateTime.Now, args);
            }

            return success;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 
        public static bool Restart()
        {
            Logger.Log("RestartQtumWallet");

            try
            {
                Process[] processList = System.Diagnostics.Process.GetProcessesByName("qtum-qt");

                if (processList.Length > 0)
                {
                    foreach (var process in processList)
                    {
                        process.Kill();
                    }
                }

                while (System.Diagnostics.Process.GetProcessesByName("qtum-qt").Length == 0)
                {
                    Thread.Sleep(500);
                }

                string cliPath = Config.QtumWalletPath;
                string rpcUser = Config.RPCUserName;
                string rpcPwd = Config.RPCPassword;
                string command = string.Format(@"{0}\qtum-qt.exe -server -rpcuser={1} -rpcpassword={2} -rpcallowip=127.0.0.1", cliPath, rpcUser, rpcPwd);

                using (Process process = new Process())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo();

                    startInfo.FileName = @"cmd";
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;            // cmd창이 숨겨지도록 하기
                    startInfo.CreateNoWindow = true;                              // cmd창을 띄우지 안도록 하기

                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;                      // cmd창에서 데이터를 가져오기
                    startInfo.RedirectStandardInput = true;                       // cmd창으로 데이터 보내기
                    startInfo.RedirectStandardError = true;                       // cmd창에서 오류 내용 가져오기        

                    process.EnableRaisingEvents = false;
                    process.StartInfo = startInfo;

                    if (process.Start() == false)
                    {
                        Logger.Log("퀀텀 월렛의 경로가 잘못되었습니다. Config.txt 파일에서 경로를 확인 해 주세요.\n");
                        return false;
                    }
                    process.StandardInput.Write(command + Environment.NewLine);
                    process.StandardInput.Close();

                    Logger.Log("Waiting for Qtum Wallet to run ...");

                    while (true)
                    {
                        processList = System.Diagnostics.Process.GetProcessesByName("qtum-qt");
                        if (processList.Length > 0)
                            break;

                        Thread.Sleep(1000);
                    }

                    while (true)
                    {
                        if (string.IsNullOrEmpty(QtumHandler.GetHDMasterKeyId()) == false)
                            break;

                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString());
                //Logger.Log("퀀텀 월렛 실행에 실패했습니다. 프로그램을 종료합니다.");
                return false;
            }

            return true;
        }
    }
}
