using System.Collections.Generic;
using UnityEngine;

public class BattleVisibleWorldSystem
{
    private readonly Dictionary<ComCharacterBase, int>
        m_characterSystemTeamIndices =
            new Dictionary<ComCharacterBase, int>();

    private readonly HashSet<int> m_visibleCharacterIds =
        new HashSet<int>();

    public void UpdateVisibleWorldData(
        MazeData mazeData,
        List<ComPartyBase> parties,
        IReadOnlyList<TreasureChest> treasureChests)
    {
        if (mazeData == null || parties == null)
        {
            return;
        }

        m_characterSystemTeamIndices.Clear();

        RegisterCharacterTeamIndices(parties);

        for (int observerSystemTeamIndex = 0;
             observerSystemTeamIndex < parties.Count;
             observerSystemTeamIndex++)
        {
            ComPartyBase observerParty =
                parties[observerSystemTeamIndex];

            if (observerParty == null)
            {
                continue;
            }

            UpdatePartyVisibleWorldData(
                mazeData,
                parties,
                observerParty,
                observerSystemTeamIndex,
                treasureChests);
        }
    }

    private void RegisterCharacterTeamIndices(
        List<ComPartyBase> parties)
    {
        for (int teamIndex = 0;
             teamIndex < parties.Count;
             teamIndex++)
        {
            ComPartyBase party = parties[teamIndex];

            if (party == null)
            {
                continue;
            }

            for (int memberIndex = 0;
                 memberIndex < party.MemberCount;
                 memberIndex++)
            {
                ComCharacterBase member;

                if (!party.TryGetMember(
                    memberIndex,
                    out member))
                {
                    continue;
                }

                m_characterSystemTeamIndices[member] = teamIndex;
            }
        }
    }

    private void UpdatePartyVisibleWorldData(
        MazeData mazeData,
        List<ComPartyBase> parties,
        ComPartyBase observerParty,
        int observerSystemTeamIndex,
        IReadOnlyList<TreasureChest> treasureChests)
    {
        VisibleWorldData visibleWorldData =
            observerParty.GetVisibleWorldData();

        if (visibleWorldData == null)
        {
            return;
        }

        visibleWorldData.Clear();
        m_visibleCharacterIds.Clear();

        AddVisibleCharacters(
            mazeData,
            parties,
            observerParty,
            observerSystemTeamIndex,
            visibleWorldData);

        AddVisibleTreasures(
            mazeData,
            observerParty,
            treasureChests,
            visibleWorldData);
    }

    private void AddVisibleCharacters(
        MazeData mazeData,
        List<ComPartyBase> parties,
        ComPartyBase observerParty,
        int observerSystemTeamIndex,
        VisibleWorldData visibleWorldData)
    {
        int partyCount = parties.Count;

        for (int targetSystemTeamIndex = 0;
             targetSystemTeamIndex < parties.Count;
             targetSystemTeamIndex++)
        {
            ComPartyBase targetParty =
                parties[targetSystemTeamIndex];

            if (targetParty == null)
            {
                continue;
            }

            for (int memberIndex = 0;
                 memberIndex < targetParty.MemberCount;
                 memberIndex++)
            {
                ComCharacterBase targetCharacter;

                if (!targetParty.TryGetMember(
                    memberIndex,
                    out targetCharacter))
                {
                    continue;
                }

                bool isAlly =
                    targetSystemTeamIndex == observerSystemTeamIndex;

                bool isVisible = isAlly
                    || IsCharacterVisibleToParty(
                        mazeData,
                        observerParty,
                        targetCharacter);

                if (!isVisible)
                {
                    continue;
                }

                int visibleTeamIndex =
                    GetRelativeTeamIndex(
                        targetSystemTeamIndex,
                        observerSystemTeamIndex,
                        partyCount);

                AddVisibleCharacter(
                    targetCharacter,
                    visibleTeamIndex,
                    visibleWorldData);
            }
        }
    }

    private void AddVisibleCharacter(
        ComCharacterBase character,
        int visibleTeamIndex,
        VisibleWorldData visibleWorldData)
    {
        if (character == null
            || character.GetCharacterId() < 0)
        {
            return;
        }

        Vector3 forward = character.transform.forward;
        forward.y = 0.0f;

        if (forward.sqrMagnitude > 0.0001f)
        {
            forward.Normalize();
        }
        else
        {
            forward = Vector3.forward;
        }

        VisibleCharacterData visibleCharacter =
            new VisibleCharacterData(
                character.GetCharacterId(),
                visibleTeamIndex,
                character.GetWorldPosition(),
                character.GetCurrentCell(),
                forward,
                character.HasTreasure(),
                character.IsKnockedOut());

        visibleWorldData.AddCharacter(visibleCharacter);

        m_visibleCharacterIds.Add(
            character.GetCharacterId());
    }

    private void AddVisibleTreasures(
        MazeData mazeData,
        ComPartyBase observerParty,
        IReadOnlyList<TreasureChest> treasureChests,
        VisibleWorldData visibleWorldData)
    {
        if (treasureChests == null)
        {
            return;
        }

        for (int i = 0; i < treasureChests.Count; i++)
        {
            TreasureChest treasureChest = treasureChests[i];

            if (treasureChest == null
                || treasureChest.TreasureId < 0
                || treasureChest.IsExported)
            {
                continue;
            }

            bool isVisible;

            if (treasureChest.IsPickedUp)
            {
                ComCharacterBase ownerCharacter =
                    treasureChest.OwnerCharacter;

                isVisible = ownerCharacter != null
                    && m_visibleCharacterIds.Contains(
                        ownerCharacter.GetCharacterId());
            }
            else
            {
                isVisible = IsCellVisibleToParty(
                    mazeData,
                    observerParty,
                    treasureChest.CellPosition);
            }

            if (!isVisible)
            {
                continue;
            }

            int ownerCharacterId = -1;
            Vector2Int cellPosition = treasureChest.CellPosition;

            if (treasureChest.OwnerCharacter != null)
            {
                ownerCharacterId =
                    treasureChest.OwnerCharacter
                        .GetCharacterId();

                cellPosition =
                    treasureChest.OwnerCharacter
                        .GetCurrentCell();
            }

            VisibleTreasureData visibleTreasure =
                new VisibleTreasureData(
                    treasureChest.TreasureId,
                    treasureChest.WorldPosition,
                    cellPosition,
                    treasureChest.TreasureType,
                    treasureChest.IsPickedUp,
                    ownerCharacterId);

            visibleWorldData.AddTreasure(visibleTreasure);
        }
    }

    private bool IsCharacterVisibleToParty(
        MazeData mazeData,
        ComPartyBase observerParty,
        ComCharacterBase targetCharacter)
    {
        if (targetCharacter == null)
        {
            return false;
        }

        return IsCellVisibleToParty(
            mazeData,
            observerParty,
            targetCharacter.GetCurrentCell());
    }

    private bool IsCellVisibleToParty(
        MazeData mazeData,
        ComPartyBase observerParty,
        Vector2Int targetCellPosition)
    {
        if (observerParty == null)
        {
            return false;
        }

        for (int memberIndex = 0;
             memberIndex < observerParty.MemberCount;
             memberIndex++)
        {
            ComCharacterBase observerCharacter;

            if (!observerParty.TryGetMember(
                memberIndex,
                out observerCharacter))
            {
                continue;
            }

            if (CanSeeCell(
                mazeData,
                observerCharacter.GetCurrentCell(),
                targetCellPosition))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanSeeCell(
        MazeData mazeData,
        Vector2Int observerCellPosition,
        Vector2Int targetCellPosition)
    {
        if (mazeData == null)
        {
            return false;
        }

        MazeCellData observerCell = mazeData.GetCell(
            observerCellPosition.x,
            observerCellPosition.y);

        MazeCellData targetCell = mazeData.GetCell(
            targetCellPosition.x,
            targetCellPosition.y);

        if (observerCell == null || targetCell == null)
        {
            return false;
        }

        if (observerCellPosition == targetCellPosition)
        {
            return true;
        }

        if (observerCell.m_isRoom
            && targetCell.m_isRoom
            && observerCell.m_roomId >= 0
            && observerCell.m_roomId == targetCell.m_roomId)
        {
            return true;
        }

        if (observerCellPosition.x == targetCellPosition.x)
        {
            MazeDirection direction =
                targetCellPosition.y > observerCellPosition.y
                    ? MazeDirection.North
                    : MazeDirection.South;

            return CanSeeStraightLine(
                mazeData,
                observerCellPosition,
                targetCellPosition,
                direction);
        }

        if (observerCellPosition.y == targetCellPosition.y)
        {
            MazeDirection direction =
                targetCellPosition.x > observerCellPosition.x
                    ? MazeDirection.East
                    : MazeDirection.West;

            return CanSeeStraightLine(
                mazeData,
                observerCellPosition,
                targetCellPosition,
                direction);
        }

        return false;
    }

    private bool CanSeeStraightLine(
        MazeData mazeData,
        Vector2Int observerCellPosition,
        Vector2Int targetCellPosition,
        MazeDirection direction)
    {
        Vector2Int directionOffset =
            GetDirectionOffset(direction);

        MazeDirection oppositeDirection =
            GetOppositeDirection(direction);

        Vector2Int currentPosition = observerCellPosition;

        while (currentPosition != targetCellPosition)
        {
            Vector2Int nextPosition =
                currentPosition + directionOffset;

            MazeCellData currentCell = mazeData.GetCell(
                currentPosition.x,
                currentPosition.y);

            MazeCellData nextCell = mazeData.GetCell(
                nextPosition.x,
                nextPosition.y);

            if (currentCell == null || nextCell == null)
            {
                return false;
            }

            if (!currentCell.IsOpen(direction)
                || !nextCell.IsOpen(oppositeDirection))
            {
                return false;
            }

            currentPosition = nextPosition;
        }

        return true;
    }

    private int GetRelativeTeamIndex(
        int targetSystemTeamIndex,
        int observerSystemTeamIndex,
        int partyCount)
    {
        if (partyCount <= 0)
        {
            return 0;
        }

        return
            (targetSystemTeamIndex
            - observerSystemTeamIndex
            + partyCount)
            % partyCount;
    }

    private Vector2Int GetDirectionOffset(
        MazeDirection direction)
    {
        switch (direction)
        {
            case MazeDirection.North:
                return Vector2Int.up;

            case MazeDirection.East:
                return Vector2Int.right;

            case MazeDirection.South:
                return Vector2Int.down;

            case MazeDirection.West:
                return Vector2Int.left;
        }

        return Vector2Int.zero;
    }

    private MazeDirection GetOppositeDirection(
        MazeDirection direction)
    {
        switch (direction)
        {
            case MazeDirection.North:
                return MazeDirection.South;

            case MazeDirection.East:
                return MazeDirection.West;

            case MazeDirection.South:
                return MazeDirection.North;

            case MazeDirection.West:
                return MazeDirection.East;
        }

        return MazeDirection.None;
    }
}