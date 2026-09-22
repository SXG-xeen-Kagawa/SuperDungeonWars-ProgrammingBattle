using UnityEngine;

namespace Party1111111
{
    public sealed class Party1111111Party : ComPartyBase
    {
        private void Start()
        {
            // パーティー開始時の初期化を書けます。
        }

        protected override void SXG_OnPartyThink()
        {
            // パーティー全体の探索・戦闘・帰還方針を書けます。
            //
            // 例:
            // if (SXG_TryGetMemberInfo(0, out PartyMemberInfo memberInfo))
            // {
            //     SXG_SetMemberMoveTargetCell(0, new Vector2Int(0, 0));
            // }
        }
    }
}