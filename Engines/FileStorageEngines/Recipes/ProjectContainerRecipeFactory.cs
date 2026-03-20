using Engines.FileStorageEngines.Abstractions;

namespace Engines.FileStorageEngines.Recipes
{
    public static class ProjectContainerRecipeFactory
    {
        public static IProjectContainerRecipe GetRecipe(ProjectTypes projectType) =>
            projectType switch
            {
                ProjectTypes.JS => new NodeProjectContainerRecipe(),
                _ => throw new NotSupportedException($"No recipe for project type: {projectType}")
            };
    }
}
