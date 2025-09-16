using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{
    public abstract class OSJob
    {
        public  string JobHandle { get; set; }
        public string JobName { get; set; }

        public OSJob(string _jobHandle)
        {
            JobHandle = _jobHandle;
        }

        public OSJob(string _jobHandle,string _jobName)
        {
            JobHandle= _jobHandle;
            JobName= _jobName;
        }
    }
}

