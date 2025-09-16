using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class OSJobFactory
    {
        public abstract OSJob CreateJob(string jobHandle);
        public abstract OSJob CreateJob(string jobHandle, string jobName);
    }
}
