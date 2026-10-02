using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

public sealed class DungeonParticleManager : MonoBehaviour
{
    private const string m_settingsPath = "DungeonParticleSettings";
    private const string m_catalogAddress = "Dungeon/ParticleCatalog";

    private static DungeonParticleManager m_instance;

    private readonly Dictionary<ParticleId, ParticleCatalog.Entry>
        m_entries =
            new Dictionary<ParticleId, ParticleCatalog.Entry>();

    private readonly List<DungeonParticleInstance> m_instances =
        new List<DungeonParticleInstance>();

    private AsyncOperationHandle<IResourceLocator> m_initializeHandle;
    private AsyncOperationHandle<IList<IResourceLocation>> m_locationsHandle;
    private AsyncOperationHandle<ParticleCatalog> m_catalogHandle;

    private int m_maxInstances = 64;
    private bool m_isInitialized;
    private bool m_isAvailable;

    public static DungeonParticleManager Instance => m_instance;
    public bool IsInitialized => m_isInitialized;
    public bool IsAvailable => m_isAvailable;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        m_instance = null;
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (m_instance != null)
        {
            return;
        }

        var managerObject = new GameObject("DungeonParticleManager");
        managerObject.AddComponent<DungeonParticleManager>();
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

        StartCoroutine(CoInitialize());
    }

    private IEnumerator CoInitialize()
    {
        var settings =
            Resources.Load<ParticleSettings>(m_settingsPath);

        if (settings == null || !settings.EnableParticles)
        {
            // 公開環境ではここで終了します。
            // Addressablesの初期化も行いません。
            m_isInitialized = true;
            yield break;
        }

        m_maxInstances = settings.MaxInstances;

        m_initializeHandle = Addressables.InitializeAsync(false);
        yield return m_initializeHandle;

        if (m_initializeHandle.Status !=
            AsyncOperationStatus.Succeeded)
        {
            FinishUnavailable("Addressablesの初期化に失敗しました。");
            yield break;
        }

        m_locationsHandle =
            Addressables.LoadResourceLocationsAsync(
                m_catalogAddress,
                typeof(ParticleCatalog));

        yield return m_locationsHandle;

        if (m_locationsHandle.Status !=
            AsyncOperationStatus.Succeeded)
        {
            FinishUnavailable("カタログの存在確認に失敗しました。");
            yield break;
        }

        var locations = m_locationsHandle.Result;

        if (locations == null || locations.Count == 0)
        {
            FinishUnavailable(null);
            yield break;
        }

        if (locations.Count != 1)
        {
            FinishUnavailable("カタログのアドレスが重複しています。");
            yield break;
        }

        m_catalogHandle =
            Addressables.LoadAssetAsync<ParticleCatalog>(
                locations[0]);

        yield return m_catalogHandle;

        if (m_catalogHandle.Status !=
            AsyncOperationStatus.Succeeded ||
            m_catalogHandle.Result == null)
        {
            FinishUnavailable("カタログのロードに失敗しました。");
            yield break;
        }

        var entries = m_catalogHandle.Result.Entries;

        if (entries != null)
        {
            foreach (var entry in entries)
            {
                if (entry == null ||
                    entry.Id == ParticleId.None ||
                    entry.Prefab == null)
                {
                    continue;
                }

                if (m_entries.ContainsKey(entry.Id))
                {
                    Debug.LogWarning(
                        $"[DungeonParticle] IDが重複しています: {entry.Id}");
                    continue;
                }

                if (entry.Prefab
                    .GetComponentsInChildren<ParticleSystem>(true)
                    .Length == 0)
                {
                    Debug.LogWarning(
                        $"[DungeonParticle] Particle Systemがありません: " +
                        $"{entry.Prefab.name}");
                    continue;
                }

                m_entries.Add(entry.Id, entry);
            }
        }

        if (m_entries.Count == 0)
        {
            FinishUnavailable(null);
            yield break;
        }

        m_isAvailable = true;
        m_isInitialized = true;
    }

    public DungeonParticleInstance Play(
        ParticleId id,
        Vector3 worldPosition,
        Color? startColor = null,
        Transform parent = null,
        int layer = -1,
        Quaternion? worldRotation = null)
    {
        if (!m_isAvailable ||
            id == ParticleId.None ||
            !m_entries.TryGetValue(id, out var entry))
        {
            return null;
        }

        RemoveDestroyedInstances();

        if (m_instances.Count >= m_maxInstances)
        {
            return null;
        }

        var scene = parent != null
            ? parent.gameObject.scene
            : SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        // 管理者の子にはせず、生成先シーンに所属させます。
        var root = new GameObject($"Particle_{id}");
        root.SetActive(false);

        SceneManager.MoveGameObjectToScene(root, scene);

        if (parent != null)
        {
            root.transform.SetParent(parent, false);
        }

        root.transform.position = worldPosition;

        // 非アクティブな親の下で生成し、
        // 色を設定してから再生します。
        var effect = Instantiate(
            entry.Prefab,
            root.transform,
            false);

        effect.transform.localPosition = Vector3.zero;

        // 回転指定時だけ、プレハブのルート回転を上書きします。
        if (worldRotation.HasValue)
        {
            effect.transform.rotation = worldRotation.Value;
        }

        // localRotation / localScaleはプレハブの値を保持します。
        effect.SetActive(true);

        var instance =
            root.AddComponent<DungeonParticleInstance>();

        if (!instance.Initialize(
                effect,
                startColor,
                entry.ColorizeChildren,
                layer,
                m_catalogHandle))
        {
            Destroy(root);
            return null;
        }

        m_instances.Add(instance);

        root.SetActive(true);
        instance.Begin();

        return instance;
    }

    public void StopAll(bool clearImmediately = true)
    {
        foreach (var instance in m_instances)
        {
            if (instance != null)
            {
                instance.Stop(clearImmediately);
            }
        }

        RemoveDestroyedInstances();
    }

    private void Update()
    {
        RemoveDestroyedInstances();
    }

    private void RemoveDestroyedInstances()
    {
        for (int i = m_instances.Count - 1; i >= 0; --i)
        {
            var instance = m_instances[i];

            if (instance == null || instance.IsDestroyRequested)
            {
                m_instances.RemoveAt(i);
            }
        }
    }

    private void FinishUnavailable(string warning)
    {
        if (!string.IsNullOrEmpty(warning))
        {
            Debug.LogWarning($"[DungeonParticle] {warning}");
        }

        m_isAvailable = false;
        m_isInitialized = true;
        m_entries.Clear();

        ReleaseHandles();
    }

    private void ReleaseHandles()
    {
        if (m_catalogHandle.IsValid())
        {
            Addressables.Release(m_catalogHandle);
            m_catalogHandle = default;
        }

        if (m_locationsHandle.IsValid())
        {
            Addressables.Release(m_locationsHandle);
            m_locationsHandle = default;
        }

        if (m_initializeHandle.IsValid())
        {
            Addressables.Release(m_initializeHandle);
            m_initializeHandle = default;
        }
    }

    private void OnDestroy()
    {
        if (m_instance != this)
        {
            return;
        }

        StopAll(true);
        m_entries.Clear();
        ReleaseHandles();

        m_instance = null;
    }
}