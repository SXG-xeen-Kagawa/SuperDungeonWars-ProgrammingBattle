using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 参加者が作成するキャラクターAIの基底クラスです。
///
/// 参加者はこのクラスを継承し、主に以下を実装します。
/// - SXG_OnCharacterThink(): 個別キャラクターの判断
/// - SXG_OnCellReached(): 新しいセルへの到達時の処理
/// - SXG_ChooseAttackTargetIndex(): 攻撃対象の選択
/// - SXG_ShouldPickUpTreasure(): 宝箱取得の判断
/// - SXG_OnTreasureExported(): 宝箱搬出時の処理
///
/// SXG_ で始まる protected メンバーは、参加者が利用・overrideできます。
/// </summary>
public abstract partial class ComCharacterBase : MonoBehaviour
{
    /// <summary>
    /// 自身のキャラクターIDを取得します。
    /// </summary>
    protected int SXG_CharacterId
    {
        get { return m_characterId; }
    }

    /// <summary>
    /// 自パーティー内でのメンバー番号を取得します。
    /// </summary>
    protected int SXG_MemberIndex
    {
        get { return m_memberIndex; }
    }

    /// <summary>
    /// 現在いるセル座標を取得します。
    /// </summary>
    protected Vector2Int SXG_CurrentCell
    {
        get { return GetCurrentCell(); }
    }

    /// <summary>
    /// 現在のワールド座標を取得します。
    /// </summary>
    protected Vector3 SXG_WorldPosition
    {
        get { return GetWorldPosition(); }
    }

    /// <summary>
    /// 現在移動中かを取得します。
    /// </summary>
    protected bool SXG_IsMoving
    {
        get { return IsMoving(); }
    }

    /// <summary>
    /// 現在ノックアウト中かを取得します。
    /// </summary>
    protected bool SXG_IsKnockedOut
    {
        get { return m_isKnockedOut; }
    }

    /// <summary>
    /// 攻撃・起き上がりなどにより、行動がロックされているかを取得します。
    /// </summary>
    protected bool SXG_IsActionLocked
    {
        get { return IsActionLocked(); }
    }

    /// <summary>
    /// 現在宝箱を所持しているかを取得します。
    /// </summary>
    protected bool SXG_HasTreasure
    {
        get { return m_carriedTreasure != null; }
    }

    protected virtual void Awake()
    {
    }

    /// <summary>
    /// 自パーティーが現在までに観測した既知マップを取得します。
    /// </summary>
    protected KnownMapView SXG_GetKnownMap()
    {
        return m_knownMapView;
    }

    /// <summary>
    /// 自身へセル座標を目標とする移動命令を設定します。
    /// 実際の移動反映はゲームシステム側で行われます。
    /// </summary>
    protected bool SXG_SetMoveTargetCell(Vector2Int targetCell)
    {
        if (m_characterRuntime == null)
        {
            return false;
        }

        SetOrder(PartyMemberOrder.CreateMoveToCell(targetCell));
        return true;
    }

    /// <summary>
    /// 自身へワールド座標を目標とする移動命令を設定します。
    /// 実際の移動反映はゲームシステム側で行われます。
    /// </summary>
    protected bool SXG_SetMoveTargetWorld(Vector3 targetWorld)
    {
        if (m_characterRuntime == null)
        {
            return false;
        }

        SetOrder(PartyMemberOrder.CreateMoveToWorld(targetWorld));
        return true;
    }

    /// <summary>
    /// 自身の移動命令を停止へ変更します。
    /// </summary>
    protected void SXG_Stop()
    {
        SetOrder(PartyMemberOrder.CreateStop());
    }

    /// <summary>
    /// 個人単位で行動を判断するため、定期的に呼ばれる任意フックです。
    ///
    /// SXG_OnPartyThink() より後に呼ばれます。
    /// 同一フレームに両方が移動命令を出した場合は、
    /// このメソッドの移動命令が優先されます。
    /// </summary>
    protected virtual void SXG_OnCharacterThink()
    {
    }

    /// <summary>
    /// 自身が新しいセルへ到達した際に呼ばれる任意フックです。
    /// 呼出時点で既知マップは最新化されています。
    /// </summary>
    protected virtual void SXG_OnCellReached(
        Vector2Int cellPosition)
    {
    }

    /// <summary>
    /// 攻撃可能な相手の候補リストから、攻撃対象の候補インデックスを選択します。
    ///
    /// 戻り値は CharacterId ではなく、
    /// attackableCharacters 内での要素番号です。
    /// 攻撃しない場合は負値、通常は -1 を返します。
    /// </summary>
    protected virtual int SXG_ChooseAttackTargetIndex(
        IReadOnlyList<VisibleCharacterData> attackableCharacters)
    {
        return -1;
    }

    /// <summary>
    /// 宝箱を取得するかを判断します。
    /// </summary>
    protected virtual bool SXG_ShouldPickUpTreasure(
        TreasureType treasureType)
    {
        return false;
    }

    /// <summary>
    /// 自身が所持していた宝箱を搬出した際に呼ばれる任意フックです。
    /// </summary>
    protected virtual void SXG_OnTreasureExported(
        TreasureType treasureType)
    {
    }
}