using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class BattleSpectatorTeamLabels : MonoBehaviour
{
    [SerializeField] private BattlePrototypeGameController m_battleGameController;
    [SerializeField] private Camera m_spectatorCamera;
    [SerializeField] private RectTransform m_labelLayer;
    [SerializeField] private TextMeshProUGUI m_labelTemplate;

    [SerializeField] private float m_labelHeight = 1.8f;
    [SerializeField] private float m_verticalOffset = 8f;
    [SerializeField] private float m_labelSpacing = 6f;

    [SerializeField] private LayerMask m_wallOcclusionMask;

    // SpawnedParties の順番。赤、緑、黄、青。
    [SerializeField]
    private Color[] m_teamTextColors =
    {
        new Color32(255, 216, 212, 255),
        new Color32(212, 245, 221, 255),
        new Color32(255, 241, 196, 255),
        new Color32(214, 231, 255, 255)
    };

    private readonly List<TextMeshProUGUI> m_labels =
        new List<TextMeshProUGUI>();

    private readonly List<ComCharacterBase> m_representatives =
        new List<ComCharacterBase>();

    private readonly List<Rect> m_visibleLabelRects =
        new List<Rect>();

    private int m_lastUpdatedFrame = -1;

    private void Awake()
    {
        HideTemplate();
    }

    private void OnEnable()
    {
        HideTemplate();
        m_lastUpdatedFrame = -1;
        Canvas.willRenderCanvases += RefreshLabels;
    }

    private void OnDisable()
    {
        Canvas.willRenderCanvases -= RefreshLabels;

        for (int i = 0; i < m_labels.Count; i++)
        {
            if (m_labels[i] != null)
            {
                m_labels[i].gameObject.SetActive(false);
            }
        }
    }

    private void HideTemplate()
    {
        if (m_labelTemplate != null)
        {
            m_labelTemplate.gameObject.SetActive(false);
        }
    }

    private void RefreshLabels()
    {
        if (m_lastUpdatedFrame == Time.frameCount)
        {
            return;
        }

        m_lastUpdatedFrame = Time.frameCount;

        if (m_battleGameController == null ||
            m_spectatorCamera == null ||
            m_labelLayer == null ||
            m_labelTemplate == null)
        {
            return;
        }

        int partyCount = m_battleGameController.SpawnedPartyCount;
        EnsureLabelCount(partyCount);
        m_visibleLabelRects.Clear();

        for (int teamIndex = 0; teamIndex < m_labels.Count; teamIndex++)
        {
            TextMeshProUGUI label = m_labels[teamIndex];

            if (teamIndex >= partyCount)
            {
                label.gameObject.SetActive(false);
                continue;
            }

            ComPartyBase party =
                m_battleGameController.SpawnedParties[teamIndex];

            ComCharacterBase representative;
            Vector2 labelPosition;
            Rect labelRect;

            if (party == null ||
                !TryChooseRepresentative(
                    party,
                    m_representatives[teamIndex],
                    label.rectTransform.rect.size,
                    out representative,
                    out labelPosition,
                    out labelRect))
            {
                m_representatives[teamIndex] = null;
                label.gameObject.SetActive(false);
                continue;
            }

            m_representatives[teamIndex] = representative;
            m_visibleLabelRects.Add(labelRect);

            string teamName = string.IsNullOrWhiteSpace(party.TeamDisplayName)
                ? string.Empty
                : "<size=75%>（" +
                  SafeText(party.TeamDisplayName) +
                  "）</size>";

            label.richText = true;
            label.alignment = TextAlignmentOptions.Bottom;
            //label.text =
            //    SafeText(party.CreatorDisplayName) +
            //    teamName +
            //    "\n<size=70%>▼</size>";
            label.text =
                SafeText(party.CreatorDisplayName) +
                "\n<size=70%>▼</size>";

            label.color = GetTeamTextColor(teamIndex);
            label.rectTransform.anchoredPosition = labelPosition;
            label.gameObject.SetActive(true);
        }
    }

    private Color GetTeamTextColor(int teamIndex)
    {
        if (m_teamTextColors != null &&
            teamIndex >= 0 &&
            teamIndex < m_teamTextColors.Length)
        {
            return m_teamTextColors[teamIndex];
        }

        return Color.white;
    }

    private void EnsureLabelCount(int count)
    {
        while (m_labels.Count < count)
        {
            TextMeshProUGUI label =
                Instantiate(m_labelTemplate, m_labelLayer);

            label.gameObject.SetActive(false);
            label.raycastTarget = false;

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);

            m_labels.Add(label);
            m_representatives.Add(null);
        }
    }

    private bool TryChooseRepresentative(
        ComPartyBase party,
        ComCharacterBase previous,
        Vector2 labelSize,
        out ComCharacterBase chosen,
        out Vector2 chosenPosition,
        out Rect chosenRect)
    {
        chosen = null;
        chosenPosition = Vector2.zero;
        chosenRect = new Rect();

        float bestScore = float.MinValue;

        for (int memberIndex = 0;
             memberIndex < party.MemberCount;
             memberIndex++)
        {
            ComCharacterBase member;

            if (!party.TryGetMember(memberIndex, out member) ||
                member == null ||
                member.IsKnockedOut())
            {
                continue;
            }

            Vector3 worldPosition =
                member.GetWorldPosition() +
                Vector3.up * m_labelHeight;

            Vector3 viewport =
                m_spectatorCamera.WorldToViewportPoint(worldPosition);

            if (viewport.z <= 0f ||
                viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f ||
                IsHiddenByWall(worldPosition))
            {
                continue;
            }

            Rect layerRect = m_labelLayer.rect;

            float x = layerRect.xMin +
                viewport.x * layerRect.width;

            float y = layerRect.yMin +
                viewport.y * layerRect.height +
                m_verticalOffset;

            Rect candidateRect = new Rect(
                x - labelSize.x * 0.5f,
                y,
                labelSize.x,
                labelSize.y);

            if (candidateRect.xMin < layerRect.xMin ||
                candidateRect.xMax > layerRect.xMax ||
                candidateRect.yMin < layerRect.yMin ||
                candidateRect.yMax > layerRect.yMax ||
                OverlapsExistingLabel(candidateRect))
            {
                continue;
            }

            // 同じキャラクターを優先し、ラベルの飛び移りを抑える。
            float score = member == previous ? 10f : 0f;
            score -=
                (viewport.x - 0.5f) * (viewport.x - 0.5f) +
                (viewport.y - 0.5f) * (viewport.y - 0.5f);

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            chosen = member;
            chosenRect = candidateRect;
            chosenPosition = new Vector2(
                x - layerRect.center.x,
                y - layerRect.center.y);
        }

        return chosen != null;
    }

    private bool IsHiddenByWall(Vector3 worldPosition)
    {
        if (m_wallOcclusionMask.value == 0)
        {
            return false;
        }

        Vector3 direction =
            worldPosition - m_spectatorCamera.transform.position;

        return Physics.Raycast(
            m_spectatorCamera.transform.position,
            direction.normalized,
            direction.magnitude,
            m_wallOcclusionMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool OverlapsExistingLabel(Rect candidate)
    {
        candidate.xMin -= m_labelSpacing;
        candidate.xMax += m_labelSpacing;
        candidate.yMin -= m_labelSpacing;
        candidate.yMax += m_labelSpacing;

        for (int i = 0; i < m_visibleLabelRects.Count; i++)
        {
            if (candidate.Overlaps(m_visibleLabelRects[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static string SafeText(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("<", "＜").Replace(">", "＞");
    }
}