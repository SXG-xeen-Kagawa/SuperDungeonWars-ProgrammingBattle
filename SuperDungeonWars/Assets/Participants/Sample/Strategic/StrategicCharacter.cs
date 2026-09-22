using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 宝箱所持中の敵だけを優先して攻撃するキャラクター。
/// 通常敵との無目的な戦闘、ノックアウト相手への追撃は行わない。
/// </summary>
public class StrategicCharacter : ComCharacterBase
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
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < attackableCharacters.Count; i++)
        {
            VisibleCharacterData candidate =
                attackableCharacters[i];

            // 通常敵との乱戦、および死体蹴りはしない。
            if (candidate == null
                || candidate.IsKnockedOut
                || !candidate.HasTreasure)
            {
                continue;
            }

            Vector3 delta =
                candidate.WorldPosition - SXG_WorldPosition;

            delta.y = 0.0f;

            float sqrDistance = delta.sqrMagnitude;

            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                selectedCandidateIndex = i;
            }
        }

        return selectedCandidateIndex;
    }
}