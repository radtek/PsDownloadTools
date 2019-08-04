using PsDownloadTools.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace PsDownloadTools.Model
{
    class CompanionManager
    {
        public static void CallCompanionThread()
        {
            using (Process p = new Process())
            {
                p.StartInfo.FileName = System.Windows.Forms.Application.ExecutablePath;
                p.StartInfo.Arguments = "-c";
                p.Start();
                p.Dispose();
            }
        }

        public static void StartCompanionThread()
        {
            new Thread(async () =>
            {
                while (true)
                {
                    Thread.Sleep(5 * 1000);

                    if (!IsMainProcessOn())
                    {
                        await Aria2Manager.GetInstance().StopServer();
                        break;
                    }
                }
                Environment.Exit(0);
            })
            { IsBackground = true }.Start();
        }

        private static Boolean IsMainProcessOn()
        {
            Process current = Process.GetCurrentProcess();
            List<Process> processes = Process.GetProcessesByName(current.ProcessName).ToList();
            return processes.Any(process => process.ProcessName.Equals(current.ProcessName) && process.Id != current.Id);
        }
    }
}
