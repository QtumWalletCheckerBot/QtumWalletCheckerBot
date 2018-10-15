using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    class BackupWallet : ICommand
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

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.BackupWallet;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("지갑 백업");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("지갑 백업 파일을 생성합니다.");
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

            Logger.Log("지갑 백업 응답 완료.\n");

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
                    Logger.Log("지갑 백업 응답 완료.\n");

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
                            string response = strings.GetString("지갑 백업 응답 완료.\n");

                            response += "\n" + QtumHandler.BackupWallet();

                            await SendMessage(requesterId, response.Trim());
                        }
                        else
                        {
                            await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                        }
                    }
                    break;
            }
        }
    }
}
