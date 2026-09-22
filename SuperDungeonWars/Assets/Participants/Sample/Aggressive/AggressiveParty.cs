using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃を優先するサンプルパーティー。
///
/// 優先順位:
/// 1. 宝箱を持つ自メンバーは自陣の搬出口へ向かう。
/// 2. 視認中の未所持宝箱を回収する。
/// 3. 視認中の敵宝箱所持者を追跡する。
/// 4. 視認中の通常の敵を追跡する。
/// 5. 見失った敵は最後に見た方向へ直進して追う。
/// 6. 分岐点・行き止まりまで来ても敵が見えなければ探索へ戻る。
/// </summary>
public class AggressiveParty : ComPartyBase
{
    private const int k_noCharacterId = -1;

    private readonly Dictionary<int, Vector2Int>
        m_lastSeenCellByCharacterId =
            new Dictionary<int, Vector2Int>();

    private readonly Dictionary<int, Vector2Int>
        m_lastSeenDirectionByCharacterId =
            new Dictionary<int, Vector2Int>();

    private int m_pursuitTargetCharacterId =
        k_noCharacterId;

    private Vector2Int m_pursuitCell;
    private Vector2Int m_pursuitDirection;
    private bool m_hasEnteredPursuitDirection;

    protected override void SXG_OnPartyThink()
    {
        if (SXG_GetMemberCount() <= 0)
        {
            return;
        }

        HashSet<int> reservedMemberIndices =
            new HashSet<int>();

        IssueReturnOrdersForTreasureCarriers(
            reservedMemberIndices);

        VisibleCharacterData visibleTarget;

        if (TryFindBestVisibleEnemy(out visibleTarget))
        {
            UpdateLastSeenTargetData(visibleTarget);

            m_pursuitTargetCharacterId =
                visibleTarget.CharacterId;

            if (TryIssueTreasureRecoveryOrder(
                reservedMemberIndices))
            {
                IssueChaseOrders(
                    visibleTarget,
                    reservedMemberIndices);

                return;
            }

            IssueChaseOrders(
                visibleTarget,
                reservedMemberIndices);

            return;
        }

        if (TryIssueTreasureRecoveryOrder(
            reservedMemberIndices))
        {
            ClearLostTargetPursuit();
            IssueExploreOrders(reservedMemberIndices);
            return;
        }

        if (TryIssueLostTargetPursuitOrders(
            reservedMemberIndices))
        {
            return;
        }

        IssueExploreOrders(reservedMemberIndices);
    }

    private void IssueReturnOrdersForTreasureCarriers(
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || !memberInfo.HasTreasure)
            {
                continue;
            }

            Vector2Int nextCell;

            if (TryFindNextStepToReturnEntrance(
                memberInfo.CurrentCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    i,
                    nextCell);
            }
            else
            {
                SXG_StopMember(i);
            }

            reservedMemberIndices.Add(i);
        }
    }

    private bool TryFindBestVisibleEnemy(
        out VisibleCharacterData bestEnemy)
    {
        bestEnemy = null;

        IReadOnlyList<VisibleCharacterData> visibleCharacters =
            SXG_GetVisibleCharacters();

        bool hasTreasureCarrier = false;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < visibleCharacters.Count; i++)
        {
            VisibleCharacterData candidate =
                visibleCharacters[i];

            if (candidate == null
                || candidate.IsKnockedOut
                || IsOwnCharacter(candidate.CharacterId))
            {
                continue;
            }

            float sqrDistance =
                GetNearestAvailableMemberSqrDistance(
                    candidate.WorldPosition);

            if (candidate.HasTreasure)
            {
                if (!hasTreasureCarrier
                    || sqrDistance < bestSqrDistance)
                {
                    hasTreasureCarrier = true;
                    bestSqrDistance = sqrDistance;
                    bestEnemy = candidate;
                }

                continue;
            }

            if (hasTreasureCarrier)
            {
                continue;
            }

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestEnemy = candidate;
            }
        }

        return bestEnemy != null;
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
        float bestSqrDistance = float.MaxValue;

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
                targetWorldPosition -
                memberInfo.WorldPosition;

            delta.y = 0.0f;

            float sqrDistance = delta.sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
            }
        }

        return bestSqrDistance;
    }

    private void UpdateLastSeenTargetData(
        VisibleCharacterData visibleTarget)
    {
        if (visibleTarget == null)
        {
            return;
        }

        Vector2Int direction = Vector2Int.zero;
        Vector2Int previousCell;

        if (m_lastSeenCellByCharacterId.TryGetValue(
            visibleTarget.CharacterId,
            out previousCell))
        {
            direction = GetCardinalDirection(
                visibleTarget.CellPosition - previousCell);
        }

        if (direction == Vector2Int.zero)
        {
            direction = GetCardinalDirection(
                visibleTarget.Forward);
        }

        if (direction != Vector2Int.zero)
        {
            m_lastSeenDirectionByCharacterId[
                visibleTarget.CharacterId] =
                direction;
        }

        m_lastSeenCellByCharacterId[
            visibleTarget.CharacterId] =
            visibleTarget.CellPosition;

        m_pursuitTargetCharacterId =
            visibleTarget.CharacterId;

        m_pursuitCell =
            visibleTarget.CellPosition;

        if (direction != Vector2Int.zero)
        {
            m_pursuitDirection = direction;
        }
    }

    private void IssueChaseOrders(
        VisibleCharacterData visibleTarget,
        HashSet<int> reservedMemberIndices)
    {
        if (visibleTarget == null)
        {
            return;
        }

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || reservedMemberIndices.Contains(i))
            {
                continue;
            }

            if (memberInfo.CurrentCell !=
                visibleTarget.CellPosition)
            {
                Vector2Int nextCell;

                if (TryFindNextStepToTarget(
                    memberInfo.CurrentCell,
                    visibleTarget.CellPosition,
                    out nextCell))
                {
                    SXG_SetMemberMoveTargetCell(
                        i,
                        nextCell);
                }
                else
                {
                    SXG_StopMember(i);
                }

                continue;
            }

            SXG_SetMemberMoveTargetWorld(
                i,
                visibleTarget.WorldPosition);
        }
    }

    private void IssueChaseOrders(
        Vector2Int targetCell,
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || reservedMemberIndices.Contains(i))
            {
                continue;
            }

            Vector2Int nextCell;

            if (TryFindNextStepToTarget(
                memberInfo.CurrentCell,
                targetCell,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    i,
                    nextCell);
            }
            else
            {
                SXG_StopMember(i);
            }
        }
    }

    private bool TryIssueLostTargetPursuitOrders(
        HashSet<int> reservedMemberIndices)
    {
        if (m_pursuitTargetCharacterId ==
            k_noCharacterId)
        {
            return false;
        }

        if (m_pursuitDirection == Vector2Int.zero)
        {
            ClearLostTargetPursuit();
            return false;
        }

        if (HasAvailableMemberOutsideCell(
            m_pursuitCell,
            reservedMemberIndices))
        {
            IssueChaseOrders(
                m_pursuitCell,
                reservedMemberIndices);

            return true;
        }

        Vector2Int nextPursuitCell;

        if (!TryFindNextPursuitCell(
            out nextPursuitCell))
        {
            ClearLostTargetPursuit();
            return false;
        }

        m_pursuitCell = nextPursuitCell;
        m_hasEnteredPursuitDirection = true;

        IssueChaseOrders(
            m_pursuitCell,
            reservedMemberIndices);

        return true;
    }

    private bool HasAvailableMemberOutsideCell(
        Vector2Int targetCell,
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || reservedMemberIndices.Contains(i))
            {
                continue;
            }

            if (memberInfo.CurrentCell != targetCell)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindNextPursuitCell(
        out Vector2Int nextCell)
    {
        nextCell = m_pursuitCell;

        KnownMapView knownMap = SXG_GetKnownMap();

        if (knownMap == null
            || !knownMap.IsObserved(m_pursuitCell))
        {
            return false;
        }

        if (m_hasEnteredPursuitDirection
            && IsPursuitBranchCell(
                knownMap,
                m_pursuitCell,
                m_pursuitDirection))
        {
            return false;
        }

        return knownMap.TryGetOpenNeighbor(
            m_pursuitCell,
            m_pursuitDirection,
            out nextCell);
    }

    private bool IsPursuitBranchCell(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int forwardDirection)
    {
        int openDirectionCount = 0;
        bool hasForwardPath = false;

        CountOpenDirection(
            knownMap,
            currentCell,
            Vector2Int.up,
            forwardDirection,
            ref openDirectionCount,
            ref hasForwardPath);

        CountOpenDirection(
            knownMap,
            currentCell,
            Vector2Int.right,
            forwardDirection,
            ref openDirectionCount,
            ref hasForwardPath);

        CountOpenDirection(
            knownMap,
            currentCell,
            Vector2Int.down,
            forwardDirection,
            ref openDirectionCount,
            ref hasForwardPath);

        CountOpenDirection(
            knownMap,
            currentCell,
            Vector2Int.left,
            forwardDirection,
            ref openDirectionCount,
            ref hasForwardPath);

        return !hasForwardPath
            || openDirectionCount >= 3;
    }

    private void CountOpenDirection(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        Vector2Int forwardDirection,
        ref int openDirectionCount,
        ref bool hasForwardPath)
    {
        Vector2Int neighborCell;

        if (!knownMap.TryGetOpenNeighbor(
            currentCell,
            direction,
            out neighborCell))
        {
            return;
        }

        openDirectionCount++;

        if (direction == forwardDirection)
        {
            hasForwardPath = true;
        }
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

        int collectorMemberIndex;

        if (!TryFindNearestAvailableMember(
            targetTreasure.CellPosition,
            reservedMemberIndices,
            out collectorMemberIndex))
        {
            return false;
        }

        PartyMemberInfo collectorInfo;

        if (!SXG_TryGetMemberInfo(
            collectorMemberIndex,
            out collectorInfo)
            || collectorInfo == null)
        {
            return false;
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

            reservedMemberIndices.Add(
                collectorMemberIndex);

            return true;
        }

        SXG_SetMemberMoveTargetWorld(
            collectorMemberIndex,
            targetTreasure.WorldPosition);

        reservedMemberIndices.Add(
            collectorMemberIndex);

        return true;
    }

    private bool TryFindBestVisibleUncarriedTreasure(
        out VisibleTreasureData bestTreasure)
    {
        bestTreasure = null;

        IReadOnlyList<VisibleTreasureData> visibleTreasures =
            SXG_GetVisibleTreasures();

        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < visibleTreasures.Count; i++)
        {
            VisibleTreasureData candidate =
                visibleTreasures[i];

            if (candidate == null
                || candidate.IsCarried)
            {
                continue;
            }

            float sqrDistance =
                GetNearestAvailableMemberSqrDistance(
                    candidate.WorldPosition);

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                bestTreasure = candidate;
            }
        }

        return bestTreasure != null;
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
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || memberInfo.HasTreasure
                || reservedMemberIndices.Contains(i))
            {
                continue;
            }

            List<Vector2Int> path;

            if (!TryFindKnownPath(
                memberInfo.CurrentCell,
                targetCell,
                out path))
            {
                continue;
            }

            int pathLength = path.Count - 1;

            if (pathLength < bestPathLength)
            {
                bestPathLength = pathLength;
                resultMemberIndex = i;
            }
        }

        return resultMemberIndex >= 0;
    }

    private void IssueExploreOrders(
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(i, out memberInfo)
                || memberInfo == null
                || memberInfo.IsKnockedOut
                || reservedMemberIndices.Contains(i))
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

    private bool TryFindNextStepToReturnEntrance(
        Vector2Int startCell,
        out Vector2Int nextCell)
    {
        nextCell = startCell;

        Vector2Int returnEntranceCell;

        if (!SXG_TryGetReturnEntranceCell(
            out returnEntranceCell))
        {
            return false;
        }

        return TryFindNextStepToTarget(
            startCell,
            returnEntranceCell,
            out nextCell);
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

        Dictionary<Vector2Int, Vector2Int> previousCellByCell =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(startCell);
        previousCellByCell.Add(
            startCell,
            startCell);

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

            Vector2Int unknownNeighborCell;

            if (TryFindUnknownOpenNeighbor(
                knownMap,
                currentCell,
                out unknownNeighborCell))
            {
                List<Vector2Int> path =
                    BuildPath(
                        startCell,
                        currentCell,
                        previousCellByCell);

                if (path.Count >= 2)
                {
                    nextCell = path[1];
                    return true;
                }

                nextCell = unknownNeighborCell;
                return true;
            }

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.up,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.right,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.down,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.left,
                queue,
                previousCellByCell);
        }

        return false;
    }

    private bool TryFindUnknownOpenNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        out Vector2Int nextCell)
    {
        nextCell = currentCell;

        if (TryGetUnknownOpenNeighbor(
            knownMap,
            currentCell,
            Vector2Int.up,
            out nextCell))
        {
            return true;
        }

        if (TryGetUnknownOpenNeighbor(
            knownMap,
            currentCell,
            Vector2Int.right,
            out nextCell))
        {
            return true;
        }

        if (TryGetUnknownOpenNeighbor(
            knownMap,
            currentCell,
            Vector2Int.down,
            out nextCell))
        {
            return true;
        }

        return TryGetUnknownOpenNeighbor(
            knownMap,
            currentCell,
            Vector2Int.left,
            out nextCell);
    }

    private bool TryGetUnknownOpenNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        out Vector2Int nextCell)
    {
        nextCell = currentCell;

        Vector2Int candidateCell;

        if (!knownMap.TryGetOpenNeighbor(
            currentCell,
            direction,
            out candidateCell))
        {
            return false;
        }

        if (knownMap.IsObserved(candidateCell))
        {
            return false;
        }

        nextCell = candidateCell;
        return true;
    }

    private bool TryFindNextStepToTarget(
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

        Dictionary<Vector2Int, Vector2Int> previousCellByCell =
            new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(startCell);
        previousCellByCell.Add(
            startCell,
            startCell);

        while (queue.Count > 0)
        {
            Vector2Int currentCell = queue.Dequeue();

            if (currentCell == targetCell)
            {
                path = BuildPath(
                    startCell,
                    targetCell,
                    previousCellByCell);

                return path.Count > 0;
            }

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.up,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.right,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.down,
                queue,
                previousCellByCell);

            EnqueueReachableNeighbor(
                knownMap,
                currentCell,
                Vector2Int.left,
                queue,
                previousCellByCell);
        }

        return false;
    }

    private void EnqueueReachableNeighbor(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        Queue<Vector2Int> queue,
        Dictionary<Vector2Int, Vector2Int> previousCellByCell)
    {
        Vector2Int neighborCell =
            currentCell + direction;

        if (previousCellByCell.ContainsKey(neighborCell)
            || !knownMap.CanMove(
                currentCell,
                neighborCell))
        {
            return;
        }

        previousCellByCell.Add(
            neighborCell,
            currentCell);

        queue.Enqueue(neighborCell);
    }

    private List<Vector2Int> BuildPath(
        Vector2Int startCell,
        Vector2Int goalCell,
        Dictionary<Vector2Int, Vector2Int> previousCellByCell)
    {
        List<Vector2Int> path =
            new List<Vector2Int>();

        if (!previousCellByCell.ContainsKey(goalCell))
        {
            return path;
        }

        Vector2Int currentCell = goalCell;

        while (true)
        {
            path.Add(currentCell);

            if (currentCell == startCell)
            {
                break;
            }

            currentCell =
                previousCellByCell[currentCell];
        }

        path.Reverse();
        return path;
    }

    private void ClearLostTargetPursuit()
    {
        m_pursuitTargetCharacterId =
            k_noCharacterId;

        m_pursuitCell = Vector2Int.zero;
        m_pursuitDirection = Vector2Int.zero;
        m_hasEnteredPursuitDirection = false;
    }

    private Vector2Int GetCardinalDirection(
        Vector2Int delta)
    {
        if (delta == Vector2Int.up
            || delta == Vector2Int.right
            || delta == Vector2Int.down
            || delta == Vector2Int.left)
        {
            return delta;
        }

        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            && delta.x != 0)
        {
            return delta.x > 0
                ? Vector2Int.right
                : Vector2Int.left;
        }

        if (delta.y != 0)
        {
            return delta.y > 0
                ? Vector2Int.up
                : Vector2Int.down;
        }

        return Vector2Int.zero;
    }

    private Vector2Int GetCardinalDirection(
        Vector3 direction)
    {
        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2Int.zero;
        }

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.z))
        {
            return direction.x >= 0.0f
                ? Vector2Int.right
                : Vector2Int.left;
        }

        return direction.z >= 0.0f
            ? Vector2Int.up
            : Vector2Int.down;
    }
}