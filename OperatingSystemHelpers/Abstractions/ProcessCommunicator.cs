using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemHelpers.Abstractions
{
    public abstract class ProcessCommunicator
    {
        protected static Process processInstance;
        protected static bool commandProcessing;
        public ProcessCommunicator()
        {
        }
        public abstract void StartProcess(string workingDirPath = null);

        public abstract void ExecuteCommand(string command, DataReceivedEventHandler outputCb, DataReceivedEventHandler errorCb);

        public abstract void StartTransaction();
        public abstract void EndTransaction();
        public abstract void EndProcess();

    }
}
