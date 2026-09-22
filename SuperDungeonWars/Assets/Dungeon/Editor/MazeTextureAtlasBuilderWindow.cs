using System.IO;
using UnityEditor;
using UnityEngine;

public class MazeTextureAtlasBuilderWindow : EditorWindow
{
    private const int s_gridCount = 4;
    private const int s_tileSize = 1024;
    private const int s_atlasSize = s_gridCount * s_tileSize;
    private const int s_requiredTextureCount = s_gridCount * s_gridCount;

    [SerializeField]
    private Texture2D[] m_sourceTextures =
        new Texture2D[s_requiredTextureCount];

    [MenuItem("Tools/Maze Explore/Build 4x4 Texture Atlas")]
    private static void Open()
    {
        MazeTextureAtlasBuilderWindow window =
            GetWindow<MazeTextureAtlasBuilderWindow>();

        window.titleContent =
            new GUIContent("Maze Atlas Builder");

        window.minSize = new Vector2(700.0f, 560.0f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "4×4 テクスチャアトラス出力",
            EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "1024×1024 のテクスチャを 16 枚指定し、"
            + "4096×4096 の PNG アトラスとして出力します。\n"
            + "スロットの左下が Atlas 座標 (0, 0) です。",
            MessageType.Info);

        DrawTextureSlots();

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(
            !CanBuildAtlas(out string validationMessage)))
        {
            if (GUILayout.Button(
                "4096×4096 PNG を出力",
                GUILayout.Height(36.0f)))
            {
                BuildAtlas();
            }
        }

        if (!CanBuildAtlas(out string message))
        {
            EditorGUILayout.HelpBox(
                message,
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "推奨する初期配置は、下段から順に「通路床・一般部屋床・中央大広間床・予備」、"
            + "その上段に「壁・壁上面・予備・予備」です。"
            + "残り 2 段は今後のテーマ差分や装飾用に確保できます。",
            MessageType.None);
    }

    private void DrawTextureSlots()
    {
        EditorGUILayout.LabelField(
            "テクスチャ配置",
            EditorStyles.boldLabel);

        for (int rowFromTop = s_gridCount - 1;
             rowFromTop >= 0;
             rowFromTop--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int column = 0;
                 column < s_gridCount;
                 column++)
            {
                int index = GetIndex(column, rowFromTop);

                EditorGUILayout.BeginVertical(
                    GUI.skin.box,
                    GUILayout.Width(165.0f));

                EditorGUILayout.LabelField(
                    GetSlotLabel(column, rowFromTop),
                    EditorStyles.miniBoldLabel);

                m_sourceTextures[index] =
                    (Texture2D)EditorGUILayout.ObjectField(
                        m_sourceTextures[index],
                        typeof(Texture2D),
                        false,
                        GUILayout.Width(145.0f),
                        GUILayout.Height(145.0f));

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private bool CanBuildAtlas(out string message)
    {
        for (int index = 0;
             index < s_requiredTextureCount;
             index++)
        {
            Texture2D texture = m_sourceTextures[index];

            // 未設定スロットは黒で出力するため許可する。
            if (texture == null)
            {
                continue;
            }

            if (texture.width != s_tileSize
                || texture.height != s_tileSize)
            {
                message =
                    $"スロット {index} の「{texture.name}」が "
                    + $"{s_tileSize}×{s_tileSize} ではありません。"
                    + $"現在は {texture.width}×{texture.height} です。";

                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private void BuildAtlas()
    {
        string outputPath =
            EditorUtility.SaveFilePanelInProject(
                "4×4 テクスチャアトラスを保存",
                "MazeSurfaceAtlas_4x4",
                "png",
                "出力先を Assets フォルダ内に指定してください。");

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }

        RenderTexture previousActiveRenderTexture =
            RenderTexture.active;

        RenderTexture atlasRenderTexture = null;
        Texture2D atlasTexture = null;
        bool isPixelMatrixPushed = false;

        try
        {
            atlasRenderTexture =
                RenderTexture.GetTemporary(
                    s_atlasSize,
                    s_atlasSize,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Default);

            RenderTexture.active = atlasRenderTexture;

            // 未設定スロットはこの黒背景をそのまま使う。
            GL.Clear(
                true,
                true,
                Color.black);

            GL.PushMatrix();
            isPixelMatrixPushed = true;

            GL.LoadPixelMatrix(
                0.0f,
                s_atlasSize,
                0.0f,
                s_atlasSize);

            for (int row = 0;
                 row < s_gridCount;
                 row++)
            {
                for (int column = 0;
                     column < s_gridCount;
                     column++)
                {
                    int index = GetIndex(column, row);

                    Texture2D sourceTexture =
                        m_sourceTextures[index];

                    // 未設定箇所は黒地を維持する。
                    if (sourceTexture == null)
                    {
                        continue;
                    }

                    Rect destinationRect = new Rect(
                        column * s_tileSize,
                        row * s_tileSize,
                        s_tileSize,
                        s_tileSize);

                    Graphics.DrawTexture(
                        destinationRect,
                        sourceTexture);
                }
            }

            GL.PopMatrix();
            isPixelMatrixPushed = false;

            RenderTexture.active = atlasRenderTexture;

            atlasTexture = new Texture2D(
                s_atlasSize,
                s_atlasSize,
                TextureFormat.RGBA32,
                false,
                false);

            atlasTexture.ReadPixels(
                new Rect(
                    0.0f,
                    0.0f,
                    s_atlasSize,
                    s_atlasSize),
                0,
                0);

            atlasTexture.Apply(
                false,
                false);

            byte[] pngBytes =
                atlasTexture.EncodeToPNG();

            File.WriteAllBytes(
                outputPath,
                pngBytes);

            AssetDatabase.ImportAsset(
                outputPath,
                ImportAssetOptions.ForceUpdate);

            ApplyAtlasImportSettings(outputPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Texture2D outputTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    outputPath);

            Selection.activeObject = outputTexture;

            EditorGUIUtility.PingObject(outputTexture);

            Debug.Log(
                $"[{nameof(MazeTextureAtlasBuilderWindow)}] "
                + $"4096×4096 アトラスを出力しました: {outputPath}");
        }
        finally
        {
            if (isPixelMatrixPushed)
            {
                GL.PopMatrix();
            }

            RenderTexture.active =
                previousActiveRenderTexture;

            if (atlasRenderTexture != null)
            {
                RenderTexture.ReleaseTemporary(
                    atlasRenderTexture);
            }

            if (atlasTexture != null)
            {
                DestroyImmediate(atlasTexture);
            }
        }
    }

    private void ApplyAtlasImportSettings(
        string assetPath)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(assetPath)
            as TextureImporter;

        if (importer == null)
        {
            return;
        }

        importer.textureType =
            TextureImporterType.Default;

        importer.wrapMode =
            TextureWrapMode.Clamp;

        importer.filterMode =
            FilterMode.Bilinear;

        importer.mipmapEnabled = true;

        importer.sRGBTexture = true;

        importer.alphaSource =
            TextureImporterAlphaSource.None;

        importer.textureCompression =
            TextureImporterCompression.Compressed;

        importer.SaveAndReimport();
    }

    private int GetIndex(
        int column,
        int rowFromBottom)
    {
        return rowFromBottom * s_gridCount
            + column;
    }

    private string GetSlotLabel(
        int column,
        int rowFromBottom)
    {
        int index = GetIndex(
            column,
            rowFromBottom);

        return $"({column}, {rowFromBottom}) / Slot {index}";
    }
}