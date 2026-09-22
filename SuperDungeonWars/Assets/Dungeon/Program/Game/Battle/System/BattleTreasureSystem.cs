using System.Collections.Generic;
using UnityEngine;

public class BattleTreasureSystem
{
    private readonly List<TreasureChest> m_treasureChests =
        new List<TreasureChest>();

    private readonly Dictionary<ComCharacterBase, ComPartyBase>
        m_partyByCharacter =
            new Dictionary<ComCharacterBase, ComPartyBase>();

    private const float s_exportFocusScoreHoldSeconds = 0.8f;

    private readonly Dictionary<ComCharacterBase, float>
        m_exportFocusScoreHoldEndTimeByCharacter =
            new Dictionary<ComCharacterBase, float>();

    private readonly Dictionary<ComCharacterBase, TreasureType>
        m_exportedTreasureTypeByCharacter =
            new Dictionary<ComCharacterBase, TreasureType>();

    private readonly Dictionary<ComCharacterBase, TreasureChest>
        m_pendingDeliveryTreasureByCharacter =
            new Dictionary<ComCharacterBase, TreasureChest>();

    private readonly Dictionary<
        ComCharacterBase,
        CharacterAnimationController>
        m_deliveryAnimationControllerByCharacter =
            new Dictionary<
                ComCharacterBase,
                CharacterAnimationController>();

    private MazeData m_mazeData;
    private BattleGameRuleData m_ruleData;

    public void Initialize(
        MazeData mazeData,
        BattleGameRuleData ruleData,
        IReadOnlyList<ComPartyBase> parties,
        IReadOnlyList<TreasureChest> treasureChests)
    {
        Shutdown();

        m_mazeData = mazeData;
        m_ruleData = ruleData;

        RegisterParties(parties);
        RegisterTreasureChests(treasureChests);
    }

    public void Shutdown()
    {
        foreach (
            KeyValuePair<
                ComCharacterBase,
                CharacterAnimationController> pair
            in m_deliveryAnimationControllerByCharacter)
        {
            if (pair.Value != null)
            {
                pair.Value.m_onDeliveryThrowRelease -=
                    OnDeliveryThrowRelease;
            }
        }

        m_pendingDeliveryTreasureByCharacter.Clear();
        m_deliveryAnimationControllerByCharacter.Clear();

        for (int i = 0; i < m_treasureChests.Count; i++)
        {
            TreasureChest treasureChest =
                m_treasureChests[i];

            if (treasureChest != null)
            {
                treasureChest.SetBattleTreasureSystem(
                    null);
            }
        }

        m_treasureChests.Clear();
        m_partyByCharacter.Clear();
        m_exportFocusScoreHoldEndTimeByCharacter.Clear();
        m_exportedTreasureTypeByCharacter.Clear();

        m_mazeData = null;
        m_ruleData = null;
    }

    public void TryRequestPickUpTreasure(
        TreasureChest treasureChest,
        ComCharacterBase character)
    {
        if (treasureChest == null
            || character == null
            || character.IsKnockedOut()
            || character.HasTreasure()
            || treasureChest.IsPickedUp
            || treasureChest.IsExported
            || !treasureChest.IsPickupAvailable())
        {
            return;
        }

        if (!character.TryPickUpTreasure(
            treasureChest.TreasureType))
        {
            return;
        }

        if (!character.SetCarriedTreasure(
            treasureChest))
        {
            return;
        }

        if (!treasureChest.AttachToCharacter(
            character))
        {
            character.ClearCarriedTreasure(
                treasureChest);

            return;
        }

        Debug.Log(
            character.name
            + " が "
            + treasureChest.TreasureType
            + " を取得しました。");
    }

    public void TryExportTreasure(
        ComCharacterBase character)
    {
        if (character == null
            || character.IsKnockedOut()
            || character.IsActionLocked()
            || !character.HasTreasure()
            || m_pendingDeliveryTreasureByCharacter
                .ContainsKey(character))
        {
            return;
        }

        ComPartyBase party;

        if (!m_partyByCharacter.TryGetValue(
                character,
                out party)
            || party == null
            || !party.IsExportEntranceCell(
                character.GetCurrentCell()))
        {
            return;
        }

        TreasureChest treasureChest =
            character.GetCarriedTreasure() as TreasureChest;

        if (treasureChest == null)
        {
            return;
        }

        CharacterAnimationController animationController;

        if (!m_deliveryAnimationControllerByCharacter
            .TryGetValue(
                character,
                out animationController)
            || animationController == null)
        {
            Debug.LogWarning(
                "BattleTreasureSystem: 納品用の"
                + " CharacterAnimationController がありません。",
                character);

            return;
        }

        float lockSeconds = m_ruleData != null
            ? m_ruleData.DeliveryThrowLockSeconds
            : 1.2f;

        m_pendingDeliveryTreasureByCharacter.Add(
            character,
            treasureChest);

        character.LockActionUntil(
            Time.time + lockSeconds);

        animationController.PlayDeliveryThrow();
    }

    public void CancelDeliveryThrow(
        ComCharacterBase character)
    {
        if (character == null)
        {
            return;
        }

        if (!m_pendingDeliveryTreasureByCharacter.Remove(
                character))
        {
            return;
        }

        CharacterAnimationController animationController;

        if (m_deliveryAnimationControllerByCharacter
            .TryGetValue(
                character,
                out animationController)
            && animationController != null)
        {
            animationController.CancelDeliveryThrow();
        }
    }

    private void OnDeliveryThrowRelease(
        ComCharacterBase character)
    {
        if (character == null)
        {
            return;
        }

        TreasureChest treasureChest;

        if (!m_pendingDeliveryTreasureByCharacter
            .TryGetValue(
                character,
                out treasureChest)
            || treasureChest == null
            || character.IsKnockedOut()
            || character.GetCarriedTreasure() != treasureChest)
        {
            CancelDeliveryThrow(character);
            return;
        }

        ComPartyBase party;

        if (!m_partyByCharacter.TryGetValue(
                character,
                out party)
            || party == null)
        {
            CancelDeliveryThrow(character);
            return;
        }

        ExplorerAgent agent =
            character.GetExplorerAgent() as ExplorerAgent;

        Transform runtimeTransform =
            agent != null
                ? agent.GetRuntimeTransform()
                : null;

        if (runtimeTransform == null)
        {
            CancelDeliveryThrow(character);
            return;
        }

        float horizontalImpulse = m_ruleData != null
            ? m_ruleData.DeliveryThrowHorizontalImpulse
            : 4.5f;

        float upwardImpulse = m_ruleData != null
            ? m_ruleData.DeliveryThrowUpwardImpulse
            : 2.5f;

        float torqueImpulse = m_ruleData != null
            ? m_ruleData.DeliveryThrowTorqueImpulse
            : 3.0f;

        float visibleSeconds = m_ruleData != null
            ? m_ruleData.DeliveryTreasureVisibleSeconds
            : 1.2f;

        Vector3 throwDirection = runtimeTransform.forward;
        throwDirection.y = 0.0f;

        if (!treasureChest.ThrowForExport(
                character,
                throwDirection,
                horizontalImpulse,
                upwardImpulse,
                torqueImpulse,
                visibleSeconds))
        {
            CancelDeliveryThrow(character);
            return;
        }

        m_pendingDeliveryTreasureByCharacter.Remove(
            character);

        TreasureType exportedTreasureType =
            treasureChest.TreasureType;

        m_exportFocusScoreHoldEndTimeByCharacter[character] =
            Time.time + s_exportFocusScoreHoldSeconds;

        m_exportedTreasureTypeByCharacter[character] =
            exportedTreasureType;

        character.ClearCarriedTreasure(
            treasureChest);

        party.AddExportedTreasure(
            treasureChest);

        character.NotifyTreasureExported(
            exportedTreasureType);

        Debug.Log(
            character.name
            + " が "
            + exportedTreasureType
            + " を投擲搬出しました。");
    }

    public bool TryGetExportFocusTreasureType(
        ComCharacterBase character,
        out TreasureType exportedTreasureType)
    {
        exportedTreasureType = default(TreasureType);

        if (character == null
            || !character.gameObject.activeInHierarchy)
        {
            return false;
        }

        float holdEndTime;

        if (!m_exportFocusScoreHoldEndTimeByCharacter
            .TryGetValue(
                character,
                out holdEndTime)
            || Time.time >= holdEndTime)
        {
            return false;
        }

        return m_exportedTreasureTypeByCharacter
            .TryGetValue(
                character,
                out exportedTreasureType);
    }

    public void DropCarriedTreasure(
        ComCharacterBase character)
    {
        DropCarriedTreasure(
            character,
            Vector3.zero);
    }

    public void DropCarriedTreasure(
        ComCharacterBase character,
        Vector3 dropDirection)
    {
        if (character == null)
        {
            return;
        }

        CancelDeliveryThrow(character);

        if (!character.HasTreasure())
        {
            return;
        }

        TreasureChest treasureChest =
            character.GetCarriedTreasure() as TreasureChest;

        if (treasureChest == null)
        {
            return;
        }

        float pickupLockSeconds = m_ruleData != null
            ? m_ruleData.TreasurePickupLockSeconds
            : 0.0f;

        if (!treasureChest.DetachAndDrop(
            character,
            dropDirection,
            pickupLockSeconds))
        {
            return;
        }

        character.ClearCarriedTreasure(
            treasureChest);
    }

    private void RegisterParties(
        IReadOnlyList<ComPartyBase> parties)
    {
        if (parties == null)
        {
            return;
        }

        for (int partyIndex = 0;
             partyIndex < parties.Count;
             partyIndex++)
        {
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
                ComCharacterBase member;

                if (!party.TryGetMember(
                    memberIndex,
                    out member))
                {
                    continue;
                }

                if (m_partyByCharacter.ContainsKey(member))
                {
                    continue;
                }

                m_partyByCharacter.Add(member, party);

                ExplorerAgent agent =
                    member.GetExplorerAgent() as ExplorerAgent;

                CharacterAnimationController animationController =
                    agent != null
                        ? agent.GetCharacterAnimationController()
                        : null;

                if (animationController == null)
                {
                    Debug.LogWarning(
                        "BattleTreasureSystem: Runtime Character の"
                        + " CharacterAnimationController がありません。",
                        member);

                    continue;
                }

                animationController.m_onDeliveryThrowRelease -=
                    OnDeliveryThrowRelease;

                animationController.m_onDeliveryThrowRelease +=
                    OnDeliveryThrowRelease;

                m_deliveryAnimationControllerByCharacter.Add(
                    member,
                    animationController);
            }
        }
    }

    private void RegisterTreasureChests(
        IReadOnlyList<TreasureChest> treasureChests)
    {
        if (treasureChests == null)
        {
            return;
        }

        for (int i = 0; i < treasureChests.Count; i++)
        {
            TreasureChest treasureChest =
                treasureChests[i];

            if (treasureChest == null)
            {
                continue;
            }

            m_treasureChests.Add(treasureChest);

            treasureChest.SetBattleTreasureSystem(
                this);
        }
    }

    private Vector3 FindTreasureDropPosition(
        Vector3 centerPosition)
    {
        if (m_mazeData == null || m_ruleData == null)
        {
            return centerPosition;
        }

        float minDistance =
            m_ruleData.TreasureScatterMinDistance;

        float maxDistance =
            m_ruleData.TreasureScatterMaxDistance;

        for (int i = 0; i < 12; i++)
        {
            Vector2 direction2D =
                Random.insideUnitCircle;

            if (direction2D.sqrMagnitude <= 0.0001f)
            {
                continue;
            }

            direction2D.Normalize();

            float distance = Random.Range(
                minDistance,
                maxDistance);

            Vector3 candidate = centerPosition;

            candidate.x += direction2D.x * distance;
            candidate.z += direction2D.y * distance;

            Vector2Int candidateCell;

            if (!m_mazeData.TryWorldToCell(
                candidate,
                out candidateCell))
            {
                continue;
            }

            MazeCellData cell = m_mazeData.GetCell(
                candidateCell.x,
                candidateCell.y);

            if (cell != null && !cell.IsBlockedCell())
            {
                return candidate;
            }
        }

        return centerPosition;
    }
}