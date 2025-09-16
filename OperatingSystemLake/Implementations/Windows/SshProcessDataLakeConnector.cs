using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.Windows
{
    public class SshProcessDataLakeConnector : IProcessDataLakeConnector
    {
        private readonly string _ipToSshConnect;
        private readonly string _dataLakeUserName;
        public SshProcessDataLakeConnector(string ipToSshConnect,string dataLakeUserName) { 
            _ipToSshConnect = ipToSshConnect;
            _dataLakeUserName= dataLakeUserName;
        }
        public void ConnectProcessWithDataLake(Process processToConnect)
        {
            using var writer = processToConnect.StandardInput;
            writer.WriteLine($"""ssh {_dataLakeUserName}@{_ipToSshConnect}""");

        }
    }
}
