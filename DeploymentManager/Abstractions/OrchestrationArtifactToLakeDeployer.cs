using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Abstractions
{
    public abstract class OrchestrationArtifactToLakeDeployer
    {
        public OSOrchestrationArtifactBuilder ArtifactBuilder;

        public OrchestrationArtifactToLakeDeployer(OSOrchestrationArtifactBuilder ArtifactBuilder)
        {
            this.ArtifactBuilder = ArtifactBuilder;
        }

        public abstract void DeployArtifact();
    }
}
