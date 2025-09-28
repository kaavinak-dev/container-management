using DeploymentManager.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Implementations
{
    public class ArtifactDeploymentThroughIP : OrchestrationArtifactToLakeDeployer
    {
        private string _remoteIp;
        public ArtifactDeploymentThroughIP(OSOrchestrationArtifactBuilder artifactBuilder,string remoteIP):base(artifactBuilder) 
        { 
        
            _remoteIp = remoteIP;
        }

        public override void DeployArtifact()
        {
            this.ArtifactBuilder.BuildArtifactForDeployment();
            var processCommunicator = this.ArtifactBuilder.GetArtifactBuilderProcessInstance();
            processCommunicator.StartProcess();
            processCommunicator.ExecuteCommand($"curl -X POST -H \"Content-Type: application/x-tar\" --data-binary \"@D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-orchestrator-dependencies\\OSArtifacts.tar\" \"http://{_remoteIp}:2375/build?t=osprocessmanager:latest&dockerfile=windows/Dockerfile\""
                    , (err, outputLogs) =>
                    {
                    if (outputLogs.Data != null)
                    {
                        Console.WriteLine(outputLogs.Data);
                    }
                    },
                    (err, errorLogs) =>
                    {
                        if (errorLogs.Data != null)
                        {
                            Console.WriteLine(errorLogs.Data);
                        }
                    }
                    );

            processCommunicator.EndTransaction();
            processCommunicator.EndProcess();


        }
    }
}
