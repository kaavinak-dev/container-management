using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Abstractions
{
    public abstract class BaseOSLake
    {
        protected Dictionary<string, object> LakeProperties = new();
        

        public abstract Process GetOSLakeAttachedProcess(ProcessStartInfo processConfig,IProcessDataLakeConnector processDataLakeConnector);
        public abstract object GetOSLakeLockObject();

        public abstract bool GetLakeInstanceStatus();

        public abstract void LakeInstanceInstantiated();

        public void InstantiateLakeInstance()
        {
            if (GetLakeInstanceStatus() == false)
            {
                lock (GetOSLakeLockObject())
                {
                    if (GetLakeInstanceStatus() == false)
                    {
                        LakeInstanceInstantiated();
                    }
                }
            }
        }
        
    }
}
