using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleInGameHudTeamCard : MonoBehaviour
{
    [SerializeField] private Image m_teamColorBarImage;
    [SerializeField] private Image m_teamIconImage;
    [SerializeField] private Image m_teamIconFrameImage;
    [SerializeField] private TextMeshProUGUI m_teamNameText;
    [SerializeField] private TextMeshProUGUI m_availableCountText;
    [SerializeField] private Image[] m_memberStateImages;
    [SerializeField] private Transform m_exportedTreasureRoot;
    [SerializeField] private Image m_smallTreasureIconPrefab;
    [SerializeField] private Image m_centralTreasureIconPrefab;
    [SerializeField] private Color m_knockedOutColor = Color.gray;

    [SerializeField]
    private bool m_centerExportedTreasures = true;

    [SerializeField] private float m_treasureIconSpacing = 6.0f;

    [SerializeField] private float m_treasureDropHeight = 70.0f;

    [SerializeField] private float m_treasureDropDuration = 0.45f;

    [SerializeField] private float m_treasureDropRotationDegrees = 540.0f;

    private readonly List<TreasureIconVisual> m_treasureIconVisuals =
        new List<TreasureIconVisual>();

    private ComPartyBase m_currentParty;


    private class TreasureIconVisual
    {
        public TreasureChest m_treasureChest;
        public Image m_iconImage;
        public Vector2 m_targetPosition;
        public float m_animationStartTime;
        public bool m_isDropAnimating;
        public float m_startRotationZ;
        public float m_rotationDirection;
    }



    private void Update()
    {
        UpdateTreasureDropAnimations();
    }

    private void UpdateTreasureDropAnimations()
    {
        for (int i = 0; i < m_treasureIconVisuals.Count; i++)
        {
            TreasureIconVisual visual =
                m_treasureIconVisuals[i];

            if (!visual.m_isDropAnimating
                || visual.m_iconImage == null)
            {
                continue;
            }

            float duration = Mathf.Max(
                0.01f,
                m_treasureDropDuration);

            float rate = Mathf.Clamp01(
                (Time.time - visual.m_animationStartTime)
                / duration);

            float dropRate = rate * rate;

            RectTransform iconRectTransform =
                visual.m_iconImage.rectTransform;

            iconRectTransform.anchoredPosition =
                visual.m_targetPosition
                + Vector2.up
                    * Mathf.Lerp(
                        m_treasureDropHeight,
                        0.0f,
                        dropRate);

            float rotationZ = Mathf.Lerp(
                visual.m_startRotationZ,
                0.0f,
                rate);

            iconRectTransform.localRotation =
                Quaternion.Euler(0.0f, 0.0f, rotationZ);

            if (rate >= 1.0f)
            {
                iconRectTransform.anchoredPosition =
                    visual.m_targetPosition;

                iconRectTransform.localRotation =
                    Quaternion.identity;

                visual.m_isDropAnimating = false;
            }
        }
    }



    public void Refresh(
        ComPartyBase party,
        int systemTeamIndex)
    {
        if (party == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (m_currentParty != party)
        {
            ClearTreasureIcons();
            m_currentParty = party;
        }

        gameObject.SetActive(true);

        Color teamColor =
            CharacterTeamColorConstants.GetTeamBaseColor(
                systemTeamIndex);

        ApplyTeamIdentity(party, teamColor);
        RefreshMemberStates(party, teamColor);

        RefreshExportedTreasures(
            party.GetExportedTreasureChests());
    }

    private void ApplyTeamIdentity(
        ComPartyBase party,
        Color teamColor)
    {
        if (m_teamColorBarImage != null)
        {
            m_teamColorBarImage.color = teamColor;
        }

        if (m_teamIconFrameImage != null)
        {
            m_teamIconFrameImage.color = teamColor;
        }

        if (m_teamNameText != null)
        {
            m_teamNameText.text = party.TeamDisplayName;
        }

        if (m_teamIconImage != null)
        {
            Sprite teamIcon = party.TeamIconSprite;

            m_teamIconImage.sprite = teamIcon;
            m_teamIconImage.enabled = teamIcon != null;
        }
    }

    private void RefreshMemberStates(
        ComPartyBase party,
        Color teamColor)
    {
        int memberCount = party.MemberCount;
        int availableCount = 0;

        for (int i = 0; i < memberCount; i++)
        {
            ComCharacterBase member;

            bool hasMember = party.TryGetMember(
                i,
                out member);

            bool isAvailable = hasMember
                && member != null
                && !member.IsKnockedOut();

            if (isAvailable)
            {
                availableCount++;
            }

            if (m_memberStateImages == null
                || i >= m_memberStateImages.Length
                || m_memberStateImages[i] == null)
            {
                continue;
            }

            m_memberStateImages[i].color = isAvailable
                ? teamColor
                : m_knockedOutColor;
        }

        if (m_availableCountText != null)
        {
            m_availableCountText.text =
                "行動可能 " + availableCount + " / "
                + memberCount;
        }
    }

    private void RefreshExportedTreasures(
        IReadOnlyList<ITreasureRuntime> exportedTreasures)
    {
        if (exportedTreasures == null)
        {
            return;
        }

        for (int i = 0; i < exportedTreasures.Count; i++)
        {
            TreasureChest treasureChest =
                exportedTreasures[i] as TreasureChest;

            if (treasureChest == null
                || HasTreasureIcon(treasureChest))
            {
                continue;
            }

            Image iconPrefab =
                treasureChest.TreasureType
                    == TreasureType.CentralChest
                ? m_centralTreasureIconPrefab
                : m_smallTreasureIconPrefab;

            CreateTreasureIcon(treasureChest, iconPrefab);
        }

        LayoutTreasureIcons();
    }

    private void CreateTreasureIcon(
        TreasureChest treasureChest,
        Image prefab)
    {
        if (treasureChest == null
            || prefab == null
            || m_exportedTreasureRoot == null)
        {
            return;
        }

        Image icon = Instantiate(
            prefab,
            m_exportedTreasureRoot);

        icon.gameObject.SetActive(true);

        TreasureIconVisual visual =
            new TreasureIconVisual();

        visual.m_treasureChest = treasureChest;
        visual.m_iconImage = icon;
        visual.m_animationStartTime = Time.time;
        visual.m_isDropAnimating = true;

        visual.m_rotationDirection =
            treasureChest.TreasureId % 2 == 0
                ? 1.0f
                : -1.0f;

        visual.m_startRotationZ =
            -m_treasureDropRotationDegrees
            * visual.m_rotationDirection;

        m_treasureIconVisuals.Add(visual);
    }

    private void LayoutTreasureIcons()
    {
        if (m_treasureIconVisuals.Count <= 0)
        {
            return;
        }

        float totalWidth = 0.0f;

        for (int i = 0; i < m_treasureIconVisuals.Count; i++)
        {
            totalWidth += GetTreasureIconWidth(
                m_treasureIconVisuals[i].m_iconImage);

            if (i < m_treasureIconVisuals.Count - 1)
            {
                totalWidth += m_treasureIconSpacing;
            }
        }

        float currentX = -totalWidth * 0.5f;

        for (int i = 0; i < m_treasureIconVisuals.Count; i++)
        {
            TreasureIconVisual visual =
                m_treasureIconVisuals[i];

            if (visual.m_iconImage == null)
            {
                continue;
            }

            RectTransform iconRectTransform =
                visual.m_iconImage.rectTransform;

            float iconWidth =
                GetTreasureIconWidth(visual.m_iconImage);

            iconRectTransform.anchorMin =
                new Vector2(0.5f, 0.5f);

            iconRectTransform.anchorMax =
                new Vector2(0.5f, 0.5f);

            iconRectTransform.pivot =
                new Vector2(0.0f, 0.5f);

            visual.m_targetPosition =
                new Vector2(currentX, 0.0f);

            if (!visual.m_isDropAnimating)
            {
                iconRectTransform.anchoredPosition =
                    visual.m_targetPosition;

                iconRectTransform.localRotation =
                    Quaternion.identity;
            }

            currentX += iconWidth + m_treasureIconSpacing;
        }
    }

    private float GetTreasureIconWidth(Image icon)
    {
        if (icon == null)
        {
            return 0.0f;
        }

        float width = icon.rectTransform.rect.width;

        if (width > 0.0f)
        {
            return width;
        }

        return 32.0f;
    }


    private bool HasTreasureIcon(
        TreasureChest treasureChest)
    {
        for (int i = 0; i < m_treasureIconVisuals.Count; i++)
        {
            if (m_treasureIconVisuals[i].m_treasureChest
                == treasureChest)
            {
                return true;
            }
        }

        return false;
    }

    private void ClearTreasureIcons()
    {
        for (int i = 0; i < m_treasureIconVisuals.Count; i++)
        {
            Image iconImage =
                m_treasureIconVisuals[i].m_iconImage;

            if (iconImage != null)
            {
                Destroy(iconImage.gameObject);
            }
        }

        m_treasureIconVisuals.Clear();
    }
}