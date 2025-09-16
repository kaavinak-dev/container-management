using Domain.Entities.Abstractions;
using Domain.Ports.OSPorts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Implementations.Windows
{
    public class WindowsApplier: BaseApplier
    {

        public WindowsApplier(OSJobManager jobManager,OSProcessDiagnosticManager processDiagnosticManager) : base(jobManager,processDiagnosticManager)
        {
            
        }

        public override OSJob CreateJob(string jobName)
        {
            return jobManager.CreateJobInOS(jobName);
        }
    }
}
