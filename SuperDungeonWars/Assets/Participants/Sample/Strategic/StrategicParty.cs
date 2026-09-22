using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱回収・搬出を主目的とし、敵の宝箱所持者だけを
/// 必要人数で阻止するパーティー。
/// </summary>
public class StrategicParty : ComPartyBase
{
    private const int k_noCharacterId = -1;
    private const int k_maxChasers = 2;

    private int m_lostCarrierCharacterId = k_noCharacterId;
    private Vector2Int m_lastSeenCarrierCell;
    private Vector2Int m_lastSeenCarrierDirection;
    private bool m_hasEnteredLostCarrierDirection;

    private readonly Dictionary<int, Vector2Int>
        m_previousVisibleCellByCharacterId =
            new Dictionary<int, Vector2Int>();

    protected override void SXG_OnPartyThink()
    {
        if (SXG_GetMemberCount() <= 0)
        {
            return;
        }

        HashSet<int> reservedMemberIndices =
            new HashSet<int>();

        IssueExportOrders(reservedMemberIndices);
        TryIssueTreasureRecoveryOrder(reservedMemberIndices);

        VisibleCharacterData visibleCarrier;

        if (TryFindBestVisibleEnemyCarrier(out visibleCarrier))
        {
            UpdateLostCarrierData(visibleCarrier);

            IssueChaseOrders(
                visibleCarrier.CellPosition,
                k_maxChasers,
                reservedMemberIndices);
        }
        else
        {
            TryIssueLostCarrierPursuitOrders(
                reservedMemberIndices);
        }

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

    private bool TryIssueTreasureRecoveryOrder(
        HashSet<int> reservedMemberIndices)
    {
        VisibleTreasureData targetTreasure;

        if (!TryFindBestVisibleUncarriedTreasure(
            out targetTreasure))
        {
            return false;
        }

        int collectorIndex;

        if (!TryFindNearestAvailableMember(
            targetTreasure.CellPosition,
            reservedMemberIndices,
            out collectorIndex))
        {
            return false;
        }

        PartyMemberInfo collectorInfo;

        if (!SXG_TryGetMemberInfo(
            collectorIndex,
            out collectorInfo))
        {
            return false;
        }

        if (collectorInfo.CurrentCell ==
            targetTreasure.CellPosition)
        {
            SXG_SetMemberMoveTargetWorld(
                collectorIndex,
                targetTreasure.WorldPosition);
        }
        else
        {
            Vector2Int nextCell;

            if (TryFindNextStepToTarget(
                collectorInfo.CurrentCell,
                targetTreasure.CellPosition,
                out nextCell))
            {
                SXG_SetMemberMoveTargetCell(
                    collectorIndex,
                    nextCell);
            }
            else
            {
                SXG_StopMember(collectorIndex);
            }
        }

        reservedMemberIndices.Add(collectorIndex);
        return true;
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

            float distance =
                GetNearestAvailableMemberSqrDistance(
                    candidate.WorldPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCarrier = candidate;
            }
        }

        return bestCarrier != null;
    }

    private void UpdateLostCarrierData(
        VisibleCharacterData visibleCarrier)
    {
        Vector2Int direction = Vector2Int.zero;
        Vector2Int previousCell;

        if (m_previousVisibleCellByCharacterId.TryGetValue(
            visibleCarrier.CharacterId,
            out previousCell))
        {
            direction = GetCardinalDirection(
                visibleCarrier.CellPosition - previousCell);
        }

        if (direction == Vector2Int.zero)
        {
            direction = GetCardinalDirection(
                visibleCarrier.Forward);
        }

        m_previousVisibleCellByCharacterId[
            visibleCarrier.CharacterId] =
            visibleCarrier.CellPosition;

        m_lostCarrierCharacterId =
            visibleCarrier.CharacterId;

        m_lastSeenCarrierCell =
            visibleCarrier.CellPosition;

        if (direction != Vector2Int.zero)
        {
            m_lastSeenCarrierDirection = direction;
        }

        m_hasEnteredLostCarrierDirection = false;
    }

    private bool TryIssueLostCarrierPursuitOrders(
        HashSet<int> reservedMemberIndices)
    {
        if (m_lostCarrierCharacterId == k_noCharacterId
            || m_lastSeenCarrierDirection == Vector2Int.zero)
        {
            return false;
        }

        if (HasAvailableMemberOutsideCell(
            m_lastSeenCarrierCell,
            reservedMemberIndices))
        {
            IssueChaseOrders(
                m_lastSeenCarrierCell,
                1,
                reservedMemberIndices);

            return true;
        }

        KnownMapView knownMap = SXG_GetKnownMap();

        if (knownMap == null
            || (m_hasEnteredLostCarrierDirection
                && IsBranchOrDeadEnd(
                    knownMap,
                    m_lastSeenCarrierCell,
                    m_lastSeenCarrierDirection)))
        {
            ClearLostCarrierPursuit();
            return false;
        }

        Vector2Int nextCell;

        if (!knownMap.TryGetOpenNeighbor(
            m_lastSeenCarrierCell,
            m_lastSeenCarrierDirection,
            out nextCell))
        {
            ClearLostCarrierPursuit();
            return false;
        }

        m_lastSeenCarrierCell = nextCell;
        m_hasEnteredLostCarrierDirection = true;

        IssueChaseOrders(
            m_lastSeenCarrierCell,
            1,
            reservedMemberIndices);

        return true;
    }

    private void IssueChaseOrders(
        Vector2Int targetCell,
        int maxChaserCount,
        HashSet<int> reservedMemberIndices)
    {
        List<int> candidates = new List<int>();

        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(i, out memberInfo)
                && IsAvailableMember(
                    memberInfo,
                    reservedMemberIndices))
            {
                candidates.Add(i);
            }
        }

        candidates.Sort(
            delegate (int a, int b)
            {
                PartyMemberInfo aInfo;
                PartyMemberInfo bInfo;

                SXG_TryGetMemberInfo(a, out aInfo);
                SXG_TryGetMemberInfo(b, out bInfo);

                return GetPathLength(
                    aInfo.CurrentCell,
                    targetCell).CompareTo(
                    GetPathLength(
                        bInfo.CurrentCell,
                        targetCell));
            });

        int issuedCount = 0;

        for (int i = 0;
             i < candidates.Count
             && issuedCount < maxChaserCount;
             i++)
        {
            int memberIndex = candidates[i];
            PartyMemberInfo memberInfo;

            if (!SXG_TryGetMemberInfo(
                memberIndex,
                out memberInfo))
            {
                continue;
            }

            Vector2Int nextCell;

            if (!TryFindNextStepToTarget(
                memberInfo.CurrentCell,
                targetCell,
                out nextCell))
            {
                continue;
            }

            SXG_SetMemberMoveTargetCell(
                memberIndex,
                nextCell);

            reservedMemberIndices.Add(memberIndex);
            issuedCount++;
        }
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

    private bool TryFindNearestAvailableMember(
        Vector2Int targetCell,
        HashSet<int> reservedMemberIndices,
        out int resultIndex)
    {
        resultIndex = -1;
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
                resultIndex = i;
            }
        }

        return resultIndex >= 0;
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

    private bool HasAvailableMemberOutsideCell(
        Vector2Int targetCell,
        HashSet<int> reservedMemberIndices)
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            PartyMemberInfo memberInfo;

            if (SXG_TryGetMemberInfo(i, out memberInfo)
                && IsAvailableMember(
                    memberInfo,
                    reservedMemberIndices)
                && memberInfo.CurrentCell != targetCell)
            {
                return true;
            }
        }

        return false;
    }

    private float GetNearestAvailableMemberSqrDistance(
        Vector3 targetPosition)
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
                targetPosition - memberInfo.WorldPosition;

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

    private bool IsBranchOrDeadEnd(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int forwardDirection)
    {
        int openCount = 0;
        bool hasForwardPath = false;

        CountOpenDirection(knownMap, currentCell, Vector2Int.up,
            forwardDirection, ref openCount, ref hasForwardPath);
        CountOpenDirection(knownMap, currentCell, Vector2Int.right,
            forwardDirection, ref openCount, ref hasForwardPath);
        CountOpenDirection(knownMap, currentCell, Vector2Int.down,
            forwardDirection, ref openCount, ref hasForwardPath);
        CountOpenDirection(knownMap, currentCell, Vector2Int.left,
            forwardDirection, ref openCount, ref hasForwardPath);

        return !hasForwardPath || openCount >= 3;
    }

    private void CountOpenDirection(
        KnownMapView knownMap,
        Vector2Int currentCell,
        Vector2Int direction,
        Vector2Int forwardDirection,
        ref int openCount,
        ref bool hasForwardPath)
    {
        Vector2Int neighbor;

        if (!knownMap.TryGetOpenNeighbor(
            currentCell,
            direction,
            out neighbor))
        {
            return;
        }

        openCount++;

        if (direction == forwardDirection)
        {
            hasForwardPath = true;
        }
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

    private void ClearLostCarrierPursuit()
    {
        m_lostCarrierCharacterId = k_noCharacterId;
        m_lastSeenCarrierCell = Vector2Int.zero;
        m_lastSeenCarrierDirection = Vector2Int.zero;
        m_hasEnteredLostCarrierDirection = false;
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