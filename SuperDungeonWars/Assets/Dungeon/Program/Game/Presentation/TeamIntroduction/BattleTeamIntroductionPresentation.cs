using System.Collections.Generic;
using System.Collections.Generic;
using UnityEngine;

public sealed class BattleTeamIntroductionPresentation : MonoBehaviour
{
    private const int TeamCount = 4;

    [SerializeField]
    private BattleTeamPreviewController
        m_battleTeamPreviewController;

    [SerializeField]
    private BattleTeamIntroductionTeamPanel[] m_teamPanels;


    private void Awake()
    {
        if (m_battleTeamPreviewController == null)
        {
            Debug.LogError(
                "BattleTeamIntroductionPresentation: "
                + "BattleTeamPreviewController が未設定です。",
                this);
        }

        if (m_teamPanels == null
            || m_teamPanels.Length != TeamCount)
        {
            Debug.LogError(
                "BattleTeamIntroductionPresentation: "
                + "Team Panels は4件設定してください。",
                this);
        }
    }

    public void Show(
        IReadOnlyList<ComPartyBase> spawnedParties)
    {
        gameObject.SetActive(true);

        if (m_battleTeamPreviewController != null)
        {
            m_battleTeamPreviewController.Show(
                spawnedParties,
                BattleTeamPreviewPose.Introduction);
        }

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleTeamIntroductionTeamPanel teamPanel =
                GetTeamPanelTeamIndex(
                    teamIndex);

            if (teamPanel == null)
            {
                continue;
            }

            ComPartyBase party =
                GetPartyTeamIndex(
                    spawnedParties,
                    teamIndex);

            RenderTexture previewTexture =
                m_battleTeamPreviewController != null
                    ? m_battleTeamPreviewController
                    .GetPreviewTextureTeamIndex(
                        teamIndex)
                    : null;

            teamPanel.Set(
                party,
                previewTexture);
        }
    }

    public void Hide()
    {
        if (m_battleTeamPreviewController != null)
        {
            // 紹介は試合開始前のため、必ず実キャラを元の迷路内へ戻す。
            m_battleTeamPreviewController.Hide(
                true);
        }

        for (int teamIndex = 0;
             teamIndex < TeamCount;
             teamIndex++)
        {
            BattleTeamIntroductionTeamPanel teamPanel =
                GetTeamPanelTeamIndex(
                    teamIndex);

            if (teamPanel != null)
            {
                teamPanel.Clear();
            }
        }

        gameObject.SetActive(false);
    }

    private BattleTeamIntroductionTeamPanel
        GetTeamPanelTeamIndex(
            int teamIndex)
    {
        if (m_teamPanels == null
            || teamIndex < 0
            || teamIndex >= m_teamPanels.Length)
        {
            return null;
        }

        return m_teamPanels[teamIndex];
    }

    private ComPartyBase GetPartyTeamIndex(
        IReadOnlyList<ComPartyBase> spawnedParties,
        int teamIndex)
    {
        if (spawnedParties == null
            || teamIndex < 0
            || teamIndex >= spawnedParties.Count)
        {
            return null;
        }

        return spawnedParties[teamIndex];
    }
}