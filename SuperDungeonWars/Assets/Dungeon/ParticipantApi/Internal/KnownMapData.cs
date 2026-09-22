using System.Collections.Generic;
using UnityEngine;

public delegate void KnownCellUpdatedHandler(
    int x,
    int y,
    KnownCellData knownCellData);

public delegate void KnownMapUpdatedHandler();

public enum KnownCellViewType
{
    Unknown,
    Blocked,
    Corridor,
    Room,
}

public class KnownCellData
{
    public bool m_isObserved;
    public bool m_isRoom;
    public bool m_isCentralHall;
    public int m_roomId;
    public MazeDirection m_knownOpenDirections;

    public KnownCellData()
    {
        m_isObserved = false;
        m_isRoom = false;
        m_isCentralHall = false;
        m_roomId = -1;
        m_knownOpenDirections = MazeDirection.None;
    }

    public bool IsBlockedCell()
    {
        return m_knownOpenDirections == MazeDirection.None;
    }

    public bool IsOpen(MazeDirection direction)
    {
        return (m_knownOpenDirections & direction) != 0;
    }

    public KnownCellViewType GetViewType()
    {
        if (m_isObserved == false)
        {
            return KnownCellViewType.Unknown;
        }

        if (m_knownOpenDirections == MazeDirection.None)
        {
            return KnownCellViewType.Blocked;
        }

        if (m_isRoom)
        {
            return KnownCellViewType.Room;
        }

        return KnownCellViewType.Corridor;
    }
}

public class KnownMapData
{
    public int m_width;
    public int m_height;
    public KnownCellData[,] m_cells;

    private readonly List<Vector2Int> m_centralHallCells =
        new List<Vector2Int>();

    public event KnownCellUpdatedHandler m_onKnownCellUpdated;
    public event KnownMapUpdatedHandler m_onKnownMapUpdated;

    public IReadOnlyList<Vector2Int> CentralHallCells
    {
        get { return m_centralHallCells; }
    }

    public KnownMapData(int width, int height)
    {
        m_width = width;
        m_height = height;
        m_cells = new KnownCellData[m_width, m_height];

        for (int y = 0; y < m_height; y++)
        {
            for (int x = 0; x < m_width; x++)
            {
                m_cells[x, y] = new KnownCellData();
            }
        }
    }

    public KnownCellData GetCell(int x, int y)
    {
        if (x < 0 || x >= m_width || y < 0 || y >= m_height)
        {
            return null;
        }

        return m_cells[x, y];
    }

    public bool IsObserved(int x, int y)
    {
        KnownCellData cell = GetCell(x, y);
        return cell != null && cell.m_isObserved;
    }

    public Vector2Int[] GetCentralHallCells()
    {
        return m_centralHallCells.ToArray();
    }

    public void SetCentralHallCells(
        List<Vector2Int> CentralHallCells)
    {
        m_centralHallCells.Clear();

        if (CentralHallCells != null)
        {
            for (int i = 0; i < CentralHallCells.Count; i++)
            {
                Vector2Int goalCell = CentralHallCells[i];

                if (GetCell(goalCell.x, goalCell.y) == null)
                {
                    continue;
                }

                if (m_centralHallCells.Contains(goalCell))
                {
                    continue;
                }

                m_centralHallCells.Add(goalCell);
            }
        }

        if (m_onKnownMapUpdated != null)
        {
            m_onKnownMapUpdated();
        }
    }



    public void ObserveCentralHallCells(
        MazeData mazeData)
    {
        if (mazeData == null)
        {
            return;
        }

        for (int i = 0;
             i < m_centralHallCells.Count;
             i++)
        {
            Vector2Int centralHallCell =
                m_centralHallCells[i];

            MazeCellData sourceCell =
                mazeData.GetCell(
                    centralHallCell.x,
                    centralHallCell.y);

            if (sourceCell == null)
            {
                continue;
            }

            ObserveCell(sourceCell);
        }
    }



    public void ObserveCell(MazeCellData sourceCell)
    {
        if (sourceCell == null)
        {
            return;
        }

        KnownCellData knownCell = GetCell(
            sourceCell.m_x,
            sourceCell.m_y);

        if (knownCell == null)
        {
            return;
        }

        bool isChanged = false;

        if (knownCell.m_isObserved != true)
        {
            knownCell.m_isObserved = true;
            isChanged = true;
        }

        if (knownCell.m_isRoom != sourceCell.m_isRoom)
        {
            knownCell.m_isRoom = sourceCell.m_isRoom;
            isChanged = true;
        }

        if (knownCell.m_isCentralHall != sourceCell.m_isCentralHall)
        {
            knownCell.m_isCentralHall = sourceCell.m_isCentralHall;
            isChanged = true;
        }

        if (knownCell.m_roomId != sourceCell.m_roomId)
        {
            knownCell.m_roomId = sourceCell.m_roomId;
            isChanged = true;
        }

        if (knownCell.m_knownOpenDirections
            != sourceCell.m_openDirections)
        {
            knownCell.m_knownOpenDirections =
                sourceCell.m_openDirections;

            isChanged = true;
        }

        if (isChanged == false)
        {
            return;
        }

        if (m_onKnownCellUpdated != null)
        {
            m_onKnownCellUpdated(
                sourceCell.m_x,
                sourceCell.m_y,
                knownCell);
        }

        if (m_onKnownMapUpdated != null)
        {
            m_onKnownMapUpdated();
        }
    }
}