using System.Collections.Generic;
using UnityEngine;

public class MazeRenderer : MonoBehaviour
{
    private class CellVisualSet
    {
        public GameObject m_floorObject;
        public GameObject m_solidBlockObject;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject m_floorPrefab;
    [SerializeField] private GameObject m_solidBlockPrefab;

    [Header("Layout")]
    [SerializeField] private float m_floorHeight = 0.2f;
    [SerializeField] private float m_solidBlockHeight = 2.0f;

    [SerializeField] private float m_outerWallThickness = 0.2f;

    // 内部の壁ブロックと外壁が同一平面にならないよう、
    // 外壁を迷路の外側へ少しだけ逃がす距離。
    [SerializeField] private float m_outerWallOutset = 0.02f;


    [Header("Physics")]
    [SerializeField] private float m_wallCollisionHeight = 20.0f;



    [Header("Surface")]
    [SerializeField] private Material m_mazeSurfaceMaterial;

    [SerializeField] private Material m_outerBoundaryMaterial;

    private readonly List<Mesh> m_runtimeMeshes =
        new List<Mesh>();

    private Mesh m_corridorFloorMesh;
    private Mesh m_roomFloorMesh;
    private Mesh m_centralHallFloorMesh;
    private Mesh m_solidBlockMesh;



    private MazeData m_mazeData;
    private KnownMapData m_knownMapData;

    private readonly List<GameObject> m_spawnedObjects =
        new List<GameObject>();

    private CellVisualSet[,] m_cellVisualSets;

    public MazeData GetMazeData()
    {
        return m_mazeData;
    }

    public void Build(MazeData mazeData)
    {
        Clear();

        m_mazeData = mazeData;

        if (m_mazeData == null)
        {
            return;
        }

        CreateRuntimeMeshes();

        m_cellVisualSets = new CellVisualSet[
            m_mazeData.m_width,
            m_mazeData.m_height];

        for (int y = 0; y < m_mazeData.m_height; y++)
        {
            for (int x = 0; x < m_mazeData.m_width; x++)
            {
                MazeCellData cell = m_mazeData.GetCell(x, y);
                Vector3 cellWorld = m_mazeData.CellToWorld(x, y);

                CellVisualSet visualSet =
                    new CellVisualSet();

                visualSet.m_floorObject =
                    CreateFloor(cell, cellWorld);

                visualSet.m_solidBlockObject =
                    CreateSolidBlock(cell, cellWorld);

                m_cellVisualSets[x, y] = visualSet;
            }
        }

        CreateOuterBoundaryWalls();

        RefreshAllVisibility();
    }

    public void BindKnownMapData(KnownMapData knownMapData)
    {
        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated -=
                OnKnownMapUpdated;
        }

        m_knownMapData = knownMapData;

        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated +=
                OnKnownMapUpdated;
        }

        RefreshAllVisibility();
    }

    public void Clear()
    {
        if (m_knownMapData != null)
        {
            m_knownMapData.m_onKnownMapUpdated -=
                OnKnownMapUpdated;

            m_knownMapData = null;
        }

        for (int i = m_spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (m_spawnedObjects[i] == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(m_spawnedObjects[i]);
            }
            else
            {
                DestroyImmediate(m_spawnedObjects[i]);
            }
#else
            Destroy(m_spawnedObjects[i]);
#endif
        }

        m_spawnedObjects.Clear();
        m_cellVisualSets = null;
        ClearRuntimeMeshes();
        m_mazeData = null;
    }

    private void OnKnownMapUpdated()
    {
        RefreshAllVisibility();
    }

    private void RefreshAllVisibility()
    {
        if (m_mazeData == null
            || m_cellVisualSets == null)
        {
            return;
        }

        for (int y = 0; y < m_mazeData.m_height; y++)
        {
            for (int x = 0; x < m_mazeData.m_width; x++)
            {
                RefreshCellVisibility(x, y);
            }
        }
    }

    private void RefreshCellVisibility(int x, int y)
    {
        CellVisualSet visualSet = m_cellVisualSets[x, y];

        if (visualSet == null)
        {
            return;
        }

        bool showFloor = false;
        bool showSolidBlock = false;

        MazeCellData mazeCell = m_mazeData.GetCell(x, y);

        if (mazeCell == null)
        {
            return;
        }

        if (m_knownMapData == null)
        {
            if (mazeCell.IsBlockedCell())
            {
                showSolidBlock = true;
            }
            else
            {
                showFloor = true;
            }
        }
        else
        {
            KnownCellData knownCell =
                m_knownMapData.GetCell(x, y);

            if (knownCell != null
                && knownCell.m_isObserved)
            {
                KnownCellViewType viewType =
                    knownCell.GetViewType();

                if (viewType == KnownCellViewType.Blocked)
                {
                    showSolidBlock = true;
                }
                else if (viewType == KnownCellViewType.Corridor
                    || viewType == KnownCellViewType.Room)
                {
                    showFloor = true;
                }
            }
        }

        SetActiveSafe(
            visualSet.m_floorObject,
            showFloor);

        SetActiveSafe(
            visualSet.m_solidBlockObject,
            showSolidBlock);
    }

    private GameObject CreateFloor(
        MazeCellData cell,
        Vector3 cellWorld)
    {
        if (cell == null || cell.IsBlockedCell())
        {
            return null;
        }

        Mesh floorMesh = m_corridorFloorMesh;

        if (cell.m_isCentralHall)
        {
            floorMesh = m_centralHallFloorMesh;
        }
        else if (cell.m_isRoom)
        {
            floorMesh = m_roomFloorMesh;
        }

        GameObject floor = CreateMazeSurfaceObject(
            m_floorPrefab,
            $"Floor_{cell.m_x}_{cell.m_y}",
            floorMesh,
            cellWorld,
            false);

        return floor;
    }

    private void CreateOuterBoundaryWalls()
    {
        if (m_mazeData == null)
        {
            return;
        }

        Vector3 minCellWorld =
            m_mazeData.CellToWorld(0, 0);

        Vector3 maxCellWorld =
            m_mazeData.CellToWorld(
                m_mazeData.m_width - 1,
                m_mazeData.m_height - 1);

        float minX = Mathf.Min(
            minCellWorld.x,
            maxCellWorld.x);

        float maxX = Mathf.Max(
            minCellWorld.x,
            maxCellWorld.x);

        float minZ = Mathf.Min(
            minCellWorld.z,
            maxCellWorld.z);

        float maxZ = Mathf.Max(
            minCellWorld.z,
            maxCellWorld.z);

        float halfCellSize =
            m_mazeData.CellSize * 0.5f;

        float wallY =
            m_solidBlockHeight * 0.5f;

        float outerWallOutset =
            Mathf.Max(0.0f, m_outerWallOutset);

        float leftWallX =
            minX
            - halfCellSize
            - m_outerWallThickness * 0.5f
            - outerWallOutset;

        float rightWallX =
            maxX
            + halfCellSize
            + m_outerWallThickness * 0.5f
            + outerWallOutset;

        float bottomWallZ =
            minZ
            - halfCellSize
            - m_outerWallThickness * 0.5f
            - outerWallOutset;

        float topWallZ =
            maxZ
            + halfCellSize
            + m_outerWallThickness * 0.5f
            + outerWallOutset;

        CreateOuterWall(
            "OuterWall_Left",
            new Vector3(
                leftWallX,
                wallY,
                (minZ + maxZ) * 0.5f),
            new Vector3(
                m_outerWallThickness,
                m_solidBlockHeight,
                m_mazeData.m_height
                    * m_mazeData.CellSize
                    + m_outerWallThickness * 2.0f));

        CreateOuterWall(
            "OuterWall_Right",
            new Vector3(
                rightWallX,
                wallY,
                (minZ + maxZ) * 0.5f),
            new Vector3(
                m_outerWallThickness,
                m_solidBlockHeight,
                m_mazeData.m_height
                    * m_mazeData.CellSize
                    + m_outerWallThickness * 2.0f));

        CreateOuterWall(
            "OuterWall_Bottom",
            new Vector3(
                (minX + maxX) * 0.5f,
                wallY,
                bottomWallZ),
            new Vector3(
                m_mazeData.m_width
                    * m_mazeData.CellSize
                    + m_outerWallThickness * 2.0f,
                m_solidBlockHeight,
                m_outerWallThickness));

        CreateOuterWall(
            "OuterWall_Top",
            new Vector3(
                (minX + maxX) * 0.5f,
                wallY,
                topWallZ),
            new Vector3(
                m_mazeData.m_width
                    * m_mazeData.CellSize
                    + m_outerWallThickness * 2.0f,
                m_solidBlockHeight,
                m_outerWallThickness));
    }

    private void CreateOuterWall(
        string wallName,
        Vector3 worldPosition,
        Vector3 worldScale)
    {
        Mesh wallMesh = CreateAndRegisterRuntimeBoxMesh(
            $"{wallName}_Mesh",
            worldScale,
            RuntimeBoxMeshFactory.SurfaceType.Ceiling,
            RuntimeBoxMeshFactory.SurfaceType.Wall,
            RuntimeBoxMeshFactory.SurfaceType.Fallback);

        GameObject wall = CreateMazeSurfaceObject(
            null,
            wallName,
            wallMesh,
            worldPosition,
            true);

        MeshRenderer meshRenderer =
            wall.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
        {
            if (m_outerBoundaryMaterial != null)
            {
                meshRenderer.sharedMaterial =
                    m_outerBoundaryMaterial;
            }
            else
            {
                Debug.LogWarning(
                    $"[{nameof(MazeRenderer)}] 外周壁用 Material が未設定です。"
                    + $" {nameof(m_outerBoundaryMaterial)} に"
                    + " 単色の URP/Unlit Material を設定してください。");
            }
        }

        ExtendWallBoxColliderUpward(wall);
    }

    private GameObject CreateSolidBlock(
        MazeCellData cell,
        Vector3 cellWorld)
    {
        if (cell == null || cell.IsBlockedCell() == false)
        {
            return null;
        }

        GameObject solidBlock = CreateMazeSurfaceObject(
            m_solidBlockPrefab,
            $"SolidBlock_{cell.m_x}_{cell.m_y}",
            m_solidBlockMesh,
            new Vector3(
                cellWorld.x,
                m_solidBlockHeight * 0.5f,
                cellWorld.z),
            true);

        ExtendWallBoxColliderUpward(solidBlock);

        return solidBlock;
    }

    private void SetActiveSafe(
        GameObject targetObject,
        bool isActive)
    {
        if (targetObject == null)
        {
            return;
        }

        if (targetObject.activeSelf != isActive)
        {
            targetObject.SetActive(isActive);
        }
    }


    private const string s_mazeWallLayerName = "MazeWall";

    private void ApplyMazeWallLayer(GameObject wallObject)
    {
        if (wallObject == null)
        {
            return;
        }

        int mazeWallLayer = LayerMask.NameToLayer(s_mazeWallLayerName);
        if (mazeWallLayer < 0)
        {
            Debug.LogWarning(
                $"[{nameof(MazeRenderer)}] Layer \"{s_mazeWallLayerName}\" が見つかりません。"
                + " Project Settings > Tags and Layers で追加してください。"
            );
            return;
        }

        ApplyLayerRecursively(wallObject.transform, mazeWallLayer);
    }

    private void ApplyLayerRecursively(Transform targetTransform, int layer)
    {
        if (targetTransform == null)
        {
            return;
        }

        targetTransform.gameObject.layer = layer;

        for (int childIndex = 0; childIndex < targetTransform.childCount; childIndex++)
        {
            ApplyLayerRecursively(targetTransform.GetChild(childIndex), layer);
        }
    }


    private void ExtendWallBoxColliderUpward(
    GameObject wallObject)
    {
        if (wallObject == null)
        {
            return;
        }

        BoxCollider boxCollider =
            wallObject.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            return;
        }

        float collisionHeight = Mathf.Max(
            m_solidBlockHeight,
            m_wallCollisionHeight);

        Transform wallTransform =
            wallObject.transform;

        float worldScaleY = Mathf.Abs(
            wallTransform.lossyScale.y);

        if (worldScaleY <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 colliderCenter =
            boxCollider.center;

        Vector3 colliderSize =
            boxCollider.size;

        // Collider の下端をワールド Y=0 に固定し、
        // 上端を m_wallCollisionHeight まで伸ばす。
        colliderCenter.y =
            (collisionHeight * 0.5f
            - wallTransform.position.y)
            / wallTransform.lossyScale.y;

        colliderSize.y =
            collisionHeight / worldScaleY;

        boxCollider.center = colliderCenter;
        boxCollider.size = colliderSize;
    }


    private void CreateRuntimeMeshes()
    {
        Vector3 floorSize = new Vector3(
            m_mazeData.CellSize,
            m_floorHeight,
            m_mazeData.CellSize);

        Vector3 solidBlockSize = new Vector3(
            m_mazeData.CellSize,
            m_solidBlockHeight,
            m_mazeData.CellSize);

        m_corridorFloorMesh = CreateAndRegisterRuntimeBoxMesh(
            "Maze_CorridorFloor",
            floorSize,
            RuntimeBoxMeshFactory.SurfaceType.CorridorFloor,
            RuntimeBoxMeshFactory.SurfaceType.Fallback,
            RuntimeBoxMeshFactory.SurfaceType.Fallback);

        m_roomFloorMesh = CreateAndRegisterRuntimeBoxMesh(
            "Maze_RoomFloor",
            floorSize,
            RuntimeBoxMeshFactory.SurfaceType.RoomFloor,
            RuntimeBoxMeshFactory.SurfaceType.Fallback,
            RuntimeBoxMeshFactory.SurfaceType.Fallback);

        m_centralHallFloorMesh = CreateAndRegisterRuntimeBoxMesh(
            "Maze_CentralHallFloor",
            floorSize,
            RuntimeBoxMeshFactory.SurfaceType.CentralHallFloor,
            RuntimeBoxMeshFactory.SurfaceType.Fallback,
            RuntimeBoxMeshFactory.SurfaceType.Fallback);

        m_solidBlockMesh = CreateAndRegisterRuntimeBoxMesh(
            "Maze_SolidBlock",
            solidBlockSize,
            RuntimeBoxMeshFactory.SurfaceType.Ceiling,
            RuntimeBoxMeshFactory.SurfaceType.Wall,
            RuntimeBoxMeshFactory.SurfaceType.Fallback);
    }

    private Mesh CreateAndRegisterRuntimeBoxMesh(
        string meshName,
        Vector3 size,
        RuntimeBoxMeshFactory.SurfaceType topSurfaceType,
        RuntimeBoxMeshFactory.SurfaceType sideSurfaceType,
        RuntimeBoxMeshFactory.SurfaceType bottomSurfaceType)
    {
        Mesh mesh = RuntimeBoxMeshFactory.CreateBoxMesh(
            meshName,
            size,
            topSurfaceType,
            sideSurfaceType,
            bottomSurfaceType);

        m_runtimeMeshes.Add(mesh);

        return mesh;
    }

    private GameObject CreateMazeSurfaceObject(
        GameObject prefab,
        string objectName,
        Mesh mesh,
        Vector3 worldPosition,
        bool isWall)
    {
        GameObject targetObject;

        if (prefab != null)
        {
            targetObject = Instantiate(prefab, transform);
        }
        else
        {
            targetObject = new GameObject();
            targetObject.transform.SetParent(transform, false);
        }

        targetObject.name = objectName;
        targetObject.transform.position = worldPosition;
        targetObject.transform.rotation = Quaternion.identity;
        targetObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter =
            targetObject.GetComponent<MeshFilter>();

        if (meshFilter == null)
        {
            meshFilter = targetObject.AddComponent<MeshFilter>();
        }

        meshFilter.sharedMesh = mesh;

        MeshRenderer meshRenderer =
            targetObject.GetComponent<MeshRenderer>();

        if (meshRenderer == null)
        {
            meshRenderer = targetObject.AddComponent<MeshRenderer>();
        }

        if (m_mazeSurfaceMaterial != null)
        {
            meshRenderer.sharedMaterial =
                m_mazeSurfaceMaterial;
        }

        BoxCollider boxCollider =
            targetObject.GetComponent<BoxCollider>();

        if (boxCollider == null)
        {
            boxCollider = targetObject.AddComponent<BoxCollider>();
        }

        boxCollider.center = Vector3.zero;
        boxCollider.size = mesh.bounds.size;

        if (isWall)
        {
            ApplyMazeWallLayer(targetObject);
        }

        m_spawnedObjects.Add(targetObject);

        return targetObject;
    }

    private void ClearRuntimeMeshes()
    {
        for (int i = m_runtimeMeshes.Count - 1; i >= 0; i--)
        {
            Mesh mesh = m_runtimeMeshes[i];

            if (mesh == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
#else
            Destroy(mesh);
#endif
        }

        m_runtimeMeshes.Clear();

        m_corridorFloorMesh = null;
        m_roomFloorMesh = null;
        m_centralHallFloorMesh = null;
        m_solidBlockMesh = null;
    }


}