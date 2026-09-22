using System.Collections.Generic;
using UnityEngine;

public abstract partial class ComCharacterBase
{
    private ICharacterRuntime m_characterRuntime;

    private ComPartyBase m_ownerParty;
    private KnownMapData m_knownMapData;
    private KnownMapView m_knownMapView;
    private int m_memberIndex;
    private PartyMemberOrder m_currentOrder;

    private ITreasureRuntime m_carriedTreasure;

    private int m_characterId = -1;
    private bool m_isKnockedOut;
    private float m_actionLockEndTime;

    internal void SetCharacterId(int characterId)
    {
        if (characterId < 0)
        {
            return;
        }

        m_characterId = characterId;
    }

    internal void Initialize(
        ComPartyBase ownerParty,
        KnownMapData knownMapData,
        int memberIndex,
        ICharacterRuntime characterRuntime)
    {
        m_ownerParty = ownerParty;
        m_knownMapData = knownMapData;
        m_knownMapView = m_knownMapData != null
            ? new KnownMapView(m_knownMapData)
            : null;

        m_memberIndex = memberIndex;
        m_currentOrder = PartyMemberOrder.CreateStop();
        m_carriedTreasure = null;
        m_isKnockedOut = false;
        m_actionLockEndTime = 0.0f;

        m_characterRuntime = characterRuntime;

        if (m_characterRuntime == null)
        {
            Debug.LogError(
                "ComCharacterBase: Runtime Character が未設定です。"
                + " Character="
                + name,
                this);

            return;
        }

        m_characterRuntime.BindCharacter(this);
    }

    internal void Think()
    {
        SXG_OnCharacterThink();
    }

    internal void SetOrder(PartyMemberOrder order)
    {
        m_currentOrder = order;
    }

    internal void ApplyOrder()
    {
        if (m_characterRuntime == null)
        {
            return;
        }

        // ノックアウト中・攻撃中・起き上がり中は、
        // 参加者の移動命令を反映しません。
        if (m_isKnockedOut || IsActionLocked())
        {
            m_characterRuntime.StopMove();
            return;
        }

        switch (m_currentOrder.m_orderType)
        {
            case PartyMemberOrderType.MoveToCell:
                m_characterRuntime.TryMoveToCell(
                    m_currentOrder.m_targetCell);
                break;

            case PartyMemberOrderType.MoveToWorld:
                m_characterRuntime.TryMoveToWorld(
                    m_currentOrder.m_targetWorld);
                break;

            case PartyMemberOrderType.Stop:
                m_characterRuntime.StopMove();
                break;
        }
    }

    internal ICharacterRuntime GetExplorerAgent()
    {
        return m_characterRuntime;
    }

    internal int GetCharacterId()
    {
        return m_characterId;
    }

    internal int GetMemberIndex()
    {
        return m_memberIndex;
    }

    internal Vector2Int GetCurrentCell()
    {
        return m_characterRuntime != null
            ? m_characterRuntime.CurrentCell
            : Vector2Int.zero;
    }

    internal Vector3 GetWorldPosition()
    {
        return m_characterRuntime != null
            ? m_characterRuntime.WorldPosition
            : Vector3.zero;
    }

    internal bool IsMoving()
    {
        return !m_isKnockedOut
            && m_characterRuntime != null
            && m_characterRuntime.IsMoving;
    }

    internal bool IsKnockedOut()
    {
        return m_isKnockedOut;
    }

    internal bool IsActionLocked()
    {
        return Time.time < m_actionLockEndTime;
    }

    internal bool HasTreasure()
    {
        return m_carriedTreasure != null;
    }

    internal ITreasureRuntime GetCarriedTreasure()
    {
        return m_carriedTreasure;
    }

    internal void NotifyCellReached(Vector2Int cellPosition)
    {
        SXG_OnCellReached(cellPosition);
    }

    internal void SetKnockedOut(bool isKnockedOut)
    {
        m_isKnockedOut = isKnockedOut;

        if (m_isKnockedOut && m_characterRuntime != null)
        {
            m_characterRuntime.StopMove();
        }
    }

    internal void LockActionUntil(float actionLockEndTime)
    {
        m_actionLockEndTime = Mathf.Max(
            m_actionLockEndTime,
            actionLockEndTime);

        if (m_characterRuntime != null)
        {
            m_characterRuntime.StopMove();
        }
    }

    internal void ClearActionLock()
    {
        m_actionLockEndTime = 0.0f;
    }

    internal bool SetCarriedTreasure(
        ITreasureRuntime treasure)
    {
        if (treasure == null
            || m_carriedTreasure != null)
        {
            return false;
        }

        m_carriedTreasure = treasure;
        return true;
    }

    internal void ClearCarriedTreasure(
        ITreasureRuntime treasure)
    {
        if (treasure != null
            && m_carriedTreasure == treasure)
        {
            m_carriedTreasure = null;
        }
    }

    internal int SelectAttackTarget(
        IReadOnlyList<VisibleCharacterData> attackableCharacters)
    {
        return SXG_ChooseAttackTargetIndex(
            attackableCharacters);
    }

    internal bool TryPickUpTreasure(TreasureType treasureType)
    {
        return SXG_ShouldPickUpTreasure(treasureType);
    }

    internal void NotifyTreasureExported(TreasureType treasureType)
    {
        SXG_OnTreasureExported(treasureType);
    }
}