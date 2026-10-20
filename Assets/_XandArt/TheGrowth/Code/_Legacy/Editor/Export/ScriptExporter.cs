using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XandArt.TheGrowth.StoryLine;
using Object = UnityEngine.Object;

namespace XandArt.TheGrowth.Editor.Export
{
    /// <summary>
    /// Выгрузка скриптов прототипа (команды, кондишены, эвалуаторы, триггеры, компоненты шагов сюжета)
    /// из данных Odin в текст. Источники: ScriptableObject-ассеты и префабы в Assets/_XandArt/TheGrowth,
    /// сцены в Assets/_XandArt/TheGrowth/Maps. Результат — Export/Scripts в корне репозитория.
    /// Запуск из batchmode: -executeMethod XandArt.TheGrowth.Editor.Export.ScriptExporter.Export
    /// </summary>
    public static class ScriptExporter
    {
#region Constants

        private const string k_ContentRoot = "Assets/_XandArt/TheGrowth";
        private const string k_ScenesRoot = "Assets/_XandArt/TheGrowth/Maps";
        private const string k_Indent = "    ";
        private const int k_MaxDepth = 64;

#endregion

#region Entry

        [MenuItem("[TheGrowth]/Legacy/Export Scripts To Text")]
        public static void Export()
        {
            var interactive = !Application.isBatchMode;
            SceneSetup[] sceneSetup = null;
            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                sceneSetup = EditorSceneManager.GetSceneManagerSetup();
            }

            var exporter = new Exporter();
            try
            {
                exporter.Run(interactive);
            }
            finally
            {
                if (interactive)
                    EditorUtility.ClearProgressBar();

                RestoreScenes(sceneSetup);
            }

            Debug.Log($"[ScriptExporter] Done: {exporter.Summary}");
        }

        private static void RestoreScenes(SceneSetup[] sceneSetup)
        {
            if (sceneSetup is { Length: > 0 } && sceneSetup.All(s => !string.IsNullOrEmpty(s.path)))
            {
                try
                {
                    EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ScriptExporter] Scene setup restore failed: {e.Message}");
                }
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

#endregion

#region Exporter

        private sealed class Exporter
        {
            private readonly Assembly m_CodeAssembly = typeof(Command).Assembly;
            private readonly Dictionary<Type, List<FieldInfo>> m_FieldsCache = new Dictionary<Type, List<FieldInfo>>();
            private readonly Dictionary<Type, string> m_TypeNameCache = new Dictionary<Type, string>();
            private readonly HashSet<string> m_CodeNames = new HashSet<string>();
            private readonly HashSet<string> m_AmbiguousNames = new HashSet<string>();
            private readonly HashSet<string> m_UsedOutputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            private readonly List<SourceResult> m_Results = new List<SourceResult>();
            private readonly List<string> m_GlobalProblems = new List<string>();
            private readonly Dictionary<string, int> m_NodeTypes = new Dictionary<string, int>();

            private string m_OutputRoot;
            private SourceResult m_Current;
            private int m_AssetsScanned;
            private int m_PrefabsScanned;
            private int m_ScenesScanned;

            public string Summary { get; private set; }

            public void Run(bool interactive)
            {
                m_OutputRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Export", "Scripts"));
                if (Directory.Exists(m_OutputRoot))
                    Directory.Delete(m_OutputRoot, true);
                Directory.CreateDirectory(m_OutputRoot);

                CollectAmbiguousNames();

                Application.logMessageReceived += HandleLog;
                try
                {
                    var assetPaths = FindPaths("t:ScriptableObject", k_ContentRoot);
                    var prefabPaths = FindPaths("t:Prefab", k_ContentRoot);
                    var scenePaths = FindPaths("t:Scene", k_ScenesRoot);
                    var total = assetPaths.Count + prefabPaths.Count + scenePaths.Count;
                    var index = 0;

                    foreach (var path in assetPaths)
                    {
                        if (interactive)
                            EditorUtility.DisplayProgressBar("Export scripts", path, (float) index / total);
                        index++;
                        m_AssetsScanned++;
                        ExportSource(path, () => CollectAssetRoots(path));
                    }

                    foreach (var path in prefabPaths)
                    {
                        if (interactive)
                            EditorUtility.DisplayProgressBar("Export scripts", path, (float) index / total);
                        index++;
                        m_PrefabsScanned++;
                        ExportSource(path, () => CollectPrefabRoots(path));
                    }

                    foreach (var path in scenePaths)
                    {
                        if (interactive)
                            EditorUtility.DisplayProgressBar("Export scripts", path, (float) index / total);
                        index++;
                        m_ScenesScanned++;
                        ExportSource(path, () => CollectSceneRoots(path));
                    }
                }
                finally
                {
                    Application.logMessageReceived -= HandleLog;
                }

                WriteSummary();
            }

#region Sources

            private static List<string> FindPaths(string filter, string folder)
            {
                return AssetDatabase.FindAssets(filter, new[] { folder })
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct()
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToList();
            }

            private void ExportSource(string assetPath, Func<List<RootObject>> collectRoots)
            {
                m_Current = new SourceResult(assetPath);
                try
                {
                    var roots = collectRoots();
                    foreach (var root in roots)
                        WalkRoot(root);

                    if (Application.isBatchMode)
                    {
                        foreach (var root in roots)
                        {
                            if (root.Target != null && EditorUtility.IsPersistent(root.Target))
                                EditorUtility.ClearDirty(root.Target);
                        }
                    }
                }
                catch (Exception e)
                {
                    m_Current.Problems.Add($"exception: {e.GetType().Name}: {e.Message}");
                }

                if (m_Current.BlockCount > 0)
                    WriteSource(m_Current);

                if (m_Current.BlockCount > 0 || m_Current.Problems.Count > 0)
                    m_Results.Add(m_Current);

                m_Current = null;
            }

            private List<RootObject> CollectAssetRoots(string path)
            {
                var roots = new List<RootObject>();
                var main = AssetDatabase.LoadMainAssetAtPath(path);
                if (main == null)
                {
                    m_Current.Problems.Add("main asset failed to load (missing script?)");
                    return roots;
                }

                foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (obj == null)
                    {
                        m_Current.Problems.Add("sub-asset failed to load (missing script?)");
                        continue;
                    }

                    if (obj is not ScriptableObject)
                        continue;

                    var prefix = obj == main ? "" : $"{obj.name}({FormatTypeName(obj.GetType())})";
                    roots.Add(new RootObject(obj, prefix));
                }

                return roots;
            }

            private List<RootObject> CollectPrefabRoots(string path)
            {
                var roots = new List<RootObject>();
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    m_Current.Problems.Add("prefab failed to load");
                    return roots;
                }

                CollectComponents(prefab, null, roots);
                return roots;
            }

            private List<RootObject> CollectSceneRoots(string path)
            {
                var roots = new List<RootObject>();
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (!scene.IsValid())
                {
                    m_Current.Problems.Add("scene failed to open");
                    return roots;
                }

                var sceneRoots = scene.GetRootGameObjects();
                foreach (var go in sceneRoots)
                    CollectComponents(go, sceneRoots, roots);
                return roots;
            }

            private void CollectComponents(GameObject root, GameObject[] siblingRoots, List<RootObject> roots)
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    var hierarchyPath = GetHierarchyPath(transform, siblingRoots);
                    var components = transform.GetComponents<Component>();
                    var typeCounters = new Dictionary<Type, int>();
                    foreach (var component in components)
                    {
                        if (component == null)
                        {
                            m_Current.Problems.Add($"missing script on '{hierarchyPath}'");
                            continue;
                        }

                        var type = component.GetType();
                        typeCounters.TryGetValue(type, out var counter);
                        typeCounters[type] = counter + 1;

                        if (type.Assembly != m_CodeAssembly)
                            continue;

                        var sameTypeCount = components.Count(c => c != null && c.GetType() == type);
                        var typeName = FormatTypeName(type);
                        var prefix = sameTypeCount > 1
                            ? $"{typeName}[{counter}]@{hierarchyPath}"
                            : $"{typeName}@{hierarchyPath}";
                        roots.Add(new RootObject(component, prefix));
                    }
                }
            }

#endregion

#region Walker

            private void WalkRoot(RootObject root)
            {
                var visited = new HashSet<object>(ReferenceComparer.Instance);
                foreach (var field in GetSerializedFields(root.Target.GetType()))
                {
                    var value = field.GetValue(root.Target);
                    WalkValue(value, field.FieldType, JoinPath(root.Prefix, field.Name), visited, 0);
                }
            }

            private void WalkValue(object value, Type declaredType, string path, HashSet<object> visited, int depth)
            {
                if (depth > k_MaxDepth)
                {
                    m_Current.Problems.Add($"max depth reached at '{path}'");
                    return;
                }

                if (value == null || value is Object || value is string || IsSimpleValue(value))
                    return;

                if (IsNode(value))
                {
                    PrintBlock(path, value);
                    return;
                }

                if (value is IDictionary dictionary)
                {
                    var entries = dictionary.Cast<DictionaryEntry>().ToList();
                    if (entries.Any(e => IsNode(e.Key) || IsNode(e.Value)) || IsNodeType(GetDictionaryValueType(declaredType)))
                    {
                        if (entries.Count > 0)
                            PrintBlock(path, value);
                        return;
                    }

                    foreach (var entry in entries)
                        WalkValue(entry.Value, entry.Value?.GetType(), $"{path}[{FormatInline(entry.Key)}]", visited, depth + 1);
                    return;
                }

                if (value is IEnumerable enumerable)
                {
                    var items = enumerable.Cast<object>().ToList();
                    if (items.Any(IsNode) || IsNodeType(GetElementType(declaredType ?? value.GetType())))
                    {
                        if (items.Count > 0)
                            PrintBlock(path, value);
                        return;
                    }

                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (item == null)
                            continue;
                        WalkValue(item, item.GetType(), $"{path}[{i}]({FormatTypeName(item.GetType())})", visited, depth + 1);
                    }

                    return;
                }

                var type = value.GetType();
                if (type.Assembly != m_CodeAssembly)
                    return;

                if (!type.IsValueType && !visited.Add(value))
                    return;

                foreach (var field in GetSerializedFields(type))
                    WalkValue(field.GetValue(value), field.FieldType, $"{path}.{field.Name}", visited, depth + 1);
            }

            private static string JoinPath(string prefix, string field)
            {
                return string.IsNullOrEmpty(prefix) ? field : $"{prefix}.{field}";
            }

#endregion

#region Printer

            private void PrintBlock(string path, object value)
            {
                var sb = m_Current.Text;
                sb.Append('\n');
                m_Current.BlockCount++;

                if (value is IDictionary || (value is IEnumerable && !(value is string)))
                {
                    AppendLine(0, $"{path}:");
                    PrintCollectionItems(value, 1, path, 0);
                    return;
                }

                AppendLine(0, $"{path}:");
                PrintObject(value, 1, "", path, 0);
            }

            private void PrintObject(object value, int indent, string prefix, string path, int depth)
            {
                if (value == null)
                {
                    AppendLine(indent, prefix + "null");
                    return;
                }

                if (TryFormatSimple(value, out var simple))
                {
                    AppendLine(indent, prefix + simple);
                    return;
                }

                if (depth > k_MaxDepth)
                {
                    AppendLine(indent, prefix + "<max depth>");
                    m_Current.Problems.Add($"max depth reached while printing '{path}'");
                    return;
                }

                if (value is IDictionary || value is IEnumerable)
                {
                    var count = CountItems(value);
                    var listHeader = string.IsNullOrEmpty(prefix) ? "items:" : prefix.TrimEnd();
                    AppendLine(indent, count == 0 ? prefix + "[]" : listHeader);
                    if (count > 0)
                        PrintCollectionItems(value, indent + 1, path, depth + 1);
                    return;
                }

                var type = value.GetType();
                var typeName = FormatTypeName(type);
                if (!type.IsValueType)
                {
                    if (m_Current.Printed.TryGetValue(value, out var firstPath))
                    {
                        AppendLine(indent, $"{prefix}{typeName} <same instance as {firstPath}>");
                        return;
                    }

                    m_Current.Printed.Add(value, path);
                }

                if (IsNode(value))
                    CountNode(typeName);

                var fields = GetSerializedFields(type);
                if (fields.Count == 0 && type.Assembly != m_CodeAssembly)
                {
                    AppendLine(indent, $"{prefix}{typeName}({Quote(value.ToString())})");
                    return;
                }

                var inline = new List<string>();
                var complex = new List<KeyValuePair<string, object>>();
                foreach (var field in fields)
                {
                    var fieldValue = field.GetValue(value);
                    if (TryFormatInlineField(fieldValue, field.FieldType, out var text))
                        inline.Add($"{field.Name}={text}");
                    else
                        complex.Add(new KeyValuePair<string, object>(field.Name, fieldValue));
                }

                var header = inline.Count > 0 ? $"{typeName}({string.Join(", ", inline)})" : typeName;
                AppendLine(indent, prefix + header);

                foreach (var pair in complex)
                    PrintObject(pair.Value, indent + 1, $"{pair.Key}: ", $"{path}.{pair.Key}", depth + 1);
            }

            private void PrintCollectionItems(object collection, int indent, string path, int depth)
            {
                if (collection is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var key = FormatInline(entry.Key);
                        PrintObject(entry.Value, indent, $"[{key}]: ", $"{path}[{key}]", depth + 1);
                    }

                    return;
                }

                var index = 0;
                foreach (var item in (IEnumerable) collection)
                {
                    if (item == null && IsNodeType(GetElementType(collection.GetType())))
                        m_Current.NullEntries++;
                    PrintObject(item, indent, "", $"{path}[{index}]", depth + 1);
                    index++;
                }
            }

            private void AppendLine(int indent, string text)
            {
                var sb = m_Current.Text;
                for (var i = 0; i < indent; i++)
                    sb.Append(k_Indent);
                sb.Append(text);
                sb.Append('\n');
            }

            private void CountNode(string typeName)
            {
                m_Current.NodeCount++;
                m_NodeTypes.TryGetValue(typeName, out var count);
                m_NodeTypes[typeName] = count + 1;
            }

            private static int CountItems(object collection)
            {
                if (collection is ICollection plain)
                    return plain.Count;

                var count = 0;
                foreach (var _ in (IEnumerable) collection)
                    count++;
                return count;
            }

#endregion

#region Values

            private bool TryFormatInlineField(object value, Type declaredType, out string text)
            {
                if (value == null)
                {
                    text = "null";
                    return IsSimpleType(declaredType);
                }

                if (TryFormatSimple(value, out text))
                    return true;

                if (value is IDictionary || !(value is IEnumerable enumerable))
                    return false;

                var items = enumerable.Cast<object>().ToList();
                if (items.Count == 0)
                {
                    text = "[]";
                    return IsSimpleType(GetElementType(declaredType ?? value.GetType()));
                }

                var parts = new List<string>(items.Count);
                foreach (var item in items)
                {
                    if (item == null)
                    {
                        parts.Add("null");
                        continue;
                    }

                    if (!TryFormatSimple(item, out var part))
                        return false;
                    parts.Add(part);
                }

                text = $"[{string.Join(", ", parts)}]";
                return true;
            }

            private string FormatInline(object value)
            {
                if (value == null)
                    return "null";
                return TryFormatSimple(value, out var text) ? text : FormatTypeName(value.GetType());
            }

            private bool TryFormatSimple(object value, out string text)
            {
                text = null;
                switch (value)
                {
                    case null:
                        text = "null";
                        return true;
                    case string s:
                        text = Quote(s);
                        return true;
                    case bool b:
                        text = b ? "true" : "false";
                        return true;
                    case char c:
                        text = $"'{c}'";
                        return true;
                    case Enum e:
                        text = e.ToString().Replace(", ", "|");
                        return true;
                    case float f:
                        text = f.ToString("R", CultureInfo.InvariantCulture);
                        return true;
                    case double d:
                        text = d.ToString("R", CultureInfo.InvariantCulture);
                        return true;
                    case Guid g:
                        text = g.ToString();
                        return true;
                    case Object unityObject:
                        text = FormatUnityObject(unityObject);
                        return true;
                    case Vector2 v2:
                        text = $"({F(v2.x)}, {F(v2.y)})";
                        return true;
                    case Vector3 v3:
                        text = $"({F(v3.x)}, {F(v3.y)}, {F(v3.z)})";
                        return true;
                    case Vector4 v4:
                        text = $"({F(v4.x)}, {F(v4.y)}, {F(v4.z)}, {F(v4.w)})";
                        return true;
                    case Vector2Int v2i:
                        text = $"({v2i.x}, {v2i.y})";
                        return true;
                    case Vector3Int v3i:
                        text = $"({v3i.x}, {v3i.y}, {v3i.z})";
                        return true;
                    case Quaternion q:
                        text = $"({F(q.x)}, {F(q.y)}, {F(q.z)}, {F(q.w)})";
                        return true;
                    case Color color:
                        text = $"RGBA({F(color.r)}, {F(color.g)}, {F(color.b)}, {F(color.a)})";
                        return true;
                    case Color32 color32:
                        text = $"RGBA32({color32.r}, {color32.g}, {color32.b}, {color32.a})";
                        return true;
                    case Rect rect:
                        text = $"Rect({F(rect.x)}, {F(rect.y)}, {F(rect.width)}, {F(rect.height)})";
                        return true;
                    case LayerMask mask:
                        text = mask.value.ToString(CultureInfo.InvariantCulture);
                        return true;
                    case Type type:
                        text = $"typeof({FormatTypeName(type)})";
                        return true;
                    case IFormattable formattable when value.GetType().IsPrimitive || value is decimal:
                        text = formattable.ToString(null, CultureInfo.InvariantCulture);
                        return true;
                }

                return false;
            }

            private static bool IsSimpleValue(object value)
            {
                return value is bool || value is char || value is Enum || value is Guid || value is decimal
                    || value.GetType().IsPrimitive || value is Vector2 || value is Vector3 || value is Vector4
                    || value is Vector2Int || value is Vector3Int || value is Quaternion || value is Color
                    || value is Color32 || value is Rect || value is LayerMask || value is Type;
            }

            private static bool IsSimpleType(Type type)
            {
                if (type == null)
                    return false;

                return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                    || type == typeof(Guid) || typeof(Object).IsAssignableFrom(type) || type == typeof(Vector2)
                    || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Vector2Int)
                    || type == typeof(Vector3Int) || type == typeof(Quaternion) || type == typeof(Color)
                    || type == typeof(Color32) || type == typeof(Rect) || type == typeof(LayerMask)
                    || type == typeof(Type);
            }

            private static string F(float value)
            {
                return value.ToString("R", CultureInfo.InvariantCulture);
            }

            private static string Quote(string value)
            {
                var sb = new StringBuilder(value.Length + 2);
                sb.Append('"');
                foreach (var c in value)
                {
                    switch (c)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;
                        case '\\':
                            sb.Append("\\\\");
                            break;
                        case '\n':
                            sb.Append("\\n");
                            break;
                        case '\r':
                            sb.Append("\\r");
                            break;
                        case '\t':
                            sb.Append("\\t");
                            break;
                        default:
                            sb.Append(c);
                            break;
                    }
                }

                sb.Append('"');
                return sb.ToString();
            }

            private string FormatUnityObject(Object obj)
            {
                if (ReferenceEquals(obj, null))
                    return "null";
                if (obj == null)
                    return "missing";

                var assetPath = AssetDatabase.GetAssetPath(obj);
                var typeName = FormatTypeName(obj.GetType());
                switch (obj)
                {
                    case Component component:
                    {
                        var hierarchy = GetHierarchyPath(component.transform, null);
                        var location = string.IsNullOrEmpty(assetPath) ? "scene" : assetPath;
                        return $"@{hierarchy}:{typeName} ({location})";
                    }
                    case GameObject go when !string.IsNullOrEmpty(assetPath) && !AssetDatabase.IsMainAsset(go):
                        return $"@{GetHierarchyPath(go.transform, null)}:GameObject ({assetPath})";
                    case GameObject go when string.IsNullOrEmpty(assetPath):
                        return $"@{GetHierarchyPath(go.transform, null)}:GameObject (scene)";
                }

                if (string.IsNullOrEmpty(assetPath))
                    return $"@{obj.name}:{typeName} (not an asset)";

                return AssetDatabase.IsMainAsset(obj)
                    ? $"@{obj.name} ({assetPath})"
                    : $"@{obj.name}:{typeName} ({assetPath})";
            }

#endregion

#region Types

            private static bool IsNode(object value)
            {
                return value != null && IsNodeType(value.GetType());
            }

            private static bool IsNodeType(Type type)
            {
                if (type == null)
                    return false;

                return typeof(Command).IsAssignableFrom(type)
                    || typeof(Condition).IsAssignableFrom(type)
                    || typeof(IEvaluator).IsAssignableFrom(type)
                    || typeof(IStoryTrigger).IsAssignableFrom(type)
                    || typeof(GameStepComponent).IsAssignableFrom(type);
            }

            private static Type GetElementType(Type collectionType)
            {
                if (collectionType == null)
                    return null;
                if (collectionType.IsArray)
                    return collectionType.GetElementType();

                var enumerable = collectionType.IsGenericType && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ? collectionType
                    : collectionType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                return enumerable?.GetGenericArguments()[0];
            }

            private static Type GetDictionaryValueType(Type dictionaryType)
            {
                if (dictionaryType == null)
                    return null;

                var dictionary = dictionaryType.IsGenericType && dictionaryType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                    ? dictionaryType
                    : dictionaryType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
                return dictionary?.GetGenericArguments()[1];
            }

            private List<FieldInfo> GetSerializedFields(Type type)
            {
                if (m_FieldsCache.TryGetValue(type, out var cached))
                    return cached;

                var chain = new Stack<Type>();
                for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    if (typeof(Object).IsAssignableFrom(current) && current.Assembly != m_CodeAssembly)
                        break;
                    chain.Push(current);
                }

                var result = new List<FieldInfo>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                while (chain.Count > 0)
                {
                    foreach (var field in chain.Pop().GetFields(flags))
                    {
                        if (IsSerializedField(field))
                            result.Add(field);
                    }
                }

                m_FieldsCache.Add(type, result);
                return result;
            }

            private static bool IsSerializedField(FieldInfo field)
            {
                if (field.Name.IndexOf('<') >= 0)
                    return false;

                var odinSerialize = HasAttribute(field, "OdinSerializeAttribute");
                if (field.IsDefined(typeof(NonSerializedAttribute), true) && !odinSerialize)
                    return false;

                if (field.IsPublic || odinSerialize)
                    return true;

                return field.IsDefined(typeof(SerializeField), false) || field.IsDefined(typeof(SerializeReference), false);
            }

            private static bool HasAttribute(MemberInfo member, string attributeName)
            {
                foreach (var data in member.CustomAttributes)
                {
                    if (data.AttributeType.Name == attributeName)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// Имя типа печатается без namespace. Namespace добавляется, если короткое имя неоднозначно:
            /// у типа прототипа есть тёзка в сборке Code, у внешнего типа — тёзка среди типов прототипа.
            /// </summary>
            private void CollectAmbiguousNames()
            {
                Type[] codeTypes;
                try
                {
                    codeTypes = m_CodeAssembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    codeTypes = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in codeTypes)
                {
                    var name = GetShortName(type);
                    if (!m_CodeNames.Add(name))
                        m_AmbiguousNames.Add(name);
                }
            }

            private string FormatTypeName(Type type)
            {
                if (m_TypeNameCache.TryGetValue(type, out var cached))
                    return cached;

                string result;
                if (type.IsArray)
                {
                    result = FormatTypeName(type.GetElementType()) + "[]";
                }
                else
                {
                    var name = GetShortName(type);
                    var isAmbiguous = type.Assembly == m_CodeAssembly
                        ? m_AmbiguousNames.Contains(name)
                        : m_CodeNames.Contains(name);
                    if (isAmbiguous && !string.IsNullOrEmpty(type.Namespace))
                        name = $"{type.Namespace}.{name}";

                    result = type.IsGenericType
                        ? $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatTypeName))}>"
                        : name;
                }

                m_TypeNameCache.Add(type, result);
                return result;
            }

            private static string GetShortName(Type type)
            {
                var name = type.Name;
                var tick = name.IndexOf('`');
                if (tick >= 0)
                    name = name.Substring(0, tick);

                return type.IsNested && !type.IsGenericParameter
                    ? $"{GetShortName(type.DeclaringType)}.{name}"
                    : name;
            }

#endregion

#region Hierarchy

            private static string GetHierarchyPath(Transform transform, GameObject[] sceneRoots)
            {
                var parts = new List<string>();
                for (var current = transform; current != null; current = current.parent)
                    parts.Add(GetUniqueName(current, sceneRoots));
                parts.Reverse();
                return string.Join("/", parts);
            }

            private static string GetUniqueName(Transform transform, GameObject[] sceneRoots)
            {
                var name = transform.name;
                var siblings = new List<Transform>();
                if (transform.parent != null)
                {
                    foreach (Transform child in transform.parent)
                        siblings.Add(child);
                }
                else if (sceneRoots != null)
                {
                    siblings.AddRange(sceneRoots.Select(go => go.transform));
                }

                var sameName = siblings.Where(t => t.name == name).ToList();
                if (sameName.Count <= 1)
                    return name;
                return $"{name}[{sameName.IndexOf(transform)}]";
            }

#endregion

#region Output

            private void WriteSource(SourceResult source)
            {
                var guid = AssetDatabase.AssetPathToGUID(source.AssetPath);
                var header = new StringBuilder();
                header.Append($"# source: {source.AssetPath} (guid {guid})\n");
                foreach (var problem in source.Problems)
                    header.Append($"# problem: {problem}\n");

                var relative = source.AssetPath.StartsWith(k_ContentRoot + "/", StringComparison.Ordinal)
                    ? source.AssetPath.Substring(k_ContentRoot.Length + 1)
                    : source.AssetPath;
                var outputRelative = Path.ChangeExtension(relative, ".txt");
                if (!m_UsedOutputPaths.Add(outputRelative))
                {
                    outputRelative = relative + ".txt";
                    m_UsedOutputPaths.Add(outputRelative);
                }

                source.OutputPath = outputRelative.Replace('\\', '/');
                var fullPath = Path.Combine(m_OutputRoot, outputRelative);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                File.WriteAllText(fullPath, header.ToString() + source.Text, new UTF8Encoding(false));
            }

            private void WriteSummary()
            {
                var exported = m_Results.Where(r => r.BlockCount > 0).ToList();
                var problems = m_Results.Where(r => r.Problems.Count > 0).ToList();
                var totalNodes = exported.Sum(r => r.NodeCount);
                var totalBlocks = exported.Sum(r => r.BlockCount);
                var totalNulls = exported.Sum(r => r.NullEntries);

                var sb = new StringBuilder();
                sb.Append("# Legacy script export (Odin data -> text)\n");
                sb.Append($"# generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                sb.Append($"# scanned: assets {m_AssetsScanned}, prefabs {m_PrefabsScanned}, scenes {m_ScenesScanned}\n");
                sb.Append($"# exported files: {exported.Count}, script fields: {totalBlocks}, nodes: {totalNodes}, null node entries: {totalNulls}\n");
                sb.Append($"# node = Command | Condition | IEvaluator | IStoryTrigger | GameStepComponent\n");

                sb.Append("\n## Exported sources\n\n");
                foreach (var result in exported)
                {
                    var nulls = result.NullEntries > 0 ? $", null entries {result.NullEntries}" : "";
                    sb.Append($"{result.AssetPath} -> {result.OutputPath}: fields {result.BlockCount}, nodes {result.NodeCount}{nulls}\n");
                }

                sb.Append("\n## Problems\n\n");
                if (problems.Count == 0 && m_GlobalProblems.Count == 0)
                    sb.Append("none\n");
                foreach (var problem in m_GlobalProblems)
                    sb.Append($"(global): {problem}\n");
                foreach (var result in problems)
                {
                    foreach (var problem in result.Problems)
                        sb.Append($"{result.AssetPath}: {problem}\n");
                }

                sb.Append("\n## Node types\n\n");
                foreach (var pair in m_NodeTypes.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
                    sb.Append($"{pair.Value,6}  {pair.Key}\n");

                File.WriteAllText(Path.Combine(m_OutputRoot, "_summary.txt"), sb.ToString(), new UTF8Encoding(false));
                Summary = $"files {exported.Count}, fields {totalBlocks}, nodes {totalNodes}, sources with problems {problems.Count}, output {m_OutputRoot}";
            }

#endregion

#region Logs

            private void HandleLog(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Log)
                    return;
                if (condition.StartsWith("[ScriptExporter]", StringComparison.Ordinal))
                    return;

                var message = condition.Replace('\n', ' ').Replace('\r', ' ');
                if (message.Length > 400)
                    message = message.Substring(0, 400) + "...";
                var entry = $"log {type}: {message}";

                if (m_Current != null)
                    m_Current.Problems.Add(entry);
                else
                    m_GlobalProblems.Add(entry);
            }

#endregion
        }

#endregion

#region Data

        private readonly struct RootObject
        {
            public readonly Object Target;
            public readonly string Prefix;

            public RootObject(Object target, string prefix)
            {
                Target = target;
                Prefix = prefix;
            }
        }

        private sealed class SourceResult
        {
            public readonly string AssetPath;
            public readonly StringBuilder Text = new StringBuilder();
            public readonly List<string> Problems = new List<string>();
            public readonly Dictionary<object, string> Printed = new Dictionary<object, string>(ReferenceComparer.Instance);
            public int NodeCount;
            public int BlockCount;
            public int NullEntries;
            public string OutputPath;

            public SourceResult(string assetPath)
            {
                AssetPath = assetPath;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();

            public new bool Equals(object x, object y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }

#endregion
    }
}
