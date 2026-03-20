using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Recipes
{
    public class NodeProjectContainerRecipe : IProjectContainerRecipe
    {
        public string BaseImage    => "node:20-slim";
        public string BuildStep    => "RUN npm install --production";
        public string StartCommand => "node index.js";
        public ProjectTypes ProjectType => ProjectTypes.JS;
    }
}
