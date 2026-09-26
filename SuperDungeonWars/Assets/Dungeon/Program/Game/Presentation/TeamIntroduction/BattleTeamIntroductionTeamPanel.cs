using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleTeamIntroductionTeamPanel : MonoBehaviour
{
    [SerializeField]
    private RawImage m_previewRawImage;

    [SerializeField]
    private Image m_teamIconImage;

    [SerializeField]
    private TMP_Text m_creatorNameText;

    [SerializeField]
    private TMP_Text m_teamNameText;

    [SerializeField]
    private TMP_Text m_teamDescriptionText;

    public void Set(
        ComPartyBase party,
        RenderTexture previewTexture)
    {
        if (m_previewRawImage != null)
        {
            m_previewRawImage.texture = previewTexture;
            m_previewRawImage.color = Color.white;
        }

        if (m_teamIconImage != null)
        {
            m_teamIconImage.sprite =
                party != null ? party.TeamIconSprite : null;

            m_teamIconImage.color = Color.white;
        }

        if (m_teamNameText != null)
        {
            m_teamNameText.text =
                party != null ? party.TeamDisplayName : string.Empty;
        }

        if (m_creatorNameText != null)
        {
            m_creatorNameText.richText = true;

            if (party == null)
            {
                m_creatorNameText.text = string.Empty;
            }
            else if (string.IsNullOrWhiteSpace(party.Affiliation))
            {
                m_creatorNameText.text = party.CreatorDisplayName;
            }
            else
            {
                m_creatorNameText.text =
                    $"{party.CreatorDisplayName}<size=80%>（{party.Affiliation}）</size>";
            }
        }

        if (m_teamDescriptionText != null)
        {
            m_teamDescriptionText.text =
                party != null
                    ? party.TeamSimpleDescription
                    : string.Empty;
        }
    }

    public void Clear()
    {
        if (m_previewRawImage != null)
        {
            m_previewRawImage.texture = null;
        }

        if (m_teamIconImage != null)
        {
            m_teamIconImage.sprite = null;
        }

        if (m_creatorNameText != null)
        {
            m_creatorNameText.text = string.Empty;
        }

        if (m_teamNameText != null)
        {
            m_teamNameText.text = string.Empty;
        }

        if (m_teamDescriptionText != null)
        {
            m_teamDescriptionText.text = string.Empty;
        }
    }
}