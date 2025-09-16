using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Windows
{
    public class WindowsOSLake : BaseOSLake
    {
        private static readonly object lockObject;
        protected static bool osLakeInstance;

        public WindowsOSLake() { 
        
        }

        public override bool GetLakeInstanceStatus()
        {
            return osLakeInstance;            
        }

        public override Process GetOSLakeAttachedProcess(ProcessStartInfo processConfig,IProcessDataLakeConnector processDataLakeConnector)
        {
            var process = Process.Start(processConfig);

        }

        public override object GetOSLakeLockObject()
        {
            return lockObject;            
        }

        public override void LakeInstanceInstantiated()
        {
                        
        }
    }
}
