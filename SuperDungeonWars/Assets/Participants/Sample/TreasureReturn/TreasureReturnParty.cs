using System.Collections.Generic;
using UnityEngine;

public class TreasureReturnParty : ComPartyBase
{
    protected override void SXG_OnPartyThink()
    {
        if (SXG_GetMemberCount() <= 0)
        {
            return;
        }

        VisibleTreasureData targetTreasure;

        bool hasTargetTreasure =
            TryFindBestVisibleUncarriedTreasure(
                out targetTreasure);

        int collectorMemberIndex = hasTargetTreasure
            ? FindCollectorMemberIndex(targetTreasure)
            : -1;

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut)
            {
                continue;
            }

            Vector2Int nextCell;

            if (memberInfo.HasTreasure)
            {
                if (TryFindNextStepToReturnEntrance(
                    memberInfo.CurrentCell,
                    out nextCell))
                {
                    SXG_SetMemberMoveTargetCell(i, nextCell);
                }
                else if (TryFindNextStepToFrontier(
                    memberInfo.CurrentCell,
                    out nextCell))
                {
                    SXG_SetMemberMoveTargetCell(i, nextCell);
                }
                else
                {
                    SXG_StopMember(i);
                }

                continue;
            }

            if (i == collectorMemberIndex
                && TryIssueMoveOrderToTreasure(
                    i,
                    memberInfo,
                    targetTreasure))
            {
                continue;
            }

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

    private int FindCollectorMemberIndex(
        VisibleTreasureData targetTreasure)
    {
        if (targetTreasure == null)
        {
            return -1;
        }

        int bestMemberIndex = -1;
        int bestPathLength = int.MaxValue;

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

            int pathLength = GetPathLength(
                memberInfo.CurrentCell,
                targetTreasure.CellPosition);

            if (pathLength < bestPathLength)
            {
                bestPathLength = pathLength;
                bestMemberIndex = i;
            }
        }

        return bestMemberIndex;
    }

    private bool TryIssueMoveOrderToTreasure(
        int memberIndex,
        PartyMemberInfo memberInfo,
        VisibleTreasureData targetTreasure)
    {
        if (memberInfo == null || targetTreasure == null)
        {
            return false;
        }

        if (memberInfo.CurrentCell ==
            targetTreasure.CellPosition)
        {
            return SXG_SetMemberMoveTargetWorld(
                memberIndex,
                targetTreasure.WorldPosition);
        }

        Vector2Int nextCell;

        if (!TryFindNextStepToTarget(
            memberInfo.CurrentCell,
            targetTreasure.CellPosition,
            out nextCell))
        {
            return false;
        }

        return SXG_SetMemberMoveTargetCell(
            memberIndex,
            nextCell);
    }

    private bool TryFindBestVisibleUncarriedTreasure(
        out VisibleTreasureData targetTreasure)
    {
        targetTreasure = null;

        IReadOnlyList<VisibleTreasureData> treasures =
            SXG_GetVisibleTreasures();

        int bestPriority = int.MinValue;

        for (int i = 0; i < treasures.Count; i++)
        {
            VisibleTreasureData treasure = treasures[i];

            if (treasure == null || treasure.IsCarried)
            {
                continue;
            }

            int priority =
                treasure.TreasureType == TreasureType.CentralChest
                    ? 1
                    : 0;

            if (priority <= bestPriority)
            {
                continue;
            }

            bestPriority = priority;
            targetTreasure = treasure;
        }

        return targetTreasure != null;
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
                List<Vector2Int> path =
                    BuildPath(startCell, currentCell, previous);

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