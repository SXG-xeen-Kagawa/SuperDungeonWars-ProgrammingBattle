using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// サウンドの共通再生機構。
/// 起動時に自動生成され、シーンをまたいで保持されます。
/// </summary>
[DisallowMultipleComponent]
public sealed class DungeonSoundManager : MonoBehaviour
{
    private const int m_seSourceCount = 8;
    private const int m_bgmSourceCount = 2;

    private const string m_settingsResourcePath = "DungeonSoundSettings";
    private const string m_catalogAddress = "Dungeon/SoundCatalog";

    private static DungeonSoundManager m_instance;

    private readonly Dictionary<SeId, SoundCatalog.SeEntry> m_seEntries =
        new Dictionary<SeId, SoundCatalog.SeEntry>();

    private readonly Dictionary<BgmId, SoundCatalog.BgmEntry> m_bgmEntries =
        new Dictionary<BgmId, SoundCatalog.BgmEntry>();

    private readonly List<AsyncOperationHandle> m_handles =
        new List<AsyncOperationHandle>();

    private readonly AudioSource[] m_seSources =
        new AudioSource[m_seSourceCount];

    private readonly float[] m_sePlayVolumes =
        new float[m_seSourceCount];

    // 時刻ではなく再生順を記録するため、同一フレーム内でも順序が決まります。
    private readonly ulong[] m_sePlayOrders =
        new ulong[m_seSourceCount];

    private ulong m_nextSePlayOrder;

    private readonly AudioSource[] m_bgmSources =
        new AudioSource[m_bgmSourceCount];

    private readonly float[] m_bgmClipVolumes =
        new float[m_bgmSourceCount];

    private readonly float[] m_bgmGains =
        new float[m_bgmSourceCount];

    private readonly float[] m_bgmFadeStartGains =
        new float[m_bgmSourceCount];

    private readonly float[] m_bgmFadeTargetGains =
        new float[m_bgmSourceCount];

    private bool m_initializationStarted;
    private bool m_isInitialized;
    private bool m_isAvailable;

    private float m_seVolume = 1f;
    private float m_bgmVolume = 0.5f;
    private float m_defaultCrossFadeSeconds = 1f;

    private BgmId m_currentBgmId = BgmId.None;
    private int m_currentBgmSlot = -1;

    private bool m_isFading;
    private float m_fadeElapsed;
    private float m_fadeDuration;

    private bool m_hasPendingBgm;
    private BgmId m_pendingBgmId;
    private SameBgmBehaviour m_pendingSameBgmBehaviour;
    private float m_pendingCrossFadeSeconds;

    private Camera m_targetCamera = null;

    // ActionやFuncを使用せず、明示的なdelegateを定義します。
    private delegate AsyncOperationHandle<T> OperationFactory<T>();

    public static DungeonSoundManager Instance => m_instance;

    public bool IsInitialized => m_isInitialized;
    public bool IsAvailable => m_isAvailable;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        m_instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateBeforeFirstSceneLoad()
    {
        if (m_instance != null)
        {
            return;
        }

        DungeonSoundManager existing =
            UnityEngine.Object.FindFirstObjectByType<DungeonSoundManager>(
                FindObjectsInactive.Include);

        if (existing != null)
        {
            m_instance = existing;
            DontDestroyOnLoad(existing.gameObject);
            existing.InitializeIfNeeded();
            return;
        }

        GameObject managerObject = new GameObject("DungeonSoundManager");
        managerObject.AddComponent<DungeonSoundManager>();
    }

    private void Awake()
    {
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        m_instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (m_initializationStarted)
        {
            return;
        }

        m_initializationStarted = true;

        CreateAudioSources();

        SoundVolumeSettings settings =
            Resources.Load<SoundVolumeSettings>(m_settingsResourcePath);

        // 公開環境では設定アセットを置かないため、
        // Addressablesに触れず無音で初期化完了とします。
        if (settings == null || !settings.EnableSound)
        {
            CompleteInitialization(false);
            return;
        }

        m_seVolume = SanitizeVolume(settings.SeVolume);
        m_bgmVolume = SanitizeVolume(settings.BgmVolume);

        float defaultSeconds = settings.DefaultCrossFadeSeconds;

        m_defaultCrossFadeSeconds =
            IsFinite(defaultSeconds) ? Mathf.Max(0f, defaultSeconds) : 1f;

        StartCoroutine(CoInitialize());
    }

    private void CreateAudioSources()
    {
        for (int i = 0; i < m_seSourceCount; i++)
        {
            m_seSources[i] = CreateAudioSource("SE_" + i, false);
        }

        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            m_bgmSources[i] = CreateAudioSource("BGM_" + i, true);
        }
    }

    private AudioSource CreateAudioSource(string objectName, bool loop)
    {
        GameObject sourceObject = new GameObject(objectName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = 0f;

        return source;
    }

    private IEnumerator CoInitialize()
    {
        if (!TryStartOperation<IResourceLocator>(
                () => Addressables.InitializeAsync(false),
                "Addressables初期化",
                out AsyncOperationHandle<IResourceLocator> initializeHandle))
        {
            CompleteInitialization(false);
            yield break;
        }

        yield return initializeHandle;

        if (initializeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            WarnOperationFailure("Addressables初期化", initializeHandle);
            CompleteInitialization(false);
            yield break;
        }

        if (!TryStartOperation<IList<IResourceLocation>>(
                () => Addressables.LoadResourceLocationsAsync(
                    m_catalogAddress,
                    typeof(SoundCatalog)),
                "サウンドカタログ検索",
                out AsyncOperationHandle<IList<IResourceLocation>> locationsHandle))
        {
            CompleteInitialization(false);
            yield break;
        }

        yield return locationsHandle;

        if (locationsHandle.Status != AsyncOperationStatus.Succeeded)
        {
            WarnOperationFailure("サウンドカタログ検索", locationsHandle);
            CompleteInitialization(false);
            yield break;
        }

        IList<IResourceLocation> locations = locationsHandle.Result;

        // カタログ未登録は正常な無音動作として扱います。
        if (locations == null || locations.Count == 0)
        {
            CompleteInitialization(false);
            yield break;
        }

        if (locations.Count != 1)
        {
            Debug.LogWarning(
                "[DungeonSound] サウンドカタログのAddressが重複しています。" +
                " Address: " + m_catalogAddress,
                this);

            CompleteInitialization(false);
            yield break;
        }

        IResourceLocation catalogLocation = locations[0];

        if (!TryStartOperation<SoundCatalog>(
                () => Addressables.LoadAssetAsync<SoundCatalog>(catalogLocation),
                "サウンドカタログロード",
                out AsyncOperationHandle<SoundCatalog> catalogHandle))
        {
            CompleteInitialization(false);
            yield break;
        }

        yield return catalogHandle;

        if (catalogHandle.Status != AsyncOperationStatus.Succeeded ||
            catalogHandle.Result == null)
        {
            WarnOperationFailure("サウンドカタログロード", catalogHandle);
            CompleteInitialization(false);
            yield break;
        }

        RegisterCatalog(catalogHandle.Result);

        CompleteInitialization(
            m_seEntries.Count > 0 || m_bgmEntries.Count > 0);
    }

    private bool TryStartOperation<T>(
        OperationFactory<T> factory,
        string operationName,
        out AsyncOperationHandle<T> handle)
    {
        handle = default;

        try
        {
            handle = factory();

            if (!handle.IsValid())
            {
                Debug.LogWarning(
                    "[DungeonSound] " + operationName +
                    "で無効なハンドルが返されました。",
                    this);

                return false;
            }

            m_handles.Add(handle);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[DungeonSound] " + operationName + "を開始できませんでした。\n" +
                exception,
                this);

            return false;
        }
    }

    private void WarnOperationFailure(
        string operationName,
        AsyncOperationHandle handle)
    {
        string detail = handle.IsValid() &&
                        handle.OperationException != null
            ? handle.OperationException.ToString()
            : "結果を取得できませんでした。";

        Debug.LogWarning(
            "[DungeonSound] " + operationName + "に失敗しました。\n" + detail,
            this);
    }

    private void RegisterCatalog(SoundCatalog catalog)
    {
        m_seEntries.Clear();
        m_bgmEntries.Clear();

        if (catalog.SeEntries != null)
        {
            foreach (SoundCatalog.SeEntry entry in catalog.SeEntries)
            {
                if (entry == null ||
                    entry.Id == SeId.None ||
                    entry.Clip == null)
                {
                    continue;
                }

                if (m_seEntries.ContainsKey(entry.Id))
                {
                    Debug.LogWarning(
                        "[DungeonSound] SEのIDが重複しています: " + entry.Id,
                        this);

                    continue;
                }

                m_seEntries.Add(entry.Id, entry);
            }
        }

        if (catalog.BgmEntries != null)
        {
            foreach (SoundCatalog.BgmEntry entry in catalog.BgmEntries)
            {
                if (entry == null ||
                    entry.Id == BgmId.None ||
                    entry.Clip == null)
                {
                    continue;
                }

                if (m_bgmEntries.ContainsKey(entry.Id))
                {
                    Debug.LogWarning(
                        "[DungeonSound] BGMのIDが重複しています: " + entry.Id,
                        this);

                    continue;
                }

                m_bgmEntries.Add(entry.Id, entry);
            }
        }
    }

    private void CompleteInitialization(bool available)
    {
        m_isAvailable = available;
        m_isInitialized = true;

        if (!available)
        {
            m_hasPendingBgm = false;

            m_seEntries.Clear();
            m_bgmEntries.Clear();

            ReleaseHandles();
            return;
        }

        ProcessPendingBgm();
    }

    /// <summary>
    /// SEを再生します。
    /// 8枠が埋まっている場合、最も古いSEを停止して置き換えます。
    /// 初期化完了前の要求は保持しません。
    /// </summary>
    public void PlaySe(SeId id, float volume = 1f)
    {
        if (!m_isAvailable ||
            id == SeId.None ||
            !m_seEntries.TryGetValue(id, out SoundCatalog.SeEntry entry))
        {
            return;
        }

        float playVolume =
            SanitizeVolume(entry.Volume) * SanitizeVolume(volume);

        // 無音の要求で、既存のSEを押し出さないようにします。
        if (playVolume <= 0f || m_seVolume <= 0f)
        {
            return;
        }

        int slot = FindSeSlot();

        if (slot < 0)
        {
            return;
        }

        AudioSource source = m_seSources[slot];

        // 空き枠でも使用中の枠でも、状態を揃えてから再生します。
        source.Stop();
        source.clip = entry.Clip;
        source.loop = false;

        m_sePlayVolumes[slot] = playVolume;
        m_sePlayOrders[slot] = ++m_nextSePlayOrder;

        source.volume = m_seVolume * playVolume;
        source.Play();
    }

    private int FindSeSlot()
    {
        // 空き枠を優先します。
        for (int i = 0; i < m_seSourceCount; i++)
        {
            AudioSource source = m_seSources[i];

            if (source != null && !source.isPlaying)
            {
                return i;
            }
        }

        // 全枠使用中なら、再生開始順が最も古い枠を選びます。
        int oldestSlot = -1;
        ulong oldestOrder = ulong.MaxValue;

        for (int i = 0; i < m_seSourceCount; i++)
        {
            if (m_seSources[i] == null)
            {
                continue;
            }

            if (oldestSlot < 0 || m_sePlayOrders[i] < oldestOrder)
            {
                oldestSlot = i;
                oldestOrder = m_sePlayOrders[i];
            }
        }

        return oldestSlot;
    }

    /// <summary>
    /// 発生位置が指定カメラの描画範囲内にある場合だけ再生します。
    /// 壁などによる遮蔽は判定しません。
    /// </summary>
    public void PlaySeIfVisible(
        SeId id,
        Vector3 worldPosition,
        Camera camera,
        float volume = 1f)
    {
        if (camera == null)
        {
            camera = m_targetCamera;
        }

        if (!m_isAvailable || !IsPositionVisible(camera, worldPosition))
        {
            return;
        }

        // 画面内判定を通過した要求だけが再生枠を使用します。
        PlaySe(id, volume);
    }

    public void SetTargetCamera(Camera camera)
    {
        m_targetCamera = camera;
    }



    private static bool IsPositionVisible(
        Camera camera,
        Vector3 worldPosition)
    {
        // MainCameraやミニマップカメラへ自動フォールバックしません。
        if (camera == null || !camera.isActiveAndEnabled)
        {
            return false;
        }

        Vector3 viewportPosition =
            camera.WorldToViewportPoint(worldPosition);

        if (!IsFinite(viewportPosition.x) ||
            !IsFinite(viewportPosition.y) ||
            !IsFinite(viewportPosition.z))
        {
            return false;
        }

        return viewportPosition.z > 0f &&
               viewportPosition.z >= camera.nearClipPlane &&
               viewportPosition.z <= camera.farClipPlane &&
               viewportPosition.x >= 0f &&
               viewportPosition.x <= 1f &&
               viewportPosition.y >= 0f &&
               viewportPosition.y <= 1f;
    }

    public void StopAllSe()
    {
        for (int i = 0; i < m_seSourceCount; i++)
        {
            AudioSource source = m_seSources[i];

            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.volume = 0f;
            }

            m_sePlayVolumes[i] = 0f;
            m_sePlayOrders[i] = 0;
        }

        m_nextSePlayOrder = 0;
    }

    public void SetSeVolume(float volume)
    {
        m_seVolume = SanitizeVolume(volume);

        for (int i = 0; i < m_seSourceCount; i++)
        {
            if (m_seSources[i] != null)
            {
                m_seSources[i].volume =
                    m_seVolume * m_sePlayVolumes[i];
            }
        }
    }

    public void SetBgmVolume(float volume)
    {
        m_bgmVolume = SanitizeVolume(volume);
        ApplyBgmVolumes();
    }

    public void PlayBgm(
        BgmId id,
        SameBgmBehaviour sameBgmBehaviour = SameBgmBehaviour.KeepPlaying,
        float crossFadeSeconds = -1f)
    {
        // Noneは停止命令ではありません。
        if (id == BgmId.None)
        {
            return;
        }

        // 初期化中は最後のBGM要求だけ保持します。
        if (!m_isInitialized)
        {
            StorePendingBgm(id, sameBgmBehaviour, crossFadeSeconds);
            return;
        }

        // 未登録BGMでは現在のBGMや保留要求を変更しません。
        if (!m_isAvailable || !m_bgmEntries.ContainsKey(id))
        {
            return;
        }

        // フェード中も最後の有効な要求だけ保持します。
        if (m_isFading)
        {
            StorePendingBgm(id, sameBgmBehaviour, crossFadeSeconds);
            return;
        }

        BeginBgm(id, sameBgmBehaviour, crossFadeSeconds);
    }

    private void StorePendingBgm(
        BgmId id,
        SameBgmBehaviour sameBgmBehaviour,
        float crossFadeSeconds)
    {
        m_hasPendingBgm = true;
        m_pendingBgmId = id;
        m_pendingSameBgmBehaviour = sameBgmBehaviour;
        m_pendingCrossFadeSeconds = crossFadeSeconds;
    }

    private void ProcessPendingBgm()
    {
        if (!m_isInitialized ||
            !m_isAvailable ||
            m_isFading ||
            !m_hasPendingBgm)
        {
            return;
        }

        BgmId id = m_pendingBgmId;
        SameBgmBehaviour behaviour = m_pendingSameBgmBehaviour;
        float seconds = m_pendingCrossFadeSeconds;

        m_hasPendingBgm = false;

        if (!m_bgmEntries.ContainsKey(id))
        {
            return;
        }

        BeginBgm(id, behaviour, seconds);
    }

    private void BeginBgm(
        BgmId id,
        SameBgmBehaviour sameBgmBehaviour,
        float crossFadeSeconds)
    {
        if (!m_bgmEntries.TryGetValue(id, out SoundCatalog.BgmEntry entry))
        {
            return;
        }

        bool sameBgmIsPlaying =
            m_currentBgmId == id &&
            m_currentBgmSlot >= 0 &&
            m_bgmSources[m_currentBgmSlot].isPlaying;

        if (sameBgmIsPlaying &&
            sameBgmBehaviour == SameBgmBehaviour.KeepPlaying)
        {
            return;
        }

        int nextSlot = m_currentBgmSlot == 0 ? 1 : 0;
        AudioSource nextSource = m_bgmSources[nextSlot];

        nextSource.Stop();
        nextSource.clip = entry.Clip;
        nextSource.loop = true;
        nextSource.time = 0f;

        m_bgmClipVolumes[nextSlot] = SanitizeVolume(entry.Volume);
        m_bgmGains[nextSlot] = 0f;

        nextSource.volume = 0f;
        nextSource.Play();

        m_currentBgmId = id;
        m_currentBgmSlot = nextSlot;

        BeginFade(
            nextSlot == 0 ? 1f : 0f,
            nextSlot == 1 ? 1f : 0f,
            ResolveFadeSeconds(crossFadeSeconds));
    }

    public void StopBgm(float fadeSeconds = -1f)
    {
        // 初期化中・切り替え中の保留要求も取り消します。
        m_hasPendingBgm = false;

        m_currentBgmId = BgmId.None;
        m_currentBgmSlot = -1;

        if (!m_isAvailable)
        {
            return;
        }

        // 切り替え中でも、両ソースの現在ゲインから停止へ向かいます。
        BeginFade(0f, 0f, ResolveFadeSeconds(fadeSeconds));
    }

    private void BeginFade(
        float targetGain0,
        float targetGain1,
        float seconds)
    {
        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            m_bgmFadeStartGains[i] = m_bgmGains[i];
        }

        m_bgmFadeTargetGains[0] = targetGain0;
        m_bgmFadeTargetGains[1] = targetGain1;

        m_fadeElapsed = 0f;
        m_fadeDuration = seconds;
        m_isFading = true;

        if (seconds <= 0f)
        {
            FinishFade();
        }
        else
        {
            ApplyBgmVolumes();
        }
    }

    private void Update()
    {
        if (!m_isFading)
        {
            return;
        }

        m_fadeElapsed += Time.unscaledDeltaTime;

        float progress = Mathf.Clamp01(m_fadeElapsed / m_fadeDuration);

        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            m_bgmGains[i] = Mathf.Lerp(
                m_bgmFadeStartGains[i],
                m_bgmFadeTargetGains[i],
                progress);
        }

        ApplyBgmVolumes();

        if (progress >= 1f)
        {
            FinishFade();
        }
    }

    private void FinishFade()
    {
        m_isFading = false;

        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            m_bgmGains[i] = m_bgmFadeTargetGains[i];

            if (m_bgmGains[i] <= 0f)
            {
                m_bgmSources[i].Stop();
                m_bgmSources[i].clip = null;
                m_bgmClipVolumes[i] = 0f;
            }
        }

        ApplyBgmVolumes();
        ProcessPendingBgm();
    }

    private void ApplyBgmVolumes()
    {
        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            if (m_bgmSources[i] != null)
            {
                m_bgmSources[i].volume =
                    m_bgmVolume *
                    m_bgmClipVolumes[i] *
                    m_bgmGains[i];
            }
        }
    }

    private float ResolveFadeSeconds(float seconds)
    {
        if (!IsFinite(seconds) || seconds < 0f)
        {
            return m_defaultCrossFadeSeconds;
        }

        return seconds;
    }

    private static float SanitizeVolume(float volume)
    {
        if (float.IsNaN(volume))
        {
            return 0f;
        }

        return Mathf.Clamp01(volume);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ReleaseHandles()
    {
        for (int i = m_handles.Count - 1; i >= 0; i--)
        {
            AsyncOperationHandle handle = m_handles[i];

            if (!handle.IsValid())
            {
                continue;
            }

            try
            {
                Addressables.Release(handle);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "[DungeonSound] ロードハンドルの解放に失敗しました。\n" +
                    exception,
                    this);
            }
        }

        m_handles.Clear();
    }

    private void OnDestroy()
    {
        // 重複インスタンスの破棄では、本体の状態に触れません。
        if (m_instance != this)
        {
            return;
        }

        StopAllCoroutines();
        StopAllSe();

        for (int i = 0; i < m_bgmSourceCount; i++)
        {
            AudioSource source = m_bgmSources[i];

            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }
        }

        m_isFading = false;
        m_hasPendingBgm = false;

        m_seEntries.Clear();
        m_bgmEntries.Clear();

        ReleaseHandles();

        m_instance = null;
    }
}