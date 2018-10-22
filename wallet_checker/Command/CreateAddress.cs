using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    class CreateAddress : ICommand
    {
        enum eCommandState
        {
            Ready,
            WaitOpt,
        };

        eCommandState commandState = eCommandState.Ready;
        long optWaitUserId = 0;
        DateTime waitStarTime;

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.CreateAddress;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("주소 생성");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("지갑에 새 주소를 생성합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            commandState = eCommandState.WaitOpt;

            optWaitUserId = requesterId;

            waitStarTime = DateTime.Now;

            await SendMessage(requesterId, strings.GetString("Otp 인증 번호를 입력 하세요."));

            return true;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnUpdate()
        {
            await base.OnUpdate();

            if(commandState == eCommandState.WaitOpt)
            {
                if ((DateTime.Now - waitStarTime).Ticks / TimeSpan.TicksPerSecond > 60.0f )
                {
                    Logger.Log("주소 생성 응답 완료.\n");

                    IsCompleted = true;

                    commandState = eCommandState.Ready;

                    await SendMessage(optWaitUserId, strings.GetString("제한시간 초과"));                    
                }
            }
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override void OnFinish()
        {
            base.OnFinish();

            commandState = eCommandState.Ready;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override async Task OnMessage(Telegram.Bot.Types.Message message)
        {
            if(commandState == eCommandState.WaitOpt)
            {
                long requesterId = message.Chat.Id;

                if (requesterId == optWaitUserId)
                {
                    commandState = eCommandState.Ready;

                    string otpStr = message.Text.Trim();

                    if(OtpChecker.CheckOtp(otpStr))
                    {
                        string response = await MakeResponse(requesterId, DateTimeHandler.ToLocalTime(message.Date));

                        await SendMessage(requesterId, response);
                    }
                    else
                    {
                        await SendMessage(requesterId, strings.GetString("Otp 인증에 실패 했습니다."));
                    }

                    Logger.Log("주소 생성 응답 완료.\n");

                    IsCompleted = true;
                }
                 
            }
            await Task.Run(() => { });
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 확인 요청에 대한 응답메세지 생성
        private async Task<string> MakeResponse(long requesterId, DateTime requestTime)
        {
            await Task<string>.Run(() => { });

            string msgDateStr = string.Format("{0:yyyy/MM/dd HH:mm:ss}", requestTime);

            string newAddress = QtumHandler.CreateNewAddress();

            var response = strings.Format(@"
 ---------------------------------
 요청 : {0}
 응답 : {1:yyyy/MM/dd HH:mm:ss}
{2}
 ---------------------------------", msgDateStr, DateTimeHandler.GetTimeZoneNow(), GetAddressLink(newAddress));

            Logger.Log(response);
            Logger.Log("");

            return response;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}
