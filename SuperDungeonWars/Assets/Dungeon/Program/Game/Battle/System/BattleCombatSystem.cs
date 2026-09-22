using System.Collections.Generic;
using UnityEngine;

public class BattleCombatSystem
{
    private class PendingAttack
    {
        public ComCharacterBase m_attacker;
        public ComCharacterBase m_target;
        public float m_hitTime;
    }

    public enum AttackFocusTargetState
    {
        None,
        TreasureCarrier,
        Normal,
        Unavailable,
    }

    private readonly Dictionary<int, ComCharacterBase>
        m_characterById =
            new Dictionary<int, ComCharacterBase>();

    private readonly Dictionary<ComCharacterBase, int>
        m_teamIndexByCharacter =
            new Dictionary<ComCharacterBase, int>();

    private readonly Dictionary<ComCharacterBase, ComPartyBase>
        m_partyByCharacter =
            new Dictionary<ComCharacterBase, ComPartyBase>();

    private readonly Dictionary<ComCharacterBase, float>
        m_attackReadyTimeByCharacter =
            new Dictionary<ComCharacterBase, float>();

    private readonly Dictionary<ComCharacterBase, float>
        m_attackEndTimeByCharacter =
            new Dictionary<ComCharacterBase, float>();

    private const float s_attackFocusScoreHoldSeconds = 2.0f;

    private readonly Dictionary<ComCharacterBase, float>
        m_attackFocusScoreHoldEndTimeByCharacter =
            new Dictionary<ComCharacterBase, float>();

    private readonly Dictionary<
        ComCharacterBase,
        AttackFocusTargetState>
        m_attackFocusTargetStateByAttacker =
            new Dictionary<
                ComCharacterBase,
                AttackFocusTargetState>();

    private readonly Dictionary<ComCharacterBase, float>
        m_knockoutEndTimeByCharacter =
            new Dictionary<ComCharacterBase, float>();

    private readonly List<PendingAttack> m_pendingAttacks =
        new List<PendingAttack>();

    private readonly List<PendingAttack> m_dueAttacks =
        new List<PendingAttack>();

    private BattleGameRuleData m_ruleData;
    private BattleTreasureSystem m_battleTreasureSystem;

    public void Initialize(
        BattleGameRuleData ruleData,
        IReadOnlyList<ComPartyBase> parties,
        BattleTreasureSystem battleTreasureSystem)
    {
        Shutdown();

        m_ruleData = ruleData;
        m_battleTreasureSystem = battleTreasureSystem;

        if (m_ruleData == null || parties == null)
        {
            return;
        }

        for (int teamIndex = 0;
             teamIndex < parties.Count;
             teamIndex++)
        {
            ComPartyBase party = parties[teamIndex];

            if (party == null)
            {
                continue;
            }

            for (int memberIndex = 0;
                 memberIndex < party.MemberCount;
                 memberIndex++)
            {
                ComCharacterBase member;

                if (!party.TryGetMember(
                    memberIndex,
                    out member))
                {
                    continue;
                }

                RegisterCharacter(
                    party,
                    teamIndex,
                    member);
            }
        }
    }

    public void Shutdown()
    {
        m_characterById.Clear();
        m_teamIndexByCharacter.Clear();
        m_partyByCharacter.Clear();
        m_attackReadyTimeByCharacter.Clear();
        m_knockoutEndTimeByCharacter.Clear();
        m_pendingAttacks.Clear();
        m_dueAttacks.Clear();
        m_attackEndTimeByCharacter.Clear();
        m_attackFocusScoreHoldEndTimeByCharacter.Clear();
        m_attackFocusTargetStateByAttacker.Clear();

        m_ruleData = null;
        m_battleTreasureSystem = null;
    }

    public void UpdateSystem()
    {
        if (m_ruleData == null)
        {
            return;
        }

        RecoverKnockedOutCharacters();
        ResolveDueAttacks();
        RequestAttacksFromParticipants();
    }

    public bool IsCharacterAttacking(
        ComCharacterBase character)
    {
        if (character == null || character.IsKnockedOut())
        {
            return false;
        }

        float attackEndTime;

        return m_attackEndTimeByCharacter.TryGetValue(
            character,
            out attackEndTime)
            && Time.time < attackEndTime;
    }

    public bool TryGetAttackFocusScore(
        ComCharacterBase attacker,
        out float score)
    {
        score = 0.0f;

        if (!IsCharacterAttackFocusScoreHeld(
                attacker))
        {
            return false;
        }

        AttackFocusTargetState targetState;

        if (!m_attackFocusTargetStateByAttacker.TryGetValue(
                attacker,
                out targetState))
        {
            score = 620.0f;
            return true;
        }

        switch (targetState)
        {
            case AttackFocusTargetState.TreasureCarrier:
                score = 950.0f;
                return true;

            case AttackFocusTargetState.Normal:
                score = 730.0f;
                return true;

            case AttackFocusTargetState.None:
            case AttackFocusTargetState.Unavailable:
            default:
                score = 620.0f;
                return true;
        }
    }

    public bool HasEnemyInAttackArea(
        ComCharacterBase attacker)
    {
        if (attacker == null || attacker.IsKnockedOut())
        {
            return false;
        }

        int attackerTeamIndex;

        if (!m_teamIndexByCharacter.TryGetValue(
                attacker,
                out attackerTeamIndex))
        {
            return false;
        }

        foreach (
            KeyValuePair<int, ComCharacterBase> pair
            in m_characterById)
        {
            ComCharacterBase target = pair.Value;

            if (target == null
                || target == attacker
                || target.IsKnockedOut()
                || !IsRuntimeCharacterActive(target))
            {
                continue;
            }

            int targetTeamIndex;

            if (!m_teamIndexByCharacter.TryGetValue(
                    target,
                    out targetTeamIndex)
                || targetTeamIndex == attackerTeamIndex)
            {
                continue;
            }

            if (IsInAttackArea(attacker, target))
            {
                return true;
            }
        }

        return false;
    }

    private AttackFocusTargetState GetAttackFocusTargetState(
        ComCharacterBase target)
    {
        if (target == null)
        {
            return AttackFocusTargetState.None;
        }

        if (!IsRuntimeCharacterActive(target)
            || target.IsKnockedOut())
        {
            return AttackFocusTargetState.Unavailable;
        }

        if (target.HasTreasure())
        {
            return AttackFocusTargetState.TreasureCarrier;
        }

        return AttackFocusTargetState.Normal;
    }

    private bool IsCharacterAttackFocusScoreHeld(
        ComCharacterBase character)
    {
        if (character == null
            || !IsRuntimeCharacterActive(character))
        {
            return false;
        }

        float holdEndTime;

        return m_attackFocusScoreHoldEndTimeByCharacter
            .TryGetValue(
                character,
                out holdEndTime)
            && Time.time < holdEndTime;
    }

    private void RegisterCharacter(
        ComPartyBase party,
        int teamIndex,
        ComCharacterBase character)
    {
        if (party == null
            || character == null
            || character.GetCharacterId() < 0)
        {
            return;
        }

        if (m_characterById.ContainsKey(
                character.GetCharacterId()))
        {
            Debug.LogError(
                "BattleCombatSystem: CharacterId が重複しています。 id="
                + character.GetCharacterId(),
                character);

            return;
        }

        m_characterById.Add(
            character.GetCharacterId(),
            character);

        m_teamIndexByCharacter.Add(
            character,
            teamIndex);

        m_partyByCharacter.Add(
            character,
            party);

        m_attackReadyTimeByCharacter.Add(
            character,
            0.0f);

        character.ClearActionLock();

        SetKnockedOutAnimation(
            character,
            false);
    }

    private void RecoverKnockedOutCharacters()
    {
        if (m_knockoutEndTimeByCharacter.Count <= 0)
        {
            return;
        }

        List<ComCharacterBase> recoveredCharacters =
            new List<ComCharacterBase>();

        foreach (
            KeyValuePair<ComCharacterBase, float> pair
            in m_knockoutEndTimeByCharacter)
        {
            if (pair.Key != null
                && Time.time >= pair.Value)
            {
                recoveredCharacters.Add(pair.Key);
            }
        }

        for (int i = 0;
             i < recoveredCharacters.Count;
             i++)
        {
            ComCharacterBase character =
                recoveredCharacters[i];

            m_knockoutEndTimeByCharacter.Remove(
                character);

            character.SetKnockedOut(false);

            if (!SetKnockedOutPresentation(
                    character,
                    false,
                    Vector3.zero,
                    Vector3.zero))
            {
                SetKnockedOutAnimation(
                    character,
                    false);
            }

            character.LockActionUntil(
                Time.time
                + m_ruleData.KnockoutRecoveryMoveLockSeconds);
        }
    }

    private void RequestAttacksFromParticipants()
    {
        foreach (
            KeyValuePair<int, ComCharacterBase> pair
            in m_characterById)
        {
            ComCharacterBase attacker = pair.Value;

            if (!CanStartAttack(attacker))
            {
                continue;
            }

            List<VisibleCharacterData> candidates =
                CreateAttackCandidates(attacker);

            if (candidates.Count <= 0)
            {
                continue;
            }

            int selectedCandidateIndex =
                attacker.SelectAttackTarget(candidates);

            ComCharacterBase target;

            if (!TryFindCandidateTarget(
                    candidates,
                    selectedCandidateIndex,
                    out target))
            {
                continue;
            }

            StartAttack(attacker, target);
        }
    }

    private bool CanStartAttack(
        ComCharacterBase attacker)
    {
        if (attacker == null
            || !IsRuntimeCharacterActive(attacker)
            || attacker.IsKnockedOut()
            || attacker.IsActionLocked()
            || attacker.HasTreasure()
            || HasPendingAttack(attacker))
        {
            return false;
        }

        float attackReadyTime;

        if (m_attackReadyTimeByCharacter.TryGetValue(
                attacker,
                out attackReadyTime)
            && Time.time < attackReadyTime)
        {
            return false;
        }

        return true;
    }

    private List<VisibleCharacterData> CreateAttackCandidates(
        ComCharacterBase attacker)
    {
        List<VisibleCharacterData> result =
            new List<VisibleCharacterData>();

        ComPartyBase attackerParty;

        if (!m_partyByCharacter.TryGetValue(
                attacker,
                out attackerParty)
            || attackerParty == null)
        {
            return result;
        }

        int attackerTeamIndex;

        if (!m_teamIndexByCharacter.TryGetValue(
                attacker,
                out attackerTeamIndex))
        {
            return result;
        }

        VisibleWorldData visibleWorldData =
            attackerParty.GetVisibleWorldData();

        if (visibleWorldData == null)
        {
            return result;
        }

        foreach (
            KeyValuePair<int, ComCharacterBase> pair
            in m_characterById)
        {
            ComCharacterBase target = pair.Value;

            if (target == null
                || target == attacker
                || !IsRuntimeCharacterActive(target))
            {
                continue;
            }

            int targetTeamIndex;

            if (!m_teamIndexByCharacter.TryGetValue(
                    target,
                    out targetTeamIndex)
                || targetTeamIndex == attackerTeamIndex)
            {
                continue;
            }

            VisibleCharacterData visibleCharacter;

            if (!visibleWorldData.TryGetVisibleCharacter(
                    target.GetCharacterId(),
                    out visibleCharacter))
            {
                continue;
            }

            if (!IsInAttackStartArea(attacker, target))
            {
                continue;
            }

            result.Add(visibleCharacter);
        }

        return result;
    }

    private bool TryFindCandidateTarget(
        IReadOnlyList<VisibleCharacterData> candidates,
        int selectedCandidateIndex,
        out ComCharacterBase target)
    {
        target = null;

        if (candidates == null
            || selectedCandidateIndex < 0
            || selectedCandidateIndex >= candidates.Count)
        {
            return false;
        }

        int selectedCharacterId =
            candidates[selectedCandidateIndex].CharacterId;

        return m_characterById.TryGetValue(
            selectedCharacterId,
            out target);
    }

    private void StartAttack(
        ComCharacterBase attacker,
        ComCharacterBase target)
    {
        if (attacker == null
            || target == null
            || m_ruleData == null)
        {
            return;
        }

        float actionLockSeconds = Mathf.Max(
            m_ruleData.AttackMotionLockSeconds,
            m_ruleData.AttackHitDelaySeconds);

        attacker.LockActionUntil(
            Time.time + actionLockSeconds);

        m_attackEndTimeByCharacter[attacker] =
            Time.time + actionLockSeconds;

        m_attackFocusScoreHoldEndTimeByCharacter[attacker] =
            Time.time + s_attackFocusScoreHoldSeconds;

        m_attackFocusTargetStateByAttacker[attacker] =
            GetAttackFocusTargetState(target);

        ExplorerAgent targetAgent =
            target.GetExplorerAgent() as ExplorerAgent;

        CharacterRagdollController targetRagdollController =
            targetAgent != null
                ? targetAgent.GetCharacterRagdollController()
                : null;

        bool isDownTarget =
            target.IsKnockedOut()
            || (targetRagdollController != null
                && targetRagdollController.IsRagdollActive);

        ExplorerAgent attackerAgent =
            attacker.GetExplorerAgent() as ExplorerAgent;

        CharacterAnimationController animationController =
            attackerAgent != null
                ? attackerAgent.GetCharacterAnimationController()
                : null;

        if (animationController != null)
        {
            if (isDownTarget)
            {
                animationController.PlayLowKick();
            }
            else
            {
                animationController.PlayPunch();
            }
        }

        PendingAttack pendingAttack =
            new PendingAttack();

        pendingAttack.m_attacker = attacker;
        pendingAttack.m_target = target;
        pendingAttack.m_hitTime =
            Time.time + m_ruleData.AttackHitDelaySeconds;

        m_pendingAttacks.Add(pendingAttack);

        m_attackReadyTimeByCharacter[attacker] =
            Time.time
            + m_ruleData.AttackMotionLockSeconds
            + m_ruleData.AttackCooldownSeconds;
    }

    private void ResolveDueAttacks()
    {
        m_dueAttacks.Clear();

        for (int i = m_pendingAttacks.Count - 1;
             i >= 0;
             i--)
        {
            PendingAttack pendingAttack =
                m_pendingAttacks[i];

            if (pendingAttack == null
                || pendingAttack.m_attacker == null
                || pendingAttack.m_target == null)
            {
                m_pendingAttacks.RemoveAt(i);
                continue;
            }

            if (Time.time < pendingAttack.m_hitTime)
            {
                continue;
            }

            m_dueAttacks.Add(pendingAttack);
            m_pendingAttacks.RemoveAt(i);
        }

        if (m_dueAttacks.Count <= 0)
        {
            return;
        }

        Dictionary<ComCharacterBase, bool> wasKnockedOut =
            new Dictionary<ComCharacterBase, bool>();

        for (int i = 0; i < m_dueAttacks.Count; i++)
        {
            PendingAttack attack = m_dueAttacks[i];

            if (!wasKnockedOut.ContainsKey(
                    attack.m_attacker))
            {
                wasKnockedOut.Add(
                    attack.m_attacker,
                    attack.m_attacker.IsKnockedOut());
            }

            if (!wasKnockedOut.ContainsKey(
                    attack.m_target))
            {
                wasKnockedOut.Add(
                    attack.m_target,
                    attack.m_target.IsKnockedOut());
            }
        }

        for (int i = 0; i < m_dueAttacks.Count; i++)
        {
            PendingAttack attack = m_dueAttacks[i];

            if (!CanHitAtResolveTime(
                    attack,
                    wasKnockedOut))
            {
                continue;
            }

            KnockOutCharacter(
                attack.m_attacker,
                attack.m_target);
        }
    }

    private bool CanHitAtResolveTime(
        PendingAttack attack,
        Dictionary<ComCharacterBase, bool> wasKnockedOut)
    {
        if (attack == null
            || attack.m_attacker == null
            || attack.m_target == null)
        {
            return false;
        }

        bool attackerWasKnockedOut;

        if (!wasKnockedOut.TryGetValue(
                attack.m_attacker,
                out attackerWasKnockedOut)
            || attackerWasKnockedOut)
        {
            return false;
        }

        return IsInAttackArea(
            attack.m_attacker,
            attack.m_target);
    }

    private void KnockOutCharacter(
        ComCharacterBase attacker,
        ComCharacterBase target)
    {
        if (attacker == null
            || target == null
            || m_ruleData == null)
        {
            return;
        }

        bool targetWasKnockedOut =
            target.IsKnockedOut();

        Debug.Log(
            "[BattleCombatSystem.KnockOut] "
            + $"attacker={attacker.name} "
            + $"attackerId={attacker.GetCharacterId()} "
            + $"target={target.name} "
            + $"targetId={target.GetCharacterId()} "
            + $"targetWasKnockedOut={targetWasKnockedOut} "
            + $"time={Time.time:F2}",
            target);

        target.SetKnockedOut(true);

        ExplorerAgent attackerAgent =
            attacker.GetExplorerAgent() as ExplorerAgent;

        Transform attackerTransform =
            attackerAgent != null
                ? attackerAgent.GetRuntimeTransform()
                : null;

        Vector3 attackForwardDirection =
            attackerTransform != null
                ? attackerTransform.forward
                : Vector3.zero;

        attackForwardDirection.y = 0.0f;

        if (attackForwardDirection.sqrMagnitude <= 0.0001f)
        {
            attackForwardDirection =
                target.GetWorldPosition()
                - attacker.GetWorldPosition();

            attackForwardDirection.y = 0.0f;
        }

        bool isRagdollPresentationStarted =
            SetKnockedOutPresentation(
                target,
                true,
                attackForwardDirection,
                attacker.GetWorldPosition());

        if (!isRagdollPresentationStarted)
        {
            SetKnockedOutAnimation(
                target,
                true);

            ExplorerAgent targetAgent =
                target.GetExplorerAgent() as ExplorerAgent;

            if (targetAgent != null)
            {
                targetAgent.ApplyKnockback(
                    attackForwardDirection,
                    m_ruleData.KnockbackSpeed);
            }
        }

        m_knockoutEndTimeByCharacter[target] =
            Time.time + m_ruleData.KnockoutSeconds;

        if (!targetWasKnockedOut
            && m_battleTreasureSystem != null)
        {
            Vector3 treasureDropDirection =
                target.GetWorldPosition()
                - attacker.GetWorldPosition();

            treasureDropDirection.y = 0.0f;

            m_battleTreasureSystem.DropCarriedTreasure(
                target,
                treasureDropDirection);
        }
    }

    private bool HasPendingAttack(
        ComCharacterBase attacker)
    {
        for (int i = 0; i < m_pendingAttacks.Count; i++)
        {
            if (m_pendingAttacks[i].m_attacker == attacker)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsInAttackStartArea(
        ComCharacterBase attacker,
        ComCharacterBase target)
    {
        if (m_ruleData == null)
        {
            return false;
        }

        return IsInAttackArea(
            attacker,
            target,
            m_ruleData.AttackStartRange);
    }

    private bool IsInAttackArea(
        ComCharacterBase attacker,
        ComCharacterBase target)
    {
        if (m_ruleData == null)
        {
            return false;
        }

        return IsInAttackArea(
            attacker,
            target,
            m_ruleData.AttackRange);
    }

    private bool IsInAttackArea(
        ComCharacterBase attacker,
        ComCharacterBase target,
        float attackRange)
    {
        if (attacker == null
            || target == null
            || m_ruleData == null)
        {
            return false;
        }

        Vector3 toTarget =
            target.GetWorldPosition()
            - attacker.GetWorldPosition();

        toTarget.y = 0.0f;

        float sqrDistance = toTarget.sqrMagnitude;

        if (sqrDistance > attackRange * attackRange
            || sqrDistance <= 0.0001f)
        {
            return false;
        }

        ExplorerAgent attackerAgent =
            attacker.GetExplorerAgent() as ExplorerAgent;

        Transform attackerTransform =
            attackerAgent != null
                ? attackerAgent.GetRuntimeTransform()
                : null;

        if (attackerTransform == null)
        {
            return false;
        }

        Vector3 forward = attackerTransform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float angle = Vector3.Angle(
            forward.normalized,
            toTarget.normalized);

        return angle <= m_ruleData.AttackAngleDegrees * 0.5f;
    }

    private void SetKnockedOutAnimation(
        ComCharacterBase character,
        bool isKnockedOut)
    {
        if (character == null)
        {
            return;
        }

        ExplorerAgent agent =
            character.GetExplorerAgent() as ExplorerAgent;

        CharacterAnimationController animationController =
            agent != null
                ? agent.GetCharacterAnimationController()
                : null;

        if (animationController == null)
        {
            return;
        }

        animationController.SetKnockedOut(
            isKnockedOut);
    }

    private bool SetKnockedOutPresentation(
        ComCharacterBase character,
        bool isKnockedOut,
        Vector3 attackForwardDirection,
        Vector3 attackerWorldPosition)
    {
        if (character == null)
        {
            return false;
        }

        ExplorerAgent agent =
            character.GetExplorerAgent() as ExplorerAgent;

        CharacterRagdollController ragdollController =
            agent != null
                ? agent.GetCharacterRagdollController()
                : null;

        if (ragdollController == null)
        {
            return false;
        }

        if (isKnockedOut)
        {
            ragdollController.KnockOut(
                attackForwardDirection,
                attackerWorldPosition,
                m_ruleData.KnockbackSpeed);
        }
        else
        {
            ragdollController.Recover();
        }

        return true;
    }

    private bool IsRuntimeCharacterActive(
        ComCharacterBase character)
    {
        if (character == null)
        {
            return false;
        }

        ExplorerAgent agent =
            character.GetExplorerAgent() as ExplorerAgent;

        Transform runtimeTransform =
            agent != null
                ? agent.GetRuntimeTransform()
                : null;

        return runtimeTransform != null
            && runtimeTransform.gameObject.activeInHierarchy;
    }
}