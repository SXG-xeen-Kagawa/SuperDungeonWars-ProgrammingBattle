using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MazeThemeTransitionController : MonoBehaviour
{
    public delegate void FadeCompletedDelegate();

    private const string RESOURCE_PREFAB_PATH = "MazeThemeTransitionController";

    private static MazeThemeTransitionController s_instance;
    private static bool s_isCreatingInstance;

    public static MazeThemeTransitionController Instance
    {
        get
        {
            EnsureInstance();
            return s_instance;
        }
    }

    [Header("References")]
    [SerializeField]
    private RawImage m_themeRawImage;

    [SerializeField]
    private Texture m_themeTexture;

    [SerializeField]
    private Shader m_transitionShader;

    [Header("Transition")]
    [SerializeField]
    [Min(0.01f)]
    private float m_transitionDuration = 0.5f;

    [SerializeField]
    [Min(0.0f)]
    private float m_fadeOutHoldDuration = 0.8f;

    [SerializeField]
    [Min(16)]
    private int m_cellSizePixels = 96;

    [SerializeField]
    private int m_randomSeed = 12345;


    private Material m_material;
    private Texture2D m_maskTexture;
    private Color[] m_maskPixels;

    private readonly List<Vector2Int> m_revealOrder = new List<Vector2Int>();
    private readonly List<Vector2Int> m_frontierCells = new List<Vector2Int>();

    private Coroutine m_transitionCoroutine;

    private int m_gridWidth;
    private int m_gridHeight;
    private int m_appliedCellCount;

    public bool IsPlaying
    {
        get { return m_transitionCoroutine != null; }
    }

    public bool IsThemeVisible
    {
        get
        {
            return
                m_themeRawImage != null &&
                m_themeRawImage.gameObject.activeSelf;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        s_instance = null;
        s_isCreatingInstance = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstSceneLoad()
    {
        EnsureInstance();
    }

    /// <summary>
    /// テーマ画像が画面を覆った状態から、中央を起点に消していきます。
    /// 最終的に通常の画面を見せます。
    /// テーマ画像がまだ表示されていない場合も、まず即時に全面表示してから開始します。
    /// </summary>
    public void FadeIn(FadeCompletedDelegate onCompleted = null)
    {
        StartTransition(false, onCompleted);
    }

    /// <summary>
    /// テーマ画像を中央から探索するように表示し、
    /// 最終的に画面を覆います。
    /// </summary>
    public void FadeOut(FadeCompletedDelegate onCompleted = null)
    {
        StartTransition(true, onCompleted);
    }

    /// <summary>
    /// 即座にフェードアウト状態にします。フェードアウト中の演出があれば停止します。
    /// </summary>
    public void FadeOutImmediately()
    {
        if (m_transitionCoroutine != null)
        {
            StopCoroutine(m_transitionCoroutine);
            m_transitionCoroutine = null;
        }

        ShowImmediately();
    }


    /// <summary>
    /// 画面をテーマ画像で覆ってからシーンを読み込み、読込後にテーマ画像を消します。
    /// </summary>
    public void TransitionToScene(string sceneName)
    {
        StartCoroutine(TransitionToSceneCoroutine(sceneName));
    }

    /// <summary>
    /// 演出なしで、テーマ画像を画面全体に表示します。
    /// </summary>
    public void ShowImmediately()
    {
        StopCurrentTransition();

        PrepareGrid();
        FillMask(1.0f);

        m_themeRawImage.gameObject.SetActive(true);
    }

    /// <summary>
    /// 演出なしで、テーマ画像を非表示にします。
    /// </summary>
    public void HideImmediately()
    {
        StopCurrentTransition();

        if (m_themeRawImage != null)
        {
            m_themeRawImage.gameObject.SetActive(false);
        }
    }

    private static void EnsureInstance()
    {
        if (s_instance != null || s_isCreatingInstance)
        {
            return;
        }

        s_instance = FindFirstObjectByType<MazeThemeTransitionController>();

        if (s_instance != null)
        {
            return;
        }

        s_isCreatingInstance = true;

        try
        {
            MazeThemeTransitionController prefab =
                Resources.Load<MazeThemeTransitionController>(
                    RESOURCE_PREFAB_PATH);

            if (prefab == null)
            {
                Debug.LogError(
                    "Resources/MazeThemeTransitionController.prefab が見つかりません。"
                    + " フェード演出用Prefabを Resources 配下に配置してください。");

                return;
            }

            s_instance = Instantiate(prefab);
            s_instance.name = "[MazeThemeTransitionController]";
        }
        finally
        {
            s_isCreatingInstance = false;
        }
    }

    private void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;

        DontDestroyOnLoad(gameObject);

        ValidateReferences();
        CreateMaterial();

        HideImmediately();
    }

    private void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }

        if (m_material != null)
        {
            Destroy(m_material);
            m_material = null;
        }

        if (m_maskTexture != null)
        {
            Destroy(m_maskTexture);
            m_maskTexture = null;
        }
    }

    private void ValidateReferences()
    {
        if (m_themeRawImage == null)
        {
            Debug.LogError(
                "Theme Raw Image が未設定です。"
                + " MazeThemeTransitionController PrefabのInspectorで設定してください。",
                this);
        }

        if (m_themeTexture == null)
        {
            Debug.LogError(
                "Theme Texture が未設定です。"
                + " MazeThemeTransitionController PrefabのInspectorで設定してください。",
                this);
        }

        if (m_themeRawImage != null)
        {
            m_themeRawImage.texture = m_themeTexture;
            m_themeRawImage.raycastTarget = false;
        }
    }

    private void CreateMaterial()
    {
        if (m_themeRawImage == null)
        {
            return;
        }

        if (m_transitionShader == null)
        {
            m_transitionShader = Shader.Find("UI/MazeThemeTransition");
        }

        if (m_transitionShader == null)
        {
            Debug.LogError(
                "UI/MazeThemeTransition Shader が見つかりません。",
                this);

            return;
        }

        m_material = new Material(m_transitionShader);
        m_material.name = "MazeThemeTransitionMaterial";

        m_themeRawImage.material = m_material;
    }

    private void StartTransition(
        bool isFadeOut,
        FadeCompletedDelegate onCompleted)
    {
        if (m_themeRawImage == null || m_material == null)
        {
            onCompleted?.Invoke();
            return;
        }

        StopCurrentTransition();

        PrepareGrid();

        m_themeRawImage.gameObject.SetActive(true);

        if (isFadeOut)
        {
            // 通常画面から、テーマ画像で覆い始める。
            FillMask(0.0f);
        }
        else
        {
            // 初回を含め、全面を覆った状態から通常画面を見せ始める。
            FillMask(1.0f);
        }

        m_transitionCoroutine = StartCoroutine(
            PlayTransitionCoroutine(isFadeOut, onCompleted));
    }

    private IEnumerator PlayTransitionCoroutine(
        bool isFadeOut,
        FadeCompletedDelegate onCompleted)
    {
        float elapsedTime = 0.0f;
        int totalCellCount = m_revealOrder.Count;

        while (elapsedTime < m_transitionDuration)
        {
            float unscaled = Time.unscaledDeltaTime;

            elapsedTime += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / m_transitionDuration);

            int targetCellCount = Mathf.FloorToInt(
                totalCellCount * progress);

            ApplyCellsUntil(targetCellCount, isFadeOut);

            //Debug.Log("[Transition] unscaledTime=" + unscaled
            //    + ", elapsedTime=" + elapsedTime
            //    + ", progress=" + progress
            //    + ", targetCellCount=" + targetCellCount
            //    + ", appliedCellCount=" + m_appliedCellCount);

            yield return null;
        }

        ApplyCellsUntil(totalCellCount, isFadeOut);

        // 通常画面を完全に隠した後、覆われた状態を維持する。
        if (isFadeOut && m_fadeOutHoldDuration > 0.0f)
        {
            yield return new WaitForSecondsRealtime(
                m_fadeOutHoldDuration);
        }

        // 通常画面を見せ終えたら、テーマ画像を非表示にする。
        if (!isFadeOut)
        {
            m_themeRawImage.gameObject.SetActive(false);
        }

        m_transitionCoroutine = null;

        onCompleted?.Invoke();
    }

    private IEnumerator TransitionToSceneCoroutine(string sceneName)
    {
        bool isFadeOutCompleted = false;

        FadeOut(delegate
        {
            isFadeOutCompleted = true;
        });

        while (!isFadeOutCompleted)
        {
            yield return null;
        }

        AsyncOperation loadOperation =
            SceneManager.LoadSceneAsync(sceneName);

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        FadeIn();
    }

    private void StopCurrentTransition()
    {
        if (m_transitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(m_transitionCoroutine);
        m_transitionCoroutine = null;
    }

    private void PrepareGrid()
    {
        int screenWidth = Mathf.Max(Screen.width, 1);
        int screenHeight = Mathf.Max(Screen.height, 1);

        int gridWidth = Mathf.Max(
            1,
            Mathf.CeilToInt(
                (float)screenWidth / m_cellSizePixels));

        int gridHeight = Mathf.Max(
            1,
            Mathf.CeilToInt(
                (float)screenHeight / m_cellSizePixels));

        bool needsRebuild =
            m_maskTexture == null ||
            m_gridWidth != gridWidth ||
            m_gridHeight != gridHeight;

        if (!needsRebuild)
        {
            BuildRevealOrder();
            return;
        }

        m_gridWidth = gridWidth;
        m_gridHeight = gridHeight;

        if (m_maskTexture != null)
        {
            Destroy(m_maskTexture);
        }

        m_maskTexture = new Texture2D(
            m_gridWidth,
            m_gridHeight,
            TextureFormat.RGBA32,
            false,
            true);

        m_maskTexture.name = "MazeThemeTransitionMask";
        m_maskTexture.filterMode = FilterMode.Point;
        m_maskTexture.wrapMode = TextureWrapMode.Clamp;

        m_maskPixels = new Color[m_gridWidth * m_gridHeight];

        m_material.SetTexture("_MaskTex", m_maskTexture);

        BuildRevealOrder();
    }

    private void BuildRevealOrder()
    {
        m_revealOrder.Clear();
        m_frontierCells.Clear();

        bool[,] visitedCells =
            new bool[m_gridWidth, m_gridHeight];

        bool[,] queuedCells =
            new bool[m_gridWidth, m_gridHeight];

        System.Random random = new System.Random(
            m_randomSeed +
            m_gridWidth * 1000 +
            m_gridHeight);

        Vector2Int centerCell = new Vector2Int(
            m_gridWidth / 2,
            m_gridHeight / 2);

        AddVisitedCell(
            centerCell,
            visitedCells,
            queuedCells);

        while (m_frontierCells.Count > 0)
        {
            int randomIndex = random.Next(m_frontierCells.Count);

            Vector2Int nextCell = m_frontierCells[randomIndex];

            m_frontierCells.RemoveAt(randomIndex);

            if (visitedCells[nextCell.x, nextCell.y])
            {
                continue;
            }

            AddVisitedCell(
                nextCell,
                visitedCells,
                queuedCells);
        }
    }

    private void AddVisitedCell(
        Vector2Int cell,
        bool[,] visitedCells,
        bool[,] queuedCells)
    {
        if (!IsValidCell(cell) || visitedCells[cell.x, cell.y])
        {
            return;
        }

        visitedCells[cell.x, cell.y] = true;
        m_revealOrder.Add(cell);

        TryAddFrontierCell(
            new Vector2Int(cell.x + 1, cell.y),
            visitedCells,
            queuedCells);

        TryAddFrontierCell(
            new Vector2Int(cell.x - 1, cell.y),
            visitedCells,
            queuedCells);

        TryAddFrontierCell(
            new Vector2Int(cell.x, cell.y + 1),
            visitedCells,
            queuedCells);

        TryAddFrontierCell(
            new Vector2Int(cell.x, cell.y - 1),
            visitedCells,
            queuedCells);
    }

    private void TryAddFrontierCell(
        Vector2Int cell,
        bool[,] visitedCells,
        bool[,] queuedCells)
    {
        if (!IsValidCell(cell))
        {
            return;
        }

        if (visitedCells[cell.x, cell.y] || queuedCells[cell.x, cell.y])
        {
            return;
        }

        queuedCells[cell.x, cell.y] = true;
        m_frontierCells.Add(cell);
    }

    private bool IsValidCell(Vector2Int cell)
    {
        return
            cell.x >= 0 &&
            cell.x < m_gridWidth &&
            cell.y >= 0 &&
            cell.y < m_gridHeight;
    }

    private void FillMask(float value)
    {
        Color maskColor = new Color(value, value, value, value);

        for (int index = 0; index < m_maskPixels.Length; index++)
        {
            m_maskPixels[index] = maskColor;
        }

        m_maskTexture.SetPixels(m_maskPixels);
        m_maskTexture.Apply(false, false);

        m_appliedCellCount = 0;
    }

    private void ApplyCellsUntil(
        int targetCellCount,
        bool isFadeOut)
    {
        targetCellCount = Mathf.Clamp(
            targetCellCount,
            0,
            m_revealOrder.Count);

        if (targetCellCount <= m_appliedCellCount)
        {
            return;
        }

        float maskValue = isFadeOut ? 1.0f : 0.0f;

        Color maskColor = new Color(
            maskValue,
            maskValue,
            maskValue,
            maskValue);

        for (
            int index = m_appliedCellCount;
            index < targetCellCount;
            index++)
        {
            Vector2Int cell = m_revealOrder[index];

            int pixelIndex =
                cell.y * m_gridWidth +
                cell.x;

            m_maskPixels[pixelIndex] = maskColor;
        }

        m_maskTexture.SetPixels(m_maskPixels);
        m_maskTexture.Apply(false, false);

        m_appliedCellCount = targetCellCount;
    }

}