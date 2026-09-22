using System.Collections.Generic;
using UnityEngine;

public sealed class ExportedTreasureResult
{
    public int TreasureId { get; private set; }
    public TreasureType TreasureType { get; private set; }
    public int Value { get; private set; }

    public ExportedTreasureResult(
        int treasureId,
        TreasureType treasureType,
        int value)
    {
        TreasureId = treasureId;
        TreasureType = treasureType;
        Value = value;
    }

    public void SetValue(int value)
    {
        Value = value;
    }
}

public sealed class TeamBattleResult
{
    private readonly List<ExportedTreasureResult> m_treasures =
        new List<ExportedTreasureResult>();

    public int TeamIndex { get; private set; }
    public IReadOnlyList<ExportedTreasureResult> Treasures
    {
        get { return m_treasures; }
    }

    public int TotalValue { get; private set; }
    public int Rank { get; private set; }
    public bool IsTournamentRepresentative { get; private set; }

    public TeamBattleResult(int teamIndex)
    {
        TeamIndex = teamIndex;
    }

    public void AddTreasure(
        ExportedTreasureResult treasure)
    {
        if (treasure != null)
        {
            m_treasures.Add(treasure);
        }
    }

    public void RefreshTotalValue()
    {
        TotalValue = 0;

        for (int i = 0; i < m_treasures.Count; i++)
        {
            TotalValue += m_treasures[i].Value;
        }
    }

    public void SetRank(int rank)
    {
        Rank = rank;
    }

    public void SetTournamentRepresentative(
        bool isRepresentative)
    {
        IsTournamentRepresentative = isRepresentative;
    }
}

public sealed class BattleResultData
{
    private readonly List<TeamBattleResult> m_teamResults =
        new List<TeamBattleResult>();

    public IReadOnlyList<TeamBattleResult> TeamResults
    {
        get { return m_teamResults; }
    }

    public void AddTeamResult(
        TeamBattleResult teamResult)
    {
        if (teamResult != null)
        {
            m_teamResults.Add(teamResult);
        }
    }
}