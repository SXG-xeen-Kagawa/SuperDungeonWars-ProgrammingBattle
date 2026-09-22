using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleResultTeamLane : MonoBehaviour
{
    [SerializeField]
    private Image m_teamColorFrameImage;

    [SerializeField]
    private Sprite[] m_teamColorFrameSprites;

    [SerializeField]
    private Image m_teamIconBaseImage;

    [SerializeField]
    private Image m_teamIconImage;

    [SerializeField]
    private TextMeshProUGUI m_teamNameText;

    [SerializeField]
    private Sprite[] m_teamNameBaseSprites;

    [SerializeField]
    private Image m_teamNameBaseImage;

    [SerializeField]
    private Image m_rankImage;

    [SerializeField]
    private Sprite[] m_rankSprites;

    [SerializeField]
    private TextMeshProUGUI m_totalCoinText;

    [SerializeField]
    private TextMeshProUGUI m_addedCoinText;

    [SerializeField]
    private RectTransform m_addedCoinPopupRoot;

    [SerializeField]
    [Min(0.0f)]
    private float m_addedCoinPopupViewportUpOffset = 0.08f;

    [SerializeField]
    [Min(0.0f)]
    private float m_addedCoinPopupRiseDistance = 48.0f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupPopDuration = 0.10f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupHoldDuration = 0.45f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupFadeOutDuration = 0.16f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupStartScale = 0.65f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupPeakScale = 1.20f;

    [SerializeField]
    [Min(0.01f)]
    private float m_addedCoinPopupEndScale = 0.85f;

    [SerializeField]
    [Min(0.0f)]
    private float m_addedCoinPopupPopRiseDistance = 36.0f;




    [SerializeField]
    private TextMeshProUGUI m_treasureCountText;

    [SerializeField]
    private RawImage m_characterPreviewImage;


    [SerializeField]
    private RectTransform m_goldTowerRectTransform;

    [SerializeField]
    private RectTransform m_bodyRectTransform;

    [SerializeField]
    [Min(0.0f)]
    private float m_goldTowerMaximumHeight = 620.0f;

    [SerializeField]
    [Min(1)]
    private int m_goldTowerReferenceGoldAtMaximumHeight = 600;


    [SerializeField]
    private GameObject m_lightEffectObject;



    private int m_goldTowerScaleReferenceTotalCoin;
    private int m_currentTotalCoin;

    private Vector2 m_bodyBaseAnchoredPosition;
    private bool m_hasCachedGoldTowerBaseLayout;




    public void Initialize(
        int systemTeamIndex,
        ComPartyBase party,
        TeamBattleResult teamResult)
    {
        CacheGoldTowerBaseLayout();

        Color teamColor =
            CharacterTeamColorConstants.GetTeamBaseColor(
                systemTeamIndex);

        if (m_teamColorFrameImage != null)
        {
            m_teamColorFrameImage.sprite =
                m_teamColorFrameSprites[systemTeamIndex];
        }

        if (m_teamIconBaseImage != null)
        {
            m_teamIconBaseImage.color = teamColor;
        }

        if (m_teamIconImage != null)
        {
            Sprite teamIconSprite =
                party != null
                    ? party.TeamIconSprite
                    : null;

            m_teamIconImage.sprite = teamIconSprite;

            m_teamIconImage.gameObject.SetActive(
                teamIconSprite != null);
        }

        if (m_teamNameText != null)
        {
            m_teamNameText.text =
                party != null
                    ? party.TeamDisplayName
                    : "TEAM " + (systemTeamIndex + 1);
        }

        if (m_teamNameBaseImage != null)
        {
            m_teamNameBaseImage.sprite =
                m_teamNameBaseSprites[systemTeamIndex];
        }

        m_goldTowerScaleReferenceTotalCoin =
            Mathf.Max(
                1,
                m_goldTowerReferenceGoldAtMaximumHeight);

        m_currentTotalCoin = 0;

        SetRank(0);
        SetLightEffectVisible(false);
        SetPreviewTexture(null);
        SetTotalCoin(0);
        HideAddedCoin();

        if (m_treasureCountText != null)
        {
            int treasureCount =
                teamResult != null
                    ? teamResult.Treasures.Count
                    : 0;

            m_treasureCountText.text =
                string.Format(
                    "{0}<size=80%> 個</size>",
                    treasureCount);
        }
    }

    public void SetPreviewTexture(
        RenderTexture renderTexture)
    {
        if (m_characterPreviewImage == null)
        {
            return;
        }

        m_characterPreviewImage.texture =
            renderTexture;

        m_characterPreviewImage.gameObject.SetActive(
            renderTexture != null);
    }

    public void SetRank(int rank)
    {
        if (m_rankImage == null)
        {
            return;
        }

        int spriteIndex = rank - 1;

        bool canShowRank =
            spriteIndex >= 0
            && m_rankSprites != null
            && spriteIndex < m_rankSprites.Length
            && m_rankSprites[spriteIndex] != null;

        m_rankImage.gameObject.SetActive(canShowRank);

        if (!canShowRank)
        {
            return;
        }

        m_rankImage.sprite = m_rankSprites[spriteIndex];
    }

    public void SetTotalCoin(int totalCoin)
    {
        m_currentTotalCoin =
            Mathf.Max(0, totalCoin);

        if (m_totalCoinText != null)
        {
            m_totalCoinText.text =
                m_currentTotalCoin + "<size=80%> G</size>";
        }

        UpdateGoldTowerPresentation();
    }

    public void SetGoldTowerScaleReferenceTotal(
        int maximumBattleTotalCoin)
    {
        int referenceGold =
            Mathf.Max(
                m_goldTowerReferenceGoldAtMaximumHeight,
                maximumBattleTotalCoin);

        m_goldTowerScaleReferenceTotalCoin =
            Mathf.Max(1, referenceGold);

        UpdateGoldTowerPresentation();
    }

    private void CacheGoldTowerBaseLayout()
    {
        if (m_hasCachedGoldTowerBaseLayout)
        {
            return;
        }

        if (m_bodyRectTransform != null)
        {
            m_bodyBaseAnchoredPosition =
                m_bodyRectTransform.anchoredPosition;
        }

        m_hasCachedGoldTowerBaseLayout = true;
    }

    private void UpdateGoldTowerPresentation()
    {
        CacheGoldTowerBaseLayout();

        float progress =
            Mathf.Clamp01(
                (float)m_currentTotalCoin
                / Mathf.Max(
                    1,
                    m_goldTowerScaleReferenceTotalCoin));

        float targetHeight =
            m_goldTowerMaximumHeight
            * progress;

        if (m_goldTowerRectTransform != null)
        {
            Vector3 goldTowerLocalPosition =
                m_goldTowerRectTransform.localPosition;

            goldTowerLocalPosition.y =
                targetHeight;

            m_goldTowerRectTransform.localPosition =
                goldTowerLocalPosition;
        }

        if (m_bodyRectTransform == null)
        {
            return;
        }

        m_bodyRectTransform.anchoredPosition =
            m_bodyBaseAnchoredPosition
            + Vector2.up * targetHeight;
    }


    public IEnumerator CoShowAddedCoinPopup(
        int addedCoin,
        Camera previewCamera,
        Vector3 treasureWorldPosition)
    {
        if (m_addedCoinText == null
            || addedCoin <= 0)
        {
            yield break;
        }

        RectTransform rawImageRectTransform =
            m_characterPreviewImage != null
                ? m_characterPreviewImage.rectTransform
                : null;

        RectTransform popupRoot =
            m_addedCoinPopupRoot != null
                ? m_addedCoinPopupRoot
                : transform as RectTransform;

        if (rawImageRectTransform == null
            || popupRoot == null
            || previewCamera == null)
        {
            ShowAddedCoin(addedCoin);

            if (m_addedCoinPopupHoldDuration > 0.0f)
            {
                yield return new WaitForSecondsRealtime(
                    m_addedCoinPopupHoldDuration);
            }

            HideAddedCoin();

            yield break;
        }

        Vector3 viewportPosition =
            previewCamera.WorldToViewportPoint(
                treasureWorldPosition);

        if (viewportPosition.z <= 0.0f)
        {
            viewportPosition = new Vector3(
                0.5f,
                0.5f,
                1.0f);
        }

        viewportPosition.x =
            Mathf.Clamp01(viewportPosition.x);

        viewportPosition.y =
            Mathf.Clamp01(
                viewportPosition.y
                + m_addedCoinPopupViewportUpOffset);

        Vector2 startAnchoredPosition =
            new Vector2(
                Mathf.Lerp(
                    popupRoot.rect.xMin,
                    popupRoot.rect.xMax,
                    viewportPosition.x),
                Mathf.Lerp(
                    popupRoot.rect.yMin,
                    popupRoot.rect.yMax,
                    viewportPosition.y));

        Vector2 popEndAnchoredPosition =
            startAnchoredPosition
            + Vector2.up
            * m_addedCoinPopupPopRiseDistance;

        Vector2 fadeOutEndAnchoredPosition =
            popEndAnchoredPosition
            + Vector2.up
            * m_addedCoinPopupRiseDistance;

        RectTransform addedCoinRectTransform =
            m_addedCoinText.rectTransform;

        m_addedCoinText.text =
            "+" + addedCoin + " G";

        Color baseColor =
            m_addedCoinText.color;

        baseColor.a = 1.0f;
        m_addedCoinText.color = baseColor;

        addedCoinRectTransform.anchoredPosition =
            startAnchoredPosition;

        addedCoinRectTransform.localScale =
            Vector3.one
            * m_addedCoinPopupStartScale;

        m_addedCoinText.gameObject.SetActive(true);

        float elapsedTime = 0.0f;

        while (elapsedTime < m_addedCoinPopupPopDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime
                    / m_addedCoinPopupPopDuration);

            float easedProgress =
                1.0f - Mathf.Pow(
                    1.0f - progress,
                    3.0f);

            addedCoinRectTransform.anchoredPosition =
                Vector2.Lerp(
                    startAnchoredPosition,
                    popEndAnchoredPosition,
                    easedProgress);

            float scale =
                Mathf.Lerp(
                    m_addedCoinPopupStartScale,
                    m_addedCoinPopupPeakScale,
                    easedProgress);

            addedCoinRectTransform.localScale =
                Vector3.one * scale;

            yield return null;
        }

        addedCoinRectTransform.anchoredPosition =
            popEndAnchoredPosition;

        addedCoinRectTransform.localScale =
            Vector3.one
            * m_addedCoinPopupPeakScale;

        if (m_addedCoinPopupHoldDuration > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_addedCoinPopupHoldDuration);
        }

        elapsedTime = 0.0f;

        while (elapsedTime
               < m_addedCoinPopupFadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime
                    / m_addedCoinPopupFadeOutDuration);

            addedCoinRectTransform.anchoredPosition =
                Vector2.Lerp(
                    popEndAnchoredPosition,
                    fadeOutEndAnchoredPosition,
                    progress);

            float scale =
                Mathf.Lerp(
                    m_addedCoinPopupPeakScale,
                    m_addedCoinPopupEndScale,
                    progress);

            addedCoinRectTransform.localScale =
                Vector3.one * scale;

            Color color = baseColor;
            color.a = 1.0f - progress;
            m_addedCoinText.color = color;

            yield return null;
        }

        HideAddedCoin();
    }

    public void ShowAddedCoin(int addedCoin)
    {
        if (m_addedCoinText == null)
        {
            return;
        }

        if (addedCoin <= 0)
        {
            HideAddedCoin();
            return;
        }

        Color color = m_addedCoinText.color;
        color.a = 1.0f;
        m_addedCoinText.color = color;

        m_addedCoinText.text =
            "+" + addedCoin + " G";

        m_addedCoinText.gameObject.SetActive(true);
    }

    public void HideAddedCoin()
    {
        if (m_addedCoinText == null)
        {
            return;
        }

        m_addedCoinText.gameObject.SetActive(false);
    }



    public void SetLightEffectVisible(
        bool isVisible)
    {
        if (m_lightEffectObject == null)
        {
            return;
        }

        m_lightEffectObject.SetActive(
            isVisible);
    }

}