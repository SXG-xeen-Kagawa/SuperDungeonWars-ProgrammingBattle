using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterMannequinReplacer : MonoBehaviour
{
    [SerializeField] private string m_mannequinRootName = "Y Bot";

    public bool ReplaceMannequin(
        ComCharacterBase character,
        Transform systemMannequin)
    {
        if (character == null)
        {
            return false;
        }

        if (systemMannequin == null)
        {
            Debug.LogError(
                "CharacterMannequinReplacer: "
                + "Runtime Character 側の System Y Bot が未設定です。"
                + " Character="
                + character.name,
                character);

            return false;
        }

        Transform submittedMannequin =
            FindDescendantByName(
                character.transform,
                m_mannequinRootName);

        if (submittedMannequin == null)
        {
            Debug.LogWarning(
                "CharacterMannequinReplacer: 提出キャラクター内に "
                + "\"" + m_mannequinRootName + "\" がありません。"
                + "アクセサリー移植は行いません。"
                + " Character="
                + character.name,
                character);

            return true;
        }

        bool canTransferAccessories =
            DoesSubmittedMannequinContainSystemTree(
                submittedMannequin,
                systemMannequin);

        if (canTransferAccessories)
        {
            TransferAdditionalObjects(
                submittedMannequin,
                systemMannequin,
                submittedMannequin,
                systemMannequin,
                character);
        }
        else
        {
            Debug.LogWarning(
                "CharacterMannequinReplacer: 提出側 Y Bot の骨格階層が "
                + "Runtime Character 側の System Y Bot と一致しません。"
                + "Runtime Character の生成は続行し、"
                + "アクセサリー移植のみスキップします。"
                + " Character="
                + character.name,
                character);
        }

        // Destroy() はフレーム末に実行されるため、先にHierarchyから外す。
        // これにより同フレーム中の検索で提出側 Y Bot が混入しない。
        submittedMannequin.SetParent(null, true);
        submittedMannequin.gameObject.SetActive(false);
        Destroy(submittedMannequin.gameObject);

        return true;
    }

    private bool DoesSubmittedMannequinContainSystemTree(
        Transform submittedMannequin,
        Transform systemMannequin)
    {
        List<Transform> systemTransforms =
            new List<Transform>();

        CollectTransforms(
            systemMannequin,
            systemTransforms);

        for (int i = 0; i < systemTransforms.Count; i++)
        {
            Transform systemTransform =
                systemTransforms[i];

            string relativePath = GetRelativePath(
                systemMannequin,
                systemTransform);

            Transform submittedTransform =
                FindByRelativePath(
                    submittedMannequin,
                    relativePath);

            if (submittedTransform == null)
            {
                Debug.LogWarning(
                    "CharacterMannequinReplacer: 提出側に必要な骨格ノードがありません。"
                    + " MissingPath="
                    + m_mannequinRootName
                    + "/"
                    + relativePath,
                    submittedMannequin);

                return false;
            }
        }

        return true;
    }

    private void TransferAdditionalObjects(
        Transform submittedCurrent,
        Transform systemCurrent,
        Transform submittedMannequin,
        Transform systemMannequin,
        ComCharacterBase character)
    {
        for (int i = 0;
             i < submittedCurrent.childCount;
             i++)
        {
            Transform submittedChild =
                submittedCurrent.GetChild(i);

            string childRelativePath = GetRelativePath(
                submittedMannequin,
                submittedChild);

            Transform correspondingSystemTransform =
                FindByRelativePath(
                    systemMannequin,
                    childRelativePath);

            if (correspondingSystemTransform != null)
            {
                TransferAdditionalObjects(
                    submittedChild,
                    correspondingSystemTransform,
                    submittedMannequin,
                    systemMannequin,
                    character);

                continue;
            }

            CopyAccessoryObject(
                submittedChild,
                systemCurrent,
                submittedMannequin,
                systemMannequin,
                character);
        }
    }

    private void CopyAccessoryObject(
        Transform submittedAccessory,
        Transform destinationParent,
        Transform submittedMannequin,
        Transform systemMannequin,
        ComCharacterBase character)
    {
        GameObject copiedAccessory = Instantiate(
            submittedAccessory.gameObject,
            destinationParent);

        copiedAccessory.name = submittedAccessory.name;

        Transform copiedTransform =
            copiedAccessory.transform;

        copiedTransform.localPosition =
            submittedAccessory.localPosition;

        copiedTransform.localRotation =
            submittedAccessory.localRotation;

        copiedTransform.localScale =
            submittedAccessory.localScale;

        RemapSkinnedMeshBones(
            submittedAccessory,
            copiedTransform,
            submittedMannequin,
            systemMannequin);

        DisableAccessoryPhysicsAndScripts(
            copiedAccessory);

        Debug.Log(
            "CharacterMannequinReplacer: アクセサリーを移植しました。"
            + " Character="
            + character.name
            + " / SourcePath="
            + m_mannequinRootName
            + "/"
            + GetRelativePath(
                submittedMannequin,
                submittedAccessory)
            + " / DestinationParent="
            + GetRelativePath(
                systemMannequin,
                destinationParent),
            character);
    }

    private void RemapSkinnedMeshBones(
        Transform submittedAccessoryRoot,
        Transform copiedAccessoryRoot,
        Transform submittedMannequin,
        Transform systemMannequin)
    {
        SkinnedMeshRenderer[] submittedRenderers =
            submittedAccessoryRoot.GetComponentsInChildren<
                SkinnedMeshRenderer>(true);

        SkinnedMeshRenderer[] copiedRenderers =
            copiedAccessoryRoot.GetComponentsInChildren<
                SkinnedMeshRenderer>(true);

        int rendererCount = Mathf.Min(
            submittedRenderers.Length,
            copiedRenderers.Length);

        for (int rendererIndex = 0;
             rendererIndex < rendererCount;
             rendererIndex++)
        {
            SkinnedMeshRenderer submittedRenderer =
                submittedRenderers[rendererIndex];

            SkinnedMeshRenderer copiedRenderer =
                copiedRenderers[rendererIndex];

            copiedRenderer.rootBone =
                FindRemappedBone(
                    submittedRenderer.rootBone,
                    submittedAccessoryRoot,
                    copiedAccessoryRoot,
                    submittedMannequin,
                    systemMannequin);

            Transform[] submittedBones =
                submittedRenderer.bones;

            Transform[] copiedBones =
                new Transform[submittedBones.Length];

            for (int boneIndex = 0;
                 boneIndex < submittedBones.Length;
                 boneIndex++)
            {
                copiedBones[boneIndex] =
                    FindRemappedBone(
                        submittedBones[boneIndex],
                        submittedAccessoryRoot,
                        copiedAccessoryRoot,
                        submittedMannequin,
                        systemMannequin);
            }

            copiedRenderer.bones = copiedBones;
        }
    }

    private Transform FindRemappedBone(
        Transform submittedBone,
        Transform submittedAccessoryRoot,
        Transform copiedAccessoryRoot,
        Transform submittedMannequin,
        Transform systemMannequin)
    {
        if (submittedBone == null)
        {
            return null;
        }

        if (IsSameOrChildOf(
                submittedBone,
                submittedAccessoryRoot))
        {
            string accessoryPath = GetRelativePath(
                submittedAccessoryRoot,
                submittedBone);

            return FindByRelativePath(
                copiedAccessoryRoot,
                accessoryPath);
        }

        if (IsSameOrChildOf(
                submittedBone,
                submittedMannequin))
        {
            string mannequinPath = GetRelativePath(
                submittedMannequin,
                submittedBone);

            return FindByRelativePath(
                systemMannequin,
                mannequinPath);
        }

        return submittedBone;
    }

    private void DisableAccessoryPhysicsAndScripts(
        GameObject copiedAccessory)
    {
        Collider[] colliders =
            copiedAccessory.GetComponentsInChildren<
                Collider>(true);

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Rigidbody[] rigidbodies =
            copiedAccessory.GetComponentsInChildren<
                Rigidbody>(true);

        for (int i = 0; i < rigidbodies.Length; i++)
        {
            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }

        Joint[] joints =
            copiedAccessory.GetComponentsInChildren<
                Joint>(true);

        for (int i = 0; i < joints.Length; i++)
        {
            Destroy(joints[i]);
        }

        MonoBehaviour[] behaviours =
            copiedAccessory.GetComponentsInChildren<
                MonoBehaviour>(true);

        for (int i = 0; i < behaviours.Length; i++)
        {
            behaviours[i].enabled = false;
        }
    }

    private static Transform FindDescendantByName(
        Transform root,
        string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result =
                FindDescendantByName(
                    root.GetChild(i),
                    objectName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private static void CollectTransforms(
        Transform root,
        List<Transform> result)
    {
        result.Add(root);

        for (int i = 0; i < root.childCount; i++)
        {
            CollectTransforms(
                root.GetChild(i),
                result);
        }
    }

    private static Transform FindByRelativePath(
        Transform root,
        string relativePath)
    {
        if (root == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(relativePath))
        {
            return root;
        }

        return root.Find(relativePath);
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

    private static bool IsSameOrChildOf(
        Transform target,
        Transform parent)
    {
        return target == parent
            || (target != null
                && parent != null
                && target.IsChildOf(parent));
    }
}