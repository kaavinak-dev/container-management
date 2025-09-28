using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSOrchestrator.Abstractions
{
    public abstract class OSOrchestrator<Client>
    {
        protected Client OSOrchestratorClient;
        public abstract  Client  GetOSOrchestratorClient();
        public abstract Task BuildOSArtifact();
    }
}
