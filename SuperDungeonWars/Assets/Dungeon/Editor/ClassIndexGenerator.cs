using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class ClassIndexGenerator
{
    private static readonly string[] ScanRoots =
    {
        "Assets/Dungeon/Program",
        "Assets/Dungeon/ParticipantApi",
        "Assets/Dungeon/Participants",
        "Assets/Dungeon/Editor",
    };

    private static readonly string[] ExcludePathFragments =
    {
        "/DontShareAssets/",
        "/TutorialInfo/",
        "/Plugins/",
        "/Editor Default Resources/",
        "/Editor/",
    };

    private static readonly string OutputRelativePath = "AIContext/ClassIndex.md";

    private static readonly HashSet<string> UnityEventMethodNames = new HashSet<string>
    {
        "Awake", "OnEnable", "Start", "Update", "LateUpdate", "FixedUpdate",
        "OnDisable", "OnDestroy", "Reset", "OnValidate",
        "OnDrawGizmos", "OnDrawGizmosSelected",
        "OnTriggerEnter", "OnTriggerExit", "OnTriggerStay",
        "OnCollisionEnter", "OnCollisionExit", "OnCollisionStay",
    };

    [MenuItem("Tools/AI/Generate Rich ClassIndex.md")]
    public static void Generate()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var outputPath = Path.Combine(projectRoot, OutputRelativePath);

        var files = CollectTargetFiles();
        var allTypeNames = CollectAllTypeNames(files);
        var entries = new List<TypeEntry>();

        foreach (var file in files)
        {
            var source = File.ReadAllText(file, Encoding.UTF8);
            var stripped = StripComments(source);
            var namespaceName = ExtractNamespace(stripped);

            var typeMatches = Regex.Matches(
                stripped,
                @"^\s*(?:(?:public|internal|protected|private|abstract|sealed|static|partial)\s+)*(class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:<[^>{}]*>)?\s*(?:\:\s*([^{]+))?",
                RegexOptions.Multiline);

            foreach (Match match in typeMatches)
            {
                var typeKind = match.Groups[1].Value.Trim();
                var typeName = match.Groups[2].Value.Trim();
                var baseTypes = match.Groups[3].Success ? NormalizeWhitespace(match.Groups[3].Value.Trim()) : "-";

                var bodyStart = stripped.IndexOf('{', match.Index + match.Length);
                var bodyEnd = bodyStart >= 0 ? FindMatchingBrace(stripped, bodyStart) : -1;
                var body = (bodyStart >= 0 && bodyEnd > bodyStart)
                    ? stripped.Substring(bodyStart + 1, bodyEnd - bodyStart - 1)
                    : string.Empty;

                var fields = ExtractFields(body);
                var properties = ExtractProperties(body);
                var events = ExtractEvents(body);
                var delegates = ExtractDelegates(body);
                var methods = ExtractMethods(body, typeName);

                var unityEvents = methods
                    .Where(x => UnityEventMethodNames.Contains(x.Name))
                    .Select(x => x.Signature)
                    .Distinct()
                    .ToList();

                var publicMethods = methods
                    .Where(x => x.Accessibility == "public" && !UnityEventMethodNames.Contains(x.Name))
                    .Select(x => x.Signature)
                    .Distinct()
                    .ToList();

                var otherMethods = methods
                    .Where(x => x.Accessibility != "public" && !UnityEventMethodNames.Contains(x.Name))
                    .Select(x => x.Signature)
                    .Distinct()
                    .ToList();

                var signatureReferences = ExtractReferencedTypesFromSignatures(
                    allTypeNames,
                    typeName,
                    fields,
                    properties,
                    events,
                    delegates,
                    methods.Select(x => x.Signature).ToList());

                var bodyReferences = ExtractReferencedTypesFromBody(body, allTypeNames, typeName, signatureReferences);

                entries.Add(new TypeEntry
                {
                    Folder = GetFolderGroup(file),
                    FilePath = NormalizePath(file),
                    Namespace = namespaceName,
                    TypeKind = typeKind,
                    TypeName = typeName,
                    BaseTypes = baseTypes,
                    IsMonoBehaviour = baseTypes.Contains("MonoBehaviour"),
                    Fields = fields,
                    Properties = properties,
                    Events = events,
                    Delegates = delegates,
                    UnityEvents = unityEvents,
                    PublicMethods = publicMethods,
                    OtherMethods = otherMethods,
                    SignatureReferencedTypes = signatureReferences,
                    BodyReferencedTypes = bodyReferences,
                });
            }
        }

        var ordered = entries
            .OrderBy(x => x.IsMonoBehaviour ? 0 : 1)
            .ThenBy(x => x.Folder, StringComparer.Ordinal)
            .ThenBy(x => x.TypeName, StringComparer.Ordinal)
            .ToList();

        var markdown = BuildMarkdown(ordered);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        File.WriteAllText(outputPath, markdown, new UTF8Encoding(false));
        AssetDatabase.Refresh();

        Debug.Log("Rich ClassIndex.md generated: " + outputPath);
    }

    private static List<string> CollectTargetFiles()
    {
        var files = new List<string>();

        foreach (var root in ScanRoots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            var found = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            foreach (var file in found)
            {
                var normalized = NormalizePath(file);

                if (normalized.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (IsExcludedPath(normalized))
                {
                    continue;
                }

                files.Add(normalized);
            }
        }

        return files.Distinct().ToList();
    }

    private static bool IsExcludedPath(string path)
    {
        foreach (var fragment in ExcludePathFragments)
        {
            if (path.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static HashSet<string> CollectAllTypeNames(List<string> files)
    {
        var results = new HashSet<string>();

        foreach (var file in files)
        {
            var text = File.ReadAllText(file, Encoding.UTF8);
            var stripped = StripComments(text);

            var typeMatches = Regex.Matches(
                stripped,
                @"^\s*(?:(?:public|internal|protected|private|abstract|sealed|static|partial)\s+)*(class|struct|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
                RegexOptions.Multiline);

            foreach (Match match in typeMatches)
            {
                results.Add(match.Groups[2].Value.Trim());
            }
        }

        return results;
    }

    private static string BuildMarkdown(List<TypeEntry> entries)
    {
        var sb = new StringBuilder();

        var monoBehaviourCount = entries.Count(x => x.IsMonoBehaviour);
        var plainTypeCount = entries.Count - monoBehaviourCount;

        sb.AppendLine("# Class Index");
        sb.AppendLine();
        sb.AppendLine("- GeneratedAt: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("- ScanRoots: " + string.Join(", ", ScanRoots));
        sb.AppendLine("- TotalCount: " + entries.Count);
        sb.AppendLine("- MonoBehaviourCount: " + monoBehaviourCount);
        sb.AppendLine("- NonMonoBehaviourCount: " + plainTypeCount);
        sb.AppendLine();

        AppendSection(sb, "## MonoBehaviours", entries.Where(x => x.IsMonoBehaviour).ToList());
        AppendSection(sb, "## Non-MonoBehaviours", entries.Where(x => !x.IsMonoBehaviour).ToList());

        return sb.ToString();
    }

    private static void AppendSection(StringBuilder sb, string title, List<TypeEntry> entries)
    {
        sb.AppendLine(title);
        sb.AppendLine();

        if (entries.Count == 0)
        {
            sb.AppendLine("- なし");
            sb.AppendLine();
            return;
        }

        string currentFolder = null;
        foreach (var entry in entries)
        {
            if (currentFolder != entry.Folder)
            {
                currentFolder = entry.Folder;
                sb.AppendLine("### Folder: " + currentFolder);
                sb.AppendLine();
            }

            sb.AppendLine("#### " + entry.TypeName);
            sb.AppendLine();
            sb.AppendLine("- Kind: " + entry.TypeKind);
            sb.AppendLine("- Namespace: " + Safe(entry.Namespace));
            sb.AppendLine("- BaseTypes: " + Safe(entry.BaseTypes));
            sb.AppendLine("- Path: `" + entry.FilePath + "`");
            AppendList(sb, "Fields", entry.Fields, 16);
            AppendList(sb, "Properties", entry.Properties, 16);
            AppendList(sb, "Events", entry.Events, 12);
            AppendList(sb, "Delegates", entry.Delegates, 12);
            AppendList(sb, "UnityEvents", entry.UnityEvents, 16);
            AppendList(sb, "PublicMethods", entry.PublicMethods, 16);
            AppendList(sb, "OtherMethods", entry.OtherMethods, 16);
            AppendList(sb, "SignatureReferencedTypes", entry.SignatureReferencedTypes, 20);
            AppendList(sb, "BodyReferencedTypes", entry.BodyReferencedTypes, 20);
            sb.AppendLine();
        }
    }

    private static void AppendList(StringBuilder sb, string label, List<string> values, int maxCount)
    {
        if (values == null || values.Count == 0)
        {
            sb.AppendLine("- " + label + ": なし");
            return;
        }

        sb.AppendLine("- " + label + ":");
        for (int i = 0; i < values.Count && i < maxCount; i++)
        {
            sb.AppendLine("  - `" + values[i] + "`");
        }

        if (values.Count > maxCount)
        {
            sb.AppendLine("  - `... (" + (values.Count - maxCount) + " more)`");
        }
    }

    private static string ExtractNamespace(string text)
    {
        var match = Regex.Match(text, @"namespace\s+([A-Za-z_][A-Za-z0-9_\.]*)");
        return match.Success ? match.Groups[1].Value.Trim() : "(global)";
    }

    private static List<string> ExtractFields(string body)
    {
        var results = new List<string>();

        var serializeFieldMatches = Regex.Matches(
            body,
            @"\[SerializeField\]\s*(?:\r?\n\s*\[[^\]]+\]\s*)*(?:\r?\n\s*)*(public|private|protected|internal)?\s*(?:static\s+|readonly\s+|const\s+)*([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*[;=]",
            RegexOptions.Multiline);

        foreach (Match match in serializeFieldMatches)
        {
            var access = string.IsNullOrEmpty(match.Groups[1].Value) ? "private" : match.Groups[1].Value.Trim();
            var type = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var name = match.Groups[3].Value.Trim();
            results.Add("[SerializeField] " + access + " " + type + " " + name);
        }

        var fieldMatches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+(?:static\s+|readonly\s+|const\s+)*([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)",
            RegexOptions.Multiline);

        foreach (Match match in fieldMatches)
        {
            var text = match.Groups[1].Value.Trim() + " "
                + NormalizeWhitespace(match.Groups[2].Value.Trim()) + " "
                + match.Groups[3].Value.Trim();

            if (!results.Contains(text))
            {
                results.Add(text);
            }
        }

        return results.Distinct().ToList();
    }

    private static List<string> ExtractProperties(string body)
    {
        var results = new List<string>();

        var propertyMatches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|sealed\s+|new\s+)*([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\{\s*[^{}]*\bget\s*;[^{}]*\}",
            RegexOptions.Multiline);

        foreach (Match match in propertyMatches)
        {
            var access = match.Groups[1].Value.Trim();
            var type = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var name = match.Groups[3].Value.Trim();
            results.Add(access + " " + type + " " + name);
        }

        return results.Distinct().ToList();
    }

    private static List<string> ExtractEvents(string body)
    {
        var results = new List<string>();

        var eventMatches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+event\s+([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*;",
            RegexOptions.Multiline);

        foreach (Match match in eventMatches)
        {
            var access = match.Groups[1].Value.Trim();
            var type = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var name = match.Groups[3].Value.Trim();
            results.Add(access + " event " + type + " " + name);
        }

        var unityEventFieldMatches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*UnityEvent[A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)",
            RegexOptions.Multiline);

        foreach (Match match in unityEventFieldMatches)
        {
            var access = match.Groups[1].Value.Trim();
            var type = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var name = match.Groups[3].Value.Trim();
            results.Add(access + " " + type + " " + name);
        }

        return results.Distinct().ToList();
    }

    private static List<string> ExtractDelegates(string body)
    {
        var results = new List<string>();

        var matches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+delegate\s+([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)",
            RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            var access = match.Groups[1].Value.Trim();
            var returnType = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var name = match.Groups[3].Value.Trim();
            var parameters = NormalizeWhitespace(match.Groups[4].Value.Trim());
            results.Add(access + " delegate " + returnType + " " + name + "(" + parameters + ")");
        }

        return results.Distinct().ToList();
    }

    private static List<MethodEntry> ExtractMethods(string body, string typeName)
    {
        var results = new List<MethodEntry>();

        var matches = Regex.Matches(
            body,
            @"^\s*(public|private|protected|internal)\s+(?:static\s+|virtual\s+|override\s+|abstract\s+|async\s+|sealed\s+|new\s+|extern\s+)*([A-Za-z_][A-Za-z0-9_<>\[\],\.\?]*)\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(([^)]*)\)",
            RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            var access = match.Groups[1].Value.Trim();
            var returnType = NormalizeWhitespace(match.Groups[2].Value.Trim());
            var methodName = match.Groups[3].Value.Trim();
            var parameters = NormalizeWhitespace(match.Groups[4].Value.Trim());

            if (methodName == typeName)
            {
                continue;
            }

            var signature = returnType + " " + methodName + "(" + parameters + ")";
            results.Add(new MethodEntry
            {
                Accessibility = access,
                Name = methodName,
                Signature = signature
            });
        }

        return results;
    }

    private static List<string> ExtractReferencedTypesFromSignatures(
        HashSet<string> allTypeNames,
        string selfTypeName,
        List<string> fields,
        List<string> properties,
        List<string> events,
        List<string> delegates,
        List<string> methodSignatures)
    {
        var result = new HashSet<string>();
        var sourceTexts = new List<string>();

        sourceTexts.AddRange(fields);
        sourceTexts.AddRange(properties);
        sourceTexts.AddRange(events);
        sourceTexts.AddRange(delegates);
        sourceTexts.AddRange(methodSignatures);

        foreach (var text in sourceTexts)
        {
            AddReferencedTypes(text, allTypeNames, selfTypeName, result);
        }

        return result.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private static List<string> ExtractReferencedTypesFromBody(
        string body,
        HashSet<string> allTypeNames,
        string selfTypeName,
        List<string> alreadyIncluded)
    {
        var result = new HashSet<string>();
        var already = new HashSet<string>(alreadyIncluded);

        foreach (Match match in Regex.Matches(body, @"\b[A-Z][A-Za-z0-9_]*\b"))
        {
            var token = match.Value;
            if (token == selfTypeName)
            {
                continue;
            }

            if (!allTypeNames.Contains(token))
            {
                continue;
            }

            if (already.Contains(token))
            {
                continue;
            }

            result.Add(token);
        }

        return result.OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    private static void AddReferencedTypes(
        string text,
        HashSet<string> allTypeNames,
        string selfTypeName,
        HashSet<string> result)
    {
        foreach (Match match in Regex.Matches(text, @"\b[A-Z][A-Za-z0-9_]*\b"))
        {
            var token = match.Value;
            if (token == selfTypeName)
            {
                continue;
            }

            if (allTypeNames.Contains(token))
            {
                result.Add(token);
            }
        }
    }

    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        int depth = 0;
        for (int i = openBraceIndex; i < text.Length; i++)
        {
            if (text[i] == '{')
            {
                depth++;
            }
            else if (text[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static string StripComments(string text)
    {
        text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
        text = Regex.Replace(text, @"//.*?$", "", RegexOptions.Multiline);
        return text;
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }

    private static string GetFolderGroup(string filePath)
    {
        var path = NormalizePath(filePath);
        return Path.GetDirectoryName(path).Replace("\\", "/");
    }

    private static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private sealed class MethodEntry
    {
        public string Accessibility;
        public string Name;
        public string Signature;
    }

    private sealed class TypeEntry
    {
        public string Folder;
        public string FilePath;
        public string Namespace;
        public string TypeKind;
        public string TypeName;
        public string BaseTypes;
        public bool IsMonoBehaviour;
        public List<string> Fields;
        public List<string> Properties;
        public List<string> Events;
        public List<string> Delegates;
        public List<string> UnityEvents;
        public List<string> PublicMethods;
        public List<string> OtherMethods;
        public List<string> SignatureReferencedTypes;
        public List<string> BodyReferencedTypes;
    }
}