using Domain.Ports.OSPorts;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Core;
namespace OSProcessManagerInfastructure.Grpc
{
    public static class GrpcServicesInjector
    {
        public static void AddGrpcServices(this IServiceCollection services)
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5001",new GrpcChannelOptions
                {
                    Credentials=ChannelCredentials.Insecure
                });
            services.AddSingleton(channel);
            services.AddSingleton<OSApplyServiceClient, GrpcApplyServiceClient>();
        }
    }
}
