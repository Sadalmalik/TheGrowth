using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace TheGrowth.EditorTools.Content
{
    /// <summary>Перед билдом: пересобрать реестр контента и проверить его; ошибки валидации останавливают билд, предупреждения — нет.</summary>
    public sealed class ContentBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var registry = ContentRegistryBuilder.Rebuild();
            var errors = ContentValidator.ValidateProject(registry);
            ContentValidator.Log(errors);
            var count = ContentValidator.CountErrors(errors);
            if (count > 0)
                throw new BuildFailedException($"Content validation failed: {count} error(s), see console");
        }
    }
}
