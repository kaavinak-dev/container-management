using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grpc.Core;
using GrpcApplyServer;
using Domain.Entities.Abstractions;

namespace OSProcessManagerInfastructure.Grpc
{
    public class ApplyService:GrpcApplyServer.GrpcApplyService.GrpcApplyServiceBase
    {
        BaseApplier jobApplier;

        public ApplyService(BaseApplier jobApplier)
        {
            this.jobApplier = jobApplier;
        }

        public override Task<CreateJobResponse> CreateJob(CreateJobRequest request, ServerCallContext context)
        {
            var response =jobApplier.CreateJob(request.JobName);
            return null;
        }

        public override Task<CreateProcessResponse> CreateProcessInJob(CreateProcessRequest request, ServerCallContext context)
        {
            return base.CreateProcessInJob(request, context);
        }
    }
}
