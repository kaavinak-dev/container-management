using Domain.Ports.OSPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class BaseApplier
    {
        protected OSJobManager jobManager;
        protected OSProcessManager processManager;
        protected OSProcessDiagnosticManager processDiagnosticManager;

        public BaseApplier(OSJobManager jobManager,OSProcessDiagnosticManager processDiagnosticManager)
        {
            this.jobManager = jobManager;
            this.processDiagnosticManager = processDiagnosticManager;
        }

       

        public BaseApplier()
        {

        }

        public abstract OSJob CreateJob(string jobName);

    }
}
