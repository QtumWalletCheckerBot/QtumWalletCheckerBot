using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    class UpdateChecker : ICommand
    {
        enum eCommandState
        {
            Ready,
            InputWaitOtp,
            InputWaitIPFSHash,
        }

        private eCommandState commandState = eCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;

        ///--------------------------------------------------------------------------------------------------------
        /// 
        public override eCommand GetCommandType()
        {
            return eCommand.UpdateChecker;
        }

        public override string GetCommandName()
        {
            return strings.GetString("월렛체커 업데이트");
        }

        public override string GetCommandDesc()
        {
            return strings.GetString("월렛체커를 업데이트 합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            commandState = eCommandState.InputWaitOtp;

            otpWaitUserId = requesterId;

            waitStartTime = DateTime.Now;

            await SendMessage(requesterId, strings.GetString("Otp 인증 번호를 입력 하세요."));

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override void OnFinish()
        {
            base.OnFinish();

            Logger.Log("체커 업데이트 응답 완료.\n");

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
                        string otpStr = message.Text.Trim();

                        if (OtpChecker.CheckOtp(otpStr))
                        {
                            commandState = eCommandState.InputWaitIPFSHash;

                            waitStartTime = DateTime.Now;

                            await SendMessage(requesterId, strings.GetString("IPFS 해시값을 입력하세요."));
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                        }
                    }
                    break;

                case eCommandState.InputWaitIPFSHash:
                    {
                        string hashStr = message.Text.Trim();

                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = @"cmd";
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;            // cmd창이 숨겨지도록 하기
                        startInfo.CreateNoWindow = true;                              // cmd창을 띄우지 안도록 하기

                        startInfo.UseShellExecute = false;
                        startInfo.RedirectStandardOutput = true;                      // cmd창에서 데이터를 가져오기
                        startInfo.RedirectStandardInput = true;                       // cmd창으로 데이터 보내기
                        startInfo.RedirectStandardError = true;                       // cmd창에서 오류 내용 가져오기

                        startInfo.StandardOutputEncoding = Encoding.UTF8;
                        startInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();

                        try
                        {
                            string command = string.Format("update_checker {0}", hashStr);

                            using (Process process = new Process())
                            {
                                process.EnableRaisingEvents = false;
                                process.StartInfo = startInfo;

                                process.Start();
                                process.StandardInput.Write(command + Environment.NewLine);
                                process.StandardInput.Close();

                                process.WaitForExit();
                                process.Close();
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Log(e.ToString());
                        }

                        IsCompleted = true;
                    }
                    break;
            }
        }
    }
}
