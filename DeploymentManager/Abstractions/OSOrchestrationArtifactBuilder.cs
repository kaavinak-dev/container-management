using OperatingSystemHelpers.Abstractions;
using OperatingSystemHelpers.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Abstractions
{
    public abstract class OSOrchestrationArtifactBuilder
    {
        protected readonly OSNativeExecutableBuilder _executablebuilder;
        protected readonly OSTypes oSType;
        public OSOrchestrationArtifactBuilder(OSTypes osType,OSNativeExecutableBuilder executableBuilder)
        {
            _executablebuilder = executableBuilder;
            this.oSType= osType;
        }
        public abstract void BuildArtifactForDeployment();

        public abstract ProcessCommunicator GetArtifactBuilderProcessInstance();

    }
}
