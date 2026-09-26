using System.Collections.Generic;
using UnityEngine;

public abstract partial class ComPartyBase
{
    private ParticipantMazeBounds m_mazeBounds;

    private KnownMapData m_knownMapData;
    private KnownMapView m_knownMapView;

    private readonly List<ComCharacterBase> m_members =
        new List<ComCharacterBase>();

    private bool m_isStarted;

    private Vector2Int m_returnEntranceCell;
    private bool m_hasReturnEntranceCell;

    private readonly List<Vector2Int> m_exportEntranceCells =
        new List<Vector2Int>();

    private readonly List<ITreasureRuntime> m_exportedTreasures =
        new List<ITreasureRuntime>();

    private VisibleWorldData m_visibleWorldData =
        new VisibleWorldData();

    private static Sprite s_defaultTeamIconImage;

    public int MemberCount
    {
        get { return m_members.Count; }
    }

    public string CreatorDisplayName
    {
        get
        {
            return string.IsNullOrEmpty(m_creatorDisplayName)
                ? "制作者名"
                : m_creatorDisplayName;
        }
    }

    public string Affiliation
    {
        get
        {
            return string.IsNullOrEmpty(m_affiliation)
                ? "所属名"
                : m_affiliation;
        }
    }

    public string TeamDisplayName
    {
        get
        {
            return string.IsNullOrEmpty(m_teamDisplayName)
                ? "チーム名"
                : m_teamDisplayName;
        }
    }

    public string TeamSimpleDescription
    {
        get
        {
            return string.IsNullOrEmpty(m_teamSimpleDescription)
                ? "チーム簡易説明"
                : m_teamSimpleDescription;
        }
    }

    public Sprite TeamIconSprite
    {
        get
        {
            if (m_teamIconSprite != null)
            {
                return m_teamIconSprite;
            }

            if (s_defaultTeamIconImage == null)
            {
                s_defaultTeamIconImage =
                    Resources.Load<Sprite>("Textures/noimage");
            }

            return s_defaultTeamIconImage;
        }
    }

    internal ComCharacterBase GetMemberPrefab()
    {
        return m_memberPrefabs;
    }

    internal void Initialize(
        ParticipantMazeBounds mazeBounds,
        KnownMapData knownMapData,
        Vector2Int[] entranceCellsByRelativeTeamIndex)
    {
        m_members.Clear();

        m_mazeBounds = mazeBounds;

        if (m_mazeBounds == null)
        {
            Debug.LogError(
                "ComPartyBase.Initialize: 迷路範囲情報がnullです。",
                this);

            return;
        }

        m_knownMapData = knownMapData != null
            ? knownMapData
            : new KnownMapData(
                m_mazeBounds.Width,
                m_mazeBounds.Height);

        m_knownMapView = new KnownMapView(m_knownMapData);
        m_visibleWorldData = new VisibleWorldData();

        m_returnEntranceCell = Vector2Int.zero;
        m_hasReturnEntranceCell = false;

        m_exportEntranceCells.Clear();
        m_exportedTreasures.Clear();
        m_isStarted = false;

        if (entranceCellsByRelativeTeamIndex == null)
        {
            Debug.LogError(
                "ComPartyBase.Initialize: 入口セル配列がnullです。",
                this);

            return;
        }

        for (int i = 0;
             i < entranceCellsByRelativeTeamIndex.Length;
             i++)
        {
            Vector2Int entranceCell =
                entranceCellsByRelativeTeamIndex[i];

            if (!m_mazeBounds.IsInside(entranceCell))
            {
                continue;
            }

            if (!m_exportEntranceCells.Contains(entranceCell))
            {
                m_exportEntranceCells.Add(entranceCell);
            }
        }

        if (m_exportEntranceCells.Count <= 0)
        {
            Debug.LogError(
                "ComPartyBase.Initialize: 有効な入口セルがありません。",
                this);

            return;
        }

        m_returnEntranceCell = m_exportEntranceCells[0];
        m_hasReturnEntranceCell = true;
    }

    internal void RegisterMembers(ComCharacterBase[] members)
    {
        m_members.Clear();

        if (members == null)
        {
            return;
        }

        for (int i = 0; i < members.Length; i++)
        {
            if (members[i] != null)
            {
                m_members.Add(members[i]);
            }
        }
    }

    internal void StartParty()
    {
        m_isStarted = true;
    }

    internal void Think()
    {
        if (!m_isStarted)
        {
            return;
        }

        SXG_OnPartyThink();
    }

    internal bool TryGetMember(
        int memberIndex,
        out ComCharacterBase member)
    {
        member = null;

        if (memberIndex < 0
            || memberIndex >= m_members.Count)
        {
            return false;
        }

        member = m_members[memberIndex];
        return member != null;
    }

    internal Vector2Int[] GetMemberCurrentCells()
    {
        Vector2Int[] result =
            new Vector2Int[m_members.Count];

        for (int i = 0; i < m_members.Count; i++)
        {
            ComCharacterBase member = m_members[i];

            result[i] = member != null
                ? member.GetCurrentCell()
                : Vector2Int.zero;
        }

        return result;
    }

    internal KnownMapData GetKnownMapData()
    {
        return m_knownMapData;
    }

    internal VisibleWorldData GetVisibleWorldData()
    {
        return m_visibleWorldData;
    }

    internal IReadOnlyList<ITreasureRuntime> GetExportedTreasureChests()
    {
        return m_exportedTreasures;
    }

    internal bool TryGetReturnEntranceCell(
        out Vector2Int returnEntranceCell)
    {
        returnEntranceCell = m_returnEntranceCell;
        return m_hasReturnEntranceCell;
    }

    internal void SetReturnEntranceCell(
        Vector2Int returnEntranceCell)
    {
        if (m_mazeBounds == null
            || !m_mazeBounds.IsInside(returnEntranceCell))
        {
            m_hasReturnEntranceCell = false;
            return;
        }

        m_returnEntranceCell = returnEntranceCell;
        m_hasReturnEntranceCell = true;
    }

    internal bool IsExportEntranceCell(
        Vector2Int cellPosition)
    {
        return m_exportEntranceCells.Contains(cellPosition);
    }

    internal void AddExportedTreasure(
        ITreasureRuntime treasure)
    {
        if (treasure == null
            || m_exportedTreasures.Contains(treasure))
        {
            return;
        }

        m_exportedTreasures.Add(treasure);
    }

}