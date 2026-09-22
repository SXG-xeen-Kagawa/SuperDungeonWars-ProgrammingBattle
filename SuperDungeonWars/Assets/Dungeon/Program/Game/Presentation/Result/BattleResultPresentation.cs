using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleResultPresentation : MonoBehaviour
{
    private const int TeamCount = 4;

    [SerializeField]
    private BattleResultTeamLane m_teamLanePrefab;

    [SerializeField]
    private RectTransform m_teamLanesRoot;

    [SerializeField]
    private BattleTeamPreviewController
        m_battleTeamPreviewController;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultStartDelay = 0.45f;

    [SerializeField]
    [Min(0.01f)]
    private float m_totalCoinCountUpDuration = 0.16f;

    [SerializeField]
    [Min(0.0f)]
    private float m_treasureRevealInterval = 0.10f;

    [SerializeField]
    [Min(0.0f)]
    private float m_treasureOpenRevealDelay = 0.12f;

    [SerializeField]
    [Min(0.0f)]
    private float m_resultFinishHoldDuration = 0.50f;

    [SerializeField]
    [Min(0.0f)]
    private float m_centralChestPreOpenDelay = 1.25f;



    private readonly List<BattleResultTeamLane> m_spawnedTeamLanes =
        new List<BattleResultTeamLane>();

    private Coroutine m_resultSequenceCoroutine;

    private bool m_isResultSequenceStarted;
    private bool m_isResultSequenceCompleted;


    private void Awake()
    {
        if (m_teamLanePrefab == null)
        {
            Debug.LogError(
                "BattleResultPresentation: "
                + "Team Lane Prefab が未設定です。",
                this);
        }

        if (m_teamLanesRoot == null)
        {
            Debug.LogError(
                "BattleResultPresentation: "
                + "Team Lanes Root が未設定です。",
                this);
        }
    }

    public void Show(
        BattleResultData battleResult,
        IReadOnlyList<ComPartyBase> spawnedParties)
    {
        StopResultSequence();
        DestroySpawnedTeamLanes();

        gameObject.SetActive(true);

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleResultTeamLane teamLane =
                Instantiate(
                    m_teamLanePrefab,
                    m_teamLanesRoot);

            m_spawnedTeamLanes.Add(teamLane);

            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            ComPartyBase party =
                GetPartyByTeamIndex(
                    spawnedParties,
                    teamIndex);

            teamLane.Initialize(
                teamIndex,
                party,
                teamResult);
        }

        ConfigureGoldTowerScale(battleResult);

        if (m_battleTeamPreviewController != null)
        {
            m_battleTeamPreviewController.Show(
                spawnedParties,
                battleResult,
                BattleTeamPreviewPose.ResultIdle);

            for (int teamIndex = 0;
                 teamIndex < m_spawnedTeamLanes.Count;
                 teamIndex++)
            {
                BattleResultTeamLane teamLane =
                    m_spawnedTeamLanes[teamIndex];

                if (teamLane == null)
                {
                    continue;
                }

                RenderTexture previewTexture =
                    m_battleTeamPreviewController
                        .GetPreviewTextureTeamIndex(
                            teamIndex);

                teamLane.SetPreviewTexture(
                    previewTexture);
            }
        }

        m_isResultSequenceStarted = false;
        m_isResultSequenceCompleted =
            battleResult == null;
    }


    public bool IsResultSequenceCompleted
    {
        get { return m_isResultSequenceCompleted; }
    }

    public void StartResultSequence(
        BattleResultData battleResult)
    {
        if (m_isResultSequenceStarted)
        {
            return;
        }

        m_isResultSequenceStarted = true;

        if (battleResult == null)
        {
            m_isResultSequenceCompleted = true;
            return;
        }

        m_resultSequenceCoroutine =
            StartCoroutine(
                CoPlayResultSequence(
                    battleResult));
    }



    public void Hide()
    {
        StopResultSequence();

        if (m_battleTeamPreviewController != null)
        {
            m_battleTeamPreviewController.Hide(
                false);
        }

        DestroySpawnedTeamLanes();

        gameObject.SetActive(false);
    }

    private IEnumerator CoPlayResultSequence(
        BattleResultData battleResult)
    {
        while (m_battleTeamPreviewController != null
            && !m_battleTeamPreviewController.IsResultTreasurePlacementCompleted)
        {
            yield return null;
        }

        if (m_resultStartDelay > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_resultStartDelay);
        }

        int[] currentTotalValues =
            new int[TeamCount];

        int[] revealedTreasureCounts =
            new int[TeamCount];

        bool[] isDefeatPoseApplied =
            new bool[TeamCount];

        UpdateProvisionalRanks(
            currentTotalValues);

        yield return CoPlayTreasureType(
            battleResult,
            TreasureType.SmallChest,
            currentTotalValues,
            revealedTreasureCounts,
            isDefeatPoseApplied);

        yield return CoPlayTreasureType(
            battleResult,
            TreasureType.CentralChest,
            currentTotalValues,
            revealedTreasureCounts,
            isDefeatPoseApplied);

        ApplyFinalResult(
            battleResult,
            currentTotalValues,
            isDefeatPoseApplied);

        if (m_resultFinishHoldDuration > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_resultFinishHoldDuration);
        }

        m_resultSequenceCoroutine = null;
        m_isResultSequenceCompleted = true;
    }

    private IEnumerator CoPlayTreasureType(
        BattleResultData battleResult,
        TreasureType treasureType,
        int[] currentTotalValues,
        int[] revealedTreasureCounts,
        bool[] isDefeatPoseApplied)
    {
        int maximumTreasureCount =
            GetMaximumTreasureCountByType(
                battleResult,
                treasureType);

        for (int treasureOrder = 0;
             treasureOrder < maximumTreasureCount;
             treasureOrder++)
        {
            for (int teamIndex = 0;
                 teamIndex < TeamCount;
                 teamIndex++)
            {
                TeamBattleResult teamResult =
                    FindTeamResultByTeamIndex(
                        battleResult,
                        teamIndex);

                ExportedTreasureResult treasureResult =
                    GetTreasureByTypeAndOrder(
                        teamResult,
                        treasureType,
                        treasureOrder);

                if (treasureResult == null)
                {
                    continue;
                }

                yield return CoApplyTreasureResult(
                    battleResult,
                    teamIndex,
                    treasureResult,
                    currentTotalValues,
                    revealedTreasureCounts,
                    isDefeatPoseApplied);
            }
        }
    }

    private IEnumerator CoApplyTreasureResult(
        BattleResultData battleResult,
        int teamIndex,
        ExportedTreasureResult treasureResult,
        int[] currentTotalValues,
        int[] revealedTreasureCounts,
        bool[] isDefeatPoseApplied)
    {
        if (treasureResult == null)
        {
            yield break;
        }

        BattleResultTeamLane teamLane =
            GetTeamLaneTeamIndex(
                teamIndex);

        if (teamLane == null)
        {
            yield break;
        }

        int addedValue =
            Mathf.Max(
                0,
                treasureResult.Value);

        if (m_battleTeamPreviewController != null)
        {
            // 最後の金の宝箱開封前のみ長めにディレイを入れる 
            bool isCentralChest =
                treasureResult.TreasureType
                == TreasureType.CentralChest;
            if (isCentralChest
                && m_centralChestPreOpenDelay > 0.0f)
            {
                yield return new WaitForSecondsRealtime(
                    m_centralChestPreOpenDelay);
            }


            m_battleTeamPreviewController
        .SetPreviewPoseTeamIndex(
                    teamIndex,
                    BattleTeamPreviewPose
                        .ResultTreasureReact);

            m_battleTeamPreviewController
                .StartResultTreasureGlow(
                    treasureResult.TreasureId);

            if (m_treasureOpenRevealDelay > 0.0f)
            {
                yield return new WaitForSecondsRealtime(
                    m_treasureOpenRevealDelay);
            }

            //bool isCentralChest =
            //    treasureResult.TreasureType
            //    == TreasureType.CentralChest;

            //if (isCentralChest
            //    && m_centralChestPreOpenDelay > 0.0f)
            //{
            //    yield return new WaitForSecondsRealtime(
            //        m_centralChestPreOpenDelay);
            //}

            m_battleTeamPreviewController
                .SetResultTreasureOpened(
                    treasureResult.TreasureId,
                    true);

            m_battleTeamPreviewController
                .PlayResultTreasureGoldCoinFountain(
                    treasureResult.TreasureId,
                    addedValue);
        }

        Camera previewCamera = null;
        Vector3 treasureWorldPosition = Vector3.zero;

        bool canShowAddedCoinPopup = false;

        if (m_battleTeamPreviewController != null)
        {
            canShowAddedCoinPopup =
                m_battleTeamPreviewController
                    .TryGetResultTreasurePresentationPosition(
                        treasureResult.TreasureId,
                        out previewCamera,
                        out treasureWorldPosition);
        }

        if (canShowAddedCoinPopup)
        {
            yield return teamLane
                .CoShowAddedCoinPopup(
                    addedValue,
                    previewCamera,
                    treasureWorldPosition);
        }
        else
        {
            teamLane.ShowAddedCoin(
                addedValue);

            if (m_treasureRevealInterval > 0.0f)
            {
                yield return new WaitForSecondsRealtime(
                    m_treasureRevealInterval);
            }

            teamLane.HideAddedCoin();
        }

        int currentTotalValue = 0;

        if (currentTotalValues != null
            && teamIndex >= 0
            && teamIndex < currentTotalValues.Length)
        {
            currentTotalValue =
                currentTotalValues[teamIndex];
        }

        int targetTotalValue =
            currentTotalValue
            + addedValue;

        float elapsedTime = 0.0f;

        while (elapsedTime < m_totalCoinCountUpDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime
                    / m_totalCoinCountUpDuration);

            int displayTotalValue =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        currentTotalValue,
                        targetTotalValue,
                        progress));

            teamLane.SetTotalCoin(
                displayTotalValue);

            yield return null;
        }

        teamLane.SetTotalCoin(
            targetTotalValue);

        if (currentTotalValues != null
            && teamIndex >= 0
            && teamIndex < currentTotalValues.Length)
        {
            currentTotalValues[teamIndex] =
                targetTotalValue;
        }

        if (revealedTreasureCounts != null
            && teamIndex >= 0
            && teamIndex < revealedTreasureCounts.Length)
        {
            revealedTreasureCounts[teamIndex]++;
        }

        UpdateProvisionalRanks(
            currentTotalValues);

        ApplyDefeatPoseForEliminatedTeams(
            battleResult,
            currentTotalValues,
            revealedTreasureCounts,
            isDefeatPoseApplied);

        bool isDefeatPoseAppliedForTeam =
            IsDefeatPoseApplied(
                isDefeatPoseApplied,
                teamIndex);

        if (m_battleTeamPreviewController != null
            && !isDefeatPoseAppliedForTeam)
        {
            m_battleTeamPreviewController
                .SetPreviewPoseTeamIndex(
                    teamIndex,
                    BattleTeamPreviewPose.ResultIdle);
        }

        if (m_treasureRevealInterval > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_treasureRevealInterval);
        }
    }


    private void UpdateProvisionalRanks(
        int[] currentTotalValues)
    {
        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleResultTeamLane teamLane =
                GetTeamLaneTeamIndex(
                    teamIndex);

            if (teamLane == null)
            {
                continue;
            }

            int totalValue =
                currentTotalValues != null
                && teamIndex < currentTotalValues.Length
                    ? currentTotalValues[teamIndex]
                    : 0;

            if (totalValue <= 0)
            {
                teamLane.SetRank(0);
                continue;
            }

            int rank = 1;

            for (int compareTeamIndex = 0;
                 compareTeamIndex < TeamCount;
                 compareTeamIndex++)
            {
                if (compareTeamIndex == teamIndex)
                {
                    continue;
                }

                int compareTotalValue =
                    currentTotalValues != null
                    && compareTeamIndex
                        < currentTotalValues.Length
                        ? currentTotalValues[
                            compareTeamIndex]
                        : 0;

                if (compareTotalValue > totalValue)
                {
                    rank++;
                }
            }

            teamLane.SetRank(rank);
        }
    }



    private void ApplyDefeatPoseForEliminatedTeams(
    BattleResultData battleResult,
    int[] currentTotalValues,
    int[] revealedTreasureCounts,
    bool[] isDefeatPoseApplied)
    {
        if (m_battleTeamPreviewController == null
            || currentTotalValues == null
            || revealedTreasureCounts == null
            || isDefeatPoseApplied == null)
        {
            return;
        }

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            if (teamIndex >= revealedTreasureCounts.Length
                || teamIndex >= isDefeatPoseApplied.Length
                || isDefeatPoseApplied[teamIndex])
            {
                continue;
            }

            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            if (teamResult == null
                || revealedTreasureCounts[teamIndex]
                    < teamResult.Treasures.Count)
            {
                continue;
            }

            int provisionalRank =
                GetProvisionalRank(
                    teamIndex,
                    currentTotalValues);

            if (provisionalRank <= 1)
            {
                continue;
            }

            m_battleTeamPreviewController
                .SetPreviewPoseTeamIndex(
                    teamIndex,
                    BattleTeamPreviewPose.ResultDefeat);

            isDefeatPoseApplied[teamIndex] = true;
        }
    }

    private int GetProvisionalRank(
        int teamIndex,
        int[] currentTotalValues)
    {
        if (currentTotalValues == null
            || teamIndex < 0
            || teamIndex >= currentTotalValues.Length)
        {
            return 0;
        }

        int totalValue =
            currentTotalValues[teamIndex];

        int rank = 1;

        for (int compareTeamIndex = 0;
             compareTeamIndex < TeamCount;
             compareTeamIndex++)
        {
            if (compareTeamIndex == teamIndex
                || compareTeamIndex
                    >= currentTotalValues.Length)
            {
                continue;
            }

            if (currentTotalValues[compareTeamIndex]
                > totalValue)
            {
                rank++;
            }
        }

        return rank;
    }

    private bool IsDefeatPoseApplied(
        bool[] isDefeatPoseApplied,
        int teamIndex)
    {
        return isDefeatPoseApplied != null
            && teamIndex >= 0
            && teamIndex < isDefeatPoseApplied.Length
            && isDefeatPoseApplied[teamIndex];
    }




    private void ApplyFinalResult(
        BattleResultData battleResult,
        int[] currentTotalValues,
        bool[] isDefeatPoseApplied)
    {
        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleResultTeamLane teamLane =
                GetTeamLaneTeamIndex(
                    teamIndex);

            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            if (teamLane == null)
            {
                continue;
            }

            if (teamResult == null)
            {
                teamLane.SetTotalCoin(0);
                teamLane.SetRank(0);

                if (m_battleTeamPreviewController != null
                    && !IsDefeatPoseApplied(
                        isDefeatPoseApplied,
                        teamIndex))
                {
                    m_battleTeamPreviewController
                        .SetPreviewPoseTeamIndex(
                            teamIndex,
                            BattleTeamPreviewPose
                                .ResultDefeat);
                }

                continue;
            }

            teamLane.SetTotalCoin(
                teamResult.TotalValue);

            teamLane.SetRank(
                teamResult.Rank);

            teamLane.SetLightEffectVisible(
                teamResult.Rank == 1);


            if (currentTotalValues != null
                && teamIndex < currentTotalValues.Length)
            {
                currentTotalValues[teamIndex] =
                    teamResult.TotalValue;
            }

            if (m_battleTeamPreviewController == null)
            {
                continue;
            }

            if (teamResult.Rank == 1)
            {
                m_battleTeamPreviewController
                    .SetPreviewPoseTeamIndex(
                        teamIndex,
                        BattleTeamPreviewPose
                            .ResultVictory);

                continue;
            }

            if (!IsDefeatPoseApplied(
                    isDefeatPoseApplied,
                    teamIndex))
            {
                m_battleTeamPreviewController
                    .SetPreviewPoseTeamIndex(
                        teamIndex,
                        BattleTeamPreviewPose
                            .ResultDefeat);
            }
        }
    }


    private int GetMaximumTreasureCountByType(
        BattleResultData battleResult,
        TreasureType treasureType)
    {
        if (battleResult == null)
        {
            return 0;
        }

        int maximumTreasureCount = 0;

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            int treasureCount =
                GetTreasureCountByType(
                    teamResult,
                    treasureType);

            maximumTreasureCount =
                Mathf.Max(
                    maximumTreasureCount,
                    treasureCount);
        }

        return maximumTreasureCount;
    }

    private int GetTreasureCountByType(
        TeamBattleResult teamResult,
        TreasureType treasureType)
    {
        if (teamResult == null)
        {
            return 0;
        }

        int treasureCount = 0;

        for (int i = 0;
             i < teamResult.Treasures.Count;
             i++)
        {
            ExportedTreasureResult treasureResult =
                teamResult.Treasures[i];

            if (treasureResult != null
                && treasureResult.TreasureType
                    == treasureType)
            {
                treasureCount++;
            }
        }

        return treasureCount;
    }

    private ExportedTreasureResult
        GetTreasureByTypeAndOrder(
            TeamBattleResult teamResult,
            TreasureType treasureType,
            int treasureOrder)
    {
        if (teamResult == null
            || treasureOrder < 0)
        {
            return null;
        }

        int requestedTreasureId = -1;

        if (m_battleTeamPreviewController != null)
        {
            requestedTreasureId =
                m_battleTeamPreviewController
                    .GetResultTreasureIdByTypeAndOrder(
                        teamResult.TeamIndex,
                        treasureType,
                        treasureOrder);
        }

        if (requestedTreasureId >= 0)
        {
            for (int i = 0;
                 i < teamResult.Treasures.Count;
                 i++)
            {
                ExportedTreasureResult treasureResult =
                    teamResult.Treasures[i];

                if (treasureResult != null
                    && treasureResult.TreasureId
                        == requestedTreasureId)
                {
                    return treasureResult;
                }
            }
        }

        // 宝箱プレビューが存在しない場合だけ、
        // 従来どおり登録順へフォールバックする。
        int foundTreasureCount = 0;

        for (int i = 0;
             i < teamResult.Treasures.Count;
             i++)
        {
            ExportedTreasureResult treasureResult =
                teamResult.Treasures[i];

            if (treasureResult == null
                || treasureResult.TreasureType
                    != treasureType)
            {
                continue;
            }

            if (foundTreasureCount == treasureOrder)
            {
                return treasureResult;
            }

            foundTreasureCount++;
        }

        return null;
    }

    private BattleResultTeamLane
        GetTeamLaneTeamIndex(
            int teamIndex)
    {
        if (teamIndex < 0
            || teamIndex >= m_spawnedTeamLanes.Count)
        {
            return null;
        }

        return m_spawnedTeamLanes[teamIndex];
    }

    private void StopResultSequence()
    {
        if (m_resultSequenceCoroutine != null)
        {
            StopCoroutine(
                m_resultSequenceCoroutine);

            m_resultSequenceCoroutine = null;
        }

        m_isResultSequenceStarted = false;
        m_isResultSequenceCompleted = false;
    }

    private void DestroySpawnedTeamLanes()
    {
        for (int i = 0;
             i < m_spawnedTeamLanes.Count;
             i++)
        {
            BattleResultTeamLane teamLane =
                m_spawnedTeamLanes[i];

            if (teamLane != null)
            {
                Destroy(teamLane.gameObject);
            }
        }

        m_spawnedTeamLanes.Clear();
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

    private ComPartyBase GetPartyByTeamIndex(
        IReadOnlyList<ComPartyBase> spawnedParties,
        int teamIndex)
    {
        if (spawnedParties == null
            || teamIndex < 0
            || teamIndex >= spawnedParties.Count)
        {
            return null;
        }

        return spawnedParties[teamIndex];
    }

    private void ConfigureGoldTowerScale(
        BattleResultData battleResult)
    {
        int maximumTotalValue = 0;

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            TeamBattleResult teamResult =
                FindTeamResultByTeamIndex(
                    battleResult,
                    teamIndex);

            if (teamResult == null)
            {
                continue;
            }

            maximumTotalValue =
                Mathf.Max(
                    maximumTotalValue,
                    teamResult.TotalValue);
        }

        for (int teamIndex = 0;
             teamIndex < m_spawnedTeamLanes.Count;
             teamIndex++)
        {
            BattleResultTeamLane teamLane =
                m_spawnedTeamLanes[teamIndex];

            if (teamLane == null)
            {
                continue;
            }

            teamLane
        .SetGoldTowerScaleReferenceTotal(
                    maximumTotalValue);
        }
    }

}