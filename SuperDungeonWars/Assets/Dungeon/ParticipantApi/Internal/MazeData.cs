using System.Collections.Generic;
using UnityEngine;

public class MazeData
{
    public int m_width;
    public int m_height;
    public MazeCellData[,] m_cells;

    private readonly float m_cellSize;

    public float CellSize => m_cellSize;

    public MazeData(int width, int height, float cellSize)
    {
        m_width = width;
        m_height = height;
        m_cellSize = Mathf.Max(cellSize, 0.01f);
        m_cells = new MazeCellData[m_width, m_height];

        for (int y = 0; y < m_height; y++)
        {
            for (int x = 0; x < m_width; x++)
            {
                m_cells[x, y] = new MazeCellData(x, y);
            }
        }
    }

    public MazeCellData GetCell(int x, int y)
    {
        if (x < 0 || x >= m_width || y < 0 || y >= m_height)
        {
            return null;
        }

        return m_cells[x, y];
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < m_width && y >= 0 && y < m_height;
    }

    public Vector2Int GetCenterCellPosition()
    {
        return new Vector2Int(m_width / 2, m_height / 2);
    }

    public Vector3 CellToWorld(int x, int y)
    {
        float offsetX = (m_width - 1) * 0.5f;
        float offsetY = (m_height - 1) * 0.5f;

        float worldX = (x - offsetX) * m_cellSize;
        float worldZ = (y - offsetY) * m_cellSize;

        return new Vector3(worldX, 0.0f, worldZ);
    }

    public bool TryWorldToCell(
        Vector3 worldPosition,
        out Vector2Int cellPosition)
    {
        return TryWorldToCell(
            worldPosition,
            m_width,
            m_height,
            m_cellSize,
            out cellPosition);
    }

    public static bool TryWorldToCell(
        Vector3 worldPosition,
        int width,
        int height,
        float cellSize,
        out Vector2Int cellPosition)
    {
        cellPosition = Vector2Int.zero;

        if (width <= 0
            || height <= 0
            || cellSize <= 0.0f)
        {
            return false;
        }

        float offsetX = (width - 1) * 0.5f;
        float offsetY = (height - 1) * 0.5f;

        float cellX =
            worldPosition.x / cellSize + offsetX;

        float cellY =
            worldPosition.z / cellSize + offsetY;

        int x = Mathf.FloorToInt(cellX + 0.5f);
        int y = Mathf.FloorToInt(cellY + 0.5f);

        if (x < 0 || x >= width
            || y < 0 || y >= height)
        {
            return false;
        }

        cellPosition = new Vector2Int(x, y);

        return true;
    }

    public List<MazeCellData> GetRoomCells(int roomId)
    {
        List<MazeCellData> result = new List<MazeCellData>();

        for (int y = 0; y < m_height; y++)
        {
            for (int x = 0; x < m_width; x++)
            {
                MazeCellData cell = m_cells[x, y];
                if (cell.m_isRoom && cell.m_roomId == roomId)
                {
                    result.Add(cell);
                }
            }
        }

        return result;
    }

}