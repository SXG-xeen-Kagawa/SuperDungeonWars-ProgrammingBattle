using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleEndPresentation : MonoBehaviour
{
    public delegate bool IsProceedRequestedDelegate();

    public delegate void ProceedRequestedDelegate();


    [Header("UI")]
    [SerializeField]
    private CanvasGroup m_canvasGroup;

    [SerializeField]
    private RectTransform m_endContentRoot;

    [SerializeField]
    private Image m_endImage;

    [SerializeField]
    private TextMeshProUGUI m_endReasonText;

    [Header("Texts")]
    [SerializeField]
    private string m_timeLimitReachedText =
        "制限時間到達";

    [SerializeField]
    private string m_allTreasuresExportedText =
        "全財宝を搬出！";

    [Header("Show Animation")]
    [SerializeField]
    [Min(0.0f)]
    private float m_showFadeInDuration = 0.10f;

    [SerializeField]
    [Min(0.0f)]
    private float m_showEntryDuration = 0.18f;

    [SerializeField]
    [Min(0.0f)]
    private float m_showSettleDuration = 0.10f;

    [SerializeField]
    [Min(1.0f)]
    private float m_showInitialScale = 1.18f;

    [SerializeField]
    [Range(0.5f, 1.0f)]
    private float m_showImpactScale = 0.94f;

    [Header("Close Animation")]
    [SerializeField]
    [Min(0.0f)]
    private float m_fadeOutDuration = 0.25f;


    private IsProceedRequestedDelegate m_isProceedRequested;

    private ProceedRequestedDelegate m_proceedRequestedCallback;

    private Vector3 m_endContentBaseScale;

    private bool m_isWaitingForProceed;
    private bool m_isClosing;


    private void Awake()
    {
        if (m_canvasGroup == null)
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        // EndContentRoot を未設定にした場合でも、
        // 最低限 EndImage 自体をアニメーション対象にする。
        if (m_endContentRoot == null
            && m_endImage != null)
        {
            m_endContentRoot =
                m_endImage.rectTransform;
        }

        if (m_endContentRoot != null)
        {
            m_endContentBaseScale =
                m_endContentRoot.localScale;
        }
    }

    private void Update()
    {
        if (!m_isWaitingForProceed
            || m_isClosing
            || m_isProceedRequested == null)
        {
            return;
        }

        if (!m_isProceedRequested())
        {
            return;
        }

        StartCoroutine(CoCloseByProceed());
    }

    /// <summary>
    /// 試合終了UIを表示する。
    /// 表示アニメーション完了後、次画面へ進むキー入力を待つ。
    /// </summary>
    public void Show(
        BattleEndReason endReason,
        IsProceedRequestedDelegate isProceedRequested,
        ProceedRequestedDelegate proceedRequestedCallback)
    {
        gameObject.SetActive(true);

        StopAllCoroutines();

        m_isProceedRequested = isProceedRequested;
        m_proceedRequestedCallback = proceedRequestedCallback;

        m_isWaitingForProceed = false;
        m_isClosing = false;

        if (m_canvasGroup != null)
        {
            m_canvasGroup.alpha = 0.0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }

        if (m_endContentRoot != null)
        {
            m_endContentRoot.localScale =
                m_endContentBaseScale
                * m_showInitialScale;
        }

        if (m_endImage != null)
        {
            m_endImage.enabled = true;
        }

        if (m_endReasonText != null)
        {
            m_endReasonText.text =
                GetEndReasonText(endReason);

            m_endReasonText.gameObject.SetActive(true);
        }

        StartCoroutine(CoShowAndWaitForProceed());
    }

    public void Hide()
    {
        StopAllCoroutines();

        m_isWaitingForProceed = false;
        m_isClosing = false;

        m_isProceedRequested = null;
        m_proceedRequestedCallback = null;

        if (m_endContentRoot != null)
        {
            m_endContentRoot.localScale =
                m_endContentBaseScale;
        }

        gameObject.SetActive(false);
    }

    private IEnumerator CoShowAndWaitForProceed()
    {
        // 1. 少し大きく、薄く表示する。
        yield return CoFadeCanvasGroup(
            0.0f,
            1.0f,
            m_showFadeInDuration);

        // 2. 手前から飛び込むように標準より少し小さいサイズまで収束する。
        yield return CoScaleEndContent(
            m_endContentBaseScale
            * m_showInitialScale,
            m_endContentBaseScale
            * m_showImpactScale,
            m_showEntryDuration);

        // 3. 一度だけ「ドン」と着地して標準サイズに戻る。
        yield return CoScaleEndContent(
            m_endContentBaseScale
            * m_showImpactScale,
            m_endContentBaseScale,
            m_showSettleDuration);

        // アニメーションが完了してから実況者用の入力待ちを開始する。
        m_isWaitingForProceed = true;
    }

    private IEnumerator CoCloseByProceed()
    {
        m_isWaitingForProceed = false;
        m_isClosing = true;

        // 先にSceneFlowControllerへ進行許可を出す。
        // SceneFlowController側では、この通知を受けて
        // MazeThemeTransitionController による画面切替を開始する。
        ProceedRequestedDelegate callback =
            m_proceedRequestedCallback;

        if (callback != null)
        {
            callback();
        }

        yield return CoFadeCanvasGroup(
            1.0f,
            0.0f,
            m_fadeOutDuration);

        Hide();
    }

    private IEnumerator CoFadeCanvasGroup(
        float fromAlpha,
        float toAlpha,
        float duration)
    {
        if (m_canvasGroup == null)
        {
            yield break;
        }

        if (duration <= 0.0f)
        {
            m_canvasGroup.alpha = toAlpha;
            yield break;
        }

        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsedTime / duration);

            m_canvasGroup.alpha = Mathf.Lerp(
                fromAlpha,
                toAlpha,
                t);

            yield return null;
        }

        m_canvasGroup.alpha = toAlpha;
    }

    private IEnumerator CoScaleEndContent(
        Vector3 fromScale,
        Vector3 toScale,
        float duration)
    {
        if (m_endContentRoot == null)
        {
            yield break;
        }

        if (duration <= 0.0f)
        {
            m_endContentRoot.localScale = toScale;
            yield break;
        }

        float elapsedTime = 0.0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsedTime / duration);

            // 着地感を少し強めるため、補間は SmoothStep を使う。
            t = Mathf.SmoothStep(0.0f, 1.0f, t);

            m_endContentRoot.localScale = Vector3.Lerp(
                fromScale,
                toScale,
                t);

            yield return null;
        }

        m_endContentRoot.localScale = toScale;
    }

    private string GetEndReasonText(
        BattleEndReason endReason)
    {
        switch (endReason)
        {
            case BattleEndReason.AllTreasuresExported:
                return m_allTreasuresExportedText;

            case BattleEndReason.TimeLimitReached:
            default:
                return m_timeLimitReachedText;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        m_isWaitingForProceed = false;
        m_isClosing = false;

        m_isProceedRequested = null;
        m_proceedRequestedCallback = null;
    }
}