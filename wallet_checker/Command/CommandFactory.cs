using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker.Command
{
    public class CommandFactory
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        static public ICommand CreateCommand(eCommand command)
        {
            switch (command)
            {
                case eCommand.CheckState: return new CheckState();
                case eCommand.GetAddressList: return new GetAddressList();
                case eCommand.GetTransectionList: return new GetTransactionList();
                case eCommand.CreateAddress: return new CreateAddress();
                case eCommand.StartStaking: return new StartStaking();
                case eCommand.StopStaking: return new StopStaking();
                case eCommand.RestartQtumWallet: return new RestartQtumWallet();
                case eCommand.SendQtum: return new SendQtum();
                case eCommand.RemoteCommandLine: return new RemoteCommandLine();
                case eCommand.BackupWallet: return new BackupWallet();
                case eCommand.RestoreWallet: return new RestoreWallet();
                case eCommand.RestartMachine: return new RestartMachine();
                case eCommand.UpdateChecker: return new UpdateChecker();
            }

            return null;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public eCommand ParseCommand(string str)
        {
            if (str == null)
                return eCommand.None;

            var commands = System.Enum.GetValues(typeof(eCommand));

            for (int i=commands.Length-1; i>=0; --i)
            {
                eCommand command = (eCommand)commands.GetValue(i);

                if (command == eCommand.None)
                    continue;

                ICommand cmdInst = CreateCommand(command);

                if (cmdInst == null)
                    continue;

                if (str.StartsWith(((int)command).ToString()) || str.StartsWith(cmdInst.GetCommandName()))
                    return command;
            }

            return eCommand.None;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        static public string GetCommandHelpString()
        {
            string result = strings.GetString("명령을 입력하세요.\n\n");

            var commands = System.Enum.GetValues(typeof(eCommand));

            foreach (eCommand command in commands)
            {
                if (command == eCommand.None)
                    continue;

                ICommand inst = CreateCommand(command);

                if(inst == null)
                {
                    Logger.Log("Command is null! {0}", command.ToString());
                    continue;
                }

                result += strings.Format("{0}. {1}\n", (int)command, inst.GetCommandName());
                result += strings.Format("  - {0}\n\n", inst.GetCommandDesc());
            }

            return result;
        }
    }
}
