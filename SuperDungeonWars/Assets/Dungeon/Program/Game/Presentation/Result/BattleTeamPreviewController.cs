using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public enum BattleTeamPreviewPose
{
    Introduction,
    ResultIdle,
    ResultTreasureReact,
    ResultVictory,
    ResultDefeat,
}

public sealed class BattleTeamPreviewController : MonoBehaviour
{
    private const int TeamCount = 4;

    private sealed class MemberRestoreData
    {
        public ComCharacterBase Member;
        public int SystemTeamIndex;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public TreasureChest CarriedTreasureChest;
        public readonly List<GameObjectLayerRestoreData>
            GameObjectLayerRestoreDataList =
                new List<GameObjectLayerRestoreData>();
    }

    private sealed class ResultTreasureSpawnData
    {
        public int TeamIndex;
        public int TreasureIndex;
        public TreasureType TreasureType;
        public TreasureChest TreasureChest;
    }

    private sealed class GameObjectLayerRestoreData
    {
        public GameObject GameObject;
        public int Layer;
    }

    private static readonly Vector3[] s_fallbackMemberOffsets =
    {
        new Vector3(-0.85f, 0.0f,  0.55f),
        new Vector3( 0.85f, 0.0f,  0.55f),
        new Vector3(-0.85f, 0.0f, -0.55f),
        new Vector3( 0.85f, 0.0f, -0.55f),
    };

    [Header("Preview Stage Prefabs")]

    [SerializeField]
    private BattleTeamPreviewStage m_teamPreviewStagePrefab;

    [SerializeField]
    private BattleTeamPreviewStage
        m_introductionPreviewStagePrefab;

    [SerializeField]
    private TreasureChest m_resultTreasureChestPrefab;

    [SerializeField]
    private Vector3 m_stageBaseLocalPosition =
        new Vector3(0.0f, -1000.0f, 0.0f);

    [SerializeField]
    [Min(1.0f)]
    private float m_stageLocalSpacing = 30.0f;

    [SerializeField]
    private string[] m_teamPreviewLayerNames =
    {
        "BattlePreviewStage0",
        "BattlePreviewStage1",
        "BattlePreviewStage2",
        "BattlePreviewStage3",
    };

    [SerializeField]
    private Vector3 m_resultTreasureBaseLocalPosition =
        new Vector3(0.0f, 0.02f, 0.0f);

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureHorizontalRange = 0.65f;

    [Header("Result Treasure Physics Placement")]

    [SerializeField]
    [Min(1)]
    private int m_resultTreasurePlacementCandidateCount = 20;

    [SerializeField]
    [Min(1)]
    private int m_resultTreasurePlacementRetryCount = 6;

    [SerializeField]
    [Min(0.1f)]
    private float m_resultTreasureCastStartHeight = 2.0f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureDropStartClearance = 0.08f;

    [SerializeField]
    [Min(0.1f)]
    private float m_resultTreasureSettleTimeout = 2.0f;

    [SerializeField]
    [Min(0.01f)]
    private float m_resultTreasureSettleHoldSeconds = 0.25f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureSettleLinearSpeed = 0.035f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureSettleAngularSpeed = 0.12f;

    [SerializeField]
    [Min(0.1f)]
    private float m_resultTreasureFallBelowBaseDistance = 2.0f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureCentralChestFrontOffset = 0.42f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureCentralChestClearance = 0.10f;

    [SerializeField]
    private float m_resultTreasureFrontYawOffset = 0.0f;

    [SerializeField]
    [Range(0.0f, 180.0f)]
    private float m_resultTreasureRandomYawRange = 45.0f;

    [SerializeField]
    private GameObject m_resultTreasureChestGlowPrefab;

    [SerializeField]
    private GameObject m_resultTreasureChestOpenPrefab;

    [SerializeField]
    private Vector3 m_resultTreasureEffectLocalPosition =
        new Vector3(0.0f, 0.0f, 0.0f);

    [SerializeField]
    private Vector3 m_resultTreasureEffectLocalEulerAngles =
        new Vector3(0.0f, 0.0f, 0.0f);

    [SerializeField]
    [Min(0.01f)]
    private float m_resultTreasureEffectScale = 1.0f;

    [SerializeField]
    [Min(0.1f)]
    private float m_resultTreasureOpenEffectDestroyDelay = 3.0f;

    [SerializeField]
    private GameObject m_resultTreasureGoldCoinFountainPrefab;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureGoldCoinFountainMinimumDuration =
        0.35f;

    [SerializeField]
    [Min(0.01f)]
    private float m_resultTreasureGoldCoinFountainMaximumDuration =
        1.50f;

    [SerializeField]
    [Min(1)]
    private int m_resultTreasureGoldCoinFountainReferenceScore =
        150;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureGoldCoinFountainDestroyDelay =
        1.00f;

    [Header("Result Treasure Disposal")]

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureDisposeHorizontalSpeed =
        8.0f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureDisposeUpwardSpeed =
        2.5f;

    [SerializeField]
    private float m_resultTreasureDisposeTorqueImpulse =
        3.0f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultTreasureDisposeHideAfterSeconds =
        2.0f;

    private readonly List<BattleTeamPreviewStage>
        m_spawnedPreviewStages =
            new List<BattleTeamPreviewStage>();

    private readonly List<RenderTexture>
        m_runtimeRenderTextures =
            new List<RenderTexture>();

    private readonly List<TreasureChest>
        m_spawnedResultTreasureChests =
            new List<TreasureChest>();

    private readonly Dictionary<int, GameObject>
        m_resultTreasureGlowEffectByTreasureId =
            new Dictionary<int, GameObject>();

    private readonly Dictionary<int, Camera>
        m_resultTreasurePreviewCameraByTreasureId =
            new Dictionary<int, Camera>();

    private readonly List<MemberRestoreData>
        m_memberRestoreDataList =
            new List<MemberRestoreData>();

    private readonly List<ResultTreasureSpawnData>[]
        m_resultTreasureSpawnDataByTeam =
            new List<ResultTreasureSpawnData>[TeamCount];

    private readonly List<int>[]
        m_resultSmallChestRevealTreasureIdsByTeam =
            new List<int>[TeamCount];

    private readonly List<int>[]
        m_resultCentralChestRevealTreasureIdsByTeam =
            new List<int>[TeamCount];

    private int m_resultTreasurePlacementVersion;
    private int m_resultTreasurePlacementRunningCount;
    private bool m_isResultTreasurePlacementCompleted;

    private SimulationMode m_previousPhysicsSimulationMode;

    private bool m_isResultTreasurePhysicsSimulationManual;

    private void Awake()
    {
        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            m_resultTreasureSpawnDataByTeam[teamIndex] =
                new List<ResultTreasureSpawnData>();

            m_resultSmallChestRevealTreasureIdsByTeam[teamIndex] =
                new List<int>();

            m_resultCentralChestRevealTreasureIdsByTeam[teamIndex] =
                new List<int>();
        }

        Hide(false);
    }

    private void OnDestroy()
    {
        Hide(false);
    }

    public void Show(
        IReadOnlyList<ComPartyBase> spawnedParties,
        BattleTeamPreviewPose previewPose)
    {
        Show(
            spawnedParties,
            null,
            previewPose);
    }

    public void Show(
        IReadOnlyList<ComPartyBase> spawnedParties,
        BattleResultData battleResult,
        BattleTeamPreviewPose previewPose)
    {
        Hide(true);

        m_resultTreasurePlacementVersion++;
        m_resultTreasurePlacementRunningCount = 0;
        m_isResultTreasurePlacementCompleted = false;

        ClearResultTreasurePlacementData();

        CreatePreviewStages(
            previewPose);

        int teamCount = Mathf.Min(
            TeamCount,
            spawnedParties != null
                ? spawnedParties.Count
                : 0);

        BeginResultTreasurePhysicsSimulation();

        for (int teamIndex = 0;
             teamIndex < teamCount;
             teamIndex++)
        {
            ComPartyBase party =
                spawnedParties[teamIndex];

            BattleTeamPreviewStage previewStage =
                GetPreviewStageTeamIndex(
                    teamIndex);

            PlacePartyMembers(
                party,
                previewStage,
                teamIndex,
                previewPose);

            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            CreateResultTreasureChests(
                teamIndex,
                teamResult,
                previewStage);
        }

        m_isResultTreasurePlacementCompleted = true;

        EndResultTreasurePhysicsSimulation();
    }

    public void SetPreviewPose(
        BattleTeamPreviewPose previewPose)
    {
        for (int i = 0;
             i < m_memberRestoreDataList.Count;
             i++)
        {
            MemberRestoreData restoreData =
                m_memberRestoreDataList[i];

            if (restoreData == null
                || restoreData.Member == null)
            {
                continue;
            }

            PlayPreviewPose(
                restoreData.Member,
                previewPose);
        }
    }

    public void SetPreviewPoseTeamIndex(
        int systemTeamIndex,
        BattleTeamPreviewPose previewPose)
    {
        for (int i = 0;
             i < m_memberRestoreDataList.Count;
             i++)
        {
            MemberRestoreData restoreData =
                m_memberRestoreDataList[i];

            if (restoreData == null
                || restoreData.Member == null
                || restoreData.SystemTeamIndex
                    != systemTeamIndex)
            {
                continue;
            }

            PlayPreviewPose(
                restoreData.Member,
                previewPose);
        }
    }

    public RenderTexture GetPreviewTextureTeamIndex(
        int systemTeamIndex)
    {
        if (systemTeamIndex < 0
            || systemTeamIndex >= m_runtimeRenderTextures.Count)
        {
            return null;
        }

        return m_runtimeRenderTextures[
            systemTeamIndex];
    }

    public bool IsResultTreasurePlacementCompleted
    {
        get { return m_isResultTreasurePlacementCompleted; }
    }

    public int GetResultTreasureIdByTypeAndOrder(
        int teamIndex,
        TreasureType treasureType,
        int treasureOrder)
    {
        if (teamIndex < 0
            || teamIndex >= TeamCount
            || treasureOrder < 0)
        {
            return -1;
        }

        List<int> treasureIds =
            treasureType == TreasureType.CentralChest
                ? m_resultCentralChestRevealTreasureIdsByTeam[
                    teamIndex]
                : m_resultSmallChestRevealTreasureIdsByTeam[
                    teamIndex];

        if (treasureIds == null
            || treasureOrder >= treasureIds.Count)
        {
            return -1;
        }

        return treasureIds[treasureOrder];
    }

    public bool TryGetResultTreasurePresentationPosition(
        int treasureId,
        out Camera previewCamera,
        out Vector3 treasureWorldPosition)
    {
        previewCamera = null;
        treasureWorldPosition = Vector3.zero;

        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            return false;
        }

        if (!m_resultTreasurePreviewCameraByTreasureId
                .TryGetValue(
                    treasureId,
                    out previewCamera))
        {
            return false;
        }

        if (previewCamera == null)
        {
            return false;
        }

        treasureWorldPosition =
            resultTreasureChest.transform.position;

        return true;
    }

    #region 宝箱が開くときの光のエフェクト

    public bool SetResultTreasureOpened(
        int treasureId,
        bool isOpened)
    {
        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            UnityEngine.Debug.LogWarning(
                "BattleTeamPreviewController: "
                + "リザルト用宝箱が見つかりません。 treasureId="
                + treasureId,
                this);

            return false;
        }

        if (isOpened)
        {
            StopResultTreasureGlow(
                treasureId);

            resultTreasureChest
                .SetOpenedForResultPresentation(
                    true);

            PlayResultTreasureOpenEffect(
                resultTreasureChest);
        }
        else
        {
            StopResultTreasureGlow(
                treasureId);

            resultTreasureChest
                .SetOpenedForResultPresentation(
                    false);
        }

        return true;
    }

    public bool StartResultTreasureGlow(
        int treasureId)
    {
        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            UnityEngine.Debug.LogWarning(
                "BattleTeamPreviewController: "
                + "Glow開始対象のリザルト用宝箱が見つかりません。 treasureId="
                + treasureId,
                this);

            return false;
        }

        StopResultTreasureGlow(
            treasureId);

        if (m_resultTreasureChestGlowPrefab == null)
        {
            return false;
        }

        GameObject glowEffect =
            InstantiateResultTreasureEffect(
                m_resultTreasureChestGlowPrefab,
                resultTreasureChest);

        if (glowEffect == null)
        {
            return false;
        }

        m_resultTreasureGlowEffectByTreasureId[
            treasureId] =
            glowEffect;

        return true;
    }

    private void PlayResultTreasureOpenEffect(
        TreasureChest resultTreasureChest)
    {
        if (resultTreasureChest == null
            || m_resultTreasureChestOpenPrefab == null)
        {
            return;
        }

        GameObject openEffect =
            InstantiateResultTreasureEffect(
                m_resultTreasureChestOpenPrefab,
                resultTreasureChest);

        if (openEffect == null)
        {
            return;
        }

        Destroy(
            openEffect,
            m_resultTreasureOpenEffectDestroyDelay);
    }

    private GameObject InstantiateResultTreasureEffect(
        GameObject effectPrefab,
        TreasureChest resultTreasureChest)
    {
        if (effectPrefab == null
            || resultTreasureChest == null)
        {
            return null;
        }

        GameObject effect =
            Instantiate(
                effectPrefab,
                resultTreasureChest.transform,
                false);

        effect.name =
            effectPrefab.name
            + "_TreasureId"
            + resultTreasureChest.TreasureId;

        effect.transform.localPosition +=
            m_resultTreasureEffectLocalPosition;

        effect.transform.localRotation =
            effect.transform.localRotation
            * Quaternion.Euler(
                m_resultTreasureEffectLocalEulerAngles);

        effect.transform.localScale *=
            m_resultTreasureEffectScale;

        ApplyLayerToHierarchy(
            effect,
            resultTreasureChest.gameObject.layer);

        return effect;
    }

    private void StopResultTreasureGlow(
        int treasureId)
    {
        GameObject glowEffect;

        if (!m_resultTreasureGlowEffectByTreasureId
                .TryGetValue(
                    treasureId,
                    out glowEffect))
        {
            return;
        }

        if (glowEffect != null)
        {
            Destroy(glowEffect);
        }

        m_resultTreasureGlowEffectByTreasureId.Remove(
            treasureId);
    }

    private TreasureChest FindResultTreasureChest(
        int treasureId)
    {
        for (int i = 0;
             i < m_spawnedResultTreasureChests.Count;
             i++)
        {
            TreasureChest resultTreasureChest =
                m_spawnedResultTreasureChests[i];

            if (resultTreasureChest != null
                && resultTreasureChest.TreasureId
                    == treasureId)
            {
                return resultTreasureChest;
            }
        }

        return null;
    }

    public bool SetResultTreasureCoinsVisible(
        int treasureId,
        bool isVisible)
    {
        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            UnityEngine.Debug.LogWarning(
                "BattleTeamPreviewController: "
                + "コイン表示制御対象のリザルト用宝箱が見つかりません。 treasureId="
                + treasureId,
                this);

            return false;
        }

        resultTreasureChest.SetResultCoinsVisible(
            isVisible);

        return true;
    }

    public bool ThrowAwayResultTreasure(
        int treasureId)
    {
        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            UnityEngine.Debug.LogWarning(
                "BattleTeamPreviewController: "
                + "廃棄対象のリザルト用宝箱が見つかりません。 treasureId="
                + treasureId,
                this);

            return false;
        }

        Vector3 throwDirection =
            resultTreasureChest.transform.forward;

        Camera previewCamera;

        if (m_resultTreasurePreviewCameraByTreasureId
                .TryGetValue(
                    treasureId,
                    out previewCamera)
            && previewCamera != null)
        {
            throwDirection =
                (previewCamera.transform.position
                + 1.5f * previewCamera.transform.forward)
                - resultTreasureChest.transform.position;
        }

        throwDirection.y = 0.0f;

        resultTreasureChest
            .ThrowAwayForResultPresentation(
                throwDirection,
                m_resultTreasureDisposeHorizontalSpeed,
                m_resultTreasureDisposeUpwardSpeed,
                m_resultTreasureDisposeTorqueImpulse,
                m_resultTreasureDisposeHideAfterSeconds);

        return true;
    }

    private void DestroyAllResultTreasureGlowEffects()
    {
        foreach (GameObject glowEffect
                 in m_resultTreasureGlowEffectByTreasureId.Values)
        {
            if (glowEffect != null)
            {
                Destroy(glowEffect);
            }
        }

        m_resultTreasureGlowEffectByTreasureId.Clear();
    }

    #endregion

    #region 宝箱が開いたときに噴き出すコイン

    public bool PlayResultTreasureGoldCoinFountain(
        int treasureId,
        int addedScore)
    {
        TreasureChest resultTreasureChest =
            FindResultTreasureChest(
                treasureId);

        if (resultTreasureChest == null)
        {
            UnityEngine.Debug.LogWarning(
                "BattleTeamPreviewController: "
                + "金貨噴水対象のリザルト用宝箱が見つかりません。 treasureId="
                + treasureId,
                this);

            return false;
        }

        if (m_resultTreasureGoldCoinFountainPrefab == null)
        {
            return false;
        }

        GameObject goldCoinFountainEffect =
            InstantiateResultTreasureEffect(
                m_resultTreasureGoldCoinFountainPrefab,
                resultTreasureChest);

        if (goldCoinFountainEffect == null)
        {
            return false;
        }

        StartCoroutine(
            CoPlayResultTreasureGoldCoinFountain(
                goldCoinFountainEffect,
                treasureId,
                addedScore));

        return true;
    }

    private IEnumerator CoPlayResultTreasureGoldCoinFountain(
        GameObject goldCoinFountainEffect,
        int treasureId,
        int addedScore)
    {
        if (goldCoinFountainEffect == null)
        {
            yield break;
        }

        float scoreProgress =
            Mathf.Clamp01(
                (float)Mathf.Max(0, addedScore)
                / Mathf.Max(
                    1,
                    m_resultTreasureGoldCoinFountainReferenceScore));

        float displayDuration =
            Mathf.Lerp(
                m_resultTreasureGoldCoinFountainMinimumDuration,
                m_resultTreasureGoldCoinFountainMaximumDuration,
                scoreProgress);

        ParticleSystem[] particleSystems =
            goldCoinFountainEffect.GetComponentsInChildren<
                ParticleSystem>(
                    true);

        for (int i = 0;
             i < particleSystems.Length;
             i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem != null)
            {
                particleSystem.Play(true);
            }
        }

        if (displayDuration > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                displayDuration);
        }

        for (int i = 0;
             i < particleSystems.Length;
             i++)
        {
            ParticleSystem particleSystem =
                particleSystems[i];

            if (particleSystem != null)
            {
                particleSystem.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting);
            }
        }

        SetResultTreasureCoinsVisible(
            treasureId,
            false);

        ThrowAwayResultTreasure(treasureId);

        if (m_resultTreasureGoldCoinFountainDestroyDelay > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_resultTreasureGoldCoinFountainDestroyDelay);
        }

        if (goldCoinFountainEffect != null)
        {
            Destroy(goldCoinFountainEffect);
        }
    }

    #endregion

    public void Hide(
        bool restoreMembers)
    {
        if (restoreMembers)
        {
            RestoreMembers();
        }

        DestroyResultTreasureChests();
        DestroyPreviewStages();

        m_memberRestoreDataList.Clear();
    }

    private void CreatePreviewStages(
        BattleTeamPreviewPose previewPose)
    {
        BattleTeamPreviewStage previewStagePrefab =
            GetPreviewStagePrefab(
                previewPose);

        if (previewStagePrefab == null)
        {
            UnityEngine.Debug.LogError(
                "BattleTeamPreviewController: "
                + "Preview Stage Prefab が未設定です。 "
                + "previewPose=" + previewPose,
                this);

            return;
        }

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleTeamPreviewStage previewStage =
                Instantiate(
                    previewStagePrefab,
                    transform);

            previewStage.name =
                "TeamPreviewStage_"
                + previewPose
                + "_"
                + teamIndex;

            previewStage.gameObject.SetActive(true);

            previewStage.transform.localPosition =
                m_stageBaseLocalPosition
                + Vector3.right
                * m_stageLocalSpacing
                * teamIndex;

            previewStage.transform.localRotation =
                Quaternion.identity;

            previewStage.transform.localScale =
                Vector3.one;

            int previewLayer =
                GetPreviewLayerTeamIndex(
                    teamIndex);

            ApplyLayerToHierarchy(
                previewStage.gameObject,
                previewLayer);

            ApplyPreviewCameraCullingMask(
                previewStage,
                previewLayer);

            ApplyPreviewLightCullingMask(
                previewStage,
                previewLayer);

            RenderTexture runtimeRenderTexture =
                CreateRuntimeRenderTexture(
                    previewStage,
                    teamIndex);

            m_spawnedPreviewStages.Add(
                previewStage);

            m_runtimeRenderTextures.Add(
                runtimeRenderTexture);
        }
    }

    private BattleTeamPreviewStage GetPreviewStagePrefab(
        BattleTeamPreviewPose previewPose)
    {
        switch (previewPose)
        {
            case BattleTeamPreviewPose.Introduction:
                return m_introductionPreviewStagePrefab;

            case BattleTeamPreviewPose.ResultIdle:
            case BattleTeamPreviewPose.ResultTreasureReact:
            case BattleTeamPreviewPose.ResultVictory:
            case BattleTeamPreviewPose.ResultDefeat:
            default:
                return m_teamPreviewStagePrefab;
        }
    }

    private RenderTexture CreateRuntimeRenderTexture(
        BattleTeamPreviewStage previewStage,
        int teamIndex)
    {
        if (previewStage == null
            || previewStage.PreviewCamera == null)
        {
            return null;
        }

        Camera previewCamera =
            previewStage.PreviewCamera;

        RenderTexture sourceRenderTexture =
            previewCamera.targetTexture;

        if (sourceRenderTexture == null)
        {
            UnityEngine.Debug.LogError(
                "BattleTeamPreviewController: "
                + "TeamPreviewStage Prefab の Camera に "
                + "RenderTexture が設定されていません。 "
                + "teamIndex=" + teamIndex,
                previewStage);

            previewCamera.enabled = false;

            return null;
        }

        RenderTexture runtimeRenderTexture =
            Instantiate(sourceRenderTexture);

        runtimeRenderTexture.name =
            "TeamPreviewRenderTexture_"
            + teamIndex;

        previewCamera.targetTexture =
            runtimeRenderTexture;

        previewCamera.enabled = true;

        return runtimeRenderTexture;
    }

    private void CreateResultTreasureChests(
        int teamIndex,
        TeamBattleResult teamResult,
        BattleTeamPreviewStage previewStage)
    {
        if (m_resultTreasureChestPrefab == null
            || teamResult == null
            || previewStage == null
            || teamIndex < 0
            || teamIndex >= TeamCount)
        {
            return;
        }

        List<ResultTreasureSpawnData> spawnDataList =
            m_resultTreasureSpawnDataByTeam[teamIndex];

        for (int treasureIndex = 0;
             treasureIndex < teamResult.Treasures.Count;
             treasureIndex++)
        {
            ExportedTreasureResult treasureResult =
                teamResult.Treasures[treasureIndex];

            if (treasureResult == null)
            {
                continue;
            }

            TreasureChest resultTreasureChest =
                Instantiate(
                    m_resultTreasureChestPrefab,
                    previewStage.transform);

            resultTreasureChest.name =
                "ResultTreasure_"
                + "Team" + teamIndex
                + "_Index" + treasureIndex
                + "_Id" + treasureResult.TreasureId;

            resultTreasureChest.InitializeForResultPresentation(
                treasureResult.TreasureId,
                treasureResult.TreasureType);

            int previewLayer =
                GetPreviewLayerTeamIndex(teamIndex);

            ApplyLayerToHierarchy(
                resultTreasureChest.gameObject,
                previewLayer);

            resultTreasureChest.transform.localPosition =
                m_resultTreasureBaseLocalPosition
                + Vector3.up * m_resultTreasureCastStartHeight;

            resultTreasureChest.transform.rotation =
                GetResultTreasureWorldRotation(
                    previewStage,
                    resultTreasureChest.transform.position,
                    teamIndex,
                    treasureIndex);

            resultTreasureChest.SetResultPresentationVisible(
                true);

            m_spawnedResultTreasureChests.Add(
                resultTreasureChest);

            spawnDataList.Add(
                new ResultTreasureSpawnData
                {
                    TeamIndex = teamIndex,
                    TreasureIndex = treasureIndex,
                    TreasureType = treasureResult.TreasureType,
                    TreasureChest = resultTreasureChest,
                });

            if (previewStage.PreviewCamera != null)
            {
                m_resultTreasurePreviewCameraByTreasureId[
                    treasureResult.TreasureId] =
                    previewStage.PreviewCamera;
            }
        }

        if (spawnDataList.Count <= 0)
        {
            BuildResultTreasureRevealOrder(teamIndex);
            return;
        }

        PlaceResultTreasureChests(
            teamIndex,
            previewStage,
            m_resultTreasurePlacementVersion);
    }

    private void PlaceResultTreasureChests(
        int teamIndex,
        BattleTeamPreviewStage previewStage,
        int placementVersion)
    {
        List<ResultTreasureSpawnData> spawnDataList =
            m_resultTreasureSpawnDataByTeam[teamIndex];

        bool hasCentralChest = false;

        for (int i = 0; i < spawnDataList.Count; i++)
        {
            ResultTreasureSpawnData spawnData =
                spawnDataList[i];

            if (spawnData == null
                || spawnData.TreasureType
                    != TreasureType.CentralChest)
            {
                continue;
            }

            hasCentralChest = true;

            PlaceSingleResultTreasure(
                spawnData,
                previewStage,
                true,
                -1,
                placementVersion);
        }

        int normalChestOrder = hasCentralChest
            ? 1
            : 0;

        for (int i = 0; i < spawnDataList.Count; i++)
        {
            ResultTreasureSpawnData spawnData =
                spawnDataList[i];

            if (spawnData == null
                || spawnData.TreasureType
                    == TreasureType.CentralChest)
            {
                continue;
            }

            PlaceSingleResultTreasure(
                spawnData,
                previewStage,
                false,
                normalChestOrder,
                placementVersion);

            normalChestOrder++;
        }

        if (placementVersion
            != m_resultTreasurePlacementVersion)
        {
            return;
        }

        BuildResultTreasureRevealOrder(teamIndex);
    }

    private void PlaceSingleResultTreasure(
        ResultTreasureSpawnData spawnData,
        BattleTeamPreviewStage previewStage,
        bool isCentralChest,
        int normalChestOrder,
        int placementVersion)
    {
        if (spawnData == null
            || spawnData.TreasureChest == null
            || previewStage == null)
        {
            return;
        }

        TreasureChest treasureChest =
            spawnData.TreasureChest;

        bool isPlaced = false;

        for (int retryIndex = 0;
             retryIndex < m_resultTreasurePlacementRetryCount;
             retryIndex++)
        {
            if (placementVersion
                != m_resultTreasurePlacementVersion)
            {
                return;
            }

            Vector3 candidateWorldPosition;

            if (!TryFindResultTreasureDropPosition(
                    treasureChest,
                    previewStage,
                    spawnData.TeamIndex,
                    spawnData.TreasureIndex,
                    isCentralChest,
                    normalChestOrder,
                    out candidateWorldPosition))
            {
                continue;
            }

            treasureChest.transform.position =
                candidateWorldPosition;

            Physics.SyncTransforms();

            treasureChest.BeginResultPlacementPhysics();

            bool isStable = false;
            float stableSeconds = 0.0f;
            float elapsedSeconds = 0.0f;

            while (elapsedSeconds
                   < m_resultTreasureSettleTimeout)
            {
                if (placementVersion
                    != m_resultTreasurePlacementVersion)
                {
                    return;
                }

                if (treasureChest == null)
                {
                    return;
                }

                float baseWorldY =
                    previewStage.transform.TransformPoint(
                        m_resultTreasureBaseLocalPosition).y;

                if (treasureChest.transform.position.y
                    < baseWorldY
                        - m_resultTreasureFallBelowBaseDistance)
                {
                    break;
                }

                if (treasureChest
                        .IsResultPlacementStable(
                            m_resultTreasureSettleLinearSpeed,
                            m_resultTreasureSettleAngularSpeed))
                {
                    stableSeconds += Time.fixedDeltaTime;

                    if (stableSeconds
                        >= m_resultTreasureSettleHoldSeconds)
                    {
                        isStable = true;
                        break;
                    }
                }
                else
                {
                    stableSeconds = 0.0f;
                }

                Physics.Simulate(Time.fixedDeltaTime);

                elapsedSeconds += Time.fixedDeltaTime;
            }

            if (isStable)
            {
                treasureChest.FinishResultPlacementPhysics();
                isPlaced = true;
                break;
            }

            treasureChest.CancelResultPlacementPhysics();
        }

        if (isPlaced)
        {
            return;
        }

        treasureChest.CancelResultPlacementPhysics();
        treasureChest.SetResultPresentationVisible(false);

        UnityEngine.Debug.LogWarning(
            "BattleTeamPreviewController: "
            + "リザルト宝箱の物理配置に失敗しました。 "
            + "teamIndex=" + spawnData.TeamIndex
            + ", treasureIndex=" + spawnData.TreasureIndex
            + ", treasureId=" + treasureChest.TreasureId,
            treasureChest);
    }

    private bool TryFindResultTreasureDropPosition(
        TreasureChest treasureChest,
        BattleTeamPreviewStage previewStage,
        int teamIndex,
        int treasureIndex,
        bool isCentralChest,
        int normalChestOrder,
        out Vector3 dropWorldPosition)
    {
        dropWorldPosition = Vector3.zero;

        if (treasureChest == null
            || previewStage == null)
        {
            return false;
        }

        Vector3 colliderCenterOffset;
        Vector3 colliderExtents;
        float castRadius;

        if (!treasureChest
                .TryGetResultPlacementColliderInfo(
                    out colliderCenterOffset,
                    out colliderExtents,
                    out castRadius))
        {
            return false;
        }

        Vector3 baseWorldPosition =
            previewStage.transform.TransformPoint(
                m_resultTreasureBaseLocalPosition);

        int previewLayer =
            GetPreviewLayerTeamIndex(teamIndex);

        int layerMask = 1 << previewLayer;

        Vector3 bestDropPosition = Vector3.zero;
        float lowestSupportY = float.PositiveInfinity;
        bool found = false;

        int candidateCount = isCentralChest
            ? 1
            : Mathf.Max(
                1,
                m_resultTreasurePlacementCandidateCount);

        bool isFirstNormalChest =
            !isCentralChest
            && normalChestOrder == 0;

        for (int candidateIndex = 0;
             candidateIndex < candidateCount;
             candidateIndex++)
        {
            Vector3 horizontalOffset;

            if (isCentralChest)
            {
                horizontalOffset =
                    GetCentralChestHorizontalOffset(
                        previewStage,
                        baseWorldPosition);
            }
            else if (isFirstNormalChest)
            {
                horizontalOffset = Vector3.zero;
            }
            else
            {
                horizontalOffset =
                    GetResultTreasureRearCandidateHorizontalOffset(
                        previewStage,
                        baseWorldPosition,
                        teamIndex,
                        treasureIndex,
                        candidateIndex);
            }

            Vector3 castOrigin =
                baseWorldPosition
                + horizontalOffset
                + Vector3.up
                    * m_resultTreasureCastStartHeight;

            RaycastHit hit;

            float castDistance =
                m_resultTreasureCastStartHeight
                + m_resultTreasureFallBelowBaseDistance
                + colliderExtents.y
                + 1.0f;

            if (!Physics.SphereCast(
                    castOrigin,
                    castRadius,
                    Vector3.down,
                    out hit,
                    castDistance,
                    layerMask,
                    QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            if (hit.collider == null
                || hit.collider.transform.IsChildOf(
                    treasureChest.transform))
            {
                continue;
            }

            Vector3 candidateDropPosition =
                new Vector3(
                    castOrigin.x - colliderCenterOffset.x,
                    hit.point.y
                        - colliderCenterOffset.y
                        + colliderExtents.y
                        + m_resultTreasureDropStartClearance,
                    castOrigin.z - colliderCenterOffset.z);

            if (!isCentralChest
                && IsTooCloseToCentralChest(
                    teamIndex,
                    candidateDropPosition))
            {
                continue;
            }

            if (hit.point.y >= lowestSupportY)
            {
                continue;
            }

            lowestSupportY = hit.point.y;
            bestDropPosition = candidateDropPosition;
            found = true;
        }

        if (!found)
        {
            return false;
        }

        dropWorldPosition = bestDropPosition;
        return true;
    }

    private Vector3 GetCentralChestHorizontalOffset(
        BattleTeamPreviewStage previewStage,
        Vector3 baseWorldPosition)
    {
        Camera previewCamera =
            previewStage != null
                ? previewStage.PreviewCamera
                : null;

        if (previewCamera == null)
        {
            return Vector3.forward
                * m_resultTreasureCentralChestFrontOffset;
        }

        Vector3 cameraToBaseDirection =
            baseWorldPosition
            - previewCamera.transform.position;

        cameraToBaseDirection.y = 0.0f;

        if (cameraToBaseDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward
                * m_resultTreasureCentralChestFrontOffset;
        }

        return -cameraToBaseDirection.normalized
            * m_resultTreasureCentralChestFrontOffset;
    }

    private Vector3 GetResultTreasureRearCandidateHorizontalOffset(
        BattleTeamPreviewStage previewStage,
        Vector3 baseWorldPosition,
        int teamIndex,
        int treasureIndex,
        int candidateIndex)
    {
        int seed =
            teamIndex * 73856093
            ^ treasureIndex * 19349663
            ^ candidateIndex * 83492791;

        System.Random random =
            new System.Random(seed);

        Camera previewCamera =
            previewStage != null
                ? previewStage.PreviewCamera
                : null;

        Vector3 rearDirection = Vector3.forward;

        if (previewCamera != null)
        {
            rearDirection =
                baseWorldPosition
                - previewCamera.transform.position;

            rearDirection.y = 0.0f;

            if (rearDirection.sqrMagnitude > 0.0001f)
            {
                rearDirection.Normalize();
            }
            else
            {
                rearDirection = Vector3.forward;
            }
        }

        Vector3 rightDirection =
            Vector3.Cross(
                Vector3.up,
                rearDirection).normalized;

        float angle =
            Mathf.Lerp(
                -Mathf.PI * 0.5f,
                Mathf.PI * 0.5f,
                (float)random.NextDouble());

        float radius =
            Mathf.Sqrt((float)random.NextDouble())
            * m_resultTreasureHorizontalRange;

        Vector3 direction =
            rearDirection * Mathf.Cos(angle)
            + rightDirection * Mathf.Sin(angle);

        return direction * radius;
    }

    private bool IsTooCloseToCentralChest(
        int teamIndex,
        Vector3 candidateWorldPosition)
    {
        if (teamIndex < 0
            || teamIndex >= TeamCount)
        {
            return false;
        }

        List<ResultTreasureSpawnData> spawnDataList =
            m_resultTreasureSpawnDataByTeam[teamIndex];

        for (int i = 0; i < spawnDataList.Count; i++)
        {
            ResultTreasureSpawnData spawnData =
                spawnDataList[i];

            if (spawnData == null
                || spawnData.TreasureType
                    != TreasureType.CentralChest
                || spawnData.TreasureChest == null)
            {
                continue;
            }

            Vector3 difference =
                candidateWorldPosition
                - spawnData.TreasureChest.transform.position;

            difference.y = 0.0f;

            if (difference.sqrMagnitude
                < m_resultTreasureCentralChestClearance
                    * m_resultTreasureCentralChestClearance)
            {
                return true;
            }
        }

        return false;
    }

    private int GetPreviewStageTeamIndex(
        BattleTeamPreviewStage previewStage)
    {
        for (int teamIndex = 0;
             teamIndex < m_spawnedPreviewStages.Count;
             teamIndex++)
        {
            if (m_spawnedPreviewStages[teamIndex]
                == previewStage)
            {
                return teamIndex;
            }
        }

        return -1;
    }

    private void BuildResultTreasureRevealOrder(
        int teamIndex)
    {
        if (teamIndex < 0
            || teamIndex >= TeamCount)
        {
            return;
        }

        List<int> smallChestIds =
            m_resultSmallChestRevealTreasureIdsByTeam[
                teamIndex];

        List<int> centralChestIds =
            m_resultCentralChestRevealTreasureIdsByTeam[
                teamIndex];

        smallChestIds.Clear();
        centralChestIds.Clear();

        List<ResultTreasureSpawnData> spawnDataList =
            m_resultTreasureSpawnDataByTeam[teamIndex];

        spawnDataList.Sort(
            delegate (
                ResultTreasureSpawnData left,
                ResultTreasureSpawnData right)
            {
                if (left == null || left.TreasureChest == null)
                {
                    return 1;
                }

                if (right == null || right.TreasureChest == null)
                {
                    return -1;
                }

                int heightComparison =
                    right.TreasureChest.transform.position.y
                    .CompareTo(
                        left.TreasureChest.transform.position.y);

                if (heightComparison != 0)
                {
                    return heightComparison;
                }

                return left.TreasureIndex.CompareTo(
                    right.TreasureIndex);
            });

        for (int i = 0; i < spawnDataList.Count; i++)
        {
            ResultTreasureSpawnData spawnData =
                spawnDataList[i];

            if (spawnData == null
                || spawnData.TreasureChest == null)
            {
                continue;
            }

            if (spawnData.TreasureType
                == TreasureType.CentralChest)
            {
                centralChestIds.Add(
                    spawnData.TreasureChest.TreasureId);
            }
            else
            {
                smallChestIds.Add(
                    spawnData.TreasureChest.TreasureId);
            }
        }
    }

    private void ClearResultTreasurePlacementData()
    {
        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            m_resultTreasureSpawnDataByTeam[teamIndex].Clear();

            m_resultSmallChestRevealTreasureIdsByTeam[
                teamIndex].Clear();

            m_resultCentralChestRevealTreasureIdsByTeam[
                teamIndex].Clear();
        }
    }

    private Quaternion GetResultTreasureWorldRotation(
        BattleTeamPreviewStage previewStage,
        Vector3 treasureWorldPosition,
        int teamIndex,
        int treasureIndex)
    {
        Camera previewCamera =
            previewStage != null
                ? previewStage.PreviewCamera
                : null;

        if (previewCamera == null)
        {
            return Quaternion.Euler(
                0.0f,
                m_resultTreasureFrontYawOffset,
                0.0f);
        }

        Vector3 cameraDirection =
            previewCamera.transform.position
            - treasureWorldPosition;

        cameraDirection.y = 0.0f;

        if (cameraDirection.sqrMagnitude <= 0.0001f)
        {
            cameraDirection =
                previewStage.transform.forward;
        }

        float randomYawOffset =
            GetResultTreasureYawOffset(
                teamIndex,
                treasureIndex);

        Quaternion lookRotation =
            Quaternion.LookRotation(
                cameraDirection.normalized,
                Vector3.up);

        return lookRotation
            * Quaternion.Euler(
                0.0f,
                m_resultTreasureFrontYawOffset
                + randomYawOffset,
                0.0f);
    }

    private float GetResultTreasureYawOffset(
        int teamIndex,
        int treasureIndex)
    {
        int seed =
            teamIndex * 73856093
            ^ treasureIndex * 19349663;

        seed = Mathf.Abs(seed);

        float normalizedValue =
            (seed % 10000) / 9999.0f;

        return Mathf.Lerp(
            -m_resultTreasureRandomYawRange,
            m_resultTreasureRandomYawRange,
            normalizedValue);
    }

    private void DestroyResultTreasureChests()
    {
        EndResultTreasurePhysicsSimulation();

        m_resultTreasurePlacementVersion++;
        m_resultTreasurePlacementRunningCount = 0;
        m_isResultTreasurePlacementCompleted = true;

        ClearResultTreasurePlacementData();

        DestroyAllResultTreasureGlowEffects();

        for (int i = 0;
             i < m_spawnedResultTreasureChests.Count;
             i++)
        {
            TreasureChest resultTreasureChest =
                m_spawnedResultTreasureChests[i];

            if (resultTreasureChest != null)
            {
                Destroy(resultTreasureChest.gameObject);
            }
        }

        m_spawnedResultTreasureChests.Clear();
        m_resultTreasurePreviewCameraByTreasureId.Clear();
    }

    private void DestroyPreviewStages()
    {
        for (int i = 0;
             i < m_spawnedPreviewStages.Count;
             i++)
        {
            BattleTeamPreviewStage previewStage =
                m_spawnedPreviewStages[i];

            if (previewStage != null)
            {
                Destroy(previewStage.gameObject);
            }
        }

        m_spawnedPreviewStages.Clear();

        for (int i = 0;
             i < m_runtimeRenderTextures.Count;
             i++)
        {
            RenderTexture runtimeRenderTexture =
                m_runtimeRenderTextures[i];

            if (runtimeRenderTexture != null)
            {
                Destroy(runtimeRenderTexture);
            }
        }

        m_runtimeRenderTextures.Clear();
    }

    private BattleTeamPreviewStage
        GetPreviewStageTeamIndex(
            int systemTeamIndex)
    {
        if (systemTeamIndex < 0
            || systemTeamIndex >= m_spawnedPreviewStages.Count)
        {
            return null;
        }

        return m_spawnedPreviewStages[
            systemTeamIndex];
    }

    private void PlacePartyMembers(
        ComPartyBase party,
        BattleTeamPreviewStage previewStage,
        int teamIndex,
        BattleTeamPreviewPose previewPose)
    {
        if (party == null
            || previewStage == null)
        {
            return;
        }

        int previewLayer =
            GetPreviewLayerTeamIndex(
                teamIndex);

        int memberCount = party.MemberCount;

        for (int memberIndex = 0;
             memberIndex < memberCount;
             memberIndex++)
        {
            ComCharacterBase member = null;
            party.TryGetMember(memberIndex, out member);

            if (member == null)
            {
                continue;
            }

            Transform memberAnchor =
                previewStage.GetMemberAnchor(
                    memberIndex);

            Vector3 worldPosition =
                GetMemberWorldPosition(
                    previewStage,
                    memberAnchor,
                    memberIndex);

            Quaternion worldRotation =
                memberAnchor != null
                    ? memberAnchor.rotation
                    : Quaternion.identity;

            SaveMemberRestoreData(
                member,
                teamIndex);

            ApplyPreviewLayerToMember(
                member,
                previewLayer);

            PrepareMemberForPreview(
                member,
                worldPosition,
                worldRotation,
                previewPose);
        }
    }

    private Vector3 GetMemberWorldPosition(
        BattleTeamPreviewStage previewStage,
        Transform memberAnchor,
        int memberIndex)
    {
        if (memberAnchor != null)
        {
            return memberAnchor.position;
        }

        if (previewStage == null)
        {
            return Vector3.zero;
        }

        if (memberIndex >= 0
            && memberIndex < s_fallbackMemberOffsets.Length)
        {
            return previewStage.transform.TransformPoint(
                s_fallbackMemberOffsets[
                    memberIndex]);
        }

        return previewStage.transform.position;
    }

    private void SaveMemberRestoreData(
        ComCharacterBase member,
        int systemTeamIndex)
    {
        if (member == null)
        {
            return;
        }

        for (int i = 0;
             i < m_memberRestoreDataList.Count;
             i++)
        {
            MemberRestoreData restoreData =
                m_memberRestoreDataList[i];

            if (restoreData != null
                && restoreData.Member == member)
            {
                restoreData.SystemTeamIndex =
                    systemTeamIndex;

                return;
            }
        }

        ExplorerAgent explorerAgent =
            member.GetExplorerAgent() as ExplorerAgent;

        Transform runtimeTransform =
            explorerAgent != null
                ? explorerAgent.GetRuntimeTransform()
                : null;

        MemberRestoreData newRestoreData =
            new MemberRestoreData();

        newRestoreData.Member = member;
        newRestoreData.SystemTeamIndex =
            systemTeamIndex;
        newRestoreData.WorldPosition =
            runtimeTransform != null
                ? runtimeTransform.position
                : member.transform.position;
        newRestoreData.WorldRotation =
            runtimeTransform != null
                ? runtimeTransform.rotation
                : member.transform.rotation;
        newRestoreData.CarriedTreasureChest =
            member.GetCarriedTreasure() as TreasureChest;

        m_memberRestoreDataList.Add(
            newRestoreData);
    }

    private void PrepareMemberForPreview(
        ComCharacterBase member,
        Vector3 worldPosition,
        Quaternion worldRotation,
        BattleTeamPreviewPose previewPose)
    {
        member.SetOrder(
            PartyMemberOrder.CreateStop());

        member.ClearActionLock();
        member.SetKnockedOut(false);

        ExplorerAgent explorerAgent =
            member.GetExplorerAgent() as ExplorerAgent;

        CharacterRagdollController ragdollController =
            explorerAgent != null
                ? explorerAgent.GetCharacterRagdollController()
                : null;

        if (ragdollController != null)
        {
            ragdollController
                .ForceEndRagdollForResultPresentation();
        }

        TreasureChest carriedTreasureChest =
            member.GetCarriedTreasure() as TreasureChest;

        if (carriedTreasureChest != null)
        {
            carriedTreasureChest
                .SetResultPresentationVisible(
                    false);
        }

        if (explorerAgent != null)
        {
            explorerAgent.StopMove();
            explorerAgent.SetPositionToWorld(
                worldPosition);
            explorerAgent.SetRotationToWorld(
                worldRotation);
        }
        else
        {
            member.transform.SetPositionAndRotation(
                worldPosition,
                worldRotation);
        }

        PlayPreviewPose(
            member,
            previewPose);
    }

    private void PlayPreviewPose(
        ComCharacterBase member,
        BattleTeamPreviewPose previewPose)
    {
        if (member == null)
        {
            return;
        }

        ExplorerAgent explorerAgent =
            member.GetExplorerAgent() as ExplorerAgent;

        CharacterAnimationController animationController =
            explorerAgent != null
                ? explorerAgent.GetCharacterAnimationController()
                : null;

        if (animationController == null)
        {
            return;
        }

        animationController
            .BeginTeamPreviewPresentation();

        switch (previewPose)
        {
            case BattleTeamPreviewPose.Introduction:
                animationController
                    .PlayTeamPreviewIntroductionRandomIdle();
                break;

            case BattleTeamPreviewPose.ResultVictory:
                animationController
                    .PlayTeamPreviewVictory();
                break;

            case BattleTeamPreviewPose.ResultDefeat:
                animationController
                    .PlayTeamPreviewDefeat();
                break;

            case BattleTeamPreviewPose.ResultTreasureReact:
                animationController
                    .PlayTeamPreviewTreasureReact();
                break;

            case BattleTeamPreviewPose.ResultIdle:
            default:
                animationController
                    .PlayTeamPreviewIdle();
                break;
        }
    }

    private void RestoreMembers()
    {
        for (int i = 0;
             i < m_memberRestoreDataList.Count;
             i++)
        {
            MemberRestoreData restoreData =
                m_memberRestoreDataList[i];

            if (restoreData == null
                || restoreData.Member == null)
            {
                continue;
            }

            ComCharacterBase member =
                restoreData.Member;

            ExplorerAgent explorerAgent =
                member.GetExplorerAgent() as ExplorerAgent;

            if (explorerAgent != null)
            {
                explorerAgent.StopMove();
                explorerAgent.SetPositionToWorld(
                    restoreData.WorldPosition);
                explorerAgent.SetRotationToWorld(
                    restoreData.WorldRotation);
            }
            else
            {
                member.transform.SetPositionAndRotation(
                    restoreData.WorldPosition,
                    restoreData.WorldRotation);
            }

            if (restoreData.CarriedTreasureChest != null)
            {
                restoreData.CarriedTreasureChest
                    .SetResultPresentationVisible(
                        true);
            }

            CharacterAnimationController animationController =
                explorerAgent != null
                    ? explorerAgent.GetCharacterAnimationController()
                    : null;

            if (animationController != null)
            {
                animationController
                    .EndTeamPreviewPresentation();
            }

            RestoreMemberLayers(restoreData);
        }
    }

    private TeamBattleResult FindTeamResultByTeamIndex(
        BattleResultData battleResult,
        int teamIndex)
    {
        if (battleResult == null)
        {
            return null;
        }

        for (int i = 0;
             i < battleResult.TeamResults.Count;
             i++)
        {
            TeamBattleResult teamResult =
                battleResult.TeamResults[i];

            if (teamResult != null
                && teamResult.TeamIndex == teamIndex)
            {
                return teamResult;
            }
        }

        return null;
    }

    private int GetPreviewLayerTeamIndex(
        int teamIndex)
    {
        if (m_teamPreviewLayerNames == null
            || teamIndex < 0
            || teamIndex >= m_teamPreviewLayerNames.Length)
        {
            UnityEngine.Debug.LogError(
                "BattleTeamPreviewController: "
                + "Preview Layer 名が未設定です。 teamIndex="
                + teamIndex,
                this);

            return 0;
        }

        string layerName =
            m_teamPreviewLayerNames[teamIndex];

        int layer =
            LayerMask.NameToLayer(layerName);

        if (layer < 0)
        {
            UnityEngine.Debug.LogError(
                "BattleTeamPreviewController: "
                + "Layer が見つかりません。 layerName="
                + layerName,
                this);

            return 0;
        }

        return layer;
    }

    private void ApplyPreviewCameraCullingMask(
        BattleTeamPreviewStage previewStage,
        int previewLayer)
    {
        if (previewStage == null
            || previewStage.PreviewCamera == null)
        {
            return;
        }

        previewStage.PreviewCamera.cullingMask =
            1 << previewLayer;
    }

    private void ApplyPreviewLightCullingMask(
        BattleTeamPreviewStage previewStage,
        int previewLayer)
    {
        if (previewStage == null)
        {
            return;
        }

        Light[] lights =
            previewStage.GetComponentsInChildren<Light>(
                true);

        for (int i = 0;
             i < lights.Length;
             i++)
        {
            Light light = lights[i];

            if (light == null)
            {
                continue;
            }

            light.cullingMask =
                1 << previewLayer;
        }
    }

    private void ApplyPreviewLayerToMember(
        ComCharacterBase member,
        int previewLayer)
    {
        if (member == null)
        {
            return;
        }

        MemberRestoreData restoreData =
            FindMemberRestoreData(member);

        if (restoreData == null)
        {
            return;
        }

        ExplorerAgent explorerAgent =
            member.GetExplorerAgent() as ExplorerAgent;

        Transform layerRoot =
            explorerAgent != null
                ? explorerAgent.GetRuntimeTransform()
                : member.transform;

        if (layerRoot == null)
        {
            return;
        }

        Transform[] transforms =
            layerRoot.GetComponentsInChildren<Transform>(
                true);

        for (int i = 0;
             i < transforms.Length;
             i++)
        {
            Transform targetTransform =
                transforms[i];

            if (targetTransform == null)
            {
                continue;
            }

            GameObject targetObject =
                targetTransform.gameObject;

            bool isLayerAlreadySaved = false;

            for (int restoreIndex = 0;
                 restoreIndex
                     < restoreData.GameObjectLayerRestoreDataList.Count;
                 restoreIndex++)
            {
                GameObjectLayerRestoreData layerRestoreData =
                    restoreData.GameObjectLayerRestoreDataList[
                        restoreIndex];

                if (layerRestoreData != null
                    && layerRestoreData.GameObject
                        == targetObject)
                {
                    isLayerAlreadySaved = true;
                    break;
                }
            }

            if (!isLayerAlreadySaved)
            {
                GameObjectLayerRestoreData layerRestoreData =
                    new GameObjectLayerRestoreData();

                layerRestoreData.GameObject =
                    targetObject;

                layerRestoreData.Layer =
                    targetObject.layer;

                restoreData.GameObjectLayerRestoreDataList.Add(
                    layerRestoreData);
            }

            targetObject.layer = previewLayer;
        }
    }

    private void ApplyLayerToHierarchy(
        GameObject rootObject,
        int targetLayer)
    {
        if (rootObject == null)
        {
            return;
        }

        Transform[] transforms =
            rootObject.GetComponentsInChildren<Transform>(
                true);

        for (int i = 0;
             i < transforms.Length;
             i++)
        {
            Transform targetTransform =
                transforms[i];

            if (targetTransform != null)
            {
                targetTransform.gameObject.layer =
                    targetLayer;
            }
        }
    }

    private MemberRestoreData FindMemberRestoreData(
        ComCharacterBase member)
    {
        for (int i = 0;
             i < m_memberRestoreDataList.Count;
             i++)
        {
            MemberRestoreData restoreData =
                m_memberRestoreDataList[i];

            if (restoreData != null
                && restoreData.Member == member)
            {
                return restoreData;
            }
        }

        return null;
    }

    private void RestoreMemberLayers(
        MemberRestoreData restoreData)
    {
        if (restoreData == null)
        {
            return;
        }

        HashSet<GameObject> restoredGameObjects =
            new HashSet<GameObject>();

        for (int i = 0;
             i < restoreData.GameObjectLayerRestoreDataList.Count;
             i++)
        {
            GameObjectLayerRestoreData layerRestoreData =
                restoreData.GameObjectLayerRestoreDataList[i];

            if (layerRestoreData == null
                || layerRestoreData.GameObject == null
                || restoredGameObjects.Contains(
                    layerRestoreData.GameObject))
            {
                continue;
            }

            layerRestoreData.GameObject.layer =
                layerRestoreData.Layer;

            restoredGameObjects.Add(
                layerRestoreData.GameObject);
        }

        restoreData.GameObjectLayerRestoreDataList.Clear();
    }

    private void BeginResultTreasurePhysicsSimulation()
    {
        if (m_isResultTreasurePhysicsSimulationManual)
        {
            return;
        }

#if UNITY_2022_2_OR_NEWER
        m_previousPhysicsSimulationMode =
            Physics.simulationMode;

        Physics.simulationMode =
            SimulationMode.Script;
#else
        m_previousPhysicsAutoSimulation =
            Physics.autoSimulation;

        Physics.autoSimulation = false;
#endif

        m_isResultTreasurePhysicsSimulationManual = true;
    }

    private void EndResultTreasurePhysicsSimulation()
    {
        if (!m_isResultTreasurePhysicsSimulationManual)
        {
            return;
        }

        Physics.simulationMode =
            m_previousPhysicsSimulationMode;

        m_isResultTreasurePhysicsSimulationManual = false;
    }
}