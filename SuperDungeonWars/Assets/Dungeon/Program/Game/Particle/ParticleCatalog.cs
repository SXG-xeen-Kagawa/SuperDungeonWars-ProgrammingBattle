using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ParticleCatalog",
    menuName = "Dungeon/Particle/Particle Catalog")]
public sealed class ParticleCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        [SerializeField]
        private ParticleId m_id;

        [SerializeField]
        private GameObject m_prefab;

        [Tooltip(
            "色指定時、子のParticle Systemも変更します。" +
            "OFFの場合は最上位のParticle Systemだけ変更します。")]
        [SerializeField]
        private bool m_colorizeChildren = true;

        public ParticleId Id => m_id;
        public GameObject Prefab => m_prefab;
        public bool ColorizeChildren => m_colorizeChildren;
    }

    [SerializeField]
    private Entry[] m_entries = new Entry[0];

    public Entry[] Entries => m_entries;
}