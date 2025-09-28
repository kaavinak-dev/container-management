using DeploymentManager.Abstractions;
using OperatingSystemHelpers.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Implementations
{

    public delegate void WindowsOSBuildLogHandler(object err,DataReceivedEventArgs logs);

    public class WindowsNativeExecutableBuilder : OSNativeExecutableBuilder    
    {
        public WindowsNativeExecutableBuilder(ProcessCommunicator _processCommunicator)
        {
            processCommunicator = _processCommunicator;
                
        }

        private DataReceivedEventHandler handleBuildLogs = (err, logs) =>
        {
            if (logs.Data != null)
            {
                Console.WriteLine(logs.Data);
            }
        };

        private DataReceivedEventHandler handleErrorLogs = (err, logs) =>
        {
            if (logs.Data != null) {
                Console.WriteLine(logs);
            }


        };

        public override void BuildExecutable()
        {
            processCommunicator.StartProcess();
            processCommunicator.StartTransaction();
            processCommunicator.ExecuteCommand("dotnet.exe publish \"D:\\advanced-programming\\dotnet\\container-management\\os-process-manager-service\\os-process-manager-service.csproj\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true  -o \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\"",handleBuildLogs, handleErrorLogs);
            processCommunicator.EndTransaction();
            processCommunicator.EndProcess();
               
        }

        public override ProcessCommunicator GetExecutableBuilderProcessInstance()
        {
            return this.processCommunicator;
        }
    }
}
