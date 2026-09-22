using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class BattlePrototypeSceneFlowController : MonoBehaviour
{
    const float DEBUG_HIGH_SPEED_TIME_SCALE = 5.0f;     // デバッグ用途：Shift+ArrowUpでTimeScaleを変更


    public enum SceneFlow
    {
        None,
        Initialize,
        TeamIntroduction,
        CountDown,
        Playing,
        BattleEndWait,
        Result,
        Finish,
    }

    [SerializeField]
    private BattlePrototypeGameController m_battleGameController;

    [SerializeField]
    private BattleInGameHud m_battleInGameHud;

    [SerializeField]
    private BattleResultPresentation m_battleResultPresentation;

    [SerializeField]
    private BattleTeamIntroductionPresentation m_battleTeamIntroductionPresentation;


    [Header("Count Down")]
    [SerializeField]
    private BattleStartCountdownPresentation
        m_battleStartCountdownPresentation;


    [Header("Battle End")]
    [SerializeField]
    private BattleEndPresentation m_battleEndPresentation;



    private bool m_isBattleStartCountdownCompleted;
    private bool m_isBattleStartCountdownInputAccepted;
    private bool m_isBattleEndPresentationProceedRequested;


    private SceneFlow m_sceneFlow = SceneFlow.None;
    private Coroutine m_sceneFlowCoroutine;

    private bool m_isDebugHighSpeedActive;
    private float m_timeScaleBeforeDebugHighSpeed = 1.0f;



    public SceneFlow CurrentSceneFlow
    {
        get { return m_sceneFlow; }
    }

    private void Awake()
    {
        if (m_battleGameController == null)
        {
            m_battleGameController =
                GetComponent<BattlePrototypeGameController>();
        }

        if (m_battleInGameHud == null)
        {
            m_battleInGameHud =
                FindFirstObjectByType<BattleInGameHud>();
        }

        if (m_battleGameController == null)
        {
            Debug.LogError(
                "BattlePrototypeSceneFlowController: "
                + "BattlePrototypeGameController が見つかりません。",
                this);
        }
    }

    private void OnEnable()
    {
        if (m_battleGameController != null)
        {
            m_battleGameController.BattleEnded += OnBattleEnded;
        }
    }

    private void OnDisable()
    {
        if (m_battleGameController != null)
        {
            m_battleGameController.BattleEnded -= OnBattleEnded;
        }

        RestoreTimeScaleByDebug();
    }

    private void Start()
    {
        HideBattleTeamIntroductionPresentation();
        HideBattleResultPresentation();
        HideBattleStartCountdownPresentation();
        HideBattleEndPresentation();

        ChangeSceneFlow(SceneFlow.Initialize);
    }

    private void OnBattleEnded(BattleResultData battleResult)
    {
        if (m_sceneFlow != SceneFlow.Playing)
        {
            return;
        }

        RestoreTimeScaleByDebug();

        ChangeSceneFlow(SceneFlow.BattleEndWait);
    }

    private void ChangeSceneFlow(SceneFlow nextSceneFlow)
    {
        if (m_sceneFlow == nextSceneFlow)
        {
            return;
        }

        if (m_sceneFlowCoroutine != null)
        {
            StopCoroutine(m_sceneFlowCoroutine);
            m_sceneFlowCoroutine = null;
        }

        m_sceneFlow = nextSceneFlow;

        Debug.Log("[BattleFlow] " + m_sceneFlow);

        switch (m_sceneFlow)
        {
            case SceneFlow.Initialize:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoSceneInitialize());
                break;

            case SceneFlow.TeamIntroduction:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoSceneTeamIntroduction());
                break;

            case SceneFlow.CountDown:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoSceneCountDown());
                break;

            case SceneFlow.Playing:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoScenePlaying());
                break;

            case SceneFlow.BattleEndWait:
                m_sceneFlowCoroutine =
                StartCoroutine(CoSceneBattleEndWait());
                break;

            case SceneFlow.Result:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoSceneResult());
                break;

            case SceneFlow.Finish:
                m_sceneFlowCoroutine =
                    StartCoroutine(CoSceneFinish());
                break;
        }
    }

    private IEnumerator CoSceneInitialize()
    {
        // 画面を覆う 
        MazeThemeTransitionController transitionController =
            MazeThemeTransitionController.Instance;
        if (transitionController != null)
        {
            transitionController.FadeOutImmediately();
        }

        // HUDは非表示にしておく
        SetBattleHudVisible(false);

        HideBattleTeamIntroductionPresentation();
        HideBattleResultPresentation();
        HideBattleStartCountdownPresentation();
        HideBattleEndPresentation();

        if (m_battleGameController == null)
        {
            yield break;
        }

        m_battleGameController.PrepareBattle();

        yield return null;

        ChangeSceneFlow(SceneFlow.TeamIntroduction);
    }

    private IEnumerator CoSceneTeamIntroduction()
    {
        // テーマ画像で覆われた状態の裏側で紹介UIを準備する。
        ShowBattleTeamIntroductionPresentation();

        // 1フレーム待たないと紹介UI表示の生成が重すぎて、MakeTransitionのアニメーションが見れない 
        yield return null;

        // 初回は内部で即時にテーマ画像を全面表示してから開始する。
        // 2試合目以降は、前試合のFadeOut状態からそのまま開始する。
        yield return CoRevealByMazeTransition();

        Debug.Log(
            "[BattleFlow] チーム紹介。"
            + " Enter / Space でカウントダウンへ進みます。");

        yield return CoWaitForProceedKey();

        // 紹介画面を表示したまま、テーマ画像で画面全体を覆う。
        yield return CoCoverByMazeTransition();

        // 完全に覆われた後なら、紹介UIを消しても観客には見えない。
        HideBattleTeamIntroductionPresentation();

        // 次フレームまで待ち、Hierarchy/GameObjectの無効化を反映する。
        yield return null;

        ChangeSceneFlow(SceneFlow.CountDown);
    }

    private IEnumerator CoSceneCountDown()
    {
        SetBattleHudVisible(true);

        // カウントダウンの待機画面を表示しておく 
        m_isBattleStartCountdownCompleted = false;
        m_isBattleStartCountdownInputAccepted = false;
        ShowBattleStartCountdownPresentation();

        yield return CoRevealByMazeTransition();

        // 待機 
        m_isBattleStartCountdownInputAccepted = true;
        while (!m_isBattleStartCountdownCompleted)
        {
            yield return null;
        }

        ChangeSceneFlow(SceneFlow.Playing);
    }

    private IEnumerator CoScenePlaying()
    {
        Debug.Log("[BattleFlow] 試合中。");

        while (m_sceneFlow == SceneFlow.Playing)
        {
            RefreshDebugTimeScale();

            yield return null;
        }
    }

    private IEnumerator CoSceneBattleEndWait()
    {
        m_isBattleEndPresentationProceedRequested = false;

        ShowBattleEndPresentation();

        Debug.Log(
            "[BattleFlow] 試合終了。"
            + " Enter / Space でリザルトへ進みます。");

        while (!m_isBattleEndPresentationProceedRequested)
        {
            yield return null;
        }

        ChangeSceneFlow(SceneFlow.Result);
    }


    private IEnumerator CoSceneResult()
    {
        // ゲーム画面 → リザルト画面をテーマ画像で覆う。
        yield return CoCoverByMazeTransition();

        SetBattleHudVisible(false);
        HideBattleEndPresentation();

        BattleResultData battleResult =
            m_battleGameController != null
                ? m_battleGameController.BattleResult
                : null;

        ShowBattleResultPresentation(battleResult);

        Debug.Log(
            "[BattleFlow] リザルト表示。"
            + " Enter / Space で精算演出を開始します。");

        if (battleResult != null)
        {
            Debug.Log(
                "[BattleFlow] 結果チーム数="
                + battleResult.TeamResults.Count);
        }

        yield return CoRevealByMazeTransition();

        // リザルト画面を見せ、観客の視線が集まってから精算を始める。
        yield return CoWaitForProceedKey();

        if (m_battleResultPresentation != null)
        {
            m_battleResultPresentation
                .StartResultSequence(
                    battleResult);
        }

        Debug.Log(
            "[BattleFlow] リザルト精算を開始しました。");

        yield return CoWaitForResultSequence();

        Debug.Log(
            "[BattleFlow] 精算完了。"
            + " Enter / Space でFinishへ進みます。");

        yield return CoWaitForProceedKey();

        ChangeSceneFlow(SceneFlow.Finish);
    }

    private IEnumerator CoWaitForResultSequence()
    {
        if (m_battleResultPresentation == null)
        {
            yield break;
        }

        while (!m_battleResultPresentation
                   .IsResultSequenceCompleted)
        {
            yield return null;
        }
    }


    private IEnumerator CoSceneFinish()
    {
        Debug.Log("[BattleFlow] Finish。次の試合を開始します。");

        // リザルトを完全に隠してから、同じシーンを再ロードする。
        yield return CoCoverByMazeTransition();

        Scene activeScene = SceneManager.GetActiveScene();

        SceneManager.LoadSceneAsync(activeScene.buildIndex);
    }


    private void SetBattleHudVisible(bool isVisible)
    {
        if (m_battleInGameHud != null)
        {
            m_battleInGameHud.SetPresentationVisible(
                isVisible);
        }
    }



    private void HideBattleTeamIntroductionPresentation()
    {
        if (m_battleTeamIntroductionPresentation != null)
        {
            m_battleTeamIntroductionPresentation.Hide();
        }
    }

    private void ShowBattleTeamIntroductionPresentation()
    {
        if (m_battleTeamIntroductionPresentation == null)
        {
            return;
        }

        // Hierarchy 上では非アクティブにしておくため、
        // 呼び出し時に先に有効化する。
        m_battleTeamIntroductionPresentation.gameObject.SetActive(
            true);

        m_battleTeamIntroductionPresentation.Show(
            m_battleGameController != null
                ? m_battleGameController.SpawnedParties
                : null);
    }




    private void HideBattleResultPresentation()
    {
        if (m_battleResultPresentation != null)
        {
            m_battleResultPresentation.Hide();
        }
    }

    private void ShowBattleResultPresentation(
        BattleResultData battleResult)
    {
        if (m_battleResultPresentation == null)
        {
            return;
        }

        // 非アクティブな状態で Hierarchy に置かれているため、リザルト開始時に先に明示的に有効化する。
        m_battleResultPresentation.gameObject.SetActive(true);

        m_battleResultPresentation.Show(
            battleResult,
            m_battleGameController != null
                ? m_battleGameController.SpawnedParties
                : null);
    }


    private void RefreshDebugTimeScale()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            RestoreTimeScaleByDebug();
            return;
        }

        bool isShiftPressed =
            keyboard.leftShiftKey.isPressed
            || keyboard.rightShiftKey.isPressed;

        bool isFastForwardPressed =
            isShiftPressed
            && keyboard.upArrowKey.isPressed;

        if (!isFastForwardPressed)
        {
            RestoreTimeScaleByDebug();
            return;
        }

        if (!m_isDebugHighSpeedActive)
        {
            m_timeScaleBeforeDebugHighSpeed =
                Time.timeScale;

            m_isDebugHighSpeedActive = true;
        }

        Time.timeScale =
            DEBUG_HIGH_SPEED_TIME_SCALE;
    }

    private void RestoreTimeScaleByDebug()
    {
        if (!m_isDebugHighSpeedActive)
        {
            return;
        }

        Time.timeScale =
            m_timeScaleBeforeDebugHighSpeed;

        m_isDebugHighSpeedActive = false;
    }




    #region 試合開始前カウントダウン 

    private void ShowBattleStartCountdownPresentation()
    {
        if (m_battleStartCountdownPresentation == null)
        {
            Debug.LogError(
                "BattlePrototypeSceneFlowController: "
                + "BattleStartCountdownPresentation が未設定です。",
                this);

            OnBattleStartCountdownCompleted();

            return;
        }

        m_battleStartCountdownPresentation.Show(
            ()=>{
                if (!m_isBattleStartCountdownInputAccepted)
                {
                    return false;
                }
                return WasPressedProceedKey();
            },
            OnBattleStartCountdownCompleted);
    }

    private void HideBattleStartCountdownPresentation()
    {
        if (m_battleStartCountdownPresentation != null)
        {
            m_battleStartCountdownPresentation.Hide();
        }
    }

    private void OnBattleStartCountdownCompleted()
    {
        if (m_sceneFlow != SceneFlow.CountDown)
        {
            return;
        }

        if (m_battleGameController != null)
        {
            m_battleGameController.StartBattle();
        }

        m_isBattleStartCountdownCompleted = true;
    }

    #endregion




    #region 試合終了宣言 

    private void ShowBattleEndPresentation()
    {
        if (m_battleEndPresentation == null)
        {
            Debug.LogError(
                "BattlePrototypeSceneFlowController: "
                + "BattleEndPresentation が未設定です。",
                this);

            m_isBattleEndPresentationProceedRequested = true;
            return;
        }

        BattleEndReason endReason =
            m_battleGameController != null
                ? m_battleGameController.LastBattleEndReason
                : BattleEndReason.TimeLimitReached;

        m_battleEndPresentation.Show(
            endReason,
            WasPressedProceedKey,
            OnBattleEndPresentationProceedRequested);
    }

    private void HideBattleEndPresentation()
    {
        if (m_battleEndPresentation != null)
        {
            m_battleEndPresentation.Hide();
        }
    }

    private void OnBattleEndPresentationProceedRequested()
    {
        if (m_sceneFlow != SceneFlow.BattleEndWait)
        {
            return;
        }

        m_isBattleEndPresentationProceedRequested = true;
    }

    #endregion


    private IEnumerator CoCoverByMazeTransition()
    {
        MazeThemeTransitionController transitionController =
            MazeThemeTransitionController.Instance;

        if (transitionController == null)
        {
            yield break;
        }

        bool isCompleted = false;

        transitionController.FadeOut(delegate
        {
            isCompleted = true;
        });

        while (!isCompleted)
        {
            yield return null;
        }
    }

    private IEnumerator CoRevealByMazeTransition()
    {
        MazeThemeTransitionController transitionController =
            MazeThemeTransitionController.Instance;

        if (transitionController == null)
        {
            yield break;
        }

        bool isCompleted = false;

        transitionController.FadeIn(delegate
        {
            isCompleted = true;
        });

        while (!isCompleted)
        {
            yield return null;
        }
    }


    private IEnumerator CoWaitForProceedKey()
    {
        yield return null;

        while (!WasPressedProceedKey())
        {
            yield return null;
        }
    }

    private bool WasPressedProceedKey()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.enterKey.wasPressedThisFrame
            || keyboard.numpadEnterKey.wasPressedThisFrame
            || keyboard.spaceKey.wasPressedThisFrame;
    }
}