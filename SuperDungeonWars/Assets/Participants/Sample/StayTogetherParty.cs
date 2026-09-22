using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 4人でなるべくまとまって迷路を探索するサンプルパーティー。
/// 宝箱所持者の帰還と、視認中の宝箱回収を優先しつつ、
/// それ以外のメンバーはリーダーを中心に探索する。
/// </summary>
public class StayTogetherParty : ComPartyBase
{
    [SerializeField]
    private int m_maxFollowerPathDistanceBeforeWaiting = 2;

    private bool m_hasPartyTargetCell;
    private Vector2Int m_partyTargetCell;

    private bool m_hasKnownCentralChestCell;
    private Vector2Int m_knownCentralChestCell;

    private readonly HashSet<int>
        m_treasurePriorityMemberIndices =
            new HashSet<int>();


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

        UpdateKnownCentralChestCell(
            visibleTreasures);

        m_treasurePriorityMemberIndices.Clear();

        ApplyReturnTreasureOrders(
            knownMap);

        ApplyVisibleTreasureOrders(
            knownMap,
            visibleTreasures);

        PartyMemberInfo leaderInfo;

        if (SXG_TryGetMemberInfo(
            0,
            out leaderInfo) == false)
        {
            return;
        }

        if (leaderInfo.IsKnockedOut
            || leaderInfo.IsActionLocked)
        {
            MoveFollowersTowardPartyTarget(
                knownMap,
                leaderInfo.CurrentCell);

            return;
        }

        if (m_treasurePriorityMemberIndices.Contains(0))
        {
            MoveFollowersTowardPartyTarget(
                knownMap,
                leaderInfo.CurrentCell);

            return;
        }

        if (leaderInfo.IsMoving)
        {
            if (m_hasPartyTargetCell)
            {
                MoveFollowersTowardPartyTarget(
                    knownMap,
                    m_partyTargetCell);
            }

            return;
        }

        if (IsPartyTooSpreadOut(
            knownMap,
            leaderInfo.CurrentCell))
        {
            MoveFollowersTowardPartyTarget(
                knownMap,
                leaderInfo.CurrentCell);

            return;
        }

        Vector2Int leaderNextCell;

        if (TryFindLeaderNextStep(
            knownMap,
            leaderInfo.CurrentCell,
            out leaderNextCell) == false)
        {
            StopNonPriorityMembers();

            m_hasPartyTargetCell = false;
            return;
        }

        m_partyTargetCell = leaderNextCell;
        m_hasPartyTargetCell = true;

        SXG_SetMemberMoveTargetCell(
            0,
            leaderNextCell);

        MoveFollowersTowardPartyTarget(
            knownMap,
            m_partyTargetCell);
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

            if (TryFindNextStepOnKnownMap(
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

            int memberIndex;
            Vector2Int nextCell;

            if (TryFindNearestAvailableMember(
                knownMap,
                treasure.CellPosition,
                out memberIndex,
                out nextCell) == false)
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

            m_treasurePriorityMemberIndices.Add(
                memberIndex);

            if (memberInfo.CurrentCell
                == treasure.CellPosition)
            {
                SXG_SetMemberMoveTargetWorld(
                    memberIndex,
                    treasure.WorldPosition);
            }
            else
            {
                SXG_SetMemberMoveTargetCell(
                    memberIndex,
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

    private void MoveFollowersTowardPartyTarget(
        KnownMapView knownMap,
        Vector2Int partyTargetCell)
    {
        int memberCount = SXG_GetMemberCount();

        PartyMemberInfo leaderInfo;

        if (SXG_TryGetMemberInfo(
            0,
            out leaderInfo) == false)
        {
            return;
        }

        for (int memberIndex = 1;
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

            if (TryFindMemberNextStep(
                knownMap,
                memberInfo.CurrentCell,
                partyTargetCell,
                leaderInfo.CurrentCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    memberIndex,
                    nextCell);

                continue;
            }

            if (memberInfo.CurrentCell
                == partyTargetCell)
            {
                SXG_StopMember(memberIndex);
            }
        }
    }

    private bool TryFindMemberNextStep(
        KnownMapView knownMap,
        Vector2Int memberCurrentCell,
        Vector2Int partyTargetCell,
        Vector2Int leaderCurrentCell,
        out Vector2Int nextCell)
    {
        nextCell = memberCurrentCell;

        if (TryFindNextStepOnKnownMap(
            knownMap,
            memberCurrentCell,
            partyTargetCell,
            out nextCell))
        {
            return true;
        }

        if (TryFindNextStepOnKnownMap(
            knownMap,
            memberCurrentCell,
            leaderCurrentCell,
            out nextCell))
        {
            return true;
        }

        return false;
    }

    private bool IsPartyTooSpreadOut(
        KnownMapView knownMap,
        Vector2Int leaderCurrentCell)
    {
        int memberCount = SXG_GetMemberCount();

        for (int memberIndex = 1;
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

            if (memberInfo.IsKnockedOut)
            {
                continue;
            }

            int pathDistance;

            if (TryGetKnownPathDistance(
                knownMap,
                memberInfo.CurrentCell,
                leaderCurrentCell,
                out pathDistance) == false)
            {
                return true;
            }

            if (pathDistance
                > m_maxFollowerPathDistanceBeforeWaiting)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetKnownPathDistance(
        KnownMapView knownMap,
        Vector2Int startCell,
        Vector2Int targetCell,
        out int pathDistance)
    {
        pathDistance = 0;

        List<Vector2Int> path;

        if (TryFindPath(
            knownMap,
            startCell,
            targetCell,
            out path) == false)
        {
            return false;
        }

        pathDistance = path.Count - 1;
        return true;
    }

    private bool TryFindLeaderNextStep(
        KnownMapView knownMap,
        Vector2Int startCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        if (m_hasKnownCentralChestCell
            && TryFindNextStepOnKnownMap(
                knownMap,
                startCell,
                m_knownCentralChestCell,
                out nextCell))
        {
            return true;
        }

        return TryFindNextStepToFrontier(
            knownMap,
            startCell,
            out nextCell);
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

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        openCells.Enqueue(startCell);
        visitedCells.Add(startCell);

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

    private bool TryFindNextStepOnKnownMap(
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

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        openCells.Enqueue(startCell);
        visitedCells.Add(startCell);

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

    private void StopNonPriorityMembers()
    {
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

            SXG_StopMember(memberIndex);
        }
    }
}