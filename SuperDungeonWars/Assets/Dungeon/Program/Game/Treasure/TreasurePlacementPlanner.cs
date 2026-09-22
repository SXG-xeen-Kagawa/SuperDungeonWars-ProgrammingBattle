using System.Collections.Generic;
using UnityEngine;

public class TreasurePlacementPlanner
{
    public List<TreasurePlacement> CreatePlacements(MazeData mazeData)
    {
        List<TreasurePlacement> result =
            new List<TreasurePlacement>();

        if (mazeData == null)
        {
            return result;
        }

        Dictionary<int, List<MazeCellData>> roomCellsByRoomId =
            CollectRoomCellsByRoomId(mazeData);

        Vector2Int centerCellPosition =
            mazeData.GetCenterCellPosition();

        MazeCellData centerCell = mazeData.GetCell(
            centerCellPosition.x,
            centerCellPosition.y);

        if (centerCell == null || !centerCell.m_isRoom)
        {
            Debug.LogError("中央セルから中央部屋を特定できませんでした。");
            return result;
        }

        int centralRoomId = centerCell.m_roomId;

        List<int> roomIds =
            new List<int>(roomCellsByRoomId.Keys);

        roomIds.Sort();

        for (int i = 0; i < roomIds.Count; i++)
        {
            int roomId = roomIds[i];

            List<MazeCellData> roomCells =
                roomCellsByRoomId[roomId];

            Vector2Int placementCell =
                FindRoomCenterCell(roomCells);

            TreasureType treasureType =
                roomId == centralRoomId
                    ? TreasureType.CentralChest
                    : TreasureType.SmallChest;

            result.Add(new TreasurePlacement(
                treasureType,
                roomId,
                placementCell));
        }

        return result;
    }

    private Dictionary<int, List<MazeCellData>> CollectRoomCellsByRoomId(
        MazeData mazeData)
    {
        Dictionary<int, List<MazeCellData>> result =
            new Dictionary<int, List<MazeCellData>>();

        for (int y = 0; y < mazeData.m_height; y++)
        {
            for (int x = 0; x < mazeData.m_width; x++)
            {
                MazeCellData cell = mazeData.GetCell(x, y);

                if (cell == null || !cell.m_isRoom || cell.IsBlockedCell())
                {
                    continue;
                }

                List<MazeCellData> roomCells;

                if (!result.TryGetValue(cell.m_roomId, out roomCells))
                {
                    roomCells = new List<MazeCellData>();
                    result.Add(cell.m_roomId, roomCells);
                }

                roomCells.Add(cell);
            }
        }

        return result;
    }

    private Vector2Int FindRoomCenterCell(List<MazeCellData> roomCells)
    {
        float averageX = 0.0f;
        float averageY = 0.0f;

        for (int i = 0; i < roomCells.Count; i++)
        {
            averageX += roomCells[i].m_x;
            averageY += roomCells[i].m_y;
        }

        averageX /= roomCells.Count;
        averageY /= roomCells.Count;

        MazeCellData nearestCell = roomCells[0];
        float nearestDistanceSqr = float.MaxValue;

        for (int i = 0; i < roomCells.Count; i++)
        {
            MazeCellData cell = roomCells[i];

            float distanceX = cell.m_x - averageX;
            float distanceY = cell.m_y - averageY;
            float distanceSqr =
                distanceX * distanceX + distanceY * distanceY;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestCell = cell;
            }
        }

        return new Vector2Int(nearestCell.m_x, nearestCell.m_y);
    }
}