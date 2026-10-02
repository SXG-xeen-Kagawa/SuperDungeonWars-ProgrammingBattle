using UnityEngine;

[CreateAssetMenu(
    fileName = "DungeonParticleSettings",
    menuName = "Dungeon/Particle/Settings")]
public sealed class ParticleSettings : ScriptableObject
{
    [SerializeField]
    private bool m_enableParticles = true;

    [Tooltip("最大同時インスタンス数です。上限時は新しい要求を無視します。")]
    [Min(1)]
    [SerializeField]
    private int m_maxInstances = 64;

    public bool EnableParticles => m_enableParticles;
    public int MaxInstances => Mathf.Max(1, m_maxInstances);
}