#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEngine;

namespace SuperDungeonWars.Editor
{
    public sealed class ParticipantCreationWindow : EditorWindow
    {
        // ─────────────────────────────────────────────
        // 後でベースPrefabを作成したら、このファイル名だけ変更してください。
        // ─────────────────────────────────────────────
        private const string PartyBasePrefabFileName = "ParticipantPartyBase.prefab";
        private const string CharacterBasePrefabFileName = "ParticipantCharacterBase.prefab";

        private const string EditorRootPath =
            "Assets/Dungeon/Editor/ParticipantCreation";

        private const string BasePrefabFolderPath =
            EditorRootPath + "/Prefab";

        private const string TemplateFolderPath =
            EditorRootPath + "/Templates";

        private const string ParticipantRootPath =
            "Assets/Participants";

        private const string PartyTemplatePath =
            TemplateFolderPath + "/ParticipantParty.cs.txt";

        private const string CharacterTemplatePath =
            TemplateFolderPath + "/ParticipantCharacter.cs.txt";

        private const string PendingCreationSessionKey =
            "SuperDungeonWars.ParticipantCreation.PendingData";

        private const string BattlePartyRegistryAssetPath =
            "Assets/Participants/Settings/BattlePartyRegistry.asset";

        private const int MaxConnpassId = 9999999;
        private const int MaxDisplayNameLength = 20;
        private const int MaxDescriptionLength = 60;
        private const int CharacterPrefabReadyRetryLimit = 120;


        [Serializable]
        private sealed class PendingCreationData
        {
            public int m_connpassId;
            public string m_creatorDisplayName;
            public string m_teamDisplayName;
            public string m_teamSimpleDescription;
            public string m_teamIconAssetPath;
            public string m_participantName;
            public string m_participantFolderPath;
            public string m_partyScriptPath;
            public string m_characterScriptPath;
            public string m_partyPrefabPath;
            public string m_characterPrefabPath;
        }

        private int m_connpassId;
        private string m_creatorDisplayName = "挑戦者";
        private string m_teamDisplayName = "パーティー名";
        private string m_teamSimpleDescription = "";
        private Sprite m_teamIconSprite;
        private Vector2 m_scrollPosition;

        [MenuItem("プロバト/挑戦者作成")]
        private static void OpenWindow()
        {
            var window = GetWindow<ParticipantCreationWindow>();
            window.titleContent = new GUIContent("挑戦者作成");
            window.minSize = new Vector2(560, 390);

            var position = window.position;
            position.width = 620;
            position.height = 470;
            window.position = position;
        }

        private static string PartyBasePrefabPath =>
            $"{BasePrefabFolderPath}/{PartyBasePrefabFileName}";

        private static string CharacterBasePrefabPath =>
            $"{BasePrefabFolderPath}/{CharacterBasePrefabFileName}";

        private void OnGUI()
        {
            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            GUILayout.Space(10);
            GUILayout.Label("■ 必須項目", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Connpassで発行された受付番号を入力してください。",
                MessageType.Info);

            int inputId = EditorGUILayout.IntField(
                "Connpass受付番号:",
                m_connpassId);

            m_connpassId = Mathf.Clamp(inputId, 0, MaxConnpassId);

            GUILayout.Space(16);
            GUILayout.Label("■ パーティー情報", EditorStyles.boldLabel);

            m_creatorDisplayName = EditorGUILayout.TextField(
                "作成者名:",
                m_creatorDisplayName);

            m_creatorDisplayName = LimitText(
                m_creatorDisplayName,
                MaxDisplayNameLength);

            m_teamDisplayName = EditorGUILayout.TextField(
                "パーティー名:",
                m_teamDisplayName);

            m_teamDisplayName = LimitText(
                m_teamDisplayName,
                MaxDisplayNameLength);

            m_teamSimpleDescription = EditorGUILayout.TextField(
                "チームの簡単な説明:",
                m_teamSimpleDescription);

            m_teamSimpleDescription = LimitText(
                m_teamSimpleDescription,
                MaxDescriptionLength);

            m_teamIconSprite = (Sprite)EditorGUILayout.ObjectField(
                "チームアイコン:",
                m_teamIconSprite,
                typeof(Sprite),
                false);

            GUILayout.Space(12);

            EditorGUILayout.HelpBox(
                "「作成」を押すと、参加者用フォルダ、Party用スクリプト、"
                + "Character用スクリプト、2つのPrefabを作成します。\n"
                + "作成後はParty Prefabを開きます。",
                MessageType.None);

            bool canCreate = m_connpassId > 0;

            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button("作成", GUILayout.Height(34)))
                {
                    BeginCreateParticipant();
                }
            }

            if (!canCreate)
            {
                EditorGUILayout.HelpBox(
                    "Connpass受付番号を入力してください。",
                    MessageType.Warning);
            }

            GUILayout.Space(8);
            GUILayout.Label(
                $"PartyベースPrefab: {PartyBasePrefabPath}",
                EditorStyles.miniLabel);

            GUILayout.Label(
                $"CharacterベースPrefab: {CharacterBasePrefabPath}",
                EditorStyles.miniLabel);

            EditorGUILayout.EndScrollView();
        }

        private static string LimitText(string value, int maxLength)
        {
            value ??= string.Empty;

            if (value.Length <= maxLength)
            {
                return value;
            }

            return value.Substring(0, maxLength);
        }

        private void BeginCreateParticipant()
        {
            if (!ValidateBaseAssets())
            {
                return;
            }

            string participantName = $"Party{m_connpassId:D7}";
            string participantFolderPath =
                $"{ParticipantRootPath}/{participantName}";

            if (AssetDatabase.IsValidFolder(participantFolderPath)
                || Directory.Exists(participantFolderPath))
            {
                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    $"受付番号 {m_connpassId} の参加者データは既に存在します。\n\n"
                    + participantFolderPath,
                    "OK");

                return;
            }

            try
            {
                EnsureAssetFolderExists(ParticipantRootPath);
                EnsureAssetFolderExists(participantFolderPath);

                string partyScriptPath =
                    $"{participantFolderPath}/{participantName}Party.cs";

                string characterScriptPath =
                    $"{participantFolderPath}/{participantName}Character.cs";

                string partyPrefabPath =
                    $"{participantFolderPath}/{participantName}Party.prefab";

                string characterPrefabPath =
                    $"{participantFolderPath}/{participantName}Character.prefab";

                CreateParticipantScript(
                    PartyTemplatePath,
                    partyScriptPath,
                    participantName);

                CreateParticipantScript(
                    CharacterTemplatePath,
                    characterScriptPath,
                    participantName);

                var pendingData = new PendingCreationData
                {
                    m_connpassId = m_connpassId,
                    m_creatorDisplayName = m_creatorDisplayName,
                    m_teamDisplayName = m_teamDisplayName,
                    m_teamSimpleDescription = m_teamSimpleDescription,
                    m_teamIconAssetPath = m_teamIconSprite != null
                        ? AssetDatabase.GetAssetPath(m_teamIconSprite)
                        : string.Empty,
                    m_participantName = participantName,
                    m_participantFolderPath = participantFolderPath,
                    m_partyScriptPath = partyScriptPath,
                    m_characterScriptPath = characterScriptPath,
                    m_partyPrefabPath = partyPrefabPath,
                    m_characterPrefabPath = characterPrefabPath
                };

                SessionState.SetString(
                    PendingCreationSessionKey,
                    JsonUtility.ToJson(pendingData));

                AssetDatabase.Refresh();
                CompilationPipeline.RequestScriptCompilation();

                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    "参加者用スクリプトを作成しました。\n"
                    + "Unityのコンパイル完了後にPrefabを自動作成します。",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "挑戦者作成エラー",
                    "作成中にエラーが発生しました。\n\n"
                    + exception.Message,
                    "OK");
            }
        }

        private static bool ValidateBaseAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PartyBasePrefabPath) == null)
            {
                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    "Party用ベースPrefabが見つかりません。\n\n"
                    + PartyBasePrefabPath,
                    "OK");

                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(CharacterBasePrefabPath) == null)
            {
                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    "Character用ベースPrefabが見つかりません。\n\n"
                    + CharacterBasePrefabPath,
                    "OK");

                return false;
            }

            if (!File.Exists(PartyTemplatePath)
                || !File.Exists(CharacterTemplatePath))
            {
                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    "参加者用スクリプトテンプレートが見つかりません。\n\n"
                    + PartyTemplatePath + "\n"
                    + CharacterTemplatePath,
                    "OK");

                return false;
            }

            return true;
        }

        private static void CreateParticipantScript(
            string templatePath,
            string destinationPath,
            string participantName)
        {
            string source = File.ReadAllText(templatePath, Encoding.UTF8);
            source = source.Replace("#PARTICIPANT_NAME#", participantName);

            File.WriteAllText(
                destinationPath,
                source,
                new UTF8Encoding(false));
        }

        [DidReloadScripts]
        private static void CreatePrefabsAfterScriptCompilation()
        {
            string json = SessionState.GetString(
                PendingCreationSessionKey,
                string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            SessionState.EraseString(PendingCreationSessionKey);

            var data = JsonUtility.FromJson<PendingCreationData>(json);

            if (data == null)
            {
                return;
            }

            try
            {
                if (!AssetDatabase.CopyAsset(
                        CharacterBasePrefabPath,
                        data.m_characterPrefabPath))
                {
                    throw new InvalidOperationException(
                        "Character Prefabの複製に失敗しました。");
                }

                if (!AssetDatabase.CopyAsset(
                        PartyBasePrefabPath,
                        data.m_partyPrefabPath))
                {
                    throw new InvalidOperationException(
                        "Party Prefabの複製に失敗しました。");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Type characterType = GetScriptClass(data.m_characterScriptPath);

                if (characterType == null
                    || !typeof(ComCharacterBase).IsAssignableFrom(characterType))
                {
                    throw new InvalidOperationException(
                        "生成したCharacterクラスを取得できません。");
                }

                CreateCharacterComponent(
                    data.m_characterPrefabPath,
                    characterType);

                TryCreatePartyPrefabAfterCharacterPrefabReady(
                    data,
                    0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "挑戦者作成エラー",
                    "Prefabの作成中にエラーが発生しました。\n\n"
                    + exception.Message,
                    "OK");
            }
        }

        private static Type GetScriptClass(string scriptPath)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

            if (script == null)
            {
                return null;
            }

            return script.GetClass();
        }

        private static void CreateCharacterComponent(
            string characterPrefabPath,
            Type characterType)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                characterPrefabPath);

            try
            {
                if (root.GetComponent<ComCharacterBase>() != null)
                {
                    throw new InvalidOperationException(
                        "Character用ベースPrefabにはComCharacterBaseを"
                        + "事前アタッチしないでください。");
                }

                var component =
                    root.AddComponent(characterType) as ComCharacterBase;

                if (component == null)
                {
                    throw new InvalidOperationException(
                        "Characterコンポーネントの追加に失敗しました。\n"
                        + $"型: {characterType.FullName}");
                }

                bool savedSuccessfully;

                PrefabUtility.SaveAsPrefabAsset(
                    root,
                    characterPrefabPath,
                    out savedSuccessfully);

                if (!savedSuccessfully)
                {
                    throw new InvalidOperationException(
                        "Character Prefabの保存に失敗しました。\n"
                        + characterPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }



        private static T GetPrefabComponent<T>(string prefabPath)
            where T : Component
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                prefabPath);

            if (prefab == null)
            {
                return null;
            }

            return prefab.GetComponentInChildren<T>(true);
        }


        private static void TryCreatePartyPrefabAfterCharacterPrefabReady(
            PendingCreationData data,
            int retryCount)
        {
            try
            {
                AssetDatabase.ImportAsset(
                    data.m_characterPrefabPath,
                    ImportAssetOptions.ForceSynchronousImport);

                ComCharacterBase characterPrefabComponent =
                    GetPrefabComponent<ComCharacterBase>(
                        data.m_characterPrefabPath);

                if (characterPrefabComponent == null)
                {
                    if (retryCount >= CharacterPrefabReadyRetryLimit)
                    {
                        throw new InvalidOperationException(
                            "Character Prefabの保存後、ComCharacterBaseを"
                            + "取得できる状態になるまで待機しましたが、"
                            + "再試行回数の上限に達しました。\n\n"
                            + $"Prefab: {data.m_characterPrefabPath}\n"
                            + $"再試行回数: {CharacterPrefabReadyRetryLimit}");
                    }

                    int nextRetryCount = retryCount + 1;

                    EditorApplication.delayCall += () =>
                    {
                        TryCreatePartyPrefabAfterCharacterPrefabReady(
                            data,
                            nextRetryCount);
                    };

                    return;
                }

                Type partyType = GetScriptClass(data.m_partyScriptPath);

                if (partyType == null
                    || !typeof(ComPartyBase).IsAssignableFrom(partyType))
                {
                    throw new InvalidOperationException(
                        "生成したPartyクラスを取得できません。");
                }

                CreatePartyComponent(
                    data.m_partyPrefabPath,
                    partyType,
                    characterPrefabComponent,
                    data);

                var partyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    data.m_partyPrefabPath);

                if (partyPrefab == null)
                {
                    throw new InvalidOperationException(
                        "作成後のParty Prefabを読み込めません。\n"
                        + data.m_partyPrefabPath);
                }

                var partyComponent =
                    partyPrefab.GetComponentInChildren<ComPartyBase>(true);

                if (partyComponent == null)
                {
                    throw new InvalidOperationException(
                        "作成後のParty Prefab上でComPartyBaseを取得できません。\n"
                        + data.m_partyPrefabPath);
                }

                RegisterPartyPrefabToBattleRegistry(partyComponent);

                Selection.activeObject = partyPrefab;
                EditorGUIUtility.PingObject(partyPrefab);
                AssetDatabase.OpenAsset(partyPrefab);

                EditorUtility.DisplayDialog(
                    "挑戦者作成",
                    $"受付番号:{data.m_connpassId} のパーティーを作成しました。\n"
                    + "試合参加リストの先頭へ登録しました。\n\n"
                    + data.m_participantFolderPath,
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                EditorUtility.DisplayDialog(
                    "挑戦者作成エラー",
                    "Prefabの作成中にエラーが発生しました。\n\n"
                    + exception.Message,
                    "OK");
            }
        }

        private static void RegisterPartyPrefabToBattleRegistry(
            ComPartyBase partyPrefab)
        {
            var registry = AssetDatabase.LoadAssetAtPath<BattlePartyRegistry>(
                BattlePartyRegistryAssetPath);

            if (registry == null)
            {
                throw new InvalidOperationException(
                    "試合参加リストのScriptableObjectが見つかりません。\n"
                    + BattlePartyRegistryAssetPath);
            }

            registry.RegisterPartyPrefabToFrontByEditor(partyPrefab);

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }



        private static void CreatePartyComponent(
            string partyPrefabPath,
            Type partyType,
            ComCharacterBase characterPrefabComponent,
            PendingCreationData data)
        {
            if (characterPrefabComponent == null)
            {
                throw new InvalidOperationException(
                    "Character Prefab上のComCharacterBaseを取得できません。");
            }

            GameObject root = PrefabUtility.LoadPrefabContents(partyPrefabPath);

            try
            {
                if (root.GetComponent<ComPartyBase>() != null)
                {
                    throw new InvalidOperationException(
                        "Party用ベースPrefabにはComPartyBaseを"
                        + "事前アタッチしないでください。");
                }

                var component = root.AddComponent(partyType) as ComPartyBase;

                if (component == null)
                {
                    throw new InvalidOperationException(
                        "Partyコンポーネントの追加に失敗しました。");
                }

                ConfigurePartyComponent(
                    component,
                    characterPrefabComponent,
                    data);

                PrefabUtility.SaveAsPrefabAsset(root, partyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigurePartyComponent(
            ComPartyBase party,
            ComCharacterBase characterPrefab,
            PendingCreationData data)
        {
            var serializedObject = new SerializedObject(party);

            serializedObject.FindProperty("m_memberPrefabs")
                .objectReferenceValue = characterPrefab;

            serializedObject.FindProperty("m_creatorDisplayName")
                .stringValue = data.m_creatorDisplayName;

            serializedObject.FindProperty("m_teamDisplayName")
                .stringValue = data.m_teamDisplayName;

            serializedObject.FindProperty("m_teamSimpleDescription")
                .stringValue = data.m_teamSimpleDescription;

            var teamIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                data.m_teamIconAssetPath);

            serializedObject.FindProperty("m_teamIconSprite")
                .objectReferenceValue = teamIconSprite;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(party);
        }



        private static void EnsureAssetFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            string[] parts = assetFolderPath.Split('/');

            if (parts.Length == 0 || parts[0] != "Assets")
            {
                throw new InvalidOperationException(
                    "Assets配下のパスを指定してください: "
                    + assetFolderPath);
            }

            string currentPath = "Assets";

            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];

                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
                }

                currentPath = nextPath;
            }
        }
    }
}
#endif