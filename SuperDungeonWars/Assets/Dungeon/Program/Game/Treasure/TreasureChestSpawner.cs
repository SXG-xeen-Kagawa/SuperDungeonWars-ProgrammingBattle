using System.Collections.Generic;
using UnityEngine;

public class TreasureChestSpawner : MonoBehaviour
{
    [SerializeField] private TreasureChest m_treasureChestPrefab;
    [SerializeField] private Transform m_treasureRoot;

    private readonly List<TreasureChest> m_spawnedTreasureChests =
        new List<TreasureChest>();

    public IReadOnlyList<TreasureChest> SpawnedTreasureChests
    {
        get { return m_spawnedTreasureChests; }
    }

    public void Build(MazeData mazeData)
    {
        Clear();

        if (mazeData == null)
        {
            return;
        }

        if (m_treasureChestPrefab == null)
        {
            Debug.LogError(
                "TreasureChestSpawner に宝箱Prefabが設定されていません。",
                this);

            return;
        }

        TreasurePlacementPlanner planner =
            new TreasurePlacementPlanner();

        List<TreasurePlacement> placements =
            planner.CreatePlacements(mazeData);

        Transform parent = m_treasureRoot != null
            ? m_treasureRoot
            : transform;

        for (int i = 0; i < placements.Count; i++)
        {
            TreasurePlacement placement = placements[i];

            Vector3 worldPosition = mazeData.CellToWorld(
                placement.m_cellPosition.x,
                placement.m_cellPosition.y);

            TreasureChest treasureChest = Instantiate(
                m_treasureChestPrefab,
                worldPosition,
                Quaternion.identity,
                parent);

            treasureChest.name = string.Format(
                "{0}_Id{1}_Room{2}_Cell{3}_{4}",
                placement.m_treasureType,
                i,
                placement.m_roomId,
                placement.m_cellPosition.x,
                placement.m_cellPosition.y);

            treasureChest.Initialize(
                i,
                placement,
                mazeData);

            m_spawnedTreasureChests.Add(treasureChest);
        }
    }

    public void Clear()
    {
        for (int i = 0; i < m_spawnedTreasureChests.Count; i++)
        {
            TreasureChest treasureChest =
                m_spawnedTreasureChests[i];

            if (treasureChest != null)
            {
                Destroy(treasureChest.gameObject);
            }
        }

        m_spawnedTreasureChests.Clear();
    }
}