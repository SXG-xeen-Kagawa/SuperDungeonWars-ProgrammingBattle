#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ParticipantPrefabEditSceneGui
{
    private const string ParticipantRootPath = "Assets/Participants";
    private const string MemberPrefabsFieldName = "m_memberPrefabs";
    private const string TeamIconSpriteFieldName = "m_teamIconSprite";

    private const string AccessoryCatalogAssetPath =
        "Assets/Dungeon/GameAssets/Models/Accessories/"
        + "ParticipantAccessoryCatalog.asset";

    private const string CharacterBasePrefabAssetPath =
        "Assets/Dungeon/Editor/ParticipantCreation/Prefab/"
        + "ParticipantCharacterBase.prefab";

    private const string MannequinRootName = "Y Bot";

    private const float PanelWidth = 320.0f;
    private const float PartyPanelHeight = 370.0f;
    private const float CharacterPanelHeight = 340.0f;
    private const float CharacterPanelExpandedHeight = 490.0f;
    private const float PanelHeaderHeight = 30.0f;
    private const float PanelPadding = 9.0f;

    private const string PanelPositionXEditorPrefsKey =
        "ParticipantPrefabEditSceneGui.PanelPositionX.V2";

    private const string PanelPositionYEditorPrefsKey =
        "ParticipantPrefabEditSceneGui.PanelPositionY.V2";

    private static Rect m_panelRect = new Rect(
        60.0f,
        60.0f,
        PanelWidth,
        CharacterPanelHeight);

    private static bool m_isPanelDragging;
    private static Vector2 m_panelDragOffset;

    private static AttachmentPoint m_selectedAttachmentPoint =
        AttachmentPoint.Head;

    private delegate void OwnAccessorySelectedDelegate(
        GameObject accessoryAsset,
        AttachmentPoint attachmentPoint);

    private static bool m_isValidationDetailsExpanded;
    private static Vector2 m_validationScrollPosition;

    private static GameObject m_lastAddedAccessory;

    private static GUIStyle m_titleStyle;
    private static GUIStyle m_bodyLabelStyle;
    private static GUIStyle m_sectionLabelStyle;
    private static GUIStyle m_buttonStyle;
    private static GUIStyle m_noteLabelStyle;
    private static GUIStyle m_warningButtonStyle;
    private static GUIStyle m_errorDetailLabelStyle;

    private enum AttachmentPoint
    {
        Head,
        LeftHand,
        RightHand,
        Chest,
        Waist,
        LeftLeg,
        RightLeg,
    }

    static ParticipantPrefabEditSceneGui()
    {
        m_panelRect.x = EditorPrefs.GetFloat(
            PanelPositionXEditorPrefsKey,
            60.0f);

        m_panelRect.y = EditorPrefs.GetFloat(
            PanelPositionYEditorPrefsKey,
            60.0f);

        SceneView.duringSceneGui += OnSceneGui;
    }

    private static void OnSceneGui(SceneView sceneView)
    {
        PrefabStage prefabStage =
            PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage == null)
        {
            return;
        }

        if (!IsParticipantPrefabPath(prefabStage.assetPath))
        {
            return;
        }

        GameObject prefabRoot = prefabStage.prefabContentsRoot;

        if (prefabRoot == null)
        {
            return;
        }

        ComPartyBase party =
            prefabRoot.GetComponent<ComPartyBase>();

        if (party != null)
        {
            DrawPartyPanel(
                party,
                prefabStage.assetPath);

            return;
        }

        ComCharacterBase character =
            prefabRoot.GetComponent<ComCharacterBase>();

        if (character != null)
        {
            DrawCharacterPanel(
                character,
                prefabStage.assetPath,
                prefabStage);
        }
    }

    private static void DrawPartyPanel(
        ComPartyBase party,
        string partyPrefabPath)
    {
        Handles.BeginGUI();

        try
        {
            EnsureStyles();

            BeginPanel(
                "参加者 Party Prefab",
                PartyPanelHeight);

            GUILayout.Label(
                "AI編集とCharacter Prefabの編集を行えます。",
                m_bodyLabelStyle);

            GUILayout.Space(7.0f);

            if (GUILayout.Button(
                "Partyスクリプトを開く",
                m_buttonStyle,
                GUILayout.Height(26.0f)))
            {
                OpenComponentScript(
                    party,
                    "Partyスクリプトを開けません");
            }

            GUILayout.Space(4.0f);

            ComCharacterBase characterPrefab;

            bool hasCharacterPrefab =
                TryGetCharacterPrefab(
                    party,
                    out characterPrefab);

            using (new EditorGUI.DisabledScope(!hasCharacterPrefab))
            {
                if (GUILayout.Button(
                    "Character Prefabを開く",
                    m_buttonStyle,
                    GUILayout.Height(26.0f)))
                {
                    OpenPrefabAsset(
                        characterPrefab.gameObject);
                }
            }

            if (!hasCharacterPrefab)
            {
                GUILayout.Space(5.0f);

                EditorGUILayout.HelpBox(
                    "Character Prefabが設定されていません。",
                    MessageType.Warning);
            }

            GUILayout.Space(8.0f);

            GUILayout.Label(
                "参加者情報の編集",
                m_sectionLabelStyle);

            GUILayout.Label(
                "Inspector上のCom Party Baseで、開発者名、"
                + "パーティー名、チーム紹介、チームアイコンを"
                + "設定できます。",
                m_noteLabelStyle);

            GUILayout.Space(5.0f);

            Sprite teamIconSprite =
                GetTeamIconSprite(party);

            DrawTeamIconPreview(teamIconSprite);

            GUILayout.Space(7.0f);

            if (GUILayout.Button(
                "提出用ZIPを作成...",
                m_buttonStyle,
                GUILayout.Height(26.0f)))
            {
                ParticipantSubmissionZipExporter.CreateSubmissionZip(
                    party,
                    partyPrefabPath);
            }

            GUILayout.Space(4.0f);

            GUILayout.Label(
                "ZIP作成前に、Prefab参照・必須情報・"
                + "禁止コンポーネント・フォルダ外参照を検査します。",
                m_noteLabelStyle);

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                "変更後は右上の Save で保存してください。",
                m_noteLabelStyle);

            EndPanel();
        }
        finally
        {
            Handles.EndGUI();
        }
    }


    private static Sprite GetTeamIconSprite(
    ComPartyBase party)
    {
        if (party == null)
        {
            return null;
        }

        SerializedObject serializedParty =
            new SerializedObject(party);

        SerializedProperty teamIconProperty =
            serializedParty.FindProperty(
                TeamIconSpriteFieldName);

        if (teamIconProperty == null)
        {
            return null;
        }

        return teamIconProperty.objectReferenceValue
            as Sprite;
    }

    private static void DrawTeamIconPreview(
        Sprite teamIconSprite)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(
            "チームアイコン",
            m_noteLabelStyle,
            GUILayout.Width(85.0f));

        Rect previewRect =
            GUILayoutUtility.GetRect(
                54.0f,
                54.0f,
                GUILayout.Width(54.0f),
                GUILayout.Height(54.0f));

        EditorGUI.DrawRect(
            previewRect,
            new Color(0.05f, 0.06f, 0.08f, 1.0f));

        if (teamIconSprite == null)
        {
            GUI.Label(
                previewRect,
                "未設定",
                CreateTeamIconEmptyLabelStyle());

            GUILayout.Space(7.0f);

            GUILayout.Label(
                "InspectorでTeam Icon Spriteを設定してください。",
                m_noteLabelStyle);
        }
        else
        {
            DrawSpritePreview(
                previewRect,
                teamIconSprite);

            GUILayout.Space(7.0f);

            GUILayout.Label(
                teamIconSprite.name,
                m_noteLabelStyle);
        }

        GUILayout.EndHorizontal();
    }

    private static GUIStyle CreateTeamIconEmptyLabelStyle()
    {
        GUIStyle style =
            new GUIStyle(EditorStyles.centeredGreyMiniLabel);

        style.fontSize = 10;
        style.wordWrap = true;
        style.alignment = TextAnchor.MiddleCenter;

        return style;
    }

    private static void DrawSpritePreview(
        Rect previewRect,
        Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;

        Rect textureRect = sprite.textureRect;

        float spriteAspect =
            textureRect.width / textureRect.height;

        float previewAspect =
            previewRect.width / previewRect.height;

        Rect drawRect;

        if (spriteAspect >= previewAspect)
        {
            float drawHeight =
                previewRect.width / spriteAspect;

            drawRect = new Rect(
                previewRect.x,
                previewRect.y
                    + (previewRect.height - drawHeight) * 0.5f,
                previewRect.width,
                drawHeight);
        }
        else
        {
            float drawWidth =
                previewRect.height * spriteAspect;

            drawRect = new Rect(
                previewRect.x
                    + (previewRect.width - drawWidth) * 0.5f,
                previewRect.y,
                drawWidth,
                previewRect.height);
        }

        Rect textureCoordinates = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);

        GUI.DrawTextureWithTexCoords(
            drawRect,
            texture,
            textureCoordinates,
            true);
    }



    private static void DrawCharacterPanel(
        ComCharacterBase character,
        string characterPrefabPath,
        PrefabStage prefabStage)
    {
        List<string> validationMessages =
            GetAccessoryValidationMessages(character);

        float panelHeight =
            m_isValidationDetailsExpanded
                ? CharacterPanelExpandedHeight
                : CharacterPanelHeight;

        Handles.BeginGUI();

        try
        {
            EnsureStyles();

            BeginPanel(
                "参加者 Character Prefab",
                panelHeight);

            GUILayout.Label(
                "AI編集とアクセサリ追加を行えます。",
                m_bodyLabelStyle);

            GUILayout.Space(7.0f);

            if (GUILayout.Button(
                "Characterスクリプトを開く",
                m_buttonStyle,
                GUILayout.Height(26.0f)))
            {
                OpenComponentScript(
                    character,
                    "Characterスクリプトを開けません");
            }

            GUILayout.Space(4.0f);

            ComPartyBase partyPrefab;

            bool hasPartyPrefab =
                TryFindPartyPrefabInSameFolder(
                    characterPrefabPath,
                    out partyPrefab);

            using (new EditorGUI.DisabledScope(!hasPartyPrefab))
            {
                if (GUILayout.Button(
                    "Party Prefabを開く",
                    m_buttonStyle,
                    GUILayout.Height(26.0f)))
                {
                    OpenPrefabAsset(
                        partyPrefab.gameObject);
                }
            }

            if (!hasPartyPrefab)
            {
                GUILayout.Space(5.0f);

                EditorGUILayout.HelpBox(
                    "同じフォルダ内にParty Prefabがありません。",
                    MessageType.Warning);
            }

            GUILayout.Space(8.0f);

            GUILayout.Label(
                "アクセサリを追加",
                m_sectionLabelStyle);

            m_selectedAttachmentPoint =
                (AttachmentPoint)EditorGUILayout.EnumPopup(
                    "装着先",
                    m_selectedAttachmentPoint);

            GUILayout.Space(3.0f);

            if (GUILayout.Button(
                "運営アクセサリを追加...",
                m_buttonStyle,
                GUILayout.Height(25.0f)))
            {
                ShowCatalogAccessoryMenu();
            }

            GUILayout.Space(3.0f);

            if (GUILayout.Button(
                "自分のPrefab／モデルを追加...",
                m_buttonStyle,
                GUILayout.Height(25.0f)))
            {
                OpenOwnAccessoryPicker(characterPrefabPath);
            }

            if (IsLastAddedAccessorySelected())
            {
                GUILayout.Space(5.0f);

                GUILayout.Label(
                    "追加したアクセサリを選択中です。\n"
                    + "細かい位置・角度・大きさは、"
                    + "Inspector の Transform で調整してください。",
                    m_noteLabelStyle);
            }

            if (0 < validationMessages.Count)
            {
                GUILayout.Space(6.0f);

                string warningButtonLabel =
                    m_isValidationDetailsExpanded
                        ? "禁止コンポーネント "
                            + validationMessages.Count
                            + " 件  （詳細を隠す）"
                        : "禁止コンポーネント "
                            + validationMessages.Count
                            + " 件  （詳細を表示）";

                if (GUILayout.Button(
                    warningButtonLabel,
                    m_warningButtonStyle,
                    GUILayout.Height(28.0f)))
                {
                    m_isValidationDetailsExpanded =
                        !m_isValidationDetailsExpanded;
                }

                if (m_isValidationDetailsExpanded)
                {
                    m_validationScrollPosition =
                        GUILayout.BeginScrollView(
                            m_validationScrollPosition,
                            GUILayout.Height(108.0f));

                    for (int i = 0;
                        i < validationMessages.Count;
                        i++)
                    {
                        GUILayout.Label(
                            validationMessages[i],
                            m_errorDetailLabelStyle);
                    }

                    GUILayout.EndScrollView();
                }
            }

            GUILayout.FlexibleSpace();

            GUILayout.Label(
                "変更後は右上の Save で保存してください。",
                m_noteLabelStyle);

            EndPanel();
        }
        finally
        {
            Handles.EndGUI();
        }
    }

    private static void EnsureStyles()
    {
        if (m_titleStyle != null)
        {
            return;
        }

        m_titleStyle = new GUIStyle(EditorStyles.boldLabel);
        m_titleStyle.fontSize = 14;
        m_titleStyle.alignment = TextAnchor.MiddleLeft;
        m_titleStyle.normal.textColor = Color.white;

        m_bodyLabelStyle = new GUIStyle(EditorStyles.label);
        m_bodyLabelStyle.fontSize = 13;
        m_bodyLabelStyle.wordWrap = true;
        m_bodyLabelStyle.normal.textColor =
            new Color(0.92f, 0.94f, 0.97f, 1.0f);

        m_sectionLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        m_sectionLabelStyle.fontSize = 14;
        m_sectionLabelStyle.normal.textColor =
            new Color(1.0f, 0.88f, 0.52f, 1.0f);

        m_buttonStyle = new GUIStyle(GUI.skin.button);
        m_buttonStyle.fontSize = 13;
        m_buttonStyle.fontStyle = FontStyle.Bold;
        m_buttonStyle.alignment = TextAnchor.MiddleCenter;

        m_noteLabelStyle = new GUIStyle(EditorStyles.label);
        m_noteLabelStyle.fontSize = 11;
        m_noteLabelStyle.wordWrap = true;
        m_noteLabelStyle.normal.textColor =
            new Color(0.76f, 0.80f, 0.86f, 1.0f);

        m_warningButtonStyle = new GUIStyle(GUI.skin.button);
        m_warningButtonStyle.fontSize = 12;
        m_warningButtonStyle.fontStyle = FontStyle.Bold;
        m_warningButtonStyle.alignment = TextAnchor.MiddleCenter;
        m_warningButtonStyle.normal.textColor = Color.white;

        m_warningButtonStyle.normal.background =
            CreateSolidTexture(
                new Color(0.52f, 0.15f, 0.15f, 1.0f));

        m_warningButtonStyle.hover.background =
            CreateSolidTexture(
                new Color(0.66f, 0.20f, 0.20f, 1.0f));

        m_warningButtonStyle.active.background =
            CreateSolidTexture(
                new Color(0.40f, 0.09f, 0.09f, 1.0f));

        m_errorDetailLabelStyle = new GUIStyle(EditorStyles.label);
        m_errorDetailLabelStyle.fontSize = 12;
        m_errorDetailLabelStyle.wordWrap = true;
        m_errorDetailLabelStyle.normal.textColor =
            new Color(1.0f, 0.84f, 0.84f, 1.0f);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);

        texture.SetPixel(0, 0, color);
        texture.Apply();

        return texture;
    }

    private static void BeginPanel(
        string title,
        float panelHeight)
    {
        m_panelRect.width = PanelWidth;
        m_panelRect.height = panelHeight;

        HandlePanelDrag();

        EditorGUI.DrawRect(
            m_panelRect,
            new Color(0.09f, 0.11f, 0.14f, 0.97f));

        Rect headerRect = new Rect(
            m_panelRect.x,
            m_panelRect.y,
            m_panelRect.width,
            PanelHeaderHeight);

        EditorGUI.DrawRect(
            headerRect,
            new Color(0.16f, 0.23f, 0.34f, 1.0f));

        Rect titleRect = new Rect(
            headerRect.x + 9.0f,
            headerRect.y,
            headerRect.width - 18.0f,
            headerRect.height);

        GUI.Label(
            titleRect,
            title + "  （ドラッグで移動）",
            m_titleStyle);

        Rect contentRect = new Rect(
            m_panelRect.x + PanelPadding,
            m_panelRect.y + PanelHeaderHeight + 6.0f,
            m_panelRect.width - PanelPadding * 2.0f,
            m_panelRect.height
                - PanelHeaderHeight
                - PanelPadding
                - 6.0f);

        GUILayout.BeginArea(contentRect);
    }

    private static void EndPanel()
    {
        GUILayout.EndArea();
    }

    private static void HandlePanelDrag()
    {
        Event currentEvent = Event.current;

        if (currentEvent == null)
        {
            return;
        }

        Rect headerRect = new Rect(
            m_panelRect.x,
            m_panelRect.y,
            m_panelRect.width,
            PanelHeaderHeight);

        if (currentEvent.type == EventType.MouseDown
            && currentEvent.button == 0
            && headerRect.Contains(currentEvent.mousePosition))
        {
            m_isPanelDragging = true;

            m_panelDragOffset =
                currentEvent.mousePosition - m_panelRect.position;

            currentEvent.Use();

            return;
        }

        if (currentEvent.type == EventType.MouseDrag
            && m_isPanelDragging)
        {
            m_panelRect.position =
                currentEvent.mousePosition - m_panelDragOffset;

            SavePanelPosition();

            currentEvent.Use();

            return;
        }

        if (currentEvent.type == EventType.MouseUp
            && m_isPanelDragging)
        {
            m_isPanelDragging = false;

            SavePanelPosition();

            currentEvent.Use();
        }
    }

    private static void SavePanelPosition()
    {
        EditorPrefs.SetFloat(
            PanelPositionXEditorPrefsKey,
            m_panelRect.x);

        EditorPrefs.SetFloat(
            PanelPositionYEditorPrefsKey,
            m_panelRect.y);
    }

    private static void ShowCatalogAccessoryMenu()
    {
        ParticipantAccessoryCatalog catalog =
            AssetDatabase.LoadAssetAtPath<
                ParticipantAccessoryCatalog>(
                AccessoryCatalogAssetPath);

        if (catalog == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリカタログが見つかりません",
                "以下のassetを確認してください。\n\n"
                + AccessoryCatalogAssetPath,
                "OK");

            return;
        }

        ParticipantAccessoryCatalog.Entry[] entries =
            catalog.Entries;

        if (entries == null || entries.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "アクセサリカタログが空です",
                "ParticipantAccessoryCatalog.asset の Entries に、"
                + "表示名とPrefabを設定してください。",
                "OK");

            return;
        }

        GenericMenu menu = new GenericMenu();

        int validEntryCount = 0;

        for (int i = 0; i < entries.Length; i++)
        {
            ParticipantAccessoryCatalog.Entry entry =
                entries[i];

            if (entry == null)
            {
                continue;
            }

            string displayName = entry.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = "名称未設定";
            }

            GameObject accessoryPrefab = entry.Prefab;

            if (accessoryPrefab == null)
            {
                menu.AddDisabledItem(
                    new GUIContent(
                        displayName + " （Prefab未設定）"));

                continue;
            }

            validEntryCount++;

            menu.AddItem(
                new GUIContent(displayName),
                false,
                () =>
                {
                    TryAddAccessoryToCurrentCharacterPrefab(
                        accessoryPrefab,
                        m_selectedAttachmentPoint);
                });
        }

        if (validEntryCount == 0)
        {
            EditorUtility.DisplayDialog(
                "使用可能なアクセサリがありません",
                "ParticipantAccessoryCatalog.asset に"
                + "Prefabが設定された項目がありません。",
                "OK");

            return;
        }

        menu.ShowAsContext();
        GUIUtility.ExitGUI();
    }

    private static void OpenOwnAccessoryPicker(
        string characterPrefabPath)
    {
        string participantFolderPath =
            GetParticipantFolderPath(characterPrefabPath);

        if (string.IsNullOrEmpty(participantFolderPath))
        {
            EditorUtility.DisplayDialog(
                "アクセサリを選べません",
                "現在のCharacter Prefabから参加者フォルダを"
                + "特定できませんでした。",
                "OK");

            return;
        }

        List<GameObject> accessoryCandidates =
            FindOwnAccessoryCandidates(
                participantFolderPath,
                characterPrefabPath);

        if (accessoryCandidates.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "追加できるPrefab／モデルがありません",
                "自作アクセサリは、参加者Prefabと同じフォルダ"
                + "以下へ配置してください。\n\n"
                + "配置先:\n"
                + participantFolderPath
                + "\n\n"
                + "PrefabまたはFBXモデルを配置すると、"
                + "ここに候補として表示されます。",
                "OK");

            return;
        }

        OwnAccessorySelectionWindow.Open(
            participantFolderPath,
            accessoryCandidates,
            m_selectedAttachmentPoint,
            OnOwnAccessorySelected);
    }


    private static List<GameObject> FindOwnAccessoryCandidates(
    string participantFolderPath,
    string characterPrefabPath)
    {
        List<GameObject> candidates =
            new List<GameObject>();

        string[] assetGuids = AssetDatabase.FindAssets(
            "t:GameObject",
            new[] { participantFolderPath });

        for (int i = 0; i < assetGuids.Length; i++)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(assetGuids[i]);

            if (assetPath == characterPrefabPath)
            {
                continue;
            }

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    assetPath);

            if (asset == null)
            {
                continue;
            }

            if (asset.GetComponent<ComPartyBase>() != null)
            {
                continue;
            }

            if (asset.GetComponent<ComCharacterBase>() != null)
            {
                continue;
            }

            candidates.Add(asset);
        }

        candidates.Sort(
            (left, right) =>
            {
                string leftPath =
                    AssetDatabase.GetAssetPath(left);

                string rightPath =
                    AssetDatabase.GetAssetPath(right);

                return string.CompareOrdinal(
                    leftPath,
                    rightPath);
            });

        return candidates;
    }

    private static void OnOwnAccessorySelected(
        GameObject accessoryAsset,
        AttachmentPoint attachmentPoint)
    {
        PrefabStage prefabStage =
            PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "Character Prefabの編集画面を開いてください。",
                "OK");

            return;
        }

        GameObject prefabRoot =
            prefabStage.prefabContentsRoot;

        if (prefabRoot == null)
        {
            return;
        }

        ComCharacterBase character =
            prefabRoot.GetComponent<ComCharacterBase>();

        if (character == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "現在開いているPrefabはCharacter Prefabではありません。",
                "OK");

            return;
        }

        TryAddAccessory(
            character,
            prefabStage,
            accessoryAsset,
            attachmentPoint);
    }

    private static string GetPathRelativeToFolder(
        string assetPath,
        string folderPath)
    {
        if (string.IsNullOrEmpty(assetPath)
            || string.IsNullOrEmpty(folderPath))
        {
            return assetPath;
        }

        string normalizedAssetPath =
            assetPath.Replace("\\", "/");

        string normalizedFolderPath =
            folderPath.Replace("\\", "/")
                .TrimEnd('/');

        string prefix = normalizedFolderPath + "/";

        if (!normalizedAssetPath.StartsWith(prefix))
        {
            return normalizedAssetPath;
        }

        return normalizedAssetPath.Substring(prefix.Length);
    }



    private static void TryAddAccessoryToCurrentCharacterPrefab(
        GameObject accessoryAsset,
        AttachmentPoint attachmentPoint)
    {
        PrefabStage prefabStage =
            PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "Character Prefabの編集画面を開いてください。",
                "OK");

            return;
        }

        GameObject prefabRoot = prefabStage.prefabContentsRoot;

        if (prefabRoot == null)
        {
            return;
        }

        ComCharacterBase character =
            prefabRoot.GetComponent<ComCharacterBase>();

        if (character == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "現在開いているPrefabはCharacter Prefabではありません。",
                "OK");

            return;
        }

        TryAddAccessory(
            character,
            prefabStage,
            accessoryAsset,
            attachmentPoint);
    }

    private static void TryAddAccessory(
        ComCharacterBase character,
        PrefabStage prefabStage,
        GameObject accessoryAsset,
        AttachmentPoint attachmentPoint)
    {
        if (character == null
            || prefabStage == null
            || accessoryAsset == null)
        {
            return;
        }

        List<string> invalidComponents =
            GetInvalidComponentMessages(
                accessoryAsset.transform,
                null);

        if (0 < invalidComponents.Count)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "選択したPrefabまたはモデルには、"
                + "アクセサリとして使用できないコンポーネントがあります。\n\n"
                + string.Join("\n", invalidComponents),
                "OK");

            return;
        }

        Transform mannequinRoot =
            FindDescendantByName(
                character.transform,
                MannequinRootName);

        if (mannequinRoot == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "Character Prefab内に \""
                + MannequinRootName
                + "\" が見つかりません。",
                "OK");

            return;
        }

        string attachmentTransformName =
            GetAttachmentTransformName(attachmentPoint);

        Transform attachmentTransform =
            FindDescendantByName(
                mannequinRoot,
                attachmentTransformName);

        if (attachmentTransform == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "装着先のTransformが見つかりません。\n\n"
                + "装着先: "
                + GetAttachmentPointDisplayName(attachmentPoint)
                + "\nTransform名: "
                + attachmentTransformName,
                "OK");

            return;
        }

        GameObject createdAccessory;

        try
        {
            createdAccessory =
                PrefabUtility.InstantiatePrefab(
                    accessoryAsset,
                    prefabStage.scene)
                as GameObject;
        }
        catch (System.Exception exception)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "Prefabまたはモデルの生成中にエラーが発生しました。\n\n"
                + exception.Message,
                "OK");

            return;
        }

        if (createdAccessory == null)
        {
            EditorUtility.DisplayDialog(
                "アクセサリを追加できません",
                "Prefabまたはモデルを生成できませんでした。",
                "OK");

            return;
        }

        Undo.RegisterCreatedObjectUndo(
            createdAccessory,
            "Add Participant Accessory");

        createdAccessory.transform.SetParent(
            attachmentTransform,
            false);

        createdAccessory.transform.localPosition =
            Vector3.zero;

        createdAccessory.transform.localRotation =
            Quaternion.identity;

        createdAccessory.transform.localScale =
            Vector3.one;

        EditorSceneManager.MarkSceneDirty(
            prefabStage.scene);

        EditorUtility.SetDirty(character);

        m_lastAddedAccessory = createdAccessory;

        Selection.activeGameObject = createdAccessory;

        EditorGUIUtility.PingObject(createdAccessory);
    }

    private static bool IsLastAddedAccessorySelected()
    {
        if (m_lastAddedAccessory == null)
        {
            return false;
        }

        GameObject selectedGameObject =
            Selection.activeGameObject;

        if (selectedGameObject == null)
        {
            return false;
        }

        Transform selectedTransform =
            selectedGameObject.transform;

        Transform lastAddedAccessoryTransform =
            m_lastAddedAccessory.transform;

        return selectedTransform == lastAddedAccessoryTransform
            || selectedTransform.IsChildOf(
                lastAddedAccessoryTransform);
    }


    private static List<string> GetAccessoryValidationMessages(
        ComCharacterBase character)
    {
        List<string> messages = new List<string>();

        if (character == null)
        {
            return messages;
        }

        Transform mannequinRoot =
            FindDescendantByName(
                character.transform,
                MannequinRootName);

        if (mannequinRoot == null)
        {
            messages.Add(
                "\"" + MannequinRootName
                + "\" が見つかりません。");

            return messages;
        }

        GameObject baseCharacterPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterBasePrefabAssetPath);

        if (baseCharacterPrefab == null)
        {
            messages.Add(
                "基準Prefabが見つかりません。\n"
                + CharacterBasePrefabAssetPath);

            return messages;
        }

        Transform baseMannequinRoot =
            FindDescendantByName(
                baseCharacterPrefab.transform,
                MannequinRootName);

        if (baseMannequinRoot == null)
        {
            messages.Add(
                "基準Prefab内に \""
                + MannequinRootName
                + "\" が見つかりません。");

            return messages;
        }

        Dictionary<string, int> allowedComponentCounts =
            CreateComponentCountDictionary(
                baseMannequinRoot);

        return GetInvalidComponentMessages(
            mannequinRoot,
            allowedComponentCounts);
    }

    private static Dictionary<string, int>
        CreateComponentCountDictionary(
            Transform root)
    {
        Dictionary<string, int> componentCounts =
            new Dictionary<string, int>();

        if (root == null)
        {
            return componentCounts;
        }

        Component[] components =
            root.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                continue;
            }

            if (!IsForbiddenAccessoryComponent(component))
            {
                continue;
            }

            string componentKey =
                GetComponentKey(root, component);

            int count;

            if (!componentCounts.TryGetValue(
                    componentKey,
                    out count))
            {
                count = 0;
            }

            componentCounts[componentKey] = count + 1;
        }

        return componentCounts;
    }

    private static List<string> GetInvalidComponentMessages(
        Transform root,
        Dictionary<string, int> allowedComponentCounts)
    {
        List<string> messages = new List<string>();

        if (root == null)
        {
            return messages;
        }

        Dictionary<string, int> checkedComponentCounts =
            new Dictionary<string, int>();

        Component[] components =
            root.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null)
            {
                messages.Add(
                    "Missing Script: "
                    + GetRelativePathForDisplay(
                        root,
                        null));

                continue;
            }

            if (!IsForbiddenAccessoryComponent(component))
            {
                continue;
            }

            string componentKey =
                GetComponentKey(root, component);

            int checkedCount;

            if (!checkedComponentCounts.TryGetValue(
                    componentKey,
                    out checkedCount))
            {
                checkedCount = 0;
            }

            checkedCount++;

            checkedComponentCounts[componentKey] =
                checkedCount;

            int allowedCount = 0;

            if (allowedComponentCounts != null)
            {
                allowedComponentCounts.TryGetValue(
                    componentKey,
                    out allowedCount);
            }

            if (checkedCount <= allowedCount)
            {
                continue;
            }

            messages.Add(
                component.GetType().Name
                + ": "
                + GetRelativePathForDisplay(
                    root,
                    component.transform));
        }

        return messages;
    }

    private static string GetComponentKey(
        Transform root,
        Component component)
    {
        string relativePath =
            GetRelativePath(root, component.transform);

        string componentTypeName =
            component.GetType().FullName;

        return relativePath
            + "\n"
            + componentTypeName;
    }

    private static bool IsForbiddenAccessoryComponent(
        Component component)
    {
        if (component is Collider)
        {
            return true;
        }

        if (component is Rigidbody)
        {
            return true;
        }

        if (component is Joint)
        {
            return true;
        }

        if (component is CharacterController)
        {
            return true;
        }

        if (component is Animator)
        {
            return true;
        }

        if (component is MonoBehaviour)
        {
            return true;
        }

        if (component is Camera)
        {
            return true;
        }

        if (component is Light)
        {
            return true;
        }

        if (component is AudioSource)
        {
            return true;
        }

        if (component is AudioListener)
        {
            return true;
        }

        if (component is ParticleSystem)
        {
            return true;
        }

        if (component is Cloth)
        {
            return true;
        }

        string componentTypeName =
            component.GetType().FullName;

        return componentTypeName
            == "UnityEngine.AI.NavMeshAgent";
    }

    private static string GetAttachmentTransformName(
        AttachmentPoint attachmentPoint)
    {
        switch (attachmentPoint)
        {
            case AttachmentPoint.Head:
                return "mixamorig:Head";

            case AttachmentPoint.LeftHand:
                return "mixamorig:LeftHand";

            case AttachmentPoint.RightHand:
                return "mixamorig:RightHand";

            case AttachmentPoint.Chest:
                return "mixamorig:Spine2";

            case AttachmentPoint.Waist:
                return "mixamorig:Hips";

            case AttachmentPoint.LeftLeg:
                return "mixamorig:LeftUpLeg";

            case AttachmentPoint.RightLeg:
                return "mixamorig:RightUpLeg";

            default:
                return string.Empty;
        }
    }

    private static string GetAttachmentPointDisplayName(
        AttachmentPoint attachmentPoint)
    {
        switch (attachmentPoint)
        {
            case AttachmentPoint.Head:
                return "頭";

            case AttachmentPoint.LeftHand:
                return "左手";

            case AttachmentPoint.RightHand:
                return "右手";

            case AttachmentPoint.Chest:
                return "胸";

            case AttachmentPoint.Waist:
                return "腰";

            case AttachmentPoint.LeftLeg:
                return "左足";

            case AttachmentPoint.RightLeg:
                return "右足";

            default:
                return "不明";
        }
    }

    private static bool TryGetCharacterPrefab(
        ComPartyBase party,
        out ComCharacterBase characterPrefab)
    {
        characterPrefab = null;

        if (party == null)
        {
            return false;
        }

        SerializedObject serializedParty =
            new SerializedObject(party);

        SerializedProperty memberPrefabsProperty =
            serializedParty.FindProperty(
                MemberPrefabsFieldName);

        if (memberPrefabsProperty == null)
        {
            Debug.LogError(
                "[ParticipantPrefabEditSceneGui] "
                + "ComPartyBaseのシリアライズフィールドが見つかりません: "
                + MemberPrefabsFieldName);

            return false;
        }

        characterPrefab =
            memberPrefabsProperty.objectReferenceValue
            as ComCharacterBase;

        return characterPrefab != null;
    }

    private static bool TryFindPartyPrefabInSameFolder(
        string characterPrefabPath,
        out ComPartyBase partyPrefab)
    {
        partyPrefab = null;

        if (string.IsNullOrEmpty(characterPrefabPath))
        {
            return false;
        }

        string participantFolderPath =
            Path.GetDirectoryName(characterPrefabPath)
                ?.Replace("\\", "/");

        if (string.IsNullOrEmpty(participantFolderPath))
        {
            return false;
        }

        string[] prefabGuids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { participantFolderPath });

        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath =
                AssetDatabase.GUIDToAssetPath(prefabGuid);

            if (prefabPath == characterPrefabPath)
            {
                continue;
            }

            string prefabFolderPath =
                Path.GetDirectoryName(prefabPath)
                    ?.Replace("\\", "/");

            if (prefabFolderPath != participantFolderPath)
            {
                continue;
            }

            GameObject prefabAsset =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    prefabPath);

            if (prefabAsset == null)
            {
                continue;
            }

            ComPartyBase candidateParty =
                prefabAsset.GetComponent<ComPartyBase>();

            if (candidateParty == null)
            {
                continue;
            }

            partyPrefab = candidateParty;
            return true;
        }

        return false;
    }

    private static void OpenComponentScript(
        MonoBehaviour component,
        string errorTitle)
    {
        if (component == null)
        {
            EditorUtility.DisplayDialog(
                errorTitle,
                "対象コンポーネントを取得できませんでした。",
                "OK");

            return;
        }

        MonoScript script =
            MonoScript.FromMonoBehaviour(component);

        if (script == null)
        {
            EditorUtility.DisplayDialog(
                errorTitle,
                "対象スクリプトを取得できませんでした。",
                "OK");

            return;
        }

        AssetDatabase.OpenAsset(script);
    }

    private static void OpenPrefabAsset(GameObject prefabAsset)
    {
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Prefabを開けません",
                "対象Prefabを取得できませんでした。",
                "OK");

            return;
        }

        string prefabPath =
            AssetDatabase.GetAssetPath(prefabAsset);

        if (string.IsNullOrEmpty(prefabPath))
        {
            EditorUtility.DisplayDialog(
                "Prefabを開けません",
                "対象Prefabのアセットパスを取得できませんでした。",
                "OK");

            return;
        }

        PrefabStageUtility.OpenPrefab(prefabPath);
        GUIUtility.ExitGUI();
    }

    private static string GetParticipantFolderPath(
        string characterPrefabPath)
    {
        if (string.IsNullOrEmpty(characterPrefabPath))
        {
            return null;
        }

        return Path.GetDirectoryName(characterPrefabPath)
            ?.Replace("\\", "/");
    }

    private static bool IsAssetPathInsideFolder(
        string assetPath,
        string folderPath)
    {
        if (string.IsNullOrEmpty(assetPath)
            || string.IsNullOrEmpty(folderPath))
        {
            return false;
        }

        string normalizedAssetPath =
            assetPath.Replace("\\", "/");

        string normalizedFolderPath =
            folderPath.Replace("\\", "/")
                .TrimEnd('/');

        return normalizedAssetPath.StartsWith(
            normalizedFolderPath + "/");
    }

    private static bool IsParticipantPrefabPath(
        string prefabAssetPath)
    {
        if (string.IsNullOrEmpty(prefabAssetPath))
        {
            return false;
        }

        string normalizedPath =
            prefabAssetPath.Replace("\\", "/");

        return normalizedPath.StartsWith(
            ParticipantRootPath + "/");
    }

    private static Transform FindDescendantByName(
        Transform root,
        string targetName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result =
                FindDescendantByName(
                    root.GetChild(i),
                    targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static string GetRelativePath(
        Transform root,
        Transform target)
    {
        if (root == null || target == null)
        {
            return string.Empty;
        }

        if (root == target)
        {
            return string.Empty;
        }

        List<string> names = new List<string>();

        Transform current = target;

        while (current != null && current != root)
        {
            names.Add(current.name);
            current = current.parent;
        }

        if (current != root)
        {
            return string.Empty;
        }

        names.Reverse();

        return string.Join("/", names);
    }

    private static string GetRelativePathForDisplay(
        Transform root,
        Transform target)
    {
        if (target == null)
        {
            return root != null
                ? root.name
                : "不明な階層";
        }

        string relativePath =
            GetRelativePath(root, target);

        if (string.IsNullOrEmpty(relativePath))
        {
            return root.name;
        }

        return relativePath;
    }




    private sealed class OwnAccessorySelectionWindow
    : EditorWindow
    {
        private string m_participantFolderPath;
        private List<GameObject> m_accessoryCandidates;
        private AttachmentPoint m_attachmentPoint;
        private OwnAccessorySelectedDelegate m_onAccessorySelected;

        private string m_searchText = string.Empty;
        private Vector2 m_scrollPosition;

        public static void Open(
            string participantFolderPath,
            List<GameObject> accessoryCandidates,
            AttachmentPoint attachmentPoint,
            OwnAccessorySelectedDelegate onAccessorySelected)
        {
            OwnAccessorySelectionWindow window =
                CreateInstance<OwnAccessorySelectionWindow>();

            window.titleContent =
                new GUIContent("自分のアクセサリを追加");

            window.m_participantFolderPath =
                participantFolderPath;

            window.m_accessoryCandidates =
                accessoryCandidates;

            window.m_attachmentPoint =
                attachmentPoint;

            window.m_onAccessorySelected =
                onAccessorySelected;

            window.minSize = new Vector2(
                460.0f,
                320.0f);

            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(10.0f);

            GUILayout.Label(
                "自分のPrefab／モデルを追加",
                EditorStyles.boldLabel);

            GUILayout.Space(4.0f);

            EditorGUILayout.HelpBox(
                "参加者フォルダ以下にあるPrefabまたはモデルだけを"
                + "表示しています。\n"
                + "選択したアセットは「"
                + GetAttachmentPointDisplayName(
                    m_attachmentPoint)
                + "」へ追加されます。",
                MessageType.Info);

            GUILayout.Space(4.0f);

            EditorGUILayout.LabelField(
                "検索",
                EditorStyles.miniLabel);

            m_searchText =
                EditorGUILayout.TextField(m_searchText);

            GUILayout.Space(4.0f);

            m_scrollPosition =
                GUILayout.BeginScrollView(m_scrollPosition);

            for (int i = 0;
                i < m_accessoryCandidates.Count;
                i++)
            {
                GameObject candidate =
                    m_accessoryCandidates[i];

                if (candidate == null)
                {
                    continue;
                }

                string assetPath =
                    AssetDatabase.GetAssetPath(candidate);

                string relativePath =
                    GetPathRelativeToFolder(
                        assetPath,
                        m_participantFolderPath);

                if (!string.IsNullOrEmpty(m_searchText)
                    && relativePath.IndexOf(
                        m_searchText,
                        System.StringComparison.OrdinalIgnoreCase)
                        < 0)
                {
                    continue;
                }

                GUILayout.BeginHorizontal(
                    EditorStyles.helpBox);

                Texture preview =
                    AssetPreview.GetMiniThumbnail(candidate);

                GUILayout.Label(
                    preview,
                    GUILayout.Width(34.0f),
                    GUILayout.Height(34.0f));

                GUILayout.BeginVertical();

                GUILayout.Label(
                    candidate.name,
                    EditorStyles.boldLabel);

                GUILayout.Label(
                    relativePath,
                    EditorStyles.miniLabel);

                GUILayout.EndVertical();

                if (GUILayout.Button(
                    "追加",
                    GUILayout.Width(60.0f),
                    GUILayout.Height(28.0f)))
                {
                    OwnAccessorySelectedDelegate callback =
                        m_onAccessorySelected;

                    Close();

                    if (callback != null)
                    {
                        callback(
                            candidate,
                            m_attachmentPoint);
                    }

                    GUIUtility.ExitGUI();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5.0f);

            GUILayout.Label(
                "候補にないアセットは、参加者フォルダ以下へ"
                + "配置してから開き直してください。",
                EditorStyles.wordWrappedMiniLabel);
        }
    }

}

#endif