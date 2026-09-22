using System.Collections.Generic;
using UnityEngine;

public static class BattleResultCalculator
{
    public static BattleResultData CreateResult(
        List<ComPartyBase> parties,
        BattleGameRuleData battleGameRuleData)
    {
        BattleResultData result = new BattleResultData();

        if (parties == null || battleGameRuleData == null)
        {
            return result;
        }

        for (int teamIndex = 0;
             teamIndex < parties.Count;
             teamIndex++)
        {
            TeamBattleResult teamResult =
                new TeamBattleResult(teamIndex);

            ComPartyBase party = parties[teamIndex];

            if (party != null)
            {
                IReadOnlyList<ITreasureRuntime> exportedTreasures =
                    party.GetExportedTreasureChests();

                for (int i = 0;
                     i < exportedTreasures.Count;
                     i++)
                {
                    var treasureChest =
                        exportedTreasures[i];

                    if (treasureChest == null)
                    {
                        continue;
                    }

                    int value = battleGameRuleData.GetRandomTreasureValue(
                        treasureChest.TreasureType);

                    teamResult.AddTreasure(
                        new ExportedTreasureResult(
                            treasureChest.TreasureId,
                            treasureChest.TreasureType,
                            value));
                }
            }

            teamResult.RefreshTotalValue();
            result.AddTeamResult(teamResult);
        }

        AdjustPositiveTotalValues(result, battleGameRuleData);
        AssignRanks(result);
        SelectTournamentRepresentative(result);

        return result;
    }

    private static void AdjustPositiveTotalValues(
        BattleResultData result,
        BattleGameRuleData battleGameRuleData)
    {
        HashSet<int> usedPositiveTotals =
            new HashSet<int>();

        for (int i = 0; i < result.TeamResults.Count; i++)
        {
            TeamBattleResult teamResult =
                result.TeamResults[i];

            if (teamResult.TotalValue <= 0)
            {
                continue;
            }

            if (!usedPositiveTotals.Contains(teamResult.TotalValue))
            {
                usedPositiveTotals.Add(teamResult.TotalValue);
                continue;
            }

            int minTotal;
            int maxTotal;

            GetTotalRange(
                teamResult,
                battleGameRuleData,
                out minTotal,
                out maxTotal);

            int adjustedTotal = FindUnusedNearestTotal(
                teamResult.TotalValue,
                minTotal,
                maxTotal,
                usedPositiveTotals);

            ApplyTotalValue(
                teamResult,
                battleGameRuleData,
                adjustedTotal);

            teamResult.RefreshTotalValue();
            usedPositiveTotals.Add(teamResult.TotalValue);
        }
    }

    private static void GetTotalRange(
        TeamBattleResult teamResult,
        BattleGameRuleData battleGameRuleData,
        out int minTotal,
        out int maxTotal)
    {
        minTotal = 0;
        maxTotal = 0;

        for (int i = 0; i < teamResult.Treasures.Count; i++)
        {
            ExportedTreasureResult treasure =
                teamResult.Treasures[i];

            int minValue;
            int maxValue;

            if (!battleGameRuleData.TryGetTreasureValueRange(
                    treasure.TreasureType,
                    out minValue,
                    out maxValue))
            {
                continue;
            }

            minTotal += minValue;
            maxTotal += maxValue;
        }
    }

    private static int FindUnusedNearestTotal(
        int originalTotal,
        int minTotal,
        int maxTotal,
        HashSet<int> usedTotals)
    {
        for (int offset = 0;
             offset <= maxTotal - minTotal;
             offset++)
        {
            int lower = originalTotal - offset;

            if (lower >= minTotal
                && !usedTotals.Contains(lower))
            {
                return lower;
            }

            int upper = originalTotal + offset;

            if (upper <= maxTotal
                && !usedTotals.Contains(upper))
            {
                return upper;
            }
        }

        Debug.LogError(
            "正の合計価値を一意にできませんでした。"
            + " 価値帯またはチーム数を見直してください。");

        return originalTotal;
    }

    private static void ApplyTotalValue(
        TeamBattleResult teamResult,
        BattleGameRuleData battleGameRuleData,
        int targetTotal)
    {
        int minTotal;
        int maxTotal;

        GetTotalRange(
            teamResult,
            battleGameRuleData,
            out minTotal,
            out maxTotal);

        targetTotal = Mathf.Clamp(
            targetTotal,
            minTotal,
            maxTotal);

        int remainingValue = targetTotal - minTotal;

        for (int i = 0; i < teamResult.Treasures.Count; i++)
        {
            ExportedTreasureResult treasure =
                teamResult.Treasures[i];

            int minValue;
            int maxValue;

            if (!battleGameRuleData.TryGetTreasureValueRange(
                    treasure.TreasureType,
                    out minValue,
                    out maxValue))
            {
                treasure.SetValue(0);
                continue;
            }

            int additionalValue = Mathf.Min(
                remainingValue,
                maxValue - minValue);

            treasure.SetValue(
                minValue + additionalValue);

            remainingValue -= additionalValue;
        }
    }

    private static void AssignRanks(
        BattleResultData result)
    {
        List<TeamBattleResult> positiveResults =
            new List<TeamBattleResult>();

        for (int i = 0; i < result.TeamResults.Count; i++)
        {
            TeamBattleResult teamResult =
                result.TeamResults[i];

            if (teamResult.TotalValue > 0)
            {
                positiveResults.Add(teamResult);
            }
        }

        positiveResults.Sort((a, b) =>
            b.TotalValue.CompareTo(a.TotalValue));

        for (int i = 0; i < positiveResults.Count; i++)
        {
            positiveResults[i].SetRank(i + 1);
        }

        int zeroTreasureRank = positiveResults.Count + 1;

        for (int i = 0; i < result.TeamResults.Count; i++)
        {
            TeamBattleResult teamResult =
                result.TeamResults[i];

            if (teamResult.TotalValue <= 0)
            {
                teamResult.SetRank(zeroTreasureRank);
            }
        }
    }

    private static void SelectTournamentRepresentative(
        BattleResultData result)
    {
        int bestRank = int.MaxValue;

        for (int i = 0; i < result.TeamResults.Count; i++)
        {
            bestRank = Mathf.Min(
                bestRank,
                result.TeamResults[i].Rank);
        }

        List<TeamBattleResult> firstPlaceTeams =
            new List<TeamBattleResult>();

        for (int i = 0; i < result.TeamResults.Count; i++)
        {
            TeamBattleResult teamResult =
                result.TeamResults[i];

            if (teamResult.Rank == bestRank)
            {
                firstPlaceTeams.Add(teamResult);
            }
        }

        if (firstPlaceTeams.Count <= 0)
        {
            return;
        }

        int winnerIndex =
            firstPlaceTeams.Count == 1
                ? 0
                : Random.Range(
                    0,
                    firstPlaceTeams.Count);

        firstPlaceTeams[winnerIndex]
            .SetTournamentRepresentative(true);
    }
}