using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ParticipantAccessoryCatalog",
    menuName = "プロバト/Participant Accessory Catalog")]
public sealed class ParticipantAccessoryCatalog : ScriptableObject
{
    [SerializeField]
    private Entry[] m_entries;

    public Entry[] Entries
    {
        get { return m_entries; }
    }

    [Serializable]
    public sealed class Entry
    {
        [SerializeField] private string m_displayName;
        [SerializeField] private GameObject m_prefab;

        public string DisplayName
        {
            get { return m_displayName; }
        }

        public GameObject Prefab
        {
            get { return m_prefab; }
        }
    }
}