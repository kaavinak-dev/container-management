using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Recipes
{
    public interface IProjectContainerRecipe
    {
        string BaseImage { get; }
        string BuildStep { get; }
        string StartCommand { get; }
        ProjectTypes ProjectType { get; }
    }
}
