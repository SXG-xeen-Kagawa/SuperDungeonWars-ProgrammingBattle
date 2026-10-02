using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class DungeonParticleInstance : MonoBehaviour
{
    private ParticleSystem[] m_systems;
    private ParticleSystem[] m_rootSystems;

    private AsyncOperationHandle<ParticleCatalog> m_catalogHandle;
    private bool m_hasCatalogHandle;
    private bool m_initialized;
    private bool m_destroyRequested;

    public bool IsDestroyRequested => m_destroyRequested;

    public bool Initialize(
        GameObject effect,
        Color? startColor,
        bool colorizeChildren,
        int layer,
        AsyncOperationHandle<ParticleCatalog> catalogHandle)
    {
        m_systems =
            effect.GetComponentsInChildren<ParticleSystem>(true);

        if (m_systems.Length == 0)
        {
            return false;
        }

        var roots = new List<ParticleSystem>();

        foreach (var system in m_systems)
        {
            if (!HasParticleAncestor(system, effect.transform))
            {
                roots.Add(system);
            }

            system.Stop(
                false,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = system.main;
            main.playOnAwake = false;

            // 終了処理はこのクラスに統一します。
            main.stopAction = ParticleSystemStopAction.None;

            // 画面外でも進行させ、単発の終了判定を止めません。
            main.cullingMode =
                ParticleSystemCullingMode.AlwaysSimulate;
        }

        m_rootSystems = roots.ToArray();

        if (startColor.HasValue)
        {
            var targets = colorizeChildren
                ? m_systems
                : m_rootSystems;

            foreach (var system in targets)
            {
                var main = system.main;
                main.startColor = startColor.Value;
            }
        }

        if (layer >= 0 && layer <= 31)
        {
            foreach (var child in
                     effect.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        // 通常のInstantiateで作った生成物にも、
        // カタログの依存アセットを保持するための参照を持たせます。
        m_catalogHandle =
            Addressables.ResourceManager.Acquire(catalogHandle);
        m_hasCatalogHandle = true;
        m_initialized = true;

        return true;
    }

    public void Begin()
    {
        if (!m_initialized || m_destroyRequested)
        {
            return;
        }

        foreach (var system in m_rootSystems)
        {
            if (system != null && system.gameObject.activeInHierarchy)
            {
                system.Play(true);
            }
        }
    }

    public void Stop(bool clearImmediately = true)
    {
        if (!m_initialized || m_destroyRequested)
        {
            return;
        }

        var behavior = clearImmediately
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        foreach (var system in m_systems)
        {
            if (system != null)
            {
                system.Stop(false, behavior);
            }
        }

        if (clearImmediately)
        {
            DestroyInstance();
        }
    }

    private void Update()
    {
        if (!m_initialized || m_destroyRequested)
        {
            return;
        }

        foreach (var system in m_systems)
        {
            if (system != null &&
                system.gameObject.activeInHierarchy &&
                system.IsAlive(false))
            {
                return;
            }
        }

        DestroyInstance();
    }

    private void DestroyInstance()
    {
        m_destroyRequested = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (m_hasCatalogHandle && m_catalogHandle.IsValid())
        {
            Addressables.Release(m_catalogHandle);
        }

        m_hasCatalogHandle = false;
    }

    private static bool HasParticleAncestor(
        ParticleSystem system,
        Transform effectRoot)
    {
        var current = system.transform;

        while (current != effectRoot && current.parent != null)
        {
            current = current.parent;

            if (current.GetComponent<ParticleSystem>() != null)
            {
                return true;
            }
        }

        return false;
    }
}