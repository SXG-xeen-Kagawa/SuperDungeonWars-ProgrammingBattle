using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public sealed class VisibleCharacterData
{
    public int CharacterId { get; private set; }
    public int TeamIndex { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Vector2Int CellPosition { get; private set; }
    public Vector3 Forward { get; private set; }
    public bool HasTreasure { get; private set; }
    public bool IsKnockedOut { get; private set; }

    public VisibleCharacterData(
        int characterId,
        int teamIndex,
        Vector3 worldPosition,
        Vector2Int cellPosition,
        Vector3 forward,
        bool hasTreasure,
        bool isKnockedOut)
    {
        CharacterId = characterId;
        TeamIndex = teamIndex;
        WorldPosition = worldPosition;
        CellPosition = cellPosition;
        Forward = forward;
        HasTreasure = hasTreasure;
        IsKnockedOut = isKnockedOut;
    }
}

public sealed class VisibleTreasureData
{
    public int TreasureId { get; private set; }
    public Vector3 WorldPosition { get; private set; }
    public Vector2Int CellPosition { get; private set; }
    public TreasureType TreasureType { get; private set; }
    public bool IsCarried { get; private set; }
    public int OwnerCharacterId { get; private set; }

    public VisibleTreasureData(
        int treasureId,
        Vector3 worldPosition,
        Vector2Int cellPosition,
        TreasureType treasureType,
        bool isCarried,
        int ownerCharacterId)
    {
        TreasureId = treasureId;
        WorldPosition = worldPosition;
        CellPosition = cellPosition;
        TreasureType = treasureType;
        IsCarried = isCarried;
        OwnerCharacterId = ownerCharacterId;
    }
}

public sealed class VisibleWorldData
{
    private readonly List<VisibleCharacterData>
        m_visibleCharacters =
            new List<VisibleCharacterData>();

    private readonly List<VisibleTreasureData>
        m_visibleTreasures =
            new List<VisibleTreasureData>();

    private readonly ReadOnlyCollection<VisibleCharacterData>
        m_readOnlyVisibleCharacters;

    private readonly ReadOnlyCollection<VisibleTreasureData>
        m_readOnlyVisibleTreasures;

    public IReadOnlyList<VisibleCharacterData>
        VisibleCharacters
    {
        get { return m_readOnlyVisibleCharacters; }
    }

    public IReadOnlyList<VisibleTreasureData>
        VisibleTreasures
    {
        get { return m_readOnlyVisibleTreasures; }
    }

    public VisibleWorldData()
    {
        m_readOnlyVisibleCharacters =
            m_visibleCharacters.AsReadOnly();

        m_readOnlyVisibleTreasures =
            m_visibleTreasures.AsReadOnly();
    }

    public bool TryGetVisibleCharacter(
        int characterId,
        out VisibleCharacterData visibleCharacter)
    {
        for (int i = 0; i < m_visibleCharacters.Count; i++)
        {
            if (m_visibleCharacters[i].CharacterId ==
                characterId)
            {
                visibleCharacter = m_visibleCharacters[i];
                return true;
            }
        }

        visibleCharacter = null;
        return false;
    }

    public bool TryGetVisibleTreasure(
        int treasureId,
        out VisibleTreasureData visibleTreasure)
    {
        for (int i = 0; i < m_visibleTreasures.Count; i++)
        {
            if (m_visibleTreasures[i].TreasureId ==
                treasureId)
            {
                visibleTreasure = m_visibleTreasures[i];
                return true;
            }
        }

        visibleTreasure = null;
        return false;
    }

    internal void Clear()
    {
        m_visibleCharacters.Clear();
        m_visibleTreasures.Clear();
    }

    internal void AddCharacter(
        VisibleCharacterData visibleCharacter)
    {
        if (visibleCharacter == null)
        {
            return;
        }

        m_visibleCharacters.Add(visibleCharacter);
    }

    internal void AddTreasure(
        VisibleTreasureData visibleTreasure)
    {
        if (visibleTreasure == null)
        {
            return;
        }

        m_visibleTreasures.Add(visibleTreasure);
    }
}