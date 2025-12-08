using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Engines.FileStorageEngines.Abstractions
{
    public abstract class FileProcessingJobEnque
    {

        public abstract bool isValidFile(Stream fileStreamData);
        public abstract List<string> getFileExtensionsSupported();
        public abstract void DoWork(FileContainer fileToProcess);
        public abstract void EnqueJob(FileContainer fileToProcess);
    }

}
