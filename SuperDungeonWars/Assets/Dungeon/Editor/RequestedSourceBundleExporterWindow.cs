using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class RequestedSourceBundleExporterWindow : EditorWindow
{
    private const string MenuItemPath = "Tools/AI/Export Requested Source Bundle";
    private const string WindowTitle = "Source Bundle Exporter";

    // プロジェクトルートから見た出力先です。
    private const string OutputRelativePath = "AIContext/RequestedSourceBundle.txt";

    private string requestedFileNamesText = string.Empty;
    private Vector2 inputScrollPosition;

    [MenuItem(MenuItemPath)]
    private static void Open()
    {
        var window = GetWindow<RequestedSourceBundleExporterWindow>(
            true,
            WindowTitle);

        window.minSize = new Vector2(620.0f, 420.0f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "出力する C# ソースファイル名",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "ファイル名を1行ずつ入力してください。\n"
            + "拡張子 .cs は省略できます。\n\n"
            + "例:\n"
            + "ComPartyBase.cs\n"
            + "ComCharacterBase\n"
            + "BattleGameRuleData.cs\n\n"
            + "検索対象は Assets 配下の *.cs ファイルです。\n"
            + "同名ファイルが複数見つかった場合、または見つからない場合は出力しません。",
            MessageType.Info);

        inputScrollPosition = EditorGUILayout.BeginScrollView(inputScrollPosition);

        requestedFileNamesText = EditorGUILayout.TextArea(
            requestedFileNamesText,
            GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        var outputPath = GetOutputPath();

        EditorGUILayout.LabelField(
            "出力先",
            GetProjectRelativePath(outputPath),
            EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("出力", GUILayout.Height(30.0f)))
        {
            Export();
        }

        if (GUILayout.Button("入力をクリア", GUILayout.Height(30.0f)))
        {
            requestedFileNamesText = string.Empty;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void Export()
    {
        var requestedFileNames = GetRequestedFileNames(requestedFileNamesText);

        if (requestedFileNames.Count == 0)
        {
            EditorUtility.DisplayDialog(
                WindowTitle,
                "出力するファイル名が入力されていません。",
                "OK");

            return;
        }

        var sourceFilesByName = CollectSourceFilesByName();
        var resolvedFiles = new List<string>();
        var missingFileNames = new List<string>();
        var duplicatedFiles = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var requestedFileName in requestedFileNames)
        {
            List<string> matchedFiles;

            if (!sourceFilesByName.TryGetValue(
                    requestedFileName,
                    out matchedFiles))
            {
                missingFileNames.Add(requestedFileName);
                continue;
            }

            if (matchedFiles.Count != 1)
            {
                duplicatedFiles.Add(requestedFileName, matchedFiles);
                continue;
            }

            resolvedFiles.Add(matchedFiles[0]);
        }

        if (missingFileNames.Count > 0 || duplicatedFiles.Count > 0)
        {
            var errorMessage = BuildValidationErrorMessage(
                missingFileNames,
                duplicatedFiles);

            Debug.LogWarning(errorMessage);

            EditorUtility.DisplayDialog(
                WindowTitle,
                "出力を中止しました。\n\n"
                + "見つからないファイル、または同名ファイルが複数存在します。\n"
                + "詳細は Console を確認してください。",
                "OK");

            return;
        }

        var output = BuildOutput(resolvedFiles);
        var outputPath = GetOutputPath();
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.WriteAllText(outputPath, output, new UTF8Encoding(false));
        AssetDatabase.Refresh();

        Debug.Log("Requested source bundle exported: " + outputPath);

        EditorUtility.DisplayDialog(
            WindowTitle,
            "出力しました。\n\n"
            + GetProjectRelativePath(outputPath),
            "OK");
    }

    private static List<string> GetRequestedFileNames(string input)
    {
        var results = new List<string>();
        var addedFileNames = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var lines = (input ?? string.Empty).Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None);

        foreach (var line in lines)
        {
            var fileName = NormalizeRequestedFileName(line);

            if (string.IsNullOrEmpty(fileName))
            {
                continue;
            }

            // 同じファイル名が複数行に書かれていても、1回だけ出力します。
            if (addedFileNames.Add(fileName))
            {
                results.Add(fileName);
            }
        }

        return results;
    }

    private static string NormalizeRequestedFileName(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return string.Empty;
        }

        // パス付きで貼り付けられた場合でもファイル名だけを使います。
        var fileName = Path.GetFileName(trimmed);

        // .cs が省略されている場合だけ補完します。
        // 例: ComCharacterBase.Internal → ComCharacterBase.Internal.cs
        if (!fileName.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".cs";
        }

        return fileName;
    }

    private static Dictionary<string, List<string>> CollectSourceFilesByName()
    {
        var results = new Dictionary<string, List<string>>(
            StringComparer.OrdinalIgnoreCase);

        var sourceFiles = Directory.GetFiles(
            Application.dataPath,
            "*.cs",
            SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var fileName = Path.GetFileName(sourceFile);

            List<string> files;

            if (!results.TryGetValue(fileName, out files))
            {
                files = new List<string>();
                results.Add(fileName, files);
            }

            files.Add(NormalizePath(sourceFile));
        }

        return results;
    }

    private static string BuildValidationErrorMessage(
        List<string> missingFileNames,
        Dictionary<string, List<string>> duplicatedFiles)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Requested source bundle export was cancelled.");

        if (missingFileNames.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[Not Found]");

            foreach (var fileName in missingFileNames)
            {
                sb.AppendLine("- " + fileName);
            }
        }

        if (duplicatedFiles.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("[Duplicated File Name]");

            foreach (var pair in duplicatedFiles.OrderBy(
                         x => x.Key,
                         StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("- " + pair.Key);

                foreach (var path in pair.Value.OrderBy(
                             x => x,
                             StringComparer.OrdinalIgnoreCase))
                {
                    sb.AppendLine("  - " + GetProjectRelativePath(path));
                }
            }
        }

        return sb.ToString();
    }

    private static string BuildOutput(List<string> sourceFiles)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// ============================================================================");
        sb.AppendLine("// Requested Source Bundle");
        sb.AppendLine("// GeneratedAt: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("// ============================================================================");
        sb.AppendLine();

        // 入力欄に書かれた順番を維持して出力します。
        foreach (var sourceFile in sourceFiles)
        {
            var projectRelativePath = GetProjectRelativePath(sourceFile);
            var sourceText = File.ReadAllText(sourceFile, Encoding.UTF8);

            sb.AppendLine("// ============================================================================");
            sb.AppendLine("// BEGIN SOURCE: " + projectRelativePath);
            sb.AppendLine("// ============================================================================");
            sb.AppendLine();

            sb.AppendLine(sourceText.TrimEnd());

            sb.AppendLine();
            sb.AppendLine("// ============================================================================");
            sb.AppendLine("// END SOURCE: " + projectRelativePath);
            sb.AppendLine("// ============================================================================");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GetOutputPath()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;

        return NormalizePath(
            Path.Combine(projectRoot, OutputRelativePath));
    }

    private static string GetProjectRelativePath(string path)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var normalizedProjectRoot = NormalizePath(projectRoot).TrimEnd('/');
        var normalizedPath = NormalizePath(path);

        if (normalizedPath.StartsWith(
                normalizedProjectRoot + "/",
                StringComparison.OrdinalIgnoreCase))
        {
            return normalizedPath.Substring(normalizedProjectRoot.Length + 1);
        }

        return normalizedPath;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace("\\", "/");
    }
}