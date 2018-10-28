using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    class GetAddressList : ICommand
    {

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.GetAddressList;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("주소 보기");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("지갑의 주소 리스트를 조회합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            await SendMessage(requesterId, strings.Format("{0} 주소 목록을 확인하는 중 입니다.", requesterName));

            string response = await MakeResponse(requesterId, requestTime);

            await SendMessage(requesterId, response);

            Logger.Log("주소 목록 확인 응답 완료.\n");

            IsCompleted = true;

            return string.IsNullOrEmpty(response) == false;
        }

        ///--------------------------------------------------------------------------------------------------------
        /// 확인 요청에 대한 응답메세지 생성
        private async Task<string> MakeResponse(long requesterId, DateTime requestTime)
        {
            string msgDateStr = string.Format("{0:yyyy/MM/dd HH:mm:ss}", requestTime);

            Dictionary<string, double> balances = null;

            QtumHandler.GetInfo(out balances);

            string addressListStr = "";

            foreach (KeyValuePair<string, double> elem in balances)
            {
                await SendMessage(requesterId, GetAddressLink(elem.Key));
                await SendMessage(requesterId, elem.Value.ToString());
                addressListStr += string.Format("\n{0} : {1}", GetAddressLink(elem.Key), elem.Value.ToString());
            }

            var response = strings.Format(@"
 ---------------------------------
 요청 : {0}
 응답 : {1:yyyy/MM/dd HH:mm:ss}
{2}
 ---------------------------------", msgDateStr, DateTimeHandler.GetTimeZoneNow(), addressListStr);

            Logger.Log(response);
            Logger.Log("");

            return response;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}
