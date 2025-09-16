using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Abstractions
{

    
    public abstract class OSProcess
    {
        public string ProcessId { get; set; }

       
        public OSProcess()
        {

        }

        public OSProcess(string processId)
        {
            ProcessId=processId;
        }

      
                
    }
}
