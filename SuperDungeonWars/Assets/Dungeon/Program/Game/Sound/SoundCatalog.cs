using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SoundCatalog",
    menuName = "Dungeon/Sound/Sound Catalog")]
public sealed class SoundCatalog : ScriptableObject
{
    [Serializable]
    public sealed class SeEntry
    {
        [SerializeField] private SeId m_id;
        [SerializeField] private AudioClip m_clip;
        [SerializeField, Range(0f, 1f)] private float m_volume = 1f;

        public SeId Id => m_id;
        public AudioClip Clip => m_clip;
        public float Volume => Mathf.Clamp01(m_volume);
    }

    [Serializable]
    public sealed class BgmEntry
    {
        [SerializeField] private BgmId m_id;
        [SerializeField] private AudioClip m_clip;
        [SerializeField, Range(0f, 1f)] private float m_volume = 1f;

        public BgmId Id => m_id;
        public AudioClip Clip => m_clip;
        public float Volume => Mathf.Clamp01(m_volume);
    }

    [SerializeField] private SeEntry[] m_seEntries = Array.Empty<SeEntry>();
    [SerializeField] private BgmEntry[] m_bgmEntries = Array.Empty<BgmEntry>();

    public SeEntry[] SeEntries => m_seEntries;
    public BgmEntry[] BgmEntries => m_bgmEntries;
}