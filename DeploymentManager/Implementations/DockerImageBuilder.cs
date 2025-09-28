using DeploymentManager.Abstractions;
using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Implementations
{
    public class DockerImageBuilder:OSOrchestrationArtifactBuilder
    {
        public DockerImageBuilder(OSTypes osType,OSNativeExecutableBuilder executableBuilder):base(osType,executableBuilder) {
        

        }

       
        public override void BuildArtifactForDeployment()
        {
            this._executablebuilder.BuildExecutable();
            switch (this.oSType)
            {
                case OSTypes.Windows:
                    var processCommunicator = this.GetArtifactBuilderProcessInstance();
                    processCommunicator.StartProcess();
                    processCommunicator.ExecuteCommand("copy \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\Dockerfile\" \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\\windows\\Dockerfile\""
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

                    processCommunicator.ExecuteCommand("tar -cvf \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-orchestrator-dependencies\\OSArtifacts.tar\" -C  \"D:\\advanced-programming\\dotnet\\container-management\\DeploymentManager\\DeploymentComponents\\os-process-manager-binaries\" windows"
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
                    processCommunicator.EndTransaction();
                    processCommunicator.EndProcess();
                    break;

                default:
                    break;
            }

        }

        public override ProcessCommunicator GetArtifactBuilderProcessInstance()
        {
            return this._executablebuilder.processCommunicator;
        }
    }
}
