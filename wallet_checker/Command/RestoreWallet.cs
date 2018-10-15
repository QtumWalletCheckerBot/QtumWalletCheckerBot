using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    class RestoreWallet : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        enum eCommandState
        {
            Ready,
            InputWaitOtp,
            InputWaitChoiceWallet,
        }

        private eCommandState commandState = eCommandState.Ready;
        long otpWaitUserId = 0;
        DateTime waitStartTime = DateTime.MinValue;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.RestoreWallet;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("지갑 복구");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("지갑을 백업 파일로 복구 합니다.");
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

            Logger.Log("지갑 복구 응답 완료.\n");

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
                    Logger.Log("지갑 복구 응답 완료.\n");

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
                            List<string> fileList = GetBackupFileList();

                            if(fileList.Count == 0)
                            {
                                IsCompleted = true;

                                await SendMessage(requesterId, strings.GetString("백업 파일이 없습니다."));

                                return;
                            }

                            string response = strings.GetString("백업 파일을 숫자로 선택하세요.");

                            for(int i=0; i<fileList.Count; ++i)
                            {
                                response += string.Format("\n{0}. {1}", i + 1, fileList[i]);
                            }

                            waitStartTime = DateTime.Now;

                            commandState = eCommandState.InputWaitChoiceWallet;

                            await SendMessage(requesterId, response);
                        }
                        else
                        {
                            IsCompleted = true;

                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                        }
                    }
                    break;
                case eCommandState.InputWaitChoiceWallet:
                    {
                        IsCompleted = true;

                        int backupNum = 0;

                        List<string> fileList = GetBackupFileList();

                        string response = strings.GetString("지갑 복구 응답 완료.\n");

                        if ( int.TryParse(msg, out backupNum) == false || backupNum <= 0 || fileList.Count < backupNum)
                        {
                            response += "\n" + strings.GetString("잘못된 번호 입니다.");
                            await SendMessage(requesterId, response);
                        }
                        else
                        {
                            string filename = fileList[backupNum - 1];

                            await SendMessage(requesterId, strings.GetString("지갑을 복구하는 중 입니다....") + " " + filename);
                            
                            response += "\n" + QtumHandler.RestoreWallet("./backups/" + filename);

                            await SendMessage(requesterId, response);
                        }
                    }
                    break;
            }
        }

        private List<string> GetBackupFileList()
        {
            List<string> fileList = new List<string>();

            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo("./backups");
            foreach (System.IO.FileInfo File in di.GetFiles())
            {
                if (File.Extension.ToLower().CompareTo(".dat") == 0)
                {
                    fileList.Add(File.Name);
                }
            }

            return fileList;
        }
    }
}
