using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 護送・襲撃パーティー用キャラクター。
///
/// Raider（MemberIndex == 1）は通常敵も積極的に攻撃する。
/// それ以外は、敵の宝箱所持者を主な攻撃対象にする。
/// </summary>
public class ConvoyCharacter : ComCharacterBase
{
    private const int k_raiderMemberIndex = 1;

    protected override void SXG_OnCharacterThink()
    {
    }

    protected override bool SXG_ShouldPickUpTreasure(
        TreasureType treasureType)
    {
        return !SXG_HasTreasure;
    }

    protected override int SXG_ChooseAttackTargetIndex(
        IReadOnlyList<VisibleCharacterData> attackableCharacters)
    {
        if (SXG_HasTreasure
            || attackableCharacters == null)
        {
            return -1;
        }

        int selectedCandidateIndex = -1;
        bool hasCarrierTarget = false;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < attackableCharacters.Count; i++)
        {
            VisibleCharacterData candidate =
                attackableCharacters[i];

            if (candidate == null
                || candidate.IsKnockedOut)
            {
                continue;
            }

            // Raider 以外は、無目的な乱戦を避ける。
            if (SXG_MemberIndex != k_raiderMemberIndex
                && !candidate.HasTreasure)
            {
                continue;
            }

            Vector3 delta =
                candidate.WorldPosition - SXG_WorldPosition;

            delta.y = 0.0f;

            float sqrDistance = delta.sqrMagnitude;

            // 全ロール共通で、敵キャリアを最優先する。
            if (candidate.HasTreasure)
            {
                if (!hasCarrierTarget
                    || sqrDistance < bestSqrDistance)
                {
                    hasCarrierTarget = true;
                    bestSqrDistance = sqrDistance;
                    selectedCandidateIndex = i;
                }

                continue;
            }

            if (hasCarrierTarget)
            {
                continue;
            }

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                selectedCandidateIndex = i;
            }
        }

        return selectedCandidateIndex;
    }
}