using UnityEngine;

public class TreasurePlacement
{
    public TreasureType m_treasureType;
    public int m_roomId;
    public Vector2Int m_cellPosition;

    public TreasurePlacement(
        TreasureType treasureType,
        int roomId,
        Vector2Int cellPosition)
    {
        m_treasureType = treasureType;
        m_roomId = roomId;
        m_cellPosition = cellPosition;
    }
}