using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSOrchestrator.Abstractions
{
    public abstract class OSOrchestrator
    {
        public OSOrchestrator()
        {
                   
        }

        public abstract void CreateAndSetOSOrchestratorClient();
        public abstract T GetOSOrchestratorClient<T>();

        public abstract void CreateOS();

    }
}
