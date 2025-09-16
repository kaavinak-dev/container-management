using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class OSJobDiagnostics
    {
        public OSJob Job { get; set; }
        public OSJobDiagnostics(OSJob _job)
        {
            Job=_job;
        }

        public abstract List<OSProcessDiagnostics> ListAllJobProcessDiagnostics();
    }
}
