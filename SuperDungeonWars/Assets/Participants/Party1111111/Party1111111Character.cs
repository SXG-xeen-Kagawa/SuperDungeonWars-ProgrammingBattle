using System.Collections.Generic;
using UnityEngine;

namespace Party1111111
{
    public sealed class Party1111111Character : ComCharacterBase
    {
        /// <summary>
        /// 毎フレーム（またはゲーム側の思考タイミング）に呼ばれます。
        /// 個人単位の行動判断が必要な場合に使います。
        /// パーティー全体の移動方針は Party 側に書くことを推奨します。
        /// </summary>
        protected override void SXG_OnCharacterThink()
        {
        }

        /// <summary>
        /// 攻撃可能な相手から、攻撃する対象の添字を返します。
        /// -1 を返すと攻撃しません。
        /// </summary>
        protected override int SXG_ChooseAttackTargetIndex(
            IReadOnlyList<VisibleCharacterData> attackableCharacters)
        {
            return -1;
        }

        /// <summary>
        /// 財宝に接触したとき、取得するなら true を返します。
        /// たとえば「帰還を優先する」「特定種別だけ拾う」などの方針を書けます。
        /// </summary>
        protected override bool SXG_ShouldPickUpTreasure(TreasureType treasureType)
        {
            return true;
        }

        /// <summary>
        /// 財宝を出口まで運び出した直後に呼ばれます。
        /// 次の探索目標への切替や、搬出数の記録などに使えます。
        /// </summary>
        protected override void SXG_OnTreasureExported(TreasureType treasureType)
        {
        }

        /// <summary>
        /// キャラクターが目的地セルへ到達したときに呼ばれます。
        /// 到達を契機に次の行動を切り替えたい場合に使います。
        /// </summary>
        protected override void SXG_OnCellReached(Vector2Int cellPosition)
        {
        }
    }
}