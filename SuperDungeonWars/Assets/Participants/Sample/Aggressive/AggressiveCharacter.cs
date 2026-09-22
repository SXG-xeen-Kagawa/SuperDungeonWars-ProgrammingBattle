using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻撃特化パーティー用キャラクター。
///
/// 宝箱を持っていない間は、攻撃可能な敵を必ず攻撃する。
/// 宝箱所持者が攻撃候補に含まれる場合は、最優先で狙う。
/// </summary>
public class AggressiveCharacter : ComCharacterBase
{
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
        bool hasTreasureCarrierTarget = false;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0;
             i < attackableCharacters.Count;
             i++)
        {
            VisibleCharacterData candidate =
                attackableCharacters[i];

            if (candidate == null)
            {
                continue;
            }

            Vector3 delta =
                candidate.WorldPosition -
                SXG_WorldPosition;

            delta.y = 0.0f;

            float sqrDistance = delta.sqrMagnitude;

            if (candidate.HasTreasure)
            {
                if (!hasTreasureCarrierTarget
                    || sqrDistance < bestSqrDistance)
                {
                    hasTreasureCarrierTarget = true;
                    bestSqrDistance = sqrDistance;
                    selectedCandidateIndex = i;
                }

                continue;
            }

            if (hasTreasureCarrierTarget)
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