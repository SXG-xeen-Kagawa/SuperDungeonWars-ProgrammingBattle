using System;

[Flags]
public enum MazeDirection
{
    None = 0,
    North = 1 << 0,
    East = 1 << 1,
    South = 1 << 2,
    West = 1 << 3,
}

public class MazeCellData
{
    public int m_x;
    public int m_y;
    public MazeDirection m_openDirections;
    public bool m_isCentralHall;
    public bool m_isRoom;
    public int m_roomId;

    public MazeCellData(int x, int y)
    {
        m_x = x;
        m_y = y;
        m_openDirections = MazeDirection.None;
        m_isCentralHall = false;
        m_isRoom = false;
        m_roomId = -1;
    }

    public bool IsOpen(MazeDirection direction)
    {
        return (m_openDirections & direction) != 0;
    }

    public bool IsBlockedCell()
    {
        return m_openDirections == MazeDirection.None;
    }
}