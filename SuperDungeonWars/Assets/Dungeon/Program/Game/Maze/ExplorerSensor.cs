using System.Collections.Generic;
using UnityEngine;

public class ExplorerSensor
{
    private readonly MazeData m_mazeData;
    private readonly KnownMapData m_knownMapData;

    public ExplorerSensor(
        MazeData mazeData,
        KnownMapData knownMapData)
    {
        m_mazeData = mazeData;
        m_knownMapData = knownMapData;
    }

    public void ObserveFromCell(
        Vector2Int currentCellPosition)
    {
        MazeCellData currentCell = m_mazeData.GetCell(
            currentCellPosition.x,
            currentCellPosition.y);

        if (currentCell == null)
        {
            return;
        }

        m_knownMapData.ObserveCell(currentCell);

        if (currentCell.m_isRoom)
        {
            ObserveRoom(currentCell.m_roomId);
            return;
        }

        // 通路では、四方向について壁に当たるまで見通せる。
        ObserveStraightLine(
            currentCell.m_x,
            currentCell.m_y,
            Vector2Int.up);

        ObserveStraightLine(
            currentCell.m_x,
            currentCell.m_y,
            Vector2Int.right);

        ObserveStraightLine(
            currentCell.m_x,
            currentCell.m_y,
            Vector2Int.down);

        ObserveStraightLine(
            currentCell.m_x,
            currentCell.m_y,
            Vector2Int.left);
    }

    private void ObserveRoom(int roomId)
    {
        List<MazeCellData> roomCells = m_mazeData.GetRoomCells(
            roomId);

        for (int i = 0; i < roomCells.Count; i++)
        {
            m_knownMapData.ObserveCell(roomCells[i]);
        }

        // 部屋の外周も観測する。
        // これにより、外周のどこが通路へ接続している出口なのか、
        // どこがブロックなのかを既知マップ上で判別できる。
        for (int i = 0; i < roomCells.Count; i++)
        {
            ObserveAdjacentOutsideCells(
                roomCells[i],
                roomId);
        }
    }

    private void ObserveAdjacentOutsideCells(
        MazeCellData roomCell,
        int roomId)
    {
        TryObserveOutside(
            roomCell.m_x,
            roomCell.m_y + 1,
            roomId);

        TryObserveOutside(
            roomCell.m_x + 1,
            roomCell.m_y,
            roomId);

        TryObserveOutside(
            roomCell.m_x,
            roomCell.m_y - 1,
            roomId);

        TryObserveOutside(
            roomCell.m_x - 1,
            roomCell.m_y,
            roomId);
    }

    private void TryObserveOutside(
        int x,
        int y,
        int roomId)
    {
        MazeCellData cell = m_mazeData.GetCell(x, y);
        if (cell == null)
        {
            return;
        }

        if (cell.m_isRoom && cell.m_roomId == roomId)
        {
            return;
        }

        m_knownMapData.ObserveCell(cell);
    }

    private void ObserveStraightLine(
        int startX,
        int startY,
        Vector2Int direction)
    {
        int x = startX + direction.x;
        int y = startY + direction.y;

        while (true)
        {
            MazeCellData cell = m_mazeData.GetCell(x, y);
            if (cell == null)
            {
                return;
            }

            // 壁セル自身も既知にする。
            // これにより、見通しの終端が明確になる。
            m_knownMapData.ObserveCell(cell);

            if (cell.IsBlockedCell())
            {
                return;
            }

            // 通路の先で部屋が見えた場合は、
            // 部屋へ実際に入るまでは部屋全体を開示しない。
            // 部屋へ入った時点で ObserveRoom() が全体を開示する。
            x += direction.x;
            y += direction.y;
        }
    }
}