using System;
using UnityEngine;

/// <summary>
/// エディタ上で試合へ参加させるParty Prefabの一覧です。
///
/// 配列の先頭から最大4件を、試合に参加するパーティーとして使用します。
/// 参加者作成に成功したParty Prefabは先頭へ登録されます。
/// </summary>
[CreateAssetMenu(
    fileName = "BattlePartyRegistry",
    menuName = "プロバト/Battle Party Registry")]
public sealed class BattlePartyRegistry : ScriptableObject
{
    [SerializeField]
    private ComPartyBase[] m_partyPrefabs =
        Array.Empty<ComPartyBase>();

    public ComPartyBase[] PartyPrefabs
    {
        get { return m_partyPrefabs; }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 指定Party Prefabを一覧の先頭へ登録します。
    /// 同一Prefabがすでにある場合は既存登録を除去してから先頭へ移動します。
    /// </summary>
    public void RegisterPartyPrefabToFrontByEditor(
        ComPartyBase partyPrefab)
    {
        if (partyPrefab == null)
        {
            throw new ArgumentNullException(nameof(partyPrefab));
        }

        int existingCount = 0;

        for (int i = 0; i < m_partyPrefabs.Length; i++)
        {
            if (m_partyPrefabs[i] != null
                && m_partyPrefabs[i] != partyPrefab)
            {
                existingCount++;
            }
        }

        var newPartyPrefabs = new ComPartyBase[existingCount + 1];
        newPartyPrefabs[0] = partyPrefab;

        int destinationIndex = 1;

        for (int i = 0; i < m_partyPrefabs.Length; i++)
        {
            ComPartyBase existingPartyPrefab = m_partyPrefabs[i];

            if (existingPartyPrefab == null
                || existingPartyPrefab == partyPrefab)
            {
                continue;
            }

            newPartyPrefabs[destinationIndex] = existingPartyPrefab;
            destinationIndex++;
        }

        m_partyPrefabs = newPartyPrefabs;
    }
#endif
}