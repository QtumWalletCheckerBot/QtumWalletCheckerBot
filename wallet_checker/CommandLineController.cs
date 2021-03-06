﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wallet_checker_common;

namespace wallet_checker
{
    ///-//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///
    public class CommmandLineController
    {
        ///--------------------------------------------------------------------------------------------------------
        ///
        ProcessStartInfo startInfo = new ProcessStartInfo();

        ///--------------------------------------------------------------------------------------------------------
        ///
        public CommmandLineController()
        {
            startInfo.FileName = @"cmd";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;            // cmd창이 숨겨지도록 하기
            startInfo.CreateNoWindow = true;                              // cmd창을 띄우지 안도록 하기

            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;                      // cmd창에서 데이터를 가져오기
            startInfo.RedirectStandardInput = true;                       // cmd창으로 데이터 보내기
            startInfo.RedirectStandardError = true;                       // cmd창에서 오류 내용 가져오기
            
            startInfo.StandardOutputEncoding = Encoding.UTF8;
        }

        ///--------------------------------------------------------------------------------------------------------
        ///
        public string Process(string command)
        {
            string result = "";

            try
            {
                string cliPath = Config.QtumWalletPath;
                string rpcUser = Config.RPCUserName;
                string rpcPwd = Config.RPCPassword;
                command = string.Format(@"{0}\qtum-cli.exe -rpcuser={1} -rpcpassword={2} {3}", cliPath, rpcUser, rpcPwd, command);
                
                using (Process process = new Process())
                {
                    process.EnableRaisingEvents = false;
                    process.StartInfo = startInfo;

                    process.Start();
                    process.StandardInput.Write(command + Environment.NewLine);
                    process.StandardInput.Close();

                    result = process.StandardOutput.ReadToEnd();

                    int parseIdx = result.IndexOf(command);

                    if (parseIdx != -1)
                        result = result.Substring(parseIdx + command.Length);
                    
                    parseIdx = result.LastIndexOf(System.Environment.CurrentDirectory);

                    if (parseIdx != -1)
                        result = result.Substring(0, parseIdx);

                    process.WaitForExit();
                    process.Close();
                }
            }
            catch (Exception e)
            {
                result = e.ToString();
            }


            return result;
        }
        ///--------------------------------------------------------------------------------------------------------
    }
}
