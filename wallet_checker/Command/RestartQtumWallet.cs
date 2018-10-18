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
        enum eCommandState
        {
            Ready,
            InputWaitOtp,
        }

        private eCommandState commandState = eCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;
        object[] restartParams = null;
        bool success = false;

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
            commandState = eCommandState.InputWaitOtp;

            otpWaitUserId = requesterId;

            waitStartTime = DateTime.Now;

            restartParams = args;

            success = false;

            await SendMessage(requesterId, strings.GetString("Otp 인증 번호를 입력 하세요."));

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override void OnFinish()
        {
            base.OnFinish();

            Logger.Log("재기동 완료. {0}\n", success ? "Success" : "Failed");

            commandState = eCommandState.Ready;

            waitStartTime = DateTime.MinValue;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnUpdate()
        {
            await base.OnUpdate();

            if (commandState == eCommandState.Ready)
                return;

            if (waitStartTime != DateTime.MinValue)
            {
                if ((DateTime.Now - waitStartTime).Ticks / TimeSpan.TicksPerSecond > 60.0f)
                {
                    Logger.Log("재기동 완료. {0}\n", success ? "Success" : "Failed");

                    IsCompleted = true;

                    await SendMessage(otpWaitUserId, strings.GetString("제한시간 초과"));
                }
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnMessage(Telegram.Bot.Types.Message message)
        {
            await base.OnMessage(message);

            string msg = message.Text.Trim();

            long requesterId = message.Chat.Id;

            if (requesterId != otpWaitUserId)
                return;

            switch (commandState)
            {
                case eCommandState.InputWaitOtp:
                    {
                        IsCompleted = true;

                        string otpStr = message.Text.Trim();

                        if (OtpChecker.CheckOtp(otpStr))
                        {
                            string requesterName = message.Chat.Username;

                            await SendMessage(requesterId, strings.Format("{0} 지갑을 재기동합니다.", requesterName));

                            bool success = await Restart(restartParams);

                            string response = strings.Format("재기동 완료. 결과 : {0}", success ? "Success" : "Failed");

                            await SendMessage(requesterId, response);

                            Logger.Log("재기동 완료. {0}\n", success ? "Success" : "Failed");

                            if (success && Config.StartupAutoStaking)
                            {
                                ICommand autoStaking = CommandFactory.CreateCommand(eCommand.StartStaking);
                                if (autoStaking != null)
                                    await autoStaking.Process(requesterId, requesterName, DateTimeHandler.GetTimeZoneNow());
                            }
                        }
                        else
                        {
                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                        }
                    }
                    break;
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 
        public static async Task<bool> Restart(params object[] args)
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

                DateTime startTime = DateTime.Now;

                while (System.Diagnostics.Process.GetProcessesByName("qtum-qt").Length != 0)
                {
                    Thread.Sleep(500);

                    if((DateTime.Now.Ticks - startTime.Ticks) * TimeSpan.TicksPerSecond > 30)
                    {
                        processList = System.Diagnostics.Process.GetProcessesByName("qtum-qt");

                        foreach (var process in processList)
                        {
                            process.Kill();
                        }
                    }
                }

                string cliPath = Config.QtumWalletPath;
                string rpcUser = Config.RPCUserName;
                string rpcPwd = Config.RPCPassword;
                string command = string.Format(@"{0}\qtum-qt.exe -server -rpcuser={1} -rpcpassword={2} -rpcallowip=127.0.0.1", cliPath, rpcUser, rpcPwd);

                for(int i=1; args != null && i <args.Length; ++i)
                {
                    object arg = args[i];
                    if(arg != null && string.IsNullOrEmpty(arg.ToString().Trim()) == false)
                    {
                        command += " " + arg.ToString().Trim();
                    }
                }

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

                    Logger.Log("Waiting for Qtum Wallet to run ...\n");

                    var waitRunTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            processList = System.Diagnostics.Process.GetProcessesByName("qtum-qt");
                            if (processList.Length > 0)
                                break;

                            Thread.Sleep(1000);
                        }
                    });

                    await waitRunTask;

                    var waitSyncTask = Task.Run(() =>
                    {
                        while (true)
                        {
                            if (string.IsNullOrEmpty(QtumHandler.GetHDMasterKeyId(false)) == false)
                                break;

                            Thread.Sleep(1000);
                        }
                    }
                    );

                    await waitSyncTask;
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
