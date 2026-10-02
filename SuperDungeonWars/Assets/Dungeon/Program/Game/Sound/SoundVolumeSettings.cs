using UnityEngine;

[CreateAssetMenu(
    fileName = "DungeonSoundSettings",
    menuName = "Dungeon/Sound/Volume Settings")]
public sealed class SoundVolumeSettings : ScriptableObject
{
    [SerializeField] private bool m_enableSound = true;

    [SerializeField, Range(0f, 1f)]
    private float m_seVolume = 1f;

    [SerializeField, Range(0f, 1f)]
    private float m_bgmVolume = 0.5f;

    [SerializeField, Min(0f)]
    private float m_defaultCrossFadeSeconds = 1f;

    public bool EnableSound => m_enableSound;
    public float SeVolume => Mathf.Clamp01(m_seVolume);
    public float BgmVolume => Mathf.Clamp01(m_bgmVolume);
    public float DefaultCrossFadeSeconds =>
        Mathf.Max(0f, m_defaultCrossFadeSeconds);
}