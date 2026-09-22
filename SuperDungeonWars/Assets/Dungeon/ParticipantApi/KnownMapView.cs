using System;
using UnityEngine;

/// <summary>
/// 参加者プログラムへ公開する、既知マップの読み取り専用ビューです。
/// 内部の KnownMapData および KnownCellData は公開しません。
/// </summary>
public sealed class KnownMapView
{
    private readonly KnownMapData m_knownMapData;

    /// <summary>
    /// 既知マップの幅です。
    /// </summary>
    public int Width
    {
        get { return m_knownMapData.m_width; }
    }

    /// <summary>
    /// 既知マップの高さです。
    /// </summary>
    public int Height
    {
        get { return m_knownMapData.m_height; }
    }

    internal KnownMapView(KnownMapData knownMapData)
    {
        if (knownMapData == null)
        {
            throw new ArgumentNullException(
                nameof(knownMapData));
        }

        m_knownMapData = knownMapData;
    }

    /// <summary>
    /// 指定座標がマップ範囲内にあるかを返します。
    /// </summary>
    public bool IsInside(Vector2Int cellPosition)
    {
        return cellPosition.x >= 0
            && cellPosition.x < Width
            && cellPosition.y >= 0
            && cellPosition.y < Height;
    }

    /// <summary>
    /// 指定セルの読み取り情報を取得します。
    /// マップ範囲外の場合は false を返します。
    /// </summary>
    public bool TryGetCell(
        Vector2Int cellPosition,
        out KnownCellInfo cellInfo)
    {
        cellInfo = null;

        if (!IsInside(cellPosition))
        {
            return false;
        }

        KnownCellData sourceCell =
            m_knownMapData.m_cells[
                cellPosition.x,
                cellPosition.y];

        if (sourceCell == null)
        {
            return false;
        }

        bool isWalkable = sourceCell.m_isObserved
            && sourceCell.m_knownOpenDirections !=
                MazeDirection.None;

        cellInfo = new KnownCellInfo(
            cellPosition,
            sourceCell.m_isObserved,
            sourceCell.m_isRoom,
            sourceCell.m_isCentralHall,
            sourceCell.m_isRoom
                ? sourceCell.m_roomId
                : -1,
            sourceCell.m_isObserved
                ? sourceCell.m_knownOpenDirections
                : MazeDirection.None,
            isWalkable);

        return true;
    }

    /// <summary>
    /// 指定セルが観測済みかを返します。
    /// 範囲外のセルは false です。
    /// </summary>
    public bool IsObserved(Vector2Int cellPosition)
    {
        KnownCellInfo cellInfo;

        return TryGetCell(
            cellPosition,
            out cellInfo)
            && cellInfo.IsObserved;
    }

    /// <summary>
    /// 指定セルが観測済みかつ通行可能かを返します。
    /// 範囲外または未観測のセルは false です。
    /// </summary>
    public bool IsWalkable(Vector2Int cellPosition)
    {
        KnownCellInfo cellInfo;

        return TryGetCell(
            cellPosition,
            out cellInfo)
            && cellInfo.IsWalkable;
    }

    /// <summary>
    /// 指定した2セル間を既知情報だけで移動可能かを返します。
    /// 移動元・移動先は上下左右に隣接しており、両方とも観測済みかつ
    /// 通行可能である必要があります。
    /// </summary>
    public bool CanMove(
        Vector2Int from,
        Vector2Int to)
    {
        MazeDirection direction;

        if (!TryGetDirection(
            from,
            to,
            out direction))
        {
            return false;
        }

        KnownCellInfo fromCellInfo;

        if (!TryGetCell(
            from,
            out fromCellInfo)
            || !fromCellInfo.IsWalkable
            || !fromCellInfo.IsOpen(direction))
        {
            return false;
        }

        KnownCellInfo toCellInfo;

        if (!TryGetCell(
            to,
            out toCellInfo)
            || !toCellInfo.IsWalkable)
        {
            return false;
        }

        MazeDirection oppositeDirection =
            GetOppositeDirection(direction);

        return toCellInfo.IsOpen(oppositeDirection);
    }

    /// <summary>
    /// 指定セルから、指定方向へ開いている隣接セルを取得します。
    /// 出発セルは観測済みである必要がありますが、隣接セルは未観測でも構いません。
    /// </summary>
    public bool TryGetOpenNeighbor(
        Vector2Int from,
        Vector2Int direction,
        out Vector2Int neighbor)
    {
        neighbor = from;

        MazeDirection mazeDirection;

        if (!TryGetDirection(
            from,
            from + direction,
            out mazeDirection))
        {
            return false;
        }

        KnownCellInfo fromCellInfo;

        if (!TryGetCell(
            from,
            out fromCellInfo)
            || !fromCellInfo.IsObserved
            || !fromCellInfo.IsOpen(mazeDirection))
        {
            return false;
        }

        Vector2Int candidate = from + direction;

        if (!IsInside(candidate))
        {
            return false;
        }

        neighbor = candidate;
        return true;
    }

    private static bool TryGetDirection(
        Vector2Int from,
        Vector2Int to,
        out MazeDirection direction)
    {
        direction = MazeDirection.None;

        int deltaX = to.x - from.x;
        int deltaY = to.y - from.y;

        if (deltaX == 0 && deltaY == 1)
        {
            direction = MazeDirection.North;
            return true;
        }

        if (deltaX == 1 && deltaY == 0)
        {
            direction = MazeDirection.East;
            return true;
        }

        if (deltaX == 0 && deltaY == -1)
        {
            direction = MazeDirection.South;
            return true;
        }

        if (deltaX == -1 && deltaY == 0)
        {
            direction = MazeDirection.West;
            return true;
        }

        return false;
    }

    private static MazeDirection GetOppositeDirection(
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

            default:
                return MazeDirection.None;
        }
    }
}