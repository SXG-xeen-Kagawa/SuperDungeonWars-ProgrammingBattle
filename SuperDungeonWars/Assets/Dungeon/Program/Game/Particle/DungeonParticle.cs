using UnityEngine;

public static class DungeonParticle
{
    public static bool IsInitialized
    {
        get
        {
            var manager = DungeonParticleManager.Instance;
            return manager != null && manager.IsInitialized;
        }
    }

    public static bool IsAvailable
    {
        get
        {
            var manager = DungeonParticleManager.Instance;
            return manager != null && manager.IsAvailable;
        }
    }

    public static DungeonParticleInstance Play(
        ParticleId id,
        Vector3 worldPosition,
        Color? startColor = null,
        Transform parent = null,
        int layer = -1,
        Quaternion? worldRotation = null)
    {
        var manager = DungeonParticleManager.Instance;

        if (manager == null)
        {
            return null;
        }

        return manager.Play(
            id, worldPosition, startColor, parent, layer,
            worldRotation);
    }

    public static void Stop(
        DungeonParticleInstance instance,
        bool clearImmediately = true)
    {
        if (instance != null)
        {
            instance.Stop(clearImmediately);
        }
    }

    public static void StopAll(bool clearImmediately = true)
    {
        var manager = DungeonParticleManager.Instance;

        if (manager != null)
        {
            manager.StopAll(clearImmediately);
        }
    }
}