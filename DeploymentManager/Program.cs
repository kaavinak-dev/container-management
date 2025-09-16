// See https://aka.ms/new-console-template for more information


using DeploymentManager.DeploymentComponents.Abstractions;
using DeploymentManager.DeploymentComponents.Docker;
using System.Diagnostics;

async Task GenerateBuildBasedOnOS(string osType)
{
    ProcessStartInfo psi = new ProcessStartInfo();
    switch (osType)
    {
        case "Windows":
            psi = new ProcessStartInfo
                  {
                    FileName = "dotnet.exe",
                    Arguments = "publish \"D:\\advanced-programming\\dotnet\\container-management\\os-process-manager-service\\os-process-manager-service.csproj\" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true  -o \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    
                  };
            break;
        default:
            return;
    }
    using var process = Process.Start(psi);
    process.OutputDataReceived += (sender, e) =>
    {
        if (e.Data != null)
        {
            Console.WriteLine("[out] "+e.Data+"\n");
        }
    };
    process.ErrorDataReceived += (sender, e) =>
    {
        if (e.Data != null)
        {
            Console.WriteLine("[error] "+e.Data+"\n");
        }
    };
    process.BeginOutputReadLine();
    process.BeginErrorReadLine();
    process.WaitForExit();
}

async Task StartDocker(OSOrchestrator containerManager)
{
    await containerManager.SetUpOrchestrationEnvironment();
}

var osType = args[0];
GenerateBuildBasedOnOS(osType);
StartDocker(new DockerOSOrchestrator());


