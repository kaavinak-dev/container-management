using Domain.Entities.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Ports.OSPorts
{
    public abstract class OSJobQueryObject
    {
        public OSJob Job { get; set; }
        public int numberOfProcessesInJob { get; set; }

        public abstract List<long> GetAllProcessIdsInJob();

    }

    public abstract class OSJobManager
    {

        public OSProcessFactory processFactory { get; set; }
        public OSJobFactory jobFactory { get; set; }
        public OSJobManager(OSProcessFactory _processFactory, OSJobFactory _jobFactory)
        {
            processFactory = _processFactory;
            jobFactory = _jobFactory;
        }

        public abstract OSJob CreateJobInOS(string jobName);
        public abstract List<OSProcess> GetProcessesInJob(OSJob job);

        public abstract OSJobQueryObject QueryJobInformation(OSJob job);

        public abstract OSProcess AddProcessToJob(OSJob job,OSProcessManager processManager);

    }


}
