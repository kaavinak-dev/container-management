// See https://aka.ms/new-console-template for more information


using OSOrchestrator.Implementations;
using OSOrchestrator.Abstractions;
using OperatingSystemLake.Implementations.Windows;
using System.Diagnostics;
using OperatingSystemHelpers.Implementations.Windows;
using Docker.DotNet;
using System.Formats.Tar;
using Docker.DotNet.Models;
using DeploymentManager.Constants;
using OperatingSystemHelpers.Constants;
using DeploymentManager.Implementations;

async Task GenerateBuildBasedOnOS(string osType)
{
    var processCommnicator = new WindowsProcessCommunicator();
    processCommnicator.StartProcess();
    processCommnicator.StartTransaction();
    processCommnicator.ExecuteCommand("dotnet.exe publish \"D:\\advanced-programming\\dotnet\\container-management\\os-process-manager-service\\os-process-manager-service.csproj\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true  -o \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\""
        , (err, outputLogs) =>
        {
            if (outputLogs.Data != null)
            {
                Console.WriteLine(outputLogs.Data);
            }    
        },
        (err, errorLogs) =>
        {

        }
        );
    processCommnicator.ExecuteCommand("copy \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\Dockerfile\" \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\\Dockerfile\""
        , (err, outputLogs) =>
        {
            if (outputLogs.Data != null)
            {
                Console.WriteLine(outputLogs.Data);
            }    
        },
        (err, errorLogs) =>
        {

        }
        );

    processCommnicator.ExecuteCommand("tar -cvf \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-orchestrator-dependencies\\OSArtifacts.tar\" -C  \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\" windows"
        , (err, logs) =>
        {
            if (logs.Data != null)
            {
                Console.WriteLine(logs.Data);
            }
        }
        , (err, elogs) =>
        {
            if (elogs.Data != null)
            {
                Console.WriteLine(elogs.Data);
            }
        }
        );
    processCommnicator.EndTransaction();
    processCommnicator.EndProcess();
    //switch (osType)
    //{
    //    case "Windows":
    //        psi = new ProcessStartInfo
    //              {
    //                FileName = "dotnet.exe",
    //                Arguments = "publish \"D:\\advanced-programming\\dotnet\\container-management\\os-process-manager-service\\os-process-manager-service.csproj\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true  -o \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\"",
    //                RedirectStandardOutput = true,
    //                RedirectStandardError = true,
    //                UseShellExecute = false,
    //                CreateNoWindow = true,
                    
    //              };
    //        break;
    //    default:
    //        return;
    //}
    //using var process = Process.Start(psi);
    //process.OutputDataReceived += (sender, e) =>
    //{
    //    if (e.Data != null)
    //    {
    //        Console.WriteLine("[out] "+e.Data+"\n");
    //    }
    //};
    //process.ErrorDataReceived += (sender, e) =>
    //{
    //    if (e.Data != null)
    //    {
    //        Console.WriteLine("[error] "+e.Data+"\n");
    //    }
    //};
    //process.BeginOutputReadLine();
    //process.BeginErrorReadLine();
    //process.WaitForExit();
}

async Task DeployOrchestrationArtifactsToLake()
{
    var processCommnicator = new WindowsProcessCommunicator();
    processCommnicator.StartProcess();
    processCommnicator.StartTransaction();
    processCommnicator.ExecuteCommand("dotnet.exe publish \"D:\\advanced-programming\\dotnet\\container-management\\os-process-manager-service\\os-process-manager-service.csproj\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true  -o \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\""
        , (err, outputLogs) =>
        {
            if (outputLogs.Data != null)
            {
                Console.WriteLine(outputLogs.Data);
            }    
        },
        (err, errorLogs) =>
        {

        }
     );
    processCommnicator.ExecuteCommand("copy \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\Dockerfile\" \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\\Dockerfile\""
        , (err, outputLogs) =>
        {
            if (outputLogs.Data != null)
            {
                Console.WriteLine(outputLogs.Data);
            }    
        },
        (err, errorLogs) =>
        {

        }
     );

}


//async Task GetOSLakeOrchestrator(OperatingSystemLake.Abstractions.OSLakeOrchestrator oSLakeOrchestrator)
//{
//   var osLake= oSLakeOrchestrator.GetRunningOSLakeForOrchestratorType(OperatingSystemLake.Abstractions.OSLakeTypes.Windows);
//   var dockerorchestrator = new DockerClientFromOSLake(osLake.OSLakeIp).GetOSOrchestrator();
//    using (FileStream fs = new FileStream("D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-orchestrator-dependencies\\OSArtifacts.tar", FileMode.Open, FileAccess.Read))
//    {
//            var buildParams = new ImageBuildParameters
//            {
//                Dockerfile = "windows/Dockerfile",

//            };
//            var progressTracker = new Progress<JSONMessage>((message) =>
//            {
//                if (message.ErrorMessage != null)
//                {
//                    Console.WriteLine(message.ErrorMessage);
//                }
//                if (message.Progress != null)
//                {
//                    Console.WriteLine(message.Progress);
//                }
//            });
//            await dockerorchestrator.GetOSOrchestratorClient().Images.BuildImageFromDockerfileAsync(buildParams,fs,default,default,
//                           progressTracker);
                
        
//    }
//}



var osType = args[1];
var action = args[0];

if(action.ToLower() == DeploymentActions.DeployArtifact.ToString().ToLower())
{
    if (osType.ToLower() == OSTypes.Windows.ToString().ToLower())
    {
        var processCommunicator = new WindowsProcessCommunicator();
        var executableBuilder = new WindowsNativeExecutableBuilder(processCommunicator);
        var dockerImageBuilder=new DockerImageBuilder(OSTypes.Windows,executableBuilder);
        //dockerImageBuilder.BuildArtifactForDeployment();
        var remoteIp = new VirtualBoxOSLakeOrchestrator(processCommunicator).GetOSLakeIp("Windows Server");
        var dockerImageRemoteDeployer = new ArtifactDeploymentThroughIP(dockerImageBuilder, remoteIp);
        dockerImageRemoteDeployer.DeployArtifact();
    }
}

//GenerateBuildBasedOnOS(osType);
//GetOSLakeOrchestrator(new VirtualBoxOSLakeOrchestrator(new WindowsProcessCommunicator()));


