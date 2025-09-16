using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.DeploymentComponents.Abstractions
{
    public abstract class OSOrchestrator
    {
        protected Process OrchestratorProcess; 
        public abstract  Task  SetUpOrchestrationEnvironment();
        public abstract Task BuildOSArtifact();
    }
}
