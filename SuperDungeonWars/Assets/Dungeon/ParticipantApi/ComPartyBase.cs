using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 参加者が作成するパーティーAIの基底クラスです。
///
/// 参加者はこのクラスを継承し、主に以下を実装します。
/// - SXG_OnPartyThink(): パーティー全体の判断
///
/// SXG_ で始まる protected メンバーは、参加者が利用・overrideできます。
/// </summary>
public abstract partial class ComPartyBase : MonoBehaviour
{
    [Tooltip("このパーティーのメンバーとして使用するキャラクターのプレハブです。")]
    [SerializeField] private ComCharacterBase m_memberPrefabs;

    [Header("制作者・パーティー情報")]
    [Tooltip("参加者の表示名を入力します。")]
    [SerializeField] private string m_creatorDisplayName;

    [Tooltip("学校名や団体名などの所属を入力します。所属がない場合は空欄で構いません。")]
    [SerializeField] private string m_affiliation;

    [Tooltip("パーティーの表示名を入力します。")]
    [SerializeField] private string m_teamDisplayName;

    [Tooltip("パーティーの特徴を短く入力します。")]
    [TextArea(1, 2)]
    [SerializeField] private string m_teamSimpleDescription;

    [Tooltip("観戦表示などで使用するパーティーのアイコン画像です。")]
    [SerializeField] private Sprite m_teamIconSprite;

    protected virtual void Awake()
    {
    }

    

    /// <summary>
    /// 参加者が現在までに観測した既知マップを取得します。
    /// </summary>
    protected KnownMapView SXG_GetKnownMap()
    {
        return m_knownMapView;
    }

    /// <summary>
    /// 現在視認しているキャラクター情報を取得します。
    /// </summary>
    protected IReadOnlyList<VisibleCharacterData> SXG_GetVisibleCharacters()
    {
        return m_visibleWorldData.VisibleCharacters;
    }

    /// <summary>
    /// 現在視認している宝箱情報を取得します。
    /// </summary>
    protected IReadOnlyList<VisibleTreasureData> SXG_GetVisibleTreasures()
    {
        return m_visibleWorldData.VisibleTreasures;
    }

    /// <summary>
    /// 自パーティーの帰還先となる搬出口セルを取得します。
    /// </summary>
    protected bool SXG_TryGetReturnEntranceCell(
        out Vector2Int returnEntranceCell)
    {
        return TryGetReturnEntranceCell(
            out returnEntranceCell);
    }

    /// <summary>
    /// パーティーに所属するメンバー数を取得します。
    /// </summary>
    protected int SXG_GetMemberCount()
    {
        return m_members.Count;
    }

    /// <summary>
    /// 指定したメンバー番号の読み取り情報を取得します。
    /// キャラクター実体は公開しません。
    /// </summary>
    protected bool SXG_TryGetMemberInfo(
        int memberIndex,
        out PartyMemberInfo memberInfo)
    {
        memberInfo = null;

        ComCharacterBase member;

        if (!TryGetMember(memberIndex, out member))
        {
            return false;
        }

        memberInfo = new PartyMemberInfo(
            member.GetCharacterId(),
            member.GetMemberIndex(),
            member.GetCurrentCell(),
            member.GetWorldPosition(),
            member.IsMoving(),
            member.IsKnockedOut(),
            member.IsActionLocked(),
            member.HasTreasure());

        return true;
    }

    /// <summary>
    /// 指定メンバーへ、セル座標を目標とする移動命令を設定します。
    /// 実際の移動処理はゲームシステム側で行われます。
    /// </summary>
    protected bool SXG_SetMemberMoveTargetCell(
        int memberIndex,
        Vector2Int targetCell)
    {
        ComCharacterBase member;

        if (!TryGetMember(memberIndex, out member))
        {
            return false;
        }

        member.SetOrder(
            PartyMemberOrder.CreateMoveToCell(targetCell));

        return true;
    }

    /// <summary>
    /// 指定メンバーへ、ワールド座標を目標とする移動命令を設定します。
    /// 実際の移動処理はゲームシステム側で行われます。
    /// </summary>
    protected bool SXG_SetMemberMoveTargetWorld(
        int memberIndex,
        Vector3 targetWorld)
    {
        ComCharacterBase member;

        if (!TryGetMember(memberIndex, out member))
        {
            return false;
        }

        member.SetOrder(
            PartyMemberOrder.CreateMoveToWorld(targetWorld));

        return true;
    }

    /// <summary>
    /// 指定メンバーの移動命令を停止へ変更します。
    /// </summary>
    protected void SXG_StopMember(int memberIndex)
    {
        ComCharacterBase member;

        if (!TryGetMember(memberIndex, out member))
        {
            return;
        }

        member.SetOrder(PartyMemberOrder.CreateStop());
    }

    /// <summary>
    /// 全メンバーの移動命令を停止へ変更します。
    /// </summary>
    protected void SXG_StopAllMembers()
    {
        for (int i = 0; i < SXG_GetMemberCount(); i++)
        {
            SXG_StopMember(i);
        }
    }

    /// <summary>
    /// パーティー単位で行動を判断するため、定期的に呼ばれる任意フックです。
    ///
    /// メンバー個別の SXG_OnCharacterThink() より先に呼ばれます。
    /// 同一フレームに両方が移動命令を出した場合は、
    /// キャラクター個別の命令が優先されます。
    /// </summary>
    protected virtual void SXG_OnPartyThink()
    {
    }
}