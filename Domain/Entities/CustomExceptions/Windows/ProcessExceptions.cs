using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentResults;
namespace Domain.Entities.CustomExceptions.Windows
{
    public abstract class ProcessExceptions: Error
    {
        public ProcessExceptions(string message): base(message) { }
    }

}
