using UnityEngine;

/// <summary>
/// 参加者プログラムへ公開する、既知マップ上の1セルの読み取り情報です。
/// このオブジェクトは内部の KnownCellData を公開しません。
/// </summary>
public sealed class KnownCellInfo
{
    /// <summary>
    /// セル座標です。
    /// </summary>
    public Vector2Int CellPosition { get; }

    /// <summary>
    /// このセルが視界により観測済みかを示します。
    /// </summary>
    public bool IsObserved { get; }

    /// <summary>
    /// このセルが部屋に属するかを示します。
    /// 未観測セルでは false です。
    /// </summary>
    public bool IsRoom { get; }

    /// <summary>
    /// このセルが中央ホールに属するかを示します。
    /// 未観測セルでは false です。
    /// </summary>
    public bool IsCentralHall { get; }

    /// <summary>
    /// 部屋IDです。部屋ではないセル、または未観測セルでは -1 です。
    /// </summary>
    public int RoomId { get; }

    /// <summary>
    /// 観測済みの開通方向です。
    /// 未観測セルでは MazeDirection.None です。
    /// </summary>
    public MazeDirection KnownOpenDirections { get; }

    /// <summary>
    /// 観測済みかつ通行可能なセルであるかを示します。
    /// </summary>
    public bool IsWalkable { get; }

    internal KnownCellInfo(
        Vector2Int cellPosition,
        bool isObserved,
        bool isRoom,
        bool isCentralHall,
        int roomId,
        MazeDirection knownOpenDirections,
        bool isWalkable)
    {
        CellPosition = cellPosition;
        IsObserved = isObserved;
        IsRoom = isRoom;
        IsCentralHall = isCentralHall;
        RoomId = roomId;
        KnownOpenDirections = knownOpenDirections;
        IsWalkable = isWalkable;
    }

    /// <summary>
    /// 指定方向へ開通していることが既知であるかを返します。
    /// </summary>
    public bool IsOpen(MazeDirection direction)
    {
        return (KnownOpenDirections & direction) != 0;
    }
}