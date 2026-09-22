using UnityEngine;

/// <summary>
/// 参加者プログラムへ公開する、パーティーメンバーの読み取り情報です。
/// このオブジェクトはゲーム内キャラクター実体を保持しません。
/// </summary>
public sealed class PartyMemberInfo
{
    /// <summary>
    /// バトル内で一意なキャラクターIDです。
    /// </summary>
    public int CharacterId { get; }

    /// <summary>
    /// パーティー内のメンバー番号です。
    /// </summary>
    public int MemberIndex { get; }

    /// <summary>
    /// 現在いる論理セル座標です。
    /// </summary>
    public Vector2Int CurrentCell { get; }

    /// <summary>
    /// 現在のワールド座標です。
    /// </summary>
    public Vector3 WorldPosition { get; }

    /// <summary>
    /// 移動目標を持ち、移動中であるかを示します。
    /// </summary>
    public bool IsMoving { get; }

    /// <summary>
    /// ノックアウト中であるかを示します。
    /// </summary>
    public bool IsKnockedOut { get; }

    /// <summary>
    /// 攻撃・起き上がりなどにより、行動がロックされているかを示します。
    /// </summary>
    public bool IsActionLocked { get; }

    /// <summary>
    /// 宝箱を所持しているかを示します。
    /// 宝箱そのものの実体は公開しません。
    /// </summary>
    public bool HasTreasure { get; }

    internal PartyMemberInfo(
        int characterId,
        int memberIndex,
        Vector2Int currentCell,
        Vector3 worldPosition,
        bool isMoving,
        bool isKnockedOut,
        bool isActionLocked,
        bool hasTreasure)
    {
        CharacterId = characterId;
        MemberIndex = memberIndex;
        CurrentCell = currentCell;
        WorldPosition = worldPosition;
        IsMoving = isMoving;
        IsKnockedOut = isKnockedOut;
        IsActionLocked = isActionLocked;
        HasTreasure = hasTreasure;
    }
}