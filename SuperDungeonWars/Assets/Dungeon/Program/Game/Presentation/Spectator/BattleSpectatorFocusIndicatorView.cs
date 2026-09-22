using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 観戦映像の枠色と、ミニマップへ伸びる注目位置しっぽを更新する。
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleSpectatorFocusIndicatorView
    : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private BattleMazeMapView m_battleMazeMapView;

    [SerializeField]
    private RectTransform m_spectatorRootRectTransform;

    [SerializeField]
    private Image m_spectatorFrameImage;

    [SerializeField]
    private BattleSpectatorFocusTailGraphic m_tailGraphic;

    [Header("Tail")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_tailAlpha = 0.28f;

    [SerializeField]
    [Min(1.0f)]
    private float m_tailBaseHalfHeight = 14.0f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float m_tailRootCenterPull = 0.75f;

    [SerializeField]
    [Min(0.0f)]
    private float m_tailMinimumDiagonalOffset = 36.0f;


    [SerializeField]
    private Color m_noFocusFrameColor = Color.white;

    private ComCharacterBase m_focusCharacter;
    private TreasureChest m_focusTreasureChest;
    private int m_focusSystemTeamIndex = -1;

    private Canvas m_rootCanvas;
    private Camera m_uiCamera;

    private void Awake()
    {
        m_rootCanvas = GetComponentInParent<Canvas>();

        if (m_rootCanvas != null
            && m_rootCanvas.renderMode
                != RenderMode.ScreenSpaceOverlay)
        {
            m_uiCamera = m_rootCanvas.worldCamera;
        }

        RefreshFrameColor();
        ClearTail();
    }

    private void LateUpdate()
    {
        RefreshTail();
    }

    /// <summary>
    /// キャラクターを観戦対象として設定する。
    /// </summary>
    public void SetFocusCharacter(
        ComCharacterBase character,
        int systemTeamIndex)
    {
        m_focusCharacter = character;
        m_focusTreasureChest = null;
        m_focusSystemTeamIndex = systemTeamIndex;

        RefreshFrameColor();
    }

    /// <summary>
    /// 宝箱を観戦対象として設定する。
    /// </summary>
    public void SetFocusTreasureChest(
        TreasureChest treasureChest,
        int systemTeamIndex)
    {
        m_focusCharacter = null;
        m_focusTreasureChest = treasureChest;
        m_focusSystemTeamIndex = systemTeamIndex;

        RefreshFrameColor();
    }

    /// <summary>
    /// 注目表示を解除する。
    /// </summary>
    public void ClearFocus()
    {
        m_focusCharacter = null;
        m_focusTreasureChest = null;
        m_focusSystemTeamIndex = -1;

        RefreshFrameColor();
        ClearTail();
    }

    private void RefreshFrameColor()
    {
        if (m_spectatorFrameImage == null)
        {
            return;
        }

        if (m_focusSystemTeamIndex < 0)
        {
            m_spectatorFrameImage.color =
                m_noFocusFrameColor;

            return;
        }

        m_spectatorFrameImage.color =
            CharacterTeamColorConstants.GetTeamBaseColor(
                m_focusSystemTeamIndex);
    }

    private void RefreshTail()
    {
        if (!TryGetFocusWorldPosition(
                out Vector3 focusWorldPosition))
        {
            ClearTail();
            return;
        }

        if (m_battleMazeMapView == null
            || m_spectatorRootRectTransform == null
            || m_tailGraphic == null)
        {
            return;
        }

        Vector3 focusMapWorldPosition;

        if (!m_battleMazeMapView.TryGetWorldPositionOnMap(
                focusWorldPosition,
                out focusMapWorldPosition))
        {
            ClearTail();
            return;
        }

        Vector2 tipPosition;

        if (!TryConvertWorldToTailLocalPosition(
                focusMapWorldPosition,
                out tipPosition))
        {
            ClearTail();
            return;
        }

        Vector2 tipScreenPosition =
            RectTransformUtility.WorldToScreenPoint(
                m_uiCamera,
                focusMapWorldPosition);

        Vector2 spectatorLocalTipPosition;

        if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    m_spectatorRootRectTransform,
                    tipScreenPosition,
                    m_uiCamera,
                    out spectatorLocalTipPosition))
        {
            ClearTail();
            return;
        }

        Rect spectatorRect =
            m_spectatorRootRectTransform.rect;

        float spectatorCenterY =
            (spectatorRect.yMin + spectatorRect.yMax)
            * 0.5f;

        /*
         * 根元を観戦枠の縦中央へ寄せる。
         * これにより、ミニマップ上の対象位置へ向かう線が
         * 基本的に斜めになり、視線誘導としても自然になる。
         */
        float baseCenterY = Mathf.Lerp(
            spectatorLocalTipPosition.y,
            spectatorCenterY,
            m_tailRootCenterPull);

        /*
         * 注目位置が観戦枠の中央高さに近い場合でも、
         * 根元と先端が同じ高さにならないようにする。
         *
         * 対象が中央より下なら根元を上へ、
         * 対象が中央より上なら根元を下へ寄せる。
         * ちょうど中央の場合は、根元を少し上側へ置く。
         */
        float verticalDifference =
            baseCenterY - spectatorLocalTipPosition.y;

        if (Mathf.Abs(verticalDifference)
            < m_tailMinimumDiagonalOffset)
        {
            float directionToCenter =
                spectatorCenterY
                - spectatorLocalTipPosition.y;

            float offsetDirection =
                Mathf.Abs(directionToCenter) > 0.001f
                    ? Mathf.Sign(directionToCenter)
                    : 1.0f;

            baseCenterY =
                spectatorLocalTipPosition.y
                + offsetDirection
                * m_tailMinimumDiagonalOffset;
        }

        /*
         * 根元の三角形が観戦枠の上下からはみ出さないように制限する。
         */
        baseCenterY = Mathf.Clamp(
            baseCenterY,
            spectatorRect.yMin + m_tailBaseHalfHeight,
            spectatorRect.yMax - m_tailBaseHalfHeight);

        Vector3 baseTopWorldPosition =
            m_spectatorRootRectTransform.TransformPoint(
                new Vector3(
                    spectatorRect.xMin,
                    baseCenterY + m_tailBaseHalfHeight,
                    0.0f));

        Vector3 baseBottomWorldPosition =
            m_spectatorRootRectTransform.TransformPoint(
                new Vector3(
                    spectatorRect.xMin,
                    baseCenterY - m_tailBaseHalfHeight,
                    0.0f));

        Vector2 baseTopPosition;
        Vector2 baseBottomPosition;

        if (!TryConvertWorldToTailLocalPosition(
                baseTopWorldPosition,
                out baseTopPosition)
            || !TryConvertWorldToTailLocalPosition(
                baseBottomWorldPosition,
                out baseBottomPosition))
        {
            ClearTail();
            return;
        }

        Color tailColor =
            CharacterTeamColorConstants.GetTeamBaseColor(
                m_focusSystemTeamIndex);

        tailColor.a = m_tailAlpha;

        m_tailGraphic.SetTriangle(
            baseTopPosition,
            baseBottomPosition,
            tipPosition,
            tailColor);
    }



    private bool TryGetFocusWorldPosition(
        out Vector3 focusWorldPosition)
    {
        focusWorldPosition = Vector3.zero;

        if (m_focusCharacter != null
            && m_focusCharacter.gameObject.activeInHierarchy)
        {
            focusWorldPosition =
                m_focusCharacter.GetWorldPosition();

            return true;
        }

        if (m_focusTreasureChest != null
            && !m_focusTreasureChest.IsExported
            && m_focusTreasureChest.gameObject.activeInHierarchy)
        {
            focusWorldPosition =
                m_focusTreasureChest.transform.position;

            return true;
        }

        return false;
    }

    //private Vector2 GetMapLocalPosition(
    //    RectTransform tailRectTransform,
    //    Vector2 normalizedMapPosition)
    //{
    //    Rect tailRect = tailRectTransform.rect;

    //    return new Vector2(
    //        Mathf.Lerp(
    //            tailRect.xMin,
    //            tailRect.xMax,
    //            normalizedMapPosition.x),
    //        Mathf.Lerp(
    //            tailRect.yMin,
    //            tailRect.yMax,
    //            normalizedMapPosition.y));
    //}

    private bool TryConvertWorldToTailLocalPosition(
        Vector3 worldPosition,
        out Vector2 localPosition)
    {
        Vector2 screenPosition =
            RectTransformUtility.WorldToScreenPoint(
                m_uiCamera,
                worldPosition);

        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                m_tailGraphic.rectTransform,
                screenPosition,
                m_uiCamera,
                out localPosition);
    }

    private void ClearTail()
    {
        if (m_tailGraphic != null)
        {
            m_tailGraphic.ClearTriangle();
        }
    }
}