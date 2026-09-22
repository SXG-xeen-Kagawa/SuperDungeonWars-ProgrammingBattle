using UnityEngine;

public interface ICharacterRuntime
{
    Vector2Int CurrentCell { get; }
    Vector3 WorldPosition { get; }
    bool IsMoving { get; }

    void BindCharacter(ComCharacterBase character);

    bool TryMoveToCell(Vector2Int targetCell);

    bool TryMoveToWorld(Vector3 targetWorld);

    void StopMove();
}

public interface ITreasureRuntime
{
    int TreasureId { get; }

    TreasureType TreasureType { get; }
}

public sealed class ParticipantMazeBounds
{
    private readonly int m_width;
    private readonly int m_height;

    public int Width
    {
        get { return m_width; }
    }

    public int Height
    {
        get { return m_height; }
    }

    public ParticipantMazeBounds(
        int width,
        int height)
    {
        m_width = Mathf.Max(0, width);
        m_height = Mathf.Max(0, height);
    }

    public bool IsInside(
        Vector2Int cellPosition)
    {
        return IsInside(
            cellPosition.x,
            cellPosition.y);
    }

    public bool IsInside(
        int x,
        int y)
    {
        return x >= 0
            && x < m_width
            && y >= 0
            && y < m_height;
    }
}