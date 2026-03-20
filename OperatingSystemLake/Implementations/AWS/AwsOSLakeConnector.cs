using OperatingSystemLake.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OperatingSystemLake.Implementations.AWS
{
    /// <summary>
    /// OS Lake connector backed by AWS EC2.
    /// Future implementation: discovers EC2 instances tagged for container hosting
    /// and returns their IPs for Docker daemon connectivity.
    ///
    /// To implement: add AWSSDK.EC2 NuGet package and use DescribeInstances
    /// filtered by tag (e.g., "ContainerManagement:OSLake") with Platform filter
    /// for Windows vs Linux instance types.
    /// </summary>
    public class AwsOSLakeConnector : OSLakeConnector
    {
        public AwsOSLakeConnector() { }

        public override List<BaseOSLake> GetAvailableOSLakes()
        {
            throw new NotImplementedException("AWS EC2 connector not yet implemented.");
        }

        public override BaseOSLake GetOSLakeByType(OSLakeTypes osType)
        {
            throw new NotImplementedException("AWS EC2 connector not yet implemented.");
        }

        public override string GetOSLakeIp(string lakeName)
        {
            throw new NotImplementedException("AWS EC2 connector not yet implemented.");
        }
    }
}
