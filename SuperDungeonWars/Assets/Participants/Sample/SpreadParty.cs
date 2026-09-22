using System.Collections.Generic;
using UnityEngine;

public class SpreadParty : ComPartyBase
{
    private readonly HashSet<int>
        m_treasurePriorityMemberIndices =
            new HashSet<int>();

    private bool m_hasKnownCentralChestCell;
    private Vector2Int m_knownCentralChestCell;


    protected override void SXG_OnPartyThink()
    {
        int memberCount = SXG_GetMemberCount();

        if (memberCount <= 0)
        {
            return;
        }

        KnownMapView knownMap = SXG_GetKnownMap();

        if (knownMap == null)
        {
            SXG_StopAllMembers();
            return;
        }

        IReadOnlyList<VisibleTreasureData> visibleTreasures =
            SXG_GetVisibleTreasures();

        UpdateKnownCentralChestCell(visibleTreasures);

        m_treasurePriorityMemberIndices.Clear();

        ApplyReturnTreasureOrders(
            knownMap);

        ApplyVisibleTreasureOrders(
            knownMap,
            visibleTreasures);

        for (int memberIndex = 0;
             memberIndex < memberCount;
             memberIndex++)
        {
            if (m_treasurePriorityMemberIndices.Contains(
                memberIndex))
            {
                continue;
            }

            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(
                memberIndex,
                out memberInfo) == false)
            {
                continue;
            }

            if (memberInfo.IsKnockedOut
                || memberInfo.IsActionLocked)
            {
                continue;
            }

            Vector2Int nextCell;

            if (m_hasKnownCentralChestCell)
            {
                if (TryFindNextStepToTarget(
                    knownMap,
                    memberInfo.CurrentCell,
                    m_knownCentralChestCell,
                    out nextCell))
                {
                    SXG_SetMemberMoveTargetCell(
                        memberIndex,
                        nextCell);
                }
                else
                {
                    SXG_StopMember(memberIndex);
                }

                continue;
            }

            if (TryFindNextStepToFrontier(
                knownMap,
                memberInfo.CurrentCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    memberIndex,
                    nextCell);
            }
            else
            {
                SXG_StopMember(memberIndex);
            }
        }
    }

    private void UpdateKnownCentralChestCell(
        IReadOnlyList<VisibleTreasureData> visibleTreasures)
    {
        if (visibleTreasures == null)
        {
            return;
        }

        for (int i = 0; i < visibleTreasures.Count; i++)
        {
            VisibleTreasureData treasure =
                visibleTreasures[i];

            if (treasure == null
                || treasure.IsCarried
                || treasure.TreasureType
                    != TreasureType.CentralChest)
            {
                continue;
            }

            m_knownCentralChestCell =
                treasure.CellPosition;

            m_hasKnownCentralChestCell = true;
            return;
        }
    }

    private void ApplyReturnTreasureOrders(
        KnownMapView knownMap)
    {
        Vector2Int returnEntranceCell;

        if (SXG_TryGetReturnEntranceCell(
            out returnEntranceCell) == false)
        {
            return;
        }

        int memberCount = SXG_GetMemberCount();

        for (int memberIndex = 0;
             memberIndex < memberCount;
             memberIndex++)
        {
            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(
                memberIndex,
                out memberInfo) == false)
            {
                continue;
            }

            if (memberInfo.IsKnockedOut
                || memberInfo.IsActionLocked
                || memberInfo.HasTreasure == false)
            {
                continue;
            }

            m_treasurePriorityMemberIndices.Add(
                memberIndex);

            Vector2Int nextCell;

            if (TryFindNextStepToTarget(
                knownMap,
                memberInfo.CurrentCell,
                returnEntranceCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    memberIndex,
                    nextCell);
            }
            else
            {
                SXG_StopMember(memberIndex);
            }
        }
    }

    private void ApplyVisibleTreasureOrders(
        KnownMapView knownMap,
        IReadOnlyList<VisibleTreasureData> visibleTreasures)
    {
        if (visibleTreasures == null)
        {
            return;
        }

        for (int treasureIndex = 0;
             treasureIndex < visibleTreasures.Count;
             treasureIndex++)
        {
            VisibleTreasureData treasure =
                visibleTreasures[treasureIndex];

            if (treasure == null
                || treasure.IsCarried)
            {
                continue;
            }

            int targetMemberIndex;
            Vector2Int nextCell;

            if (TryFindNearestAvailableMember(
                knownMap,
                treasure.CellPosition,
                out targetMemberIndex,
                out nextCell) == false)
            {
                continue;
            }

            m_treasurePriorityMemberIndices.Add(
                targetMemberIndex);

            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(
                targetMemberIndex,
                out memberInfo) == false)
            {
                continue;
            }

            if (memberInfo.CurrentCell
                == treasure.CellPosition)
            {
                SXG_SetMemberMoveTargetWorld(
                    targetMemberIndex,
                    treasure.WorldPosition);
            }
            else
            {
                SXG_SetMemberMoveTargetCell(
                    targetMemberIndex,
                    nextCell);
            }
        }
    }

    private bool TryFindNearestAvailableMember(
        KnownMapView knownMap,
        Vector2Int targetCell,
        out int targetMemberIndex,
        out Vector2Int nextCell)
    {
        targetMemberIndex = -1;
        nextCell = targetCell;

        int shortestPathLength = int.MaxValue;
        int memberCount = SXG_GetMemberCount();

        for (int memberIndex = 0;
             memberIndex < memberCount;
             memberIndex++)
        {
            if (m_treasurePriorityMemberIndices.Contains(
                memberIndex))
            {
                continue;
            }

            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(
                memberIndex,
                out memberInfo) == false)
            {
                continue;
            }

            if (memberInfo.IsKnockedOut
                || memberInfo.IsActionLocked
                || memberInfo.HasTreasure)
            {
                continue;
            }

            List<Vector2Int> path;

            if (TryFindPath(
                knownMap,
                memberInfo.CurrentCell,
                targetCell,
                out path) == false)
            {
                continue;
            }

            if (path.Count <= 0
                || path.Count >= shortestPathLength)
            {
                continue;
            }

            shortestPathLength = path.Count;
            targetMemberIndex = memberIndex;

            if (path.Count >= 2)
            {
                nextCell = path[1];
            }
            else
            {
                nextCell = memberInfo.CurrentCell;
            }
        }

        return targetMemberIndex >= 0;
    }

    private bool TryFindNextStepToFrontier(
        KnownMapView knownMap,
        Vector2Int startCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        Queue<Vector2Int> openCells =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> visitedCells =
            new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> previousCells =
            new Dictionary<Vector2Int, Vector2Int>();

        openCells.Enqueue(startCell);
        visitedCells.Add(startCell);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        while (openCells.Count > 0)
        {
            Vector2Int currentCell =
                openCells.Dequeue();

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighborCell;

                if (knownMap.TryGetOpenNeighbor(
                    currentCell,
                    directions[i],
                    out neighborCell) == false)
                {
                    continue;
                }

                if (knownMap.IsObserved(neighborCell)
                    == false)
                {
                    List<Vector2Int> path =
                        BuildPath(
                            startCell,
                            currentCell,
                            previousCells);

                    path.Add(neighborCell);

                    if (path.Count < 2)
                    {
                        return false;
                    }

                    nextCell = path[1];
                    return true;
                }

                if (visitedCells.Add(neighborCell) == false)
                {
                    continue;
                }

                previousCells.Add(
                    neighborCell,
                    currentCell);

                openCells.Enqueue(neighborCell);
            }
        }

        return false;
    }

    private bool TryFindNextStepToTarget(
        KnownMapView knownMap,
        Vector2Int startCell,
        Vector2Int targetCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        if (startCell == targetCell)
        {
            return false;
        }

        List<Vector2Int> path;

        if (TryFindPath(
            knownMap,
            startCell,
            targetCell,
            out path) == false)
        {
            return false;
        }

        if (path.Count < 2)
        {
            return false;
        }

        nextCell = path[1];
        return true;
    }

    private bool TryFindPath(
        KnownMapView knownMap,
        Vector2Int startCell,
        Vector2Int targetCell,
        out List<Vector2Int> path)
    {
        path = null;

        if (knownMap == null
            || knownMap.IsInside(startCell) == false
            || knownMap.IsInside(targetCell) == false)
        {
            return false;
        }

        Queue<Vector2Int> openCells =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> visitedCells =
            new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> previousCells =
            new Dictionary<Vector2Int, Vector2Int>();

        openCells.Enqueue(startCell);
        visitedCells.Add(startCell);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        while (openCells.Count > 0)
        {
            Vector2Int currentCell =
                openCells.Dequeue();

            if (currentCell == targetCell)
            {
                path = BuildPath(
                    startCell,
                    targetCell,
                    previousCells);

                return true;
            }

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int neighborCell =
                    currentCell + directions[i];

                if (visitedCells.Contains(neighborCell)
                    || knownMap.CanMove(
                        currentCell,
                        neighborCell) == false)
                {
                    continue;
                }

                visitedCells.Add(neighborCell);

                previousCells.Add(
                    neighborCell,
                    currentCell);

                openCells.Enqueue(neighborCell);
            }
        }

        return false;
    }

    private List<Vector2Int> BuildPath(
        Vector2Int startCell,
        Vector2Int targetCell,
        Dictionary<Vector2Int, Vector2Int> previousCells)
    {
        List<Vector2Int> path =
            new List<Vector2Int>();

        Vector2Int currentCell = targetCell;
        path.Add(currentCell);

        while (currentCell != startCell)
        {
            currentCell = previousCells[currentCell];
            path.Add(currentCell);
        }

        path.Reverse();

        return path;
    }
}