using System.Collections.Generic;
using UnityEngine;

public class TreasureReturnCharacter : ComCharacterBase
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

            if (sqrDistance >= bestSqrDistance)
            {
                continue;
            }

            bestSqrDistance = sqrDistance;
            selectedCandidateIndex = i;
        }

        return selectedCandidateIndex;
    }
}