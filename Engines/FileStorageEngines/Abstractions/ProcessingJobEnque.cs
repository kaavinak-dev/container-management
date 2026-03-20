using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Engines.FileStorageEngines.Abstractions
{
    public abstract class ProcessingJobEnque
    {

        public abstract bool isValidProject(Stream fileStreamData);
        public abstract List<string> getProjectFileExtensionsSupported();
        public abstract Task DoWork(ProjectContainer projectToProcess);
        public abstract void EnqueJob(ProjectContainer projectToProcess);
        public abstract Task ProcessProject(Stream streamData, ProjectContainer projectContainer);
        public abstract Task<(VirusScanResults, ProjectMetaData)> VirusScanAndExtractMetaData(Stream streamData, ProjectContainer projectContainer);
    }

    public class ProjectMetaData
    {

    }

}
