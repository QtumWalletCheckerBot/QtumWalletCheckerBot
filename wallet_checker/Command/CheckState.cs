using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wallet_checker.Command
{
    class CheckState : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///

        public override eCommand GetCommandType()
        {
            return eCommand.CheckState;
        }

        public override string GetCommandName()
        {
            return strings.GetString("확인");
        }

        public override string GetCommandDesc()
        {
            return strings.GetString("지갑의 상태를 확인합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            await SendMessage(requesterId, strings.Format("{0} 지갑을 확인하는 중 입니다.", requesterName));
            
            string response = await Task<string>.Run(() => MakeCheckResponse(requestTime));

            await SendMessage(requesterId, response);

            Logger.Log("지갑 확인 응답 완료.\n");

            return string.IsNullOrEmpty(response) == false;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 확인 요청에 대한 응답메세지 생성
        private static string MakeCheckResponse(DateTime requestTime)
        {
            string msgDateStr = string.Format("{0:yyyy/MM/dd HH:mm:ss}", requestTime);

            string walletState = QtumHandler.GetInfo();

            var response = strings.Format(@"
 ---------------------------------
 요청 : {0}
 응답 : {1:yyyy/MM/dd HH:mm:ss}
{2}
 ---------------------------------", msgDateStr, DateTime.Now, walletState);

            Logger.Log(response);
            Logger.Log("");

            return response;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}
