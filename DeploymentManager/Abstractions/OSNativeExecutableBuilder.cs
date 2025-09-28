using OperatingSystemHelpers.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeploymentManager.Abstractions
{

    public abstract class OSNativeExecutableBuilder    
    {

        public ProcessCommunicator processCommunicator;       
        public abstract void BuildExecutable();

        public abstract ProcessCommunicator GetExecutableBuilderProcessInstance();

          
    }
}
