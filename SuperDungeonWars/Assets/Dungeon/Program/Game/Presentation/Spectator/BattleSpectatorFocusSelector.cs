using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 観戦カメラが注目する対象を、試合状況から選択する。
/// Cameraの追従・演出処理は担当しない。
/// </summary>
public sealed class BattleSpectatorFocusSelector
{
    public enum FocusTargetType
    {
        None,
        Character,
        TreasureChest,
    }

    public enum FocusReason
    {
        None,
        CarryingCentralChest,
        CarryingTreasure,
        ExportedCentralChest,
        ExportedTreasure,
        Attacking,
        EnemyInAttackArea,
        NearEnemy,
        CentralChest,
        NearMazeCenter,
    }

    // 宝箱を運搬し続けているだけの状態。
    // 戦闘中の候補へカメラを譲れる評価にする。
    private const float s_carryingCentralChestBaseScore = 610.0f;
    private const float s_carryingTreasureBaseScore = 550.0f;

    // 宝箱を拾った瞬間は、試合展開が変化した重要イベントとして注目する。
    private const float s_treasurePickupScoreBonus = 390.0f;
    private const float s_treasurePickupScoreHoldSeconds = 4.0f;

    // 搬出口が近い状況は、得点につながる重要な局面として注目する。
    private const float s_treasureNearExportScoreBonus = 350.0f;
    private const int s_treasureNearExportCellDistance = 8;


    private const float s_attackingTreasureCarrierScore = 950.0f;
    private const float s_attackingNormalTargetScore = 730.0f;
    private const float s_attackingUnavailableTargetScore = 620.0f;

    private const float s_enemyInAttackAreaScore = 700.0f;
    private const float s_nearEnemyBaseScore = 600.0f;
    private const float s_centralChestScore = 400.0f;
    private const float s_nearMazeCenterBaseScore = 100.0f;
    private const float s_maxNearbyInfluenceScore = 100.0f;


    private readonly BattleSpectatorFocusSettings m_settings;

    private readonly List<FocusCandidate> m_focusCandidates =
        new List<FocusCandidate>();

    private FocusTargetType m_currentTargetType;
    private ComCharacterBase m_currentCharacter;
    private TreasureChest m_currentTreasureChest;
    private FocusReason m_currentReason;
    private float m_currentScore;
    private float m_currentPrimaryScore;
    private int m_currentSystemTeamIndex;

    private float m_lastFocusChangedTime;
    private float m_lastFocusSwitchTime;
    private int m_lastRotationTargetStableId = -1;

    public FocusReason CurrentReason
    {
        get { return m_currentReason; }
    }

    public float CurrentScore
    {
        get { return m_currentScore; }
    }

    public int CurrentSystemTeamIndex
    {
        get { return m_currentSystemTeamIndex; }
    }

    public BattleSpectatorFocusSelector(
        BattleSpectatorFocusSettings settings)
    {
        m_settings = settings;
    }

    /// <summary>
    /// 試合状況を毎フレーム再評価し、切替が必要だった場合だけtrueを返す。
    /// </summary>
    public bool TrySelectFocus(
        MazeData mazeData,
        IReadOnlyList<ComPartyBase> parties,
        IReadOnlyList<TreasureChest> treasureChests,
        BattleCombatSystem battleCombatSystem,
        BattleTreasureSystem battleTreasureSystem,
        int restrictedSystemTeamIndex,
        out FocusTargetType selectedTargetType,
        out ComCharacterBase selectedCharacter,
        out TreasureChest selectedTreasureChest,
        out int selectedSystemTeamIndex,
        out FocusReason selectedReason)
    {
        selectedTargetType = FocusTargetType.None;
        selectedCharacter = null;
        selectedTreasureChest = null;
        selectedSystemTeamIndex = -1;
        selectedReason = FocusReason.None;

        BuildFocusCandidates(
            mazeData,
            parties,
            treasureChests,
            battleCombatSystem,
            battleTreasureSystem,
            restrictedSystemTeamIndex);

        FocusCandidate bestCandidate = FindBestCandidate();

        if (!bestCandidate.IsValid)
        {
            if (!HasCurrentFocus())
            {
                return false;
            }

            ClearCurrentFocus();
            return true;
        }

        float currentTime = Time.time;

        if (!HasCurrentFocus())
        {
            ApplyCandidate(bestCandidate, currentTime);

            CopyCurrentFocus(
                out selectedTargetType,
                out selectedCharacter,
                out selectedTreasureChest,
                out selectedSystemTeamIndex,
                out selectedReason);

            return true;
        }

        FocusCandidate currentCandidate;

        if (!TryFindCurrentCandidate(out currentCandidate))
        {
            ApplyCandidate(bestCandidate, currentTime);

            CopyCurrentFocus(
                out selectedTargetType,
                out selectedCharacter,
                out selectedTreasureChest,
                out selectedSystemTeamIndex,
                out selectedReason);

            return true;
        }

        // 現在対象も例外なく、そのフレームの評価で更新する。
        UpdateCurrentCandidateData(currentCandidate);

        if (IsSameTarget(bestCandidate))
        {
            return false;
        }

        float focusedSeconds =
            currentTime - m_lastFocusChangedTime;

        float switchedSeconds =
            currentTime - m_lastFocusSwitchTime;

        if (focusedSeconds >= m_settings.MaximumFocusSeconds
            && switchedSeconds >= m_settings.SwitchCooldownSeconds)
        {
            FocusCandidate rotationCandidate;

            if (TryFindRotationCandidate(
                    currentCandidate,
                    bestCandidate.m_score,
                    out rotationCandidate))
            {
                ApplyCandidate(rotationCandidate, currentTime);

                m_lastRotationTargetStableId =
                    rotationCandidate.GetStableTargetId();

                CopyCurrentFocus(
                    out selectedTargetType,
                    out selectedCharacter,
                    out selectedTreasureChest,
                    out selectedSystemTeamIndex,
                    out selectedReason);

                return true;
            }

            // 同格候補がいない場合、最大時間判定を次回まで繰延べる。
            m_lastFocusChangedTime = currentTime;
            return false;
        }

        if (focusedSeconds < m_settings.MinimumFocusSeconds
            || switchedSeconds < m_settings.SwitchCooldownSeconds)
        {
            return false;
        }

        float scoreDifference =
            bestCandidate.m_score - currentCandidate.m_score;

        if (scoreDifference < m_settings.RequiredScoreDifference)
        {
            return false;
        }

        ApplyCandidate(bestCandidate, currentTime);

        CopyCurrentFocus(
            out selectedTargetType,
            out selectedCharacter,
            out selectedTreasureChest,
            out selectedSystemTeamIndex,
            out selectedReason);

        return true;
    }

    public void ClearCurrentFocus()
    {
        m_currentTargetType = FocusTargetType.None;
        m_currentCharacter = null;
        m_currentTreasureChest = null;
        m_currentReason = FocusReason.None;
        m_currentScore = 0.0f;
        m_currentSystemTeamIndex = -1;
        m_lastFocusChangedTime = 0.0f;
        m_lastFocusSwitchTime = 0.0f;
        m_lastRotationTargetStableId = -1;

        m_currentScore = 0.0f;
        m_currentPrimaryScore = 0.0f;
        m_currentSystemTeamIndex = -1;

        m_focusCandidates.Clear();
    }

    private void BuildFocusCandidates(
        MazeData mazeData,
        IReadOnlyList<ComPartyBase> parties,
        IReadOnlyList<TreasureChest> treasureChests,
        BattleCombatSystem battleCombatSystem,
        BattleTreasureSystem battleTreasureSystem,
        int restrictedSystemTeamIndex)
    {
        m_focusCandidates.Clear();

        if (parties == null || parties.Count <= 0)
        {
            return;
        }

        Vector3 mazeCenterWorld = GetMazeCenterWorld(mazeData);

        for (int partyIndex = 0;
             partyIndex < parties.Count;
             partyIndex++)
        {
            if (restrictedSystemTeamIndex >= 0
                && partyIndex != restrictedSystemTeamIndex)
            {
                continue;
            }

            ComPartyBase party = parties[partyIndex];


            if (party == null)
            {
                continue;
            }

            int memberCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < memberCount;
                 memberIndex++)
            {
                ComCharacterBase character = null;
                party.TryGetMember(memberIndex, out character);

                if (!IsCharacterAvailable(character))
                {
                    continue;
                }

                FocusCandidate candidate =
                    EvaluateCharacterCandidate(
                        character,
                        partyIndex,
                        parties,
                        mazeData,
                        mazeCenterWorld,
                        battleCombatSystem,
                        battleTreasureSystem);

                if (candidate.IsValid)
                {
                    m_focusCandidates.Add(candidate);
                }
            }
        }

        ApplyNearbyCharacterInfluence();

        //EvaluateCentralChestCandidates(treasureChests);
    }

    private FocusCandidate EvaluateCharacterCandidate(
        ComCharacterBase character,
        int systemTeamIndex,
        IReadOnlyList<ComPartyBase> parties,
        MazeData mazeData,
        Vector3 mazeCenterWorld,
        BattleCombatSystem battleCombatSystem,
        BattleTreasureSystem battleTreasureSystem)
    {
        TreasureChest carriedTreasureChest =
            character.GetCarriedTreasure() as TreasureChest;

        if (carriedTreasureChest != null
            && !carriedTreasureChest.IsExported)
        {
            float carryingTreasureScore =
                EvaluateCarryingTreasureScore(
                    character,
                    systemTeamIndex,
                    parties,
                    mazeData,
                    carriedTreasureChest);

            FocusReason carryingTreasureReason =
                carriedTreasureChest.TreasureType
                    == TreasureType.CentralChest
                ? FocusReason.CarryingCentralChest
                : FocusReason.CarryingTreasure;

            return FocusCandidate.CreateCharacter(
                character,
                systemTeamIndex,
                carryingTreasureReason,
                carryingTreasureScore);
        }

        TreasureType exportedTreasureType;

        if (battleTreasureSystem != null
            && battleTreasureSystem
        .TryGetExportFocusTreasureType(
                    character,
                    out exportedTreasureType)
            && IsCurrentCarryingOrExportedFocusCharacter(
                character))
        {
            FocusReason exportedReason =
                exportedTreasureType
                == TreasureType.CentralChest
                    ? FocusReason.ExportedCentralChest
                    : FocusReason.ExportedTreasure;

            /*
             * 出荷前からフォーカスされていたキャラクターだけ、
             * 直前の運搬中主評価を短時間維持する。
             */
            return FocusCandidate.CreateCharacter(
                character,
                systemTeamIndex,
                exportedReason,
                m_currentPrimaryScore);
        }

        if (battleCombatSystem != null)
        {
            float attackingScore;

        if (battleCombatSystem.TryGetAttackFocusScore(
                    character,
                    out attackingScore))
            {
                return FocusCandidate.CreateCharacter(
                    character,
                    systemTeamIndex,
                    FocusReason.Attacking,
                    attackingScore);
            }
        }

        if (battleCombatSystem != null
            && battleCombatSystem.HasEnemyInAttackArea(
                character))
        {
            return FocusCandidate.CreateCharacter(
                character,
                systemTeamIndex,
                FocusReason.EnemyInAttackArea,
                s_enemyInAttackAreaScore);
        }

        float nearestEnemyDistance;

        if (TryGetNearestEnemyDistance(
                character,
                systemTeamIndex,
                parties,
                out nearestEnemyDistance))
        {
            float enemyScoreRate = 1.0f
                - Mathf.Clamp01(
                    nearestEnemyDistance
                    / m_settings.NearEnemyDistance);

            float nearEnemyScore =
                s_nearEnemyBaseScore
                + enemyScoreRate * 50.0f;

            return FocusCandidate.CreateCharacter(
                character,
                systemTeamIndex,
                FocusReason.NearEnemy,
                nearEnemyScore);
        }

        float distanceToCenter = Vector3.Distance(
            character.GetWorldPosition(),
            mazeCenterWorld);

        float nearCenterScore =
            s_nearMazeCenterBaseScore
            + 1.0f / Mathf.Max(1.0f, distanceToCenter);

        return FocusCandidate.CreateCharacter(
            character,
            systemTeamIndex,
            FocusReason.NearMazeCenter,
            nearCenterScore);
    }


    private float EvaluateCarryingTreasureScore(
    ComCharacterBase character,
    int systemTeamIndex,
    IReadOnlyList<ComPartyBase> parties,
    MazeData mazeData,
    TreasureChest treasureChest)
    {
        if (character == null || treasureChest == null)
        {
            return 0.0f;
        }

        float baseScore =
            treasureChest.TreasureType
            == TreasureType.CentralChest
            ? s_carryingCentralChestBaseScore
            : s_carryingTreasureBaseScore;

        float pickupBonus =
            GetTreasurePickupScoreBonus(treasureChest);

        float nearExportBonus = 0.0f;

        if (parties != null
            && systemTeamIndex >= 0
            && systemTeamIndex < parties.Count
            && mazeData != null)
        {
            ComPartyBase party = parties[systemTeamIndex];

            int exportCellDistance;

            if (party != null
                && TryGetNearestExportEntranceCellDistance(
                    character.GetCurrentCell(),
                    party,
                    mazeData,
                    out exportCellDistance))
            {
                float nearExportRate = 1.0f
                    - Mathf.Clamp01(
                        (float)exportCellDistance
                        / s_treasureNearExportCellDistance);

                nearExportBonus =
                    s_treasureNearExportScoreBonus
                    * nearExportRate;
            }
        }

        // 取得直後と出口接近が重なっても、イベント評価が過剰に
        // 積み上がらないよう、大きい方のボーナスだけを使う。
        return baseScore + Mathf.Max(
            pickupBonus,
            nearExportBonus);
    }

    private static float GetTreasurePickupScoreBonus(
        TreasureChest treasureChest)
    {
        if (treasureChest == null
            || treasureChest.PickedUpTime < 0.0f)
        {
            return 0.0f;
        }

        float elapsedSeconds =
            Time.time - treasureChest.PickedUpTime;

        if (elapsedSeconds < 0.0f
            || elapsedSeconds
                >= s_treasurePickupScoreHoldSeconds)
        {
            return 0.0f;
        }

        float remainingRate = 1.0f
            - elapsedSeconds
            / s_treasurePickupScoreHoldSeconds;

        return s_treasurePickupScoreBonus
            * Mathf.Clamp01(remainingRate);
    }

    private static bool TryGetNearestExportEntranceCellDistance(
        Vector2Int sourceCell,
        ComPartyBase party,
        MazeData mazeData,
        out int nearestDistance)
    {
        nearestDistance = int.MaxValue;

        if (party == null || mazeData == null)
        {
            return false;
        }

        for (int y = 0; y < mazeData.m_height; y++)
        {
            for (int x = 0; x < mazeData.m_width; x++)
            {
                Vector2Int cellPosition =
                    new Vector2Int(x, y);

        if (!party.IsExportEntranceCell(
                        cellPosition))
                {
                    continue;
                }

                int distance =
                    Mathf.Abs(sourceCell.x - x)
                    + Mathf.Abs(sourceCell.y - y);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }
        }

        return nearestDistance != int.MaxValue;
    }




    private void ApplyNearbyCharacterInfluence()
    {
        float influenceDistance =
            m_settings.NearbyCharacterInfluenceDistance;

        if (influenceDistance <= 0.0f)
        {
            return;
        }

        float influenceDistanceSquared =
            influenceDistance * influenceDistance;

        for (int targetIndex = 0;
             targetIndex < m_focusCandidates.Count;
             targetIndex++)
        {
            FocusCandidate targetCandidate =
                m_focusCandidates[targetIndex];

            if (targetCandidate.m_targetType
                != FocusTargetType.Character)
            {
                continue;
            }

            Vector3 targetPosition =
                targetCandidate.m_character.GetWorldPosition();

            float nearbyInfluenceScore = 0.0f;

            for (int nearbyIndex = 0;
                 nearbyIndex < m_focusCandidates.Count;
                 nearbyIndex++)
            {
                if (targetIndex == nearbyIndex)
                {
                    continue;
                }

                FocusCandidate nearbyCandidate =
                    m_focusCandidates[nearbyIndex];

                if (nearbyCandidate.m_targetType
                    != FocusTargetType.Character)
                {
                    continue;
                }

                Vector3 difference =
                    nearbyCandidate.m_character.GetWorldPosition()
                    - targetPosition;

                difference.y = 0.0f;

                float distanceSquared =
                    difference.sqrMagnitude;

                if (distanceSquared
                    >= influenceDistanceSquared)
                {
                    continue;
                }

                float distance = Mathf.Sqrt(distanceSquared);

                float influenceRate =
                    (influenceDistance - distance)
                    / influenceDistance;

                nearbyInfluenceScore +=
                    nearbyCandidate.m_primaryScore
                    * influenceRate;
            }

            targetCandidate.m_nearbyInfluenceScore =
                Mathf.Min(
                    nearbyInfluenceScore,
                    s_maxNearbyInfluenceScore);

            targetCandidate.m_score =
                targetCandidate.m_primaryScore
                + targetCandidate.m_nearbyInfluenceScore;

            m_focusCandidates[targetIndex] =
                targetCandidate;
        }
    }

    private void EvaluateCentralChestCandidates(
        IReadOnlyList<TreasureChest> treasureChests)
    {
        if (treasureChests == null)
        {
            return;
        }

        for (int treasureIndex = 0;
             treasureIndex < treasureChests.Count;
             treasureIndex++)
        {
            TreasureChest treasureChest =
                treasureChests[treasureIndex];

            if (treasureChest == null
                || treasureChest.IsExported
                || treasureChest.IsPickedUp
                || treasureChest.TreasureType
                    != TreasureType.CentralChest
                || !treasureChest.gameObject.activeInHierarchy)
            {
                continue;
            }

            m_focusCandidates.Add(
                FocusCandidate.CreateTreasureChest(
                    treasureChest,
                    FocusReason.CentralChest,
                    s_centralChestScore));
        }
    }

    private FocusCandidate FindBestCandidate()
    {
        FocusCandidate bestCandidate =
            FocusCandidate.CreateNone();

        for (int i = 0; i < m_focusCandidates.Count; i++)
        {
            TryReplaceBestCandidate(
                m_focusCandidates[i],
                ref bestCandidate);
        }

        return bestCandidate;
    }

    private bool TryFindCurrentCandidate(
        out FocusCandidate currentCandidate)
    {
        currentCandidate = FocusCandidate.CreateNone();

        for (int i = 0; i < m_focusCandidates.Count; i++)
        {
            FocusCandidate candidate = m_focusCandidates[i];

            if (IsSameTarget(candidate))
            {
                currentCandidate = candidate;
                return true;
            }
        }

        return false;
    }

    private bool TryFindRotationCandidate(
        FocusCandidate currentCandidate,
        float bestScore,
        out FocusCandidate rotationCandidate)
    {
        rotationCandidate = FocusCandidate.CreateNone();

        float minimumRotationScore =
            bestScore - m_settings.RotationScoreRange;

        FocusCandidate firstCandidate =
            FocusCandidate.CreateNone();

        FocusCandidate nextCandidate =
            FocusCandidate.CreateNone();

        for (int i = 0; i < m_focusCandidates.Count; i++)
        {
            FocusCandidate candidate = m_focusCandidates[i];

            if (!candidate.IsValid
                || candidate.IsSameTarget(currentCandidate)
                || candidate.m_score < minimumRotationScore)
            {
                continue;
            }

            if (!firstCandidate.IsValid
                || candidate.GetStableTargetId()
                    < firstCandidate.GetStableTargetId())
            {
                firstCandidate = candidate;
            }

            if (candidate.GetStableTargetId()
                    > m_lastRotationTargetStableId
                && (!nextCandidate.IsValid
                    || candidate.GetStableTargetId()
                        < nextCandidate.GetStableTargetId()))
            {
                nextCandidate = candidate;
            }
        }

        rotationCandidate = nextCandidate.IsValid
            ? nextCandidate
            : firstCandidate;

        return rotationCandidate.IsValid;
    }

    private bool TryGetNearestEnemyDistance(
        ComCharacterBase sourceCharacter,
        int sourceSystemTeamIndex,
        IReadOnlyList<ComPartyBase> parties,
        out float nearestDistance)
    {
        nearestDistance = float.MaxValue;

        for (int partyIndex = 0;
             partyIndex < parties.Count;
             partyIndex++)
        {
            if (partyIndex == sourceSystemTeamIndex)
            {
                continue;
            }

            ComPartyBase party = parties[partyIndex];

            if (party == null)
            {
                continue;
            }

            int partyCount = party.MemberCount;

            for (int memberIndex = 0;
                 memberIndex < partyCount;
                 memberIndex++)
            {
                ComCharacterBase enemyCharacter = null;
                party.TryGetMember(memberIndex, out enemyCharacter);

                if (!IsCharacterAvailable(enemyCharacter))
                {
                    continue;
                }

                Vector3 difference =
                    sourceCharacter.GetWorldPosition()
                    - enemyCharacter.GetWorldPosition();

                difference.y = 0.0f;

                float distance = difference.magnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }
        }

        return nearestDistance <= m_settings.NearEnemyDistance;
    }

    private static bool IsCharacterAvailable(
        ComCharacterBase character)
    {
        return character != null
            && !character.IsKnockedOut()
            && character.gameObject.activeInHierarchy;
    }

    private static Vector3 GetMazeCenterWorld(MazeData mazeData)
    {
        if (mazeData == null)
        {
            return Vector3.zero;
        }

        Vector2Int centerCell =
            mazeData.GetCenterCellPosition();

        return mazeData.CellToWorld(
            centerCell.x,
            centerCell.y);
    }

    private static void TryReplaceBestCandidate(
        FocusCandidate candidate,
        ref FocusCandidate bestCandidate)
    {
        if (!candidate.IsValid)
        {
            return;
        }

        if (!bestCandidate.IsValid
            || candidate.m_score > bestCandidate.m_score
            || (Mathf.Approximately(
                    candidate.m_score,
                    bestCandidate.m_score)
                && candidate.GetStableTargetId()
                    < bestCandidate.GetStableTargetId()))
        {
            bestCandidate = candidate;
        }
    }

    private bool HasCurrentFocus()
    {
        return m_currentTargetType != FocusTargetType.None;
    }

    private bool IsSameTarget(FocusCandidate candidate)
    {
        if (candidate.m_targetType != m_currentTargetType)
        {
            return false;
        }

        switch (candidate.m_targetType)
        {
            case FocusTargetType.Character:
                return candidate.m_character == m_currentCharacter;

            case FocusTargetType.TreasureChest:
                return candidate.m_treasureChest
                    == m_currentTreasureChest;

            default:
                return false;
        }
    }


    private bool IsCurrentCarryingOrExportedFocusCharacter(
        ComCharacterBase character)
    {
        if (m_currentTargetType
            != FocusTargetType.Character
            || m_currentCharacter != character)
        {
            return false;
        }

        return m_currentReason
            == FocusReason.CarryingCentralChest
            || m_currentReason
                == FocusReason.CarryingTreasure
            || m_currentReason
                == FocusReason.ExportedCentralChest
            || m_currentReason
                == FocusReason.ExportedTreasure;
    }


    private void UpdateCurrentCandidateData(
        FocusCandidate candidate)
    {
        m_currentReason = candidate.m_reason;
        m_currentScore = candidate.m_score;
        m_currentPrimaryScore = candidate.m_primaryScore;
        m_currentSystemTeamIndex =
            candidate.m_systemTeamIndex;
    }

    private void ApplyCandidate(
        FocusCandidate candidate,
        float currentTime)
    {
        m_currentTargetType = candidate.m_targetType;
        m_currentCharacter = candidate.m_character;
        m_currentTreasureChest = candidate.m_treasureChest;
        m_currentReason = candidate.m_reason;
        m_currentScore = candidate.m_score;
        m_currentPrimaryScore = candidate.m_primaryScore;
        m_currentSystemTeamIndex =
            candidate.m_systemTeamIndex;

        m_lastFocusChangedTime = currentTime;
        m_lastFocusSwitchTime = currentTime;
    }

    private void CopyCurrentFocus(
        out FocusTargetType selectedTargetType,
        out ComCharacterBase selectedCharacter,
        out TreasureChest selectedTreasureChest,
        out int selectedSystemTeamIndex,
        out FocusReason selectedReason)
    {
        selectedTargetType = m_currentTargetType;
        selectedCharacter = m_currentCharacter;
        selectedTreasureChest = m_currentTreasureChest;
        selectedSystemTeamIndex = m_currentSystemTeamIndex;
        selectedReason = m_currentReason;
    }

    private struct FocusCandidate
    {
        public FocusTargetType m_targetType;
        public ComCharacterBase m_character;
        public TreasureChest m_treasureChest;
        public int m_systemTeamIndex;
        public FocusReason m_reason;

        // 自身だけを評価した点。
        public float m_primaryScore;

        // 近隣キャラの影響を含む、カメラ対象としての最終点。
        public float m_score;

        public float m_nearbyInfluenceScore;

        public bool IsValid
        {
            get { return m_targetType != FocusTargetType.None; }
        }

        public bool IsSameTarget(FocusCandidate other)
        {
            if (m_targetType != other.m_targetType)
            {
                return false;
            }

            switch (m_targetType)
            {
                case FocusTargetType.Character:
                    return m_character == other.m_character;

                case FocusTargetType.TreasureChest:
                    return m_treasureChest == other.m_treasureChest;

                default:
                    return false;
            }
        }

        public int GetStableTargetId()
        {
            switch (m_targetType)
            {
                case FocusTargetType.Character:
                    return m_character != null
                        ? m_character.GetCharacterId()
                        : int.MaxValue;

                case FocusTargetType.TreasureChest:
                    return m_treasureChest != null
                        ? 100000 + m_treasureChest.TreasureId
                        : int.MaxValue;

                default:
                    return int.MaxValue;
            }
        }

        public static FocusCandidate CreateNone()
        {
            FocusCandidate candidate = new FocusCandidate();

            candidate.m_targetType = FocusTargetType.None;
            candidate.m_systemTeamIndex = -1;

            return candidate;
        }

        public static FocusCandidate CreateCharacter(
            ComCharacterBase character,
            int systemTeamIndex,
            FocusReason reason,
            float primaryScore)
        {
            FocusCandidate candidate = new FocusCandidate();

            candidate.m_targetType = FocusTargetType.Character;
            candidate.m_character = character;
            candidate.m_systemTeamIndex = systemTeamIndex;
            candidate.m_reason = reason;
            candidate.m_primaryScore = primaryScore;
            candidate.m_score = primaryScore;

            return candidate;
        }

        public static FocusCandidate CreateTreasureChest(
            TreasureChest treasureChest,
            FocusReason reason,
            float score)
        {
            FocusCandidate candidate = new FocusCandidate();

            candidate.m_targetType = FocusTargetType.TreasureChest;
            candidate.m_treasureChest = treasureChest;
            candidate.m_systemTeamIndex = -1;
            candidate.m_reason = reason;
            candidate.m_primaryScore = score;
            candidate.m_score = score;

            return candidate;
        }
    }
}