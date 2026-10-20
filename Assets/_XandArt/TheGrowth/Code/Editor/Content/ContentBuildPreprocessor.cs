using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>Перед билдом: пересобрать реестр контента и проверить его; ошибки валидации останавливают билд.</summary>
    public sealed class ContentBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var registry = ContentRegistryBuilder.Rebuild();
            var errors = ContentValidator.ValidateProject(registry);
            if (errors.Count == 0)
                return;

            ContentValidator.Log(errors);
            throw new BuildFailedException($"Content validation failed: {errors.Count} error(s), see console");
        }
    }
}
