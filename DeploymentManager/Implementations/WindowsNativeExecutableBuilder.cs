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
        private readonly string _solutionRoot;

        public WindowsNativeExecutableBuilder(ProcessCommunicator _processCommunicator, string solutionRoot)
        {
            processCommunicator = _processCommunicator;
            _solutionRoot = solutionRoot;
        }

        private DataReceivedEventHandler handleBuildLogs = (err, logs) =>
        {
            if (logs.Data != null) Console.WriteLine(logs.Data);
        };

        private DataReceivedEventHandler handleErrorLogs = (err, logs) =>
        {
            if (logs.Data != null) Console.WriteLine(logs.Data);
        };

        public override void BuildExecutable()
        {
            var projectPath = Path.Combine(_solutionRoot, "os-process-manager-service", "os-process-manager-service.csproj");
            var outputPath  = Path.Combine(_solutionRoot, "DeploymentManager", "DeploymentComponents", "os-process-manager-binaries", "windows");

            processCommunicator.StartProcess();
            processCommunicator.StartTransaction();
            processCommunicator.ExecuteCommand(
                $"dotnet.exe publish \"{projectPath}\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o \"{outputPath}\"",
                handleBuildLogs, handleErrorLogs);
            processCommunicator.EndTransaction();
            processCommunicator.EndProcess();
        }

        public override ProcessCommunicator GetExecutableBuilderProcessInstance() => processCommunicator;
    }
}
