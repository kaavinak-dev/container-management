using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSOrchestrator;

namespace  OSOrchestrator.Abstractions
{
    public interface OSOrchestratorFromOSLakeStrategy<OSOrchestratorClient>
    {
        public   OSOrchestrator<OSOrchestratorClient> GetOSOrchestrator();
    }
}
