using DeploymentManager.DeploymentComponents.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.DeploymentComponents.Docker
{
    public class DockerOSOrchestrator : OSOrchestrator
    {
        public override Task BuildOSArtifact()
        {
            throw new NotImplementedException();
        }

        public override async Task SetUpOrchestrationEnvironment()
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo()
            {
                FileName="cmd.exe",
                
                UseShellExecute=false,
                CreateNoWindow=true,
                RedirectStandardOutput=true,
                RedirectStandardError=true,
                RedirectStandardInput=true,
                

            };
            using var process = Process.Start(processStartInfo);
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                Console.WriteLine("[stdout] " + e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    Console.WriteLine("[stderr] " + e.Data);
            };
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            using var writer = process.StandardInput;
            writer.WriteLine("""@FOR /f "tokens=*" %i IN ('"C:\docker\docker-machine\docker-machine.exe" env default') DO @%i""");
            writer.WriteLine("""cd /d D:\advanced-programming\dotnet\container-management\DeploymentManager\DeploymentComponents\os-process-manager-binaries\windows\""");
            writer.WriteLine("""docker build -t windows-process-manager .""");
          //  writer.WriteLine("docker images");
            process.WaitForExit();
                  
        }
    }
}
