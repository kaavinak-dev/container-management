using OperatingSystemHelpers.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


namespace OperatingSystemHelpers.Implementations.Windows
{
    public class WindowsProcessCommunicator : ProcessCommunicator
    {
        private StreamWriter commandWriter;
        private DataReceivedEventHandler commandOutputHandler;
        private DataReceivedEventHandler commandErrorHandler;
        public WindowsProcessCommunicator() : base()
        {

        }

        public override void StartProcess(string workingDirPath = null)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,

            };
            if (!string.IsNullOrEmpty(workingDirPath))
            {
                startInfo.WorkingDirectory = workingDirPath;
            }
            processInstance = Process.Start(startInfo);
            processInstance.BeginErrorReadLine();
            processInstance.BeginOutputReadLine();
            commandWriter = processInstance.StandardInput;
            processInstance.OutputDataReceived += (err, eventMsg) =>
            {
                if (commandOutputHandler != null)
                {
                    if (eventMsg.Data == null || eventMsg.Data.Contains("_END_"))
                    {
                        processInstance.OutputDataReceived -= commandOutputHandler;
                        commandProcessing = false;
                    }
                }
            };
            processInstance.ErrorDataReceived += (err, eventMsg) =>
            {
                if (commandErrorHandler != null)
                {
                    if (eventMsg.Data == null || eventMsg.Data.Contains("_END_"))
                    {
                        processInstance.ErrorDataReceived -= commandErrorHandler;
                        commandProcessing = false;
                    }
                }
            };

        }

        public override void StartTransaction()
        {
            int counter = 0;
            while (commandProcessing)
            {
                Thread.Sleep(1000);
                counter += 1;
                if (counter == 300)
                {
                    throw new Exception("command processing start failed for process");
                }

            }

        }

        public override void EndTransaction()
        {
            int counter = 0;
            while (commandProcessing)
            {
                Thread.Sleep(1000);
                counter += 1;
                if (counter == 300)
                {
                    throw new Exception("command processing stuck for process");
                }
            }
        }

        public override void ExecuteCommand(string command, DataReceivedEventHandler outputCb, DataReceivedEventHandler errorCb)
        {
            int counter = 0;
            while (commandProcessing)
            {
                Thread.Sleep(1000);
                counter += 1;
                if (counter == 300)
                {
                    throw new Exception("command not executed");
                }
            }
            commandProcessing = true;
            commandWriter.WriteLine(command);
            commandWriter.WriteLine("echo _END_");
            commandOutputHandler = outputCb;
            commandErrorHandler = errorCb;
            processInstance.OutputDataReceived += outputCb;
            processInstance.ErrorDataReceived += errorCb;


        }



        public override void EndProcess()
        {
            processInstance.Kill();
            processInstance.WaitForExit(5000);
            processInstance.Dispose();
        }
    }
}
