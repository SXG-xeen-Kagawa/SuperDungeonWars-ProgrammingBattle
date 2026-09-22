using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 固定ロールによる護送・襲撃型パーティー。
///
/// 0: Runner  - 宝箱回収を主導する。
/// 1: Raider  - 敵キャリアを追う。
/// 2: Escort  - 自軍キャリアへ合流して護衛する。
/// 3以降: Scout - 未知領域を探索する。
/// </summary>
public class ConvoyParty : ComPartyBase
{
    private const int k_runnerMemberIndex = 0;
    private const int k_raiderMemberIndex = 1;
    private const int k_escortMemberIndex = 2;

    protected override void SXG_OnPartyThink()
    {
        if (SXG_GetMemberCount() <= 0)
        {
            return;
        }

        HashSet<int> reservedMemberIndices =
            new HashSet<int>();

        IssueExportOrders(reservedMemberIndices);
        IssueEscortOrder(reservedMemberIndices);
        TryIssueTreasureRecoveryOrder(reservedMemberIndices);
        TryIssueRaiderOrder(reservedMemberIndices);
        IssueExploreOrders(reservedMemberIndices);
    }

    private void IssueExportOrders(
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || !memberInfo.HasTreasure)
            {
                continue;
            }

            Vector2Int nextCell;

            if (TryFindNextStepToReturnEntrance(
                memberInfo.CurrentCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(i, nextCell);
            }
            else
            {
                SXG_StopMember(i);
            }

            reservedMemberIndices.Add(i);
        }
    }

    private void IssueEscortOrder(
        HashSet<int> reservedMemberIndices)
    {
        int carrierMemberIndex;

        if (!TryFindFirstTreasureCarrier(
            out carrierMemberIndex))
        {
            return;
        }

        PartyMemberInfo carrierInfo;
        PartyMemberInfo escortInfo;

        if (!SXG_TryGetMemberInfo(
                carrierMemberIndex,
                out carrierInfo)
            || !SXG_TryGetMemberInfo(
                k_escortMemberIndex,
                out escortInfo)
            || carrierInfo == null
            || escortInfo == null
            || carrierMemberIndex == k_escortMemberIndex
            || !IsAvailableMember(
                escortInfo,
                reservedMemberIndices))
        {
            return;
        }

        Vector2Int nextCell;

        if (TryFindNextStepToTarget(
            escortInfo.CurrentCell,
            carrierInfo.CurrentCell,
            out nextCell))
        {
            SXG_SetMemberMoveTargetCell(
                k_escortMemberIndex,
                nextCell);
        }
        else
        {
            SXG_StopMember(k_escortMemberIndex);
        }

        reservedMemberIndices.Add(k_escortMemberIndex);
    }

    private bool TryIssueTreasureRecoveryOrder(
        HashSet<int> reservedMemberIndices)
    {
        VisibleTreasureData targetTreasure;

        if (!TryFindBestVisibleUncarriedTreasure(
            out targetTreasure))
        {
            return false;
        }

        int collectorMemberIndex = k_runnerMemberIndex;
        PartyMemberInfo collectorInfo;

        if (!SXG_TryGetMemberInfo(
                collectorMemberIndex,
                out collectorInfo)
            || !IsAvailableMember(
                collectorInfo,
                reservedMemberIndices))
        {
            if (!TryFindNearestAvailableMember(
                targetTreasure.CellPosition,
                reservedMemberIndices,
                out collectorMemberIndex)
                || !SXG_TryGetMemberInfo(
                    collectorMemberIndex,
                    out collectorInfo))
            {
                return false;
            }
        }

        if (collectorInfo.CurrentCell !=
            targetTreasure.CellPosition)
        {
            Vector2Int nextCell;

            if (TryFindNextStepToTarget(
                collectorInfo.CurrentCell,
                targetTreasure.CellPosition,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    collectorMemberIndex,
                    nextCell);
            }
            else
            {
                SXG_StopMember(collectorMemberIndex);
            }
        }
        else
        {
            SXG_SetMemberMoveTargetWorld(
                collectorMemberIndex,
                targetTreasure.WorldPosition);
        }

        reservedMemberIndices.Add(collectorMemberIndex);
        return true;
    }

    private bool TryIssueRaiderOrder(
        HashSet<int> reservedMemberIndices)
    {
        PartyMemberInfo raiderInfo;

        if (!SXG_TryGetMemberInfo(
                k_raiderMemberIndex,
                out raiderInfo)
            || !IsAvailableMember(
                raiderInfo,
                reservedMemberIndices))
        {
            return false;
        }

        VisibleCharacterData enemyCarrier;

        if (!TryFindBestVisibleEnemyCarrier(
            raiderInfo.WorldPosition,
            out enemyCarrier))
        {
            return false;
        }

        Vector2Int nextCell;

        if (TryFindNextStepToTarget(
            raiderInfo.CurrentCell,
            enemyCarrier.CellPosition,
            out nextCell))
        {
            SXG_SetMemberMoveTargetCell(
                k_raiderMemberIndex,
                nextCell);
        }
        else if (raiderInfo.CurrentCell ==
                 enemyCarrier.CellPosition)
        {
            SXG_SetMemberMoveTargetWorld(
                k_raiderMemberIndex,
                enemyCarrier.WorldPosition);
        }
        else
        {
            SXG_StopMember(k_raiderMemberIndex);
        }

        reservedMemberIndices.Add(k_raiderMemberIndex);
        return true;
    }

    private void IssueExploreOrders(
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || !IsAvailableMember(
                    memberInfo,
                    reservedMemberIndices))
            {
                continue;
            }

            Vector2Int nextCell;

            if (TryFindNextStepToFrontier(
                memberInfo.CurrentCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(i, nextCell);
            }
            else
            {
                SXG_StopMember(i);
            }
        }
    }

    private bool TryFindFirstTreasureCarrier(
        out int carrierMemberIndex)
    {
        carrierMemberIndex = -1;

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(i, out memberInfo)
                && memberInfo != null
                && !memberInfo.IsKnockedOut
                && memberInfo.HasTreasure)
            {
                carrierMemberIndex = i;
                return true;
            }
        }

        return false;
    }

    private bool TryFindBestVisibleUncarriedTreasure(
        out VisibleTreasureData bestTreasure)
    {
        bestTreasure = null;

        IReadOnlyList<VisibleTreasureData> treasures =
            SXG_GetVisibleTreasures();

        float bestDistance = float.MaxValue;

        for (int i = 0; i < treasures.Count; i++)
        {
            VisibleTreasureData candidate = treasures[i];

            if (candidate == null || candidate.IsCarried)
            {
                continue;
            }

            float distance =
                GetNearestAvailableMemberSqrDistance(
                    candidate.WorldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTreasure = candidate;
            }
        }

        return bestTreasure != null;
    }

    private bool TryFindBestVisibleEnemyCarrier(
        Vector3 raiderWorldPosition,
        out VisibleCharacterData bestCarrier)
    {
        bestCarrier = null;

        IReadOnlyList<VisibleCharacterData> characters =
            SXG_GetVisibleCharacters();

        float bestDistance = float.MaxValue;

        for (int i = 0; i < characters.Count; i++)
        {
            VisibleCharacterData candidate = characters[i];

            if (candidate == null
                || candidate.IsKnockedOut
                || !candidate.HasTreasure
                || IsOwnCharacter(candidate.CharacterId))
            {
                continue;
            }

            Vector3 delta =
                candidate.WorldPosition - raiderWorldPosition;

            delta.y = 0.0f;

            if (delta.sqrMagnitude < bestDistance)
            {
                bestDistance = delta.sqrMagnitude;
                bestCarrier = candidate;
            }
        }

        return bestCarrier != null;
    }

    private bool TryFindNearestAvailableMember(
        Vector2Int targetCell,
        HashSet<int> reservedMemberIndices,
        out int resultMemberIndex)
    {
        resultMemberIndex = -1;
        int bestPathLength = int.MaxValue;

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || !IsAvailableMember(
                    memberInfo,
                    reservedMemberIndices))
            {
                continue;
            }

            int pathLength = GetPathLength(
                memberInfo.CurrentCell,
                targetCell);

            if (pathLength < bestPathLength)
            {
                bestPathLength = pathLength;
                resultMemberIndex = i;
            }
        }

        return resultMemberIndex >= 0;
    }

    private bool IsAvailableMember(
        PartyMemberInfo memberInfo,
        HashSet<int> reservedMemberIndices)
    {
        return memberInfo != null
            && !memberInfo.IsKnockedOut
            && !memberInfo.HasTreasure
            && !reservedMemberIndices.Contains(
                memberInfo.MemberIndex);
    }

    private bool IsOwnCharacter(int characterId)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(i, out memberInfo)
                && memberInfo != null
                && memberInfo.CharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private float GetNearestAvailableMemberSqrDistance(
        Vector3 targetWorldPosition)
    {
        float bestDistance = float.MaxValue;

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || memberInfo.HasTreasure)
            {
                continue;
            }

            Vector3 delta =
                targetWorldPosition - memberInfo.WorldPosition;

            delta.y = 0.0f;
            bestDistance = Mathf.Min(
                bestDistance,
                delta.sqrMagnitude);
        }

        return bestDistance;
    }

    private bool TryFindNextStepToReturnEntrance(
        Vector2Int startCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        Vector2Int returnEntranceCell;

        return SXG_TryGetReturnEntranceCell(
                out returnEntranceCell)
            && TryFindNextStepToTarget(
                startCell,
                returnEntranceCell,
                out nextCell);
    }

    private bool TryFindNextStepToTarget(
        Vector2Int startCell,
        Vector2Int targetCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        List<Vector2Int> path;

        if (!TryFindKnownPath(
                startCell,
                targetCell,
                out path)
            || path.Count < 2)
        {
            return false;
        }

        nextCell = path[1];
        return true;
    }

    private bool TryFindNextStepToFrontier(
        Vector2Int startCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        KnownMapView knownMap = SXG_GetKnownMap();

        if (knownMap == null
            || !knownMap.IsObserved(startCell))
        {
            return false;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> previous =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(startCell);
        previous.Add(startCell, startCell);

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();
            Vector2Int unknownNeighbor;

            if (TryFindUnknownOpenNeighbor(
                knownMap,
                currentCell,
                out unknownNeighbor))
            {
                List<Vector2Int> path = BuildPath(
                    startCell,
                    currentCell,
                    previous);

                nextCell = path.Count >= 2
                    ? path[1]
                    : unknownNeighbor;

                return true;
            }

            EnqueueNeighbors(
                knownMap,
                currentCell,
                queue,
                previous);
        }

        return false;
    }

    private bool TryFindKnownPath(
        Vector2Int startCell,
        Vector2Int targetCell,
        out List<Vector2Int> path)
    {
        path = null;

        KnownMapView knownMap = SXG_GetKnownMap();

        if (knownMap == null
            || !knownMap.IsObserved(startCell)
            || !knownMap.IsObserved(targetCell))
        {
            return false;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> previous =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(startCell);
        previous.Add(startCell, startCell);

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

            if (currentCell == targetCell)
            {
                path = BuildPath(
                    startCell,
                    targetCell,
                    previous);

                return true;
            }

            EnqueueNeighbors(
                knownMap,
                currentCell,
                queue,
                previous);
        }

        return false;
    }

    private int GetPathLength(
        Vector2Int startCell,
        Vector2Int targetCell)
    {
        List<Vector2Int> path;

        return TryFindKnownPath(
                startCell,
                targetCell,
                out path)
            ? path.Count - 1
            : int.MaxValue;
    }

    private void EnqueueNeighbors(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Queue<Vector2Int> queue,
        Dictionary<Vector2Int, Vector2Int> previous)
    {
        EnqueueNeighbor(knownMap, currentCell, Vector2Int.up, queue, previous);
        EnqueueNeighbor(knownMap, currentCell, Vector2Int.right, queue, previous);
        EnqueueNeighbor(knownMap, currentCell, Vector2Int.down, queue, previous);
        EnqueueNeighbor(knownMap, currentCell, Vector2Int.left, queue, previous);
    }

    private void EnqueueNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        Queue<Vector2Int> queue,
        Dictionary<Vector2Int, Vector2Int> previous)
    {
        Vector2Int neighbor = currentCell + direction;

        if (previous.ContainsKey(neighbor)
            || !knownMap.CanMove(currentCell, neighbor))
        {
            return;
        }

        previous.Add(neighbor, currentCell);
        queue.Enqueue(neighbor);
    }

    private bool TryFindUnknownOpenNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        out Vector2Int unknownNeighbor)
    {
        return TryGetUnknownOpenNeighbor(
                   knownMap, currentCell, Vector2Int.up, out unknownNeighbor)
            || TryGetUnknownOpenNeighbor(
                   knownMap, currentCell, Vector2Int.right, out unknownNeighbor)
            || TryGetUnknownOpenNeighbor(
                   knownMap, currentCell, Vector2Int.down, out unknownNeighbor)
            || TryGetUnknownOpenNeighbor(
                   knownMap, currentCell, Vector2Int.left, out unknownNeighbor);
    }

    private bool TryGetUnknownOpenNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        out Vector2Int unknownNeighbor)
    {
        unknownNeighbor = currentCell;

        Vector2Int candidate;

        if (!knownMap.TryGetOpenNeighbor(
                currentCell,
                direction,
                out candidate)
            || knownMap.IsObserved(candidate))
        {
            return false;
        }

        unknownNeighbor = candidate;
        return true;
    }

    private List<Vector2Int> BuildPath(
        Vector2Int startCell,
        Vector2Int goalCell,
        Dictionary<Vector2Int, Vector2Int> previous)
    {
        List<Vector2Int> path =
            new List<Vector2Int>();

        Vector2Int currentCell = goalCell;

        while (true)
        {
            path.Add(currentCell);

            if (currentCell == startCell)
            {
                break;
            }

            currentCell = previous[currentCell];
        }

        path.Reverse();
        return path;
    }
}