using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Domain.Ports;
using Domain.Ports.OSPorts;
using GrpcApplyServer;
using Newtonsoft;
using Newtonsoft.Json;
using Grpc.Net.Client;

namespace OSProcessManagerInfastructure.Grpc
{
    public class GrpcApplyServiceClient : GrpcApplyServer.GrpcApplyService.GrpcApplyServiceClient, OSApplyServiceClient
    {

        public GrpcApplyServiceClient(GrpcChannel channel) : base(channel)
        {

        }

        public Task<byte[]> CreateJobTask(string jobName)
        {
            var request = new CreateJobRequest
            {
                JobName = jobName
            };
            var response = this.CreateJob(request);           
            return Task.FromResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response)));
        }

        public Task<byte[]> CreateProcessInJobTask()
        {
            throw new NotImplementedException();
        }
    }
}
