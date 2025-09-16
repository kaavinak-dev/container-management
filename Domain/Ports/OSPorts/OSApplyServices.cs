using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Ports.OSPorts
{
    public interface OSApplyServiceClient
    {
        public Task<byte[]> CreateJobTask(string jobName);
        public Task<byte[]> CreateProcessInJobTask();
    }
}
