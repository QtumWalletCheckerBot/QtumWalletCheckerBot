using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    class GetTransactionList : ICommand
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        public override eCommand GetCommandType()
        {
            return eCommand.GetTransectionList;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandName()
        {
            return strings.GetString("트랜잭션 리스트");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public override string GetCommandDesc()
        {
            return strings.GetString("트랙젝션 리스트를 출력합니다.");
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        protected override async Task<bool> OnStart(long requesterId, string requesterName, DateTime requestTime, params object[] args)
        {
            string msgDateStr = string.Format("{0:yyyy/MM/dd HH:mm:ss}\n", requestTime);

            await SendMessage(requesterId, strings.Format("{0} {1} 트랜잭션 목록을 확인하는 중 입니다.", msgDateStr, requesterName));

            uint listCount = 10;

            if (args.Length > 1 && args[1] != null)
            {
                uint.TryParse(args[1].ToString(), out listCount);
            }

            List<QtumTxInfo> list = QtumHandler.GetTransactions(listCount);

            for(int i=0; i<list.Count; i+=10)
            {
                string listStr = "--------------------\n";

                for (int k=0; k<10; ++k)
                {
                    if ((i + k) >= list.Count)
                        break;

                    var txInfo = list[i+k];
                    listStr += txInfo.GetString() + "\n";
                }

                listStr += "--------------------";

                Logger.Log(listStr);
                Logger.Log("");

                await SendMessage(requesterId, listStr);
            }
            
            Logger.Log("트랜젝션 리스트 응답 완료.\n");
            await SendMessage(requesterId, "트랜젝션 리스트 응답 완료.\n");

            IsCompleted = true;

            return list.Count != 0;
        }

        ///--------------------------------------------------------------------------------------------------------
    }
}
