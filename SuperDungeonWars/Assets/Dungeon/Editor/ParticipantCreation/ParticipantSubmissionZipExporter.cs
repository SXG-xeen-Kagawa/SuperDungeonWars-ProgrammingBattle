#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ParticipantSubmissionZipExporter
{
    private const string ParticipantRootPath = "Assets/Participants";

    private const string MemberPrefabsFieldName = "m_memberPrefabs";
    private const string CreatorDisplayNameFieldName =
        "m_creatorDisplayName";
    private const string TeamDisplayNameFieldName =
        "m_teamDisplayName";
    private const string TeamSimpleDescriptionFieldName =
        "m_teamSimpleDescription";
    private const string TeamIconSpriteFieldName =
        "m_teamIconSprite";

    private const string CharacterBasePrefabAssetPath =
        "Assets/Dungeon/Editor/ParticipantCreation/Prefab/"
        + "ParticipantCharacterBase.prefab";

    private const string OfficialAccessoryFolderPath =
       "Assets/Dungeon/GameAssets/Models/Accessories";

    private const string MannequinRootName = "Y Bot";
    private const string SubmissionZipFolderName = "提出用ZIP";

    public static void CreateSubmissionZip(
        ComPartyBase party,
        string partyPrefabAssetPath)
    {
        if (party == null)
        {
            EditorUtility.DisplayDialog(
                "提出用ZIPを作成できません",
                "Party Prefabを取得できませんでした。",
                "OK");

            return;
        }

        PrefabStage prefabStage =
            PrefabStageUtility.GetCurrentPrefabStage();

        if (prefabStage != null
            && prefabStage.assetPath == partyPrefabAssetPath
            && prefabStage.scene.isDirty)
        {
            EditorUtility.DisplayDialog(
                "Prefabを保存してください",
                "Party Prefabに未保存の変更があります。\n\n"
                + "Prefab Mode右上の Save を押して保存してから、"
                + "もう一度「提出用ZIPを作成」を実行してください。",
                "OK");

            return;
        }


        if (string.IsNullOrEmpty(partyPrefabAssetPath))
        {
            EditorUtility.DisplayDialog(
                "提出用ZIPを作成できません",
                "Party Prefabのアセットパスを取得できませんでした。",
                "OK");

            return;
        }

        EditorUtility.DisplayDialog(
            "保存を確認してください",
            "提出用ZIPには、ディスクへ保存済みのファイルが"
            + "含まれます。\n\n"
            + "Prefab Mode右上の Save でParty／Character Prefabを"
            + "保存してから、ZIPを作成してください。",
            "OK");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        List<string> validationErrors =
            GetValidationErrors(
                party,
                partyPrefabAssetPath);

        if (0 < validationErrors.Count)
        {
            EditorUtility.DisplayDialog(
                "提出用ZIPを作成できません",
                "提出前検査で問題が見つかりました。\n"
                + "問題を修正してから、もう一度作成してください。\n\n"
                + string.Join("\n\n", validationErrors),
                "OK");

            return;
        }

        string participantFolderPath =
            GetDirectoryName(partyPrefabAssetPath);

        string participantName =
            Path.GetFileName(participantFolderPath);

        string projectRootPath =
            Directory.GetParent(Application.dataPath).FullName;

        string participantFolderFullPath =
            Path.Combine(
                projectRootPath,
                participantFolderPath);

        string submissionFolderFullPath =
            Path.Combine(
                projectRootPath,
                SubmissionZipFolderName);

        string zipFullPath =
            Path.Combine(
                submissionFolderFullPath,
                participantName + ".zip");

        try
        {
            Directory.CreateDirectory(submissionFolderFullPath);

            if (File.Exists(zipFullPath))
            {
                File.Delete(zipFullPath);
            }

            ZipFile.CreateFromDirectory(
                participantFolderFullPath,
                zipFullPath,
                System.IO.Compression.CompressionLevel.Optimal,
                true);
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog(
                "提出用ZIPを作成できません",
                "ZIP圧縮中にエラーが発生しました。\n\n"
                + exception.Message,
                "OK");

            return;
        }

        int result = EditorUtility.DisplayDialogComplex(
            "提出用ZIPを作成しました",
            "提出用ZIPを作成しました。\n\n"
            + "出力先:\n"
            + zipFullPath,
            "出力先を開く",
            "OK",
            string.Empty);

        if (result == 0)
        {
            EditorUtility.RevealInFinder(zipFullPath);
        }
    }

    private static List<string> GetValidationErrors(
        ComPartyBase party,
        string partyPrefabAssetPath)
    {
        List<string> errors = new List<string>();

        string participantFolderPath =
            GetDirectoryName(partyPrefabAssetPath);

        if (string.IsNullOrEmpty(participantFolderPath))
        {
            errors.Add(
                "Party Prefabの参加者フォルダを取得できません。");

            return errors;
        }

        string participantFolderParentPath =
            GetDirectoryName(participantFolderPath);

        if (participantFolderParentPath != ParticipantRootPath)
        {
            errors.Add(
                "Party Prefabは参加者フォルダ直下に置いてください。\n"
                + "現在: " + partyPrefabAssetPath + "\n"
                + "期待する形式: "
                + ParticipantRootPath
                + "/Partyxxxxxxx/PartyxxxxxxxParty.prefab");
        }

        if (!IsAssetPathInsideFolder(
                partyPrefabAssetPath,
                participantFolderPath))
        {
            errors.Add(
                "Party Prefabが参加者フォルダ内にありません。\n"
                + partyPrefabAssetPath);
        }

        ValidateRequiredPartyInformation(
            party,
            participantFolderPath,
            errors);

        ComCharacterBase characterPrefab;

        if (!TryGetCharacterPrefab(
                party,
                out characterPrefab))
        {
            errors.Add(
                "Party PrefabにCharacter Prefabが設定されていません。");

            return errors;
        }

        string characterPrefabPath =
            AssetDatabase.GetAssetPath(characterPrefab.gameObject);

        if (string.IsNullOrEmpty(characterPrefabPath))
        {
            errors.Add(
                "Party Prefabが参照しているCharacter Prefabの"
                + "アセットパスを取得できません。");

            return errors;
        }

        if (GetDirectoryName(characterPrefabPath)
            != participantFolderPath)
        {
            errors.Add(
                "Party Prefabが参照するCharacter Prefabは、"
                + "同じ参加者フォルダ直下に置いてください。\n"
                + "現在: " + characterPrefabPath);
        }

        ValidateCharacterPrefab(
            characterPrefabPath,
            participantFolderPath,
            errors);

        return errors;
    }

    private static void ValidateRequiredPartyInformation(
        ComPartyBase party,
        string participantFolderPath,
        List<string> errors)
    {
        SerializedObject serializedParty =
            new SerializedObject(party);

        ValidateRequiredStringProperty(
            serializedParty,
            CreatorDisplayNameFieldName,
            "開発者名",
            errors);

        ValidateRequiredStringProperty(
            serializedParty,
            TeamDisplayNameFieldName,
            "パーティー名",
            errors);

        ValidateRequiredStringProperty(
            serializedParty,
            TeamSimpleDescriptionFieldName,
            "チーム紹介",
            errors);

        SerializedProperty teamIconProperty =
            serializedParty.FindProperty(
                TeamIconSpriteFieldName);

        if (teamIconProperty == null)
        {
            errors.Add(
                "チームアイコンの設定項目を取得できません。\n"
                + "フィールド名: "
                + TeamIconSpriteFieldName);

            return;
        }

        UnityEngine.Object teamIcon =
            teamIconProperty.objectReferenceValue;

        if (teamIcon == null)
        {
            return;
        }

        string teamIconAssetPath =
            AssetDatabase.GetAssetPath(teamIcon);

        if (string.IsNullOrEmpty(teamIconAssetPath)
            || !IsAssetPathInsideFolder(
                teamIconAssetPath,
                participantFolderPath))
        {
            errors.Add(
                "チームアイコンは参加者フォルダ内の画像を"
                + "設定してください。\n"
                + "現在: "
                + (string.IsNullOrEmpty(teamIconAssetPath)
                    ? "アセットパス不明"
                    : teamIconAssetPath));
        }
    }

    private static void ValidateRequiredStringProperty(
        SerializedObject serializedObject,
        string propertyName,
        string displayName,
        List<string> errors)
    {
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);

        if (property == null)
        {
            errors.Add(
                displayName
                + "の設定項目を取得できません。\n"
                + "フィールド名: "
                + propertyName);

            return;
        }

        if (string.IsNullOrWhiteSpace(property.stringValue))
        {
            errors.Add(
                displayName + "が未入力です。");
        }
    }

    private static void ValidateCharacterPrefab(
        string characterPrefabPath,
        string participantFolderPath,
        List<string> errors)
    {
        if (GetDirectoryName(characterPrefabPath)
            != participantFolderPath)
        {
            return;
        }

        GameObject characterPrefabAsset =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                characterPrefabPath);

        if (characterPrefabAsset == null)
        {
            errors.Add(
                "Character Prefabを読み込めません。\n"
                + characterPrefabPath);

            return;
        }

        ComCharacterBase character =
            characterPrefabAsset.GetComponent<ComCharacterBase>();

        if (character == null)
        {
            errors.Add(
                "Character PrefabにComCharacterBaseがありません。\n"
                + characterPrefabPath);

            return;
        }

        ValidateForbiddenCharacterComponents(
            character,
            errors);

        ValidateCharacterDependencies(
            characterPrefabPath,
            participantFolderPath,
            errors);
    }

    private static void ValidateForbiddenCharacterComponents(
        ComCharacterBase character,
        List<string> errors)
    {
        Transform mannequinRoot =
            FindDescendantByName(
                character.transform,
                MannequinRootName);

        if (mannequinRoot == null)
        {
            errors.Add(
                "Character Prefab内に\""
                + MannequinRootName
                + "\"がありません。");

            return;
        }

        GameObject baseCharacterPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterBasePrefabAssetPath);

        if (baseCharacterPrefab == null)
        {
            errors.Add(
                "禁止コンポーネント検査用の基準Prefabがありません。\n"
                + CharacterBasePrefabAssetPath);

            return;
        }

        Transform baseMannequinRoot =
            FindDescendantByName(
                baseCharacterPrefab.transform,
                MannequinRootName);

        if (baseMannequinRoot == null)
        {
            errors.Add(
                "基準Prefab内に\""
                + MannequinRootName
                + "\"がありません。");

            return;
        }

        Dictionary<string, int> allowedComponentCounts =
            CreateComponentCountDictionary(
                baseMannequinRoot);

        List<string> invalidComponents =
            GetInvalidComponentMessages(
                mannequinRoot,
                allowedComponentCounts);

        for (int i = 0; i < invalidComponents.Count; i++)
        {
            errors.Add(
                "Character Prefabに禁止コンポーネントがあります。\n"
                + invalidComponents[i]);
        }
    }

    private static void ValidateCharacterDependencies(
        string characterPrefabPath,
        string participantFolderPath,
        List<string> errors)
    {
        string[] baseDependencyPaths =
            AssetDatabase.GetDependencies(
                CharacterBasePrefabAssetPath,
                true);

        HashSet<string> allowedBaseDependencyPaths =
            new HashSet<string>(baseDependencyPaths);

        string[] characterDependencyPaths =
            AssetDatabase.GetDependencies(
                characterPrefabPath,
                true);

        for (int i = 0;
            i < characterDependencyPaths.Length;
            i++)
        {
            string dependencyPath =
                characterDependencyPaths[i];

            if (dependencyPath == characterPrefabPath)
            {
                continue;
            }

            // Unity Package Manager管理のShader・Packageは、
            // 参加者提出ZIPへ含める必要がないため許可する。
            if (dependencyPath.StartsWith(
                    "Packages/",
                    StringComparison.Ordinal))
            {
                continue;
            }

            // Character基準Prefabが元から使用している依存は許可する。
            if (allowedBaseDependencyPaths.Contains(
                    dependencyPath))
            {
                continue;
            }

            // 参加者本人が用意したアセットは参加者フォルダ内なら許可する。
            if (IsAssetPathInsideFolder(
                    dependencyPath,
                    participantFolderPath))
            {
                continue;
            }

            // 運営が用意したアクセサリと、そのPrefab・FBX・
            // Material・Textureなどの依存アセットは許可する。
            if (IsAssetPathInsideFolder(
                    dependencyPath,
                    OfficialAccessoryFolderPath))
            {
                continue;
            }

            errors.Add(
                "Character Prefabが参加者フォルダ外の"
                + "追加アセットを参照しています。\n"
                + dependencyPath);
        }
    }

    private static bool TryGetCharacterPrefab(
        ComPartyBase party,
        out ComCharacterBase characterPrefab)
    {
        characterPrefab = null;

        SerializedObject serializedParty =
            new SerializedObject(party);

        SerializedProperty memberPrefabsProperty =
            serializedParty.FindProperty(
                MemberPrefabsFieldName);

        if (memberPrefabsProperty == null)
        {
            return false;
        }

        characterPrefab =
            memberPrefabsProperty.objectReferenceValue
            as ComCharacterBase;

        return characterPrefab != null;
    }

    private static Dictionary<string, int>
        CreateComponentCountDictionary(
            Transform root)
    {
        Dictionary<string, int> componentCounts =
            new Dictionary<string, int>();

        Component[] components =
            root.GetComponentsInChildren<Component>(true);

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];

            if (component == null
                || !IsForbiddenAccessoryComponent(component))
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
                    + GetRelativePathForDisplay(root, null));

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

    private static bool IsForbiddenAccessoryComponent(
        Component component)
    {
        if (component is Collider
            || component is Rigidbody
            || component is Joint
            || component is CharacterController
            || component is Animator
            || component is MonoBehaviour
            || component is Camera
            || component is Light
            || component is AudioSource
            || component is AudioListener
            || component is ParticleSystem
            || component is Cloth)
        {
            return true;
        }

        return component.GetType().FullName
            == "UnityEngine.AI.NavMeshAgent";
    }

    private static string GetComponentKey(
        Transform root,
        Component component)
    {
        return GetRelativePath(root, component.transform)
            + "\n"
            + component.GetType().FullName;
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
        if (root == null || target == null || root == target)
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

        return string.IsNullOrEmpty(relativePath)
            ? root.name
            : relativePath;
    }

    private static string GetDirectoryName(string assetPath)
    {
        return Path.GetDirectoryName(assetPath)
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
            folderPath.Replace("\\", "/").TrimEnd('/');

        return normalizedAssetPath.StartsWith(
            normalizedFolderPath + "/",
            StringComparison.Ordinal);
    }
}

#endif