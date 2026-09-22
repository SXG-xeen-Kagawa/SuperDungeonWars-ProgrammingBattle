using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleStartCountdownPresentation : MonoBehaviour
{
    public delegate bool IsCountdownStartRequestedDelegate();

    public delegate void CountdownCompletedDelegate();


    [Header("UI")]
    [SerializeField] private Image m_dimBackgroundImage;
    [SerializeField] private Image m_countdownImage;
    [SerializeField] private TextMeshProUGUI m_messageText;

    [Header("Countdown Sprites")]
    [SerializeField] private Sprite m_countdown3Sprite;
    [SerializeField] private Sprite m_countdown2Sprite;
    [SerializeField] private Sprite m_countdown1Sprite;
    [SerializeField] private Sprite m_explorationStartSprite;

    [Header("Message")]
    [SerializeField]
    [TextArea]
    private string m_waitingMessage =
        "財宝を持ち帰り、勝利をつかめ！";

    [Header("Timings")]
    [SerializeField]
    [Min(0.0f)]
    private float m_countdownDisplayDuration = 0.8f;

    [SerializeField]
    [Min(0.0f)]
    private float m_countdownEntryDuration = 0.18f;

    [SerializeField]
    [Min(0.0f)]
    private float m_explorationStartDisplayDuration = 0.5f;

    [SerializeField]
    [Min(0.0f)]
    private float m_fadeOutDuration = 0.4f;

    [Header("Animation")]
    [SerializeField]
    [Min(1.0f)]
    private float m_countdownInitialScale = 1.25f;


    private IsCountdownStartRequestedDelegate
        m_isCountdownStartRequested;

    private CountdownCompletedDelegate
        m_countdownCompletedCallback;

    private Color m_dimBackgroundBaseColor;
    private Vector3 m_countdownImageBaseScale;

    private bool m_isWaitingForInput;
    private bool m_isCountdownRunning;


    private void Awake()
    {
        if (m_dimBackgroundImage != null)
        {
            m_dimBackgroundBaseColor =
                m_dimBackgroundImage.color;
        }

        if (m_countdownImage != null)
        {
            m_countdownImageBaseScale =
                m_countdownImage.rectTransform.localScale;

            m_countdownImage.preserveAspect = true;
        }
    }

    private void Update()
    {
        if (!m_isWaitingForInput
            || m_isCountdownRunning
            || m_isCountdownStartRequested == null)
        {
            return;
        }

        if (!m_isCountdownStartRequested())
        {
            return;
        }

        StartCoroutine(CoPlayCountdown());
    }

    /// <summary>
    /// 待機メッセージを表示し、開始キー入力を待つ。
    /// </summary>
    public void Show(
        IsCountdownStartRequestedDelegate isCountdownStartRequested,
        CountdownCompletedDelegate countdownCompletedCallback)
    {
        gameObject.SetActive(true);

        StopAllCoroutines();

        m_isCountdownStartRequested =
            isCountdownStartRequested;

        m_countdownCompletedCallback =
            countdownCompletedCallback;

        m_isWaitingForInput = true;
        m_isCountdownRunning = false;

        if (m_dimBackgroundImage != null)
        {
            m_dimBackgroundImage.color =
                m_dimBackgroundBaseColor;
        }

        if (m_countdownImage != null)
        {
            m_countdownImage.enabled = false;
            m_countdownImage.sprite = null;
            m_countdownImage.color = Color.white;

            m_countdownImage.rectTransform.localScale =
                m_countdownImageBaseScale;
        }

        if (m_messageText != null)
        {
            m_messageText.gameObject.SetActive(true);
            m_messageText.text = m_waitingMessage;
        }
    }

    public void Hide()
    {
        StopAllCoroutines();

        m_isWaitingForInput = false;
        m_isCountdownRunning = false;

        m_isCountdownStartRequested = null;
        m_countdownCompletedCallback = null;

        gameObject.SetActive(false);
    }

    private IEnumerator CoPlayCountdown()
    {
        m_isWaitingForInput = false;
        m_isCountdownRunning = true;

        if (m_messageText != null)
        {
            m_messageText.gameObject.SetActive(false);
        }

        if (m_countdownImage != null)
        {
            m_countdownImage.enabled = true;
        }

        yield return CoShowCountdownSprite(
            m_countdown3Sprite);

        yield return CoShowCountdownSprite(
            m_countdown2Sprite);

        yield return CoShowCountdownSprite(
            m_countdown1Sprite);

        ShowSpriteImmediately(m_explorationStartSprite);
        m_countdownImage.transform.localScale = Vector3.one * 2.0f;

        // 「探索開始！」を表示した瞬間に、試合開始を通知する。
        CountdownCompletedDelegate callback =
            m_countdownCompletedCallback;

        if (callback != null)
        {
            callback();
        }

        yield return new WaitForSecondsRealtime(
            m_explorationStartDisplayDuration);

        yield return CoFadeOutAndHide();
    }

    private IEnumerator CoShowCountdownSprite(Sprite sprite)
    {
        if (m_countdownImage == null)
        {
            yield break;
        }

        m_countdownImage.sprite = sprite;
        m_countdownImage.color =
            new Color(1.0f, 1.0f, 1.0f, 0.0f);

        m_countdownImage.rectTransform.localScale =
            m_countdownImageBaseScale
            * m_countdownInitialScale;

        float elapsedTime = 0.0f;

        while (elapsedTime < m_countdownDisplayDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float entryT = Mathf.Clamp01(
                elapsedTime / m_countdownEntryDuration);

            m_countdownImage.color =
                new Color(1.0f, 1.0f, 1.0f, entryT);

            m_countdownImage.rectTransform.localScale =
                Vector3.Lerp(
                    m_countdownImageBaseScale
                    * m_countdownInitialScale,
                    m_countdownImageBaseScale,
                    entryT);

            yield return null;
        }

        m_countdownImage.color = Color.white;

        m_countdownImage.rectTransform.localScale =
            m_countdownImageBaseScale;
    }

    private void ShowSpriteImmediately(Sprite sprite)
    {
        if (m_countdownImage == null)
        {
            return;
        }

        m_countdownImage.sprite = sprite;
        m_countdownImage.color = Color.white;

        m_countdownImage.rectTransform.localScale =
            m_countdownImageBaseScale;
    }

    private IEnumerator CoFadeOutAndHide()
    {
        float elapsedTime = 0.0f;

        Color countdownColor =
            m_countdownImage != null
                ? m_countdownImage.color
                : Color.white;

        Color backgroundColor =
            m_dimBackgroundImage != null
                ? m_dimBackgroundImage.color
                : Color.clear;

        while (elapsedTime < m_fadeOutDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsedTime / m_fadeOutDuration);

            if (m_countdownImage != null)
            {
                m_countdownImage.color =
                    new Color(
                        countdownColor.r,
                        countdownColor.g,
                        countdownColor.b,
                        Mathf.Lerp(
                            countdownColor.a,
                            0.0f,
                            t));
            }

            if (m_dimBackgroundImage != null)
            {
                m_dimBackgroundImage.color =
                    new Color(
                        backgroundColor.r,
                        backgroundColor.g,
                        backgroundColor.b,
                        Mathf.Lerp(
                            backgroundColor.a,
                            0.0f,
                            t));
            }

            yield return null;
        }

        Hide();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        m_isWaitingForInput = false;
        m_isCountdownRunning = false;

        m_isCountdownStartRequested = null;
        m_countdownCompletedCallback = null;
    }
}