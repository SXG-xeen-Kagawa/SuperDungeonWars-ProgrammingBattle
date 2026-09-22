//#define MAZE_GENERATOR_USE_RANDOM_SEED


using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MazeGenerationSettings
{
    [Header("Map Size")]
    public int m_width = 31;
    public int m_height = 31;

    [Header("Seed")]
    public bool m_useRandomSeed = true;
    public int m_seed = 12345;

    [Header("Room Count")]
    public int m_randomRoomCountMin = 8;
    public int m_randomRoomCountMax = 14;

    [Header("Room Width")]
    public int m_roomMinWidth = 2;
    public int m_roomMaxWidth = 7;

    [Header("Room Height")]
    public int m_roomMinHeight = 2;
    public int m_roomMaxHeight = 7;

    [Header("Room Margin")]
    public int m_roomMargin = 1;

    [Header("Loops")]
    public int m_extraConnectionCount = 20;

    [Header("Post Process")]
    public bool m_reduceCorridorPlaza = true;
    public int m_corridorPlazaReductionPassCount = 8;

    [Header("Room Adjacency Cleanup")]
    public bool m_cleanupRoomAdjacentCorridors = true;
    public int m_roomAdjacentCleanupPassCount = 6;

    [Header("Corridor To Room Promotion")]
    public bool m_promoteSmallCorridorAreaToRoom = true;
    public int m_promotedRoomMinArea = 4;
    public int m_promotedRoomMaxArea = 12;
    public int m_promotedRoomMaxWidth = 4;
    public int m_promotedRoomMaxHeight = 4;
    public int m_promotedRoomMaxAspectGap = 2;
    public int m_promotedRoomMaxOuterOpenNeighbors = 10;

    [Header("Fairness")]
    public bool m_useFourFoldRotationalSymmetry = true;
}

public static class MazeGenerator
{
    private const int s_minimumMapSize = 41;

    private const int s_centerRoomSize = 5;
    private const int s_smallRoomSize = 3;
    private const int s_roomWallMargin = 1;

    private const int s_initialSmallRoomSetCount = 2;

    private const int s_minAdditionalRoomSetCount = 0;
    private const int s_maxAdditionalRoomSetCount = 2;

    /*
     * 既存通路を含む長方形共有部屋へ昇格できる、
     * C4対称セットの最大数。
     *
     * 実際には候補が尽きた時点で終了する。
     * これは極端に部屋だらけになる事故を避ける安全上限。
     */
    private const int s_maxLoopRoomPromotionSetCount = 8;

    /*
     * 新たに壁を掘ってよい最大セル数。
     * 既存通路を部屋扱いへ変更するセル数は含まない。
     */
    private const int s_maxLoopRoomPromotionCellCount = 3;

    /*
     * 昇格対象となる完成後の長方形部屋の辺長。
     *
     * 3x3 ～ 5x5 を対象にする。
     * たとえば4x4の内部に壁が1～3セルだけ残っている場所も、
     * 今回から部屋候補になる。
     */
    private const int s_minLoopRoomPromotionRoomSize = 3;
    private const int s_maxLoopRoomPromotionRoomSize = 5;


    /*
     * 中央以外の共有経路を作りやすくするため、
     * 前回より少し多めにループ軌道を追加する。
     */
    private const int s_minExtraLoopOrbitCount = 5;
    private const int s_maxExtraLoopOrbitCount = 8;

    private const int s_coarseFirstCoordinate = 2;
    private const int s_coarseStep = 2;

    private const int s_maxGenerationAttemptCount = 128;

    private const int s_minimumEntranceToCenterStepCount = 10;
    private const int s_minimumEntranceToCenterTurnCount = 3;
    private const int s_maximumStraightRunOnCenterPath = 5;

    /*
     * 中央扉の直前ではなく、中央から最低3粗グリッド以上離れた
     * 共有分岐を要求する。
     *
     * 粗グリッド1辺は実セル2マスなので、中央扉付近だけでの
     * 接続ではなく、中央外側に遭遇・待ち伏せ領域を保証する。
     */
    private const int s_minimumSharedJunctionDistanceFromCenter = 3;


    /*
     * 通常試合で使用する、事前審査済み迷路の seed 一覧。
     *
     * 迷路候補を採用したら、この配列へ追加する。
     * 将来的には ScriptableObject 化してもよいが、
     * まずは MazeGenerator.cs 内で管理する。
     */
    private static readonly int[] s_preapprovedMazeSeeds =
    {
        1041505397,
        1013712272,
        -1533320660,
        1156319047,
        1285940961,
        627168301,
        -217070758,
        1860040321,
        -577281470,
        -896116957,
        1242416424,
        544936298,
        -587164381,
    };



    private class RoomNode
    {
        public int m_roomId;
        public Vector2Int m_center;
        public List<Vector2Int> m_doorCells =
            new List<Vector2Int>();
    }

    private class AdditionalRoomPlan
    {
        public Vector2Int m_roomCenter;
        public Vector2Int m_doorDirection;
        public List<Vector2Int> m_connectorPath =
            new List<Vector2Int>();
    }

    private struct CoarseEdge
    {
        public Vector2Int m_a;
        public Vector2Int m_b;

        public CoarseEdge(
            Vector2Int a,
            Vector2Int b)
        {
            m_a = a;
            m_b = b;
        }
    }

    private class CoarseEdgeOrbit
    {
        public string m_key;
        public List<CoarseEdge> m_edges =
            new List<CoarseEdge>();
    }

    private class DisjointSet
    {
        private int[] m_parent;
        private int[] m_rank;
        private int m_componentCount;

        public int ComponentCount
        {
            get
            {
                return m_componentCount;
            }
        }

        public DisjointSet(int count)
        {
            m_parent = new int[count];
            m_rank = new int[count];
            m_componentCount = count;

            for (int index = 0; index < count; index++)
            {
                m_parent[index] = index;
            }
        }

        private DisjointSet(
            int[] parent,
            int[] rank,
            int componentCount)
        {
            m_parent = parent;
            m_rank = rank;
            m_componentCount = componentCount;
        }

        public DisjointSet Clone()
        {
            return new DisjointSet(
                (int[])m_parent.Clone(),
                (int[])m_rank.Clone(),
                m_componentCount);
        }

        public int Find(int value)
        {
            if (m_parent[value] != value)
            {
                m_parent[value] = Find(m_parent[value]);
            }

            return m_parent[value];
        }

        public bool Union(
            int a,
            int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);

            if (rootA == rootB)
            {
                return false;
            }

            if (m_rank[rootA] < m_rank[rootB])
            {
                m_parent[rootA] = rootB;
            }
            else if (m_rank[rootA] > m_rank[rootB])
            {
                m_parent[rootB] = rootA;
            }
            else
            {
                m_parent[rootB] = rootA;
                m_rank[rootA]++;
            }

            m_componentCount--;

            return true;
        }
    }

    public static MazeData Generate(
        MazeGenerationSettings settings,
        float cellSize)
    {
        MazeGenerationSettings normalizedSettings =
            NormalizeSettings(settings);

        int usedSeed;
        string seedSource;

        System.Random random = CreateRandom(
            normalizedSettings,
            out usedSeed,
            out seedSource);

        /*
         * MazeGenerator 自身は System.Random を使うが、
         * 今後の処理追加や生成中の UnityEngine.Random 利用に備え、
         * Unity 側のランダム状態も必ず元に戻す。
         */
        UnityEngine.Random.State previousUnityRandomState =
            UnityEngine.Random.state;

        try
        {
            UnityEngine.Random.InitState(usedSeed);

            Debug.Log(
                "MazeGenerator: 迷路生成開始"
                + " randomKey="
                + usedSeed
                + " source="
                + seedSource);

            Exception lastException = null;

            for (int attemptIndex = 0;
                 attemptIndex < s_maxGenerationAttemptCount;
                 attemptIndex++)
            {
                try
                {
                    return GenerateInternal(
                        normalizedSettings,
                        cellSize,
                        random,
                        usedSeed,
                        attemptIndex);
                }
                catch (Exception exception)
                {
                    lastException = exception;
                }
            }

            throw new InvalidOperationException(
                "MazeGenerator: C4RoomGraphV3を生成できませんでした。 randomKey="
                + usedSeed
                + " source="
                + seedSource,
                lastException);
        }
        finally
        {
            /*
             * 迷路生成が成功・失敗・例外のいずれでも、
             * 他のゲーム処理が使用する UnityEngine.Random の状態を戻す。
             */
            UnityEngine.Random.state =
                previousUnityRandomState;
        }
    }

    private static MazeData GenerateInternal(
        MazeGenerationSettings settings,
        float cellSize,
        System.Random random,
        int usedSeed,
        int attemptIndex)
    {
        int width = settings.m_width;
        int height = settings.m_height;

        bool[,] openMap = new bool[width, height];
        bool[,] roomMap = new bool[width, height];
        int[,] roomIdMap = new int[width, height];
        bool[,] protectedWallMap = new bool[width, height];
        bool[,] doorMap = new bool[width, height];

        InitializeRoomIdMap(roomIdMap);

        List<RoomNode> roomNodes =
            new List<RoomNode>();

        int nextRoomId = 0;

        CreateCentralRoom(
            width,
            ref nextRoomId,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap,
            roomNodes);

        CreateInitialSmallRoomSets(
            random,
            width,
            ref nextRoomId,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap,
            roomNodes);

        List<Vector2Int> coarseNodes;
        Dictionary<Vector2Int, int> coarseNodeIndices;

        CreateCoarseNodes(
            width,
            protectedWallMap,
            out coarseNodes,
            out coarseNodeIndices);

        HashSet<Vector2Int> terminalNodes =
            CreateTerminalNodes(
                width,
                roomNodes);

        ValidateTerminalNodes(
            terminalNodes,
            coarseNodeIndices);

        List<CoarseEdgeOrbit> edgeOrbits =
            CreateAllUsableCoarseEdgeOrbits(
                width,
                protectedWallMap,
                doorMap,
                coarseNodeIndices);

        List<CoarseEdge> selectedEdges =
            CreateRandomFourFoldCoarseGraph(
                random,
                coarseNodeIndices,
                edgeOrbits);

        selectedEdges = PruneUnnecessaryBranches(
            selectedEdges,
            coarseNodes,
            terminalNodes);

        ValidateCoarseGraphConnection(
            selectedEdges,
            coarseNodes,
            terminalNodes);

        /*
         * ★今回追加した品質検査。
         *
         * 中央へ接続する4つのノードを閉鎖した状態でも、
         * 4入口が同じ粗グリッド連結成分に残ることを確認する。
         *
         * つまり、中央部屋に入らなくても他チームの経路へ
         * 回り込み・追跡・待ち伏せできることを保証する。
         */
        ValidatePreCenterSharedRoutes(
            width,
            selectedEdges);

        ValidateCenterRouteQuality(
            width,
            selectedEdges);

        CarveCentralRoomConnections(
            width,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap);

        CarveSmallRoomConnections(
            roomNodes,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap);

        CarveCoarseGraph(
            selectedEdges,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap);

        CreateOuterEntranceLines(
            width,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap,
            doorMap);

        int additionalRoomSetCount =
            CreateAdditionalRoomSetsInEmptySpace(
                random,
                width,
                ref nextRoomId,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap,
                roomNodes);

        int promotedLoopRoomSetCount =
            PromoteClosedLoopAreasToRooms(
                random,
                width,
                ref nextRoomId,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap,
                roomNodes);

        ValidateGeneratedMap(
            width,
            height,
            openMap,
            roomNodes);

        MazeData mazeData = new MazeData(
            width,
            height,
            cellSize);

        ApplyMapsToMazeData(
            mazeData,
            openMap,
            roomMap,
            roomIdMap);

        OpenOuterEntrance(
            mazeData,
            width / 2,
            height - 1,
            MazeDirection.North);

        OpenOuterEntrance(
            mazeData,
            0,
            height / 2,
            MazeDirection.West);

        OpenOuterEntrance(
            mazeData,
            width / 2,
            0,
            MazeDirection.South);

        OpenOuterEntrance(
            mazeData,
            width - 1,
            height / 2,
            MazeDirection.East);

        MarkCentralHallCells(mazeData);

        Debug.Log(
            "MazeGenerator: seed="
            + usedSeed
            + " template=C4RoomGraphV3"
            + " attempt="
            + attemptIndex
            + " rooms="
            + roomNodes.Count
            + " coarseEdges="
            + selectedEdges.Count
            + " additionalRoomSets="
            + additionalRoomSetCount
            + " promotedLoopRoomSets="
            + promotedLoopRoomSetCount
            + " preCenterSharedRoutes=true");

        return mazeData;
    }

    private static MazeGenerationSettings NormalizeSettings(
        MazeGenerationSettings settings)
    {
        MazeGenerationSettings result =
            new MazeGenerationSettings();

        if (settings != null)
        {
            result.m_width = settings.m_width;
            result.m_height = settings.m_height;
            result.m_useRandomSeed = settings.m_useRandomSeed;
            result.m_seed = settings.m_seed;

            result.m_randomRoomCountMin =
                settings.m_randomRoomCountMin;
            result.m_randomRoomCountMax =
                settings.m_randomRoomCountMax;
            result.m_roomMinWidth =
                settings.m_roomMinWidth;
            result.m_roomMaxWidth =
                settings.m_roomMaxWidth;
            result.m_roomMinHeight =
                settings.m_roomMinHeight;
            result.m_roomMaxHeight =
                settings.m_roomMaxHeight;
            result.m_roomMargin =
                settings.m_roomMargin;
            result.m_extraConnectionCount =
                settings.m_extraConnectionCount;

            result.m_reduceCorridorPlaza =
                settings.m_reduceCorridorPlaza;
            result.m_corridorPlazaReductionPassCount =
                settings.m_corridorPlazaReductionPassCount;

            result.m_cleanupRoomAdjacentCorridors =
                settings.m_cleanupRoomAdjacentCorridors;
            result.m_roomAdjacentCleanupPassCount =
                settings.m_roomAdjacentCleanupPassCount;

            result.m_promoteSmallCorridorAreaToRoom =
                settings.m_promoteSmallCorridorAreaToRoom;
            result.m_promotedRoomMinArea =
                settings.m_promotedRoomMinArea;
            result.m_promotedRoomMaxArea =
                settings.m_promotedRoomMaxArea;
            result.m_promotedRoomMaxWidth =
                settings.m_promotedRoomMaxWidth;
            result.m_promotedRoomMaxHeight =
                settings.m_promotedRoomMaxHeight;
            result.m_promotedRoomMaxAspectGap =
                settings.m_promotedRoomMaxAspectGap;
            result.m_promotedRoomMaxOuterOpenNeighbors =
                settings.m_promotedRoomMaxOuterOpenNeighbors;

            result.m_useFourFoldRotationalSymmetry =
                settings.m_useFourFoldRotationalSymmetry;
        }

        int size = Mathf.Max(
            result.m_width,
            result.m_height);

        size = Mathf.Max(
            size,
            s_minimumMapSize);

        if ((size & 1) == 0)
        {
            size++;
        }

        result.m_width = size;
        result.m_height = size;
        result.m_useFourFoldRotationalSymmetry = true;

        return result;
    }

    private static System.Random CreateRandom(
        MazeGenerationSettings settings,
        out int usedSeed,
        out string seedSource)
    {
#if MAZE_GENERATOR_USE_RANDOM_SEED
        usedSeed = CreateNonDeterministicSeed();
        seedSource = "Random";
#else
        usedSeed = GetRandomPreapprovedMazeSeed();
        seedSource = "PreapprovedTable";
#endif

        return new System.Random(usedSeed);
    }

    private static int GetRandomPreapprovedMazeSeed()
    {
        if (s_preapprovedMazeSeeds == null
            || s_preapprovedMazeSeeds.Length == 0)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 事前審査済み迷路のseedテーブルが空です。");
        }

        /*
         * UnityEngine.Random は使わない。
         * そのため、この seed 選択自体もゲーム全体の
         * UnityEngine.Random 状態へ影響しない。
         */
        System.Random seedSelectorRandom =
            new System.Random(
                CreateNonDeterministicSeed());

        int index = seedSelectorRandom.Next(
            0,
            s_preapprovedMazeSeeds.Length);

        return s_preapprovedMazeSeeds[index];
    }

    private static int CreateNonDeterministicSeed()
    {
        return Guid.NewGuid().GetHashCode();
    }

    private static void InitializeRoomIdMap(
        int[,] roomIdMap)
    {
        int width = roomIdMap.GetLength(0);
        int height = roomIdMap.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                roomIdMap[x, y] = -1;
            }
        }
    }

    private static void CreateCentralRoom(
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        int center = width / 2;
        int halfSize = s_centerRoomSize / 2;

        RoomNode roomNode = new RoomNode();
        roomNode.m_roomId = nextRoomId;
        roomNode.m_center = new Vector2Int(center, center);

        nextRoomId++;

        CreateRoom(
            roomNode,
            center - halfSize,
            center - halfSize,
            s_centerRoomSize,
            s_centerRoomSize,
            openMap,
            roomMap,
            roomIdMap,
            protectedWallMap);

        AddDoor(
            roomNode,
            new Vector2Int(center, center + 3),
            doorMap);

        AddDoor(
            roomNode,
            new Vector2Int(center - 3, center),
            doorMap);

        AddDoor(
            roomNode,
            new Vector2Int(center, center - 3),
            doorMap);

        AddDoor(
            roomNode,
            new Vector2Int(center + 3, center),
            doorMap);

        roomNodes.Add(roomNode);
    }

    private static void CreateInitialSmallRoomSets(
        System.Random random,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        for (int setIndex = 0;
             setIndex < s_initialSmallRoomSetCount;
             setIndex++)
        {
            Vector2Int sourceCenter;

            if (TryFindInitialSmallRoomSetPlacement(
                    random,
                    width,
                    protectedWallMap,
                    out sourceCenter) == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 初期小部屋セットの配置場所が見つかりません。");
            }

            CreateFourFoldSmallRoomSet(
                sourceCenter,
                width,
                ref nextRoomId,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap,
                roomNodes);
        }
    }

    private static bool TryFindInitialSmallRoomSetPlacement(
        System.Random random,
        int width,
        bool[,] protectedWallMap,
        out Vector2Int sourceCenter)
    {
        int center = width / 2;

        List<Vector2Int> candidates =
            new List<Vector2Int>();

        for (int y = center + 4;
             y <= width - 7;
             y += 2)
        {
            for (int x = center + 5;
                 x <= width - 6;
                 x += 2)
            {
                candidates.Add(new Vector2Int(x, y));
            }
        }

        ShuffleList(random, candidates);

        for (int index = 0;
             index < candidates.Count;
             index++)
        {
            if (CanReserveFourFoldRoomSet(
                    candidates[index],
                    width,
                    protectedWallMap))
            {
                sourceCenter = candidates[index];
                return true;
            }
        }

        sourceCenter = Vector2Int.zero;
        return false;
    }

    private static bool CanReserveFourFoldRoomSet(
        Vector2Int sourceCenter,
        int width,
        bool[,] protectedWallMap)
    {
        for (int rotation = 0; rotation < 4; rotation++)
        {
            Vector2Int roomCenter = Rotate(
                sourceCenter,
                width,
                rotation);

            if (CanReserveRoomArea(
                    roomCenter,
                    s_smallRoomSize,
                    width,
                    protectedWallMap) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanReserveRoomArea(
        Vector2Int center,
        int roomSize,
        int width,
        bool[,] protectedWallMap)
    {
        int halfSize = roomSize / 2;

        int minX = center.x - halfSize - s_roomWallMargin;
        int maxX = center.x + halfSize + s_roomWallMargin;
        int minY = center.y - halfSize - s_roomWallMargin;
        int maxY = center.y + halfSize + s_roomWallMargin;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (x <= 0
                    || x >= width - 1
                    || y <= 0
                    || y >= width - 1)
                {
                    return false;
                }

                if (protectedWallMap[x, y])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void CreateFourFoldSmallRoomSet(
        Vector2Int sourceCenter,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        for (int rotation = 0; rotation < 4; rotation++)
        {
            Vector2Int roomCenter = Rotate(
                sourceCenter,
                width,
                rotation);

            Vector2Int doorDirection = RotateDirection(
                Vector2Int.right,
                rotation);

            Vector2Int doorCell = roomCenter
                + doorDirection * 2;

            RoomNode roomNode = new RoomNode();
            roomNode.m_roomId = nextRoomId;
            roomNode.m_center = roomCenter;

            nextRoomId++;

            int halfSize = s_smallRoomSize / 2;

            CreateRoom(
                roomNode,
                roomCenter.x - halfSize,
                roomCenter.y - halfSize,
                s_smallRoomSize,
                s_smallRoomSize,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap);

            AddDoor(
                roomNode,
                doorCell,
                doorMap);

            roomNodes.Add(roomNode);
        }
    }

    private static void CreateRoom(
        RoomNode roomNode,
        int minX,
        int minY,
        int roomWidth,
        int roomHeight,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap)
    {
        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        int maxX = minX + roomWidth - 1;
        int maxY = minY + roomHeight - 1;

        for (int y = minY - s_roomWallMargin;
             y <= maxY + s_roomWallMargin;
             y++)
        {
            for (int x = minX - s_roomWallMargin;
                 x <= maxX + s_roomWallMargin;
                 x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                {
                    continue;
                }

                protectedWallMap[x, y] = true;
            }
        }

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                openMap[x, y] = true;
                roomMap[x, y] = true;
                roomIdMap[x, y] = roomNode.m_roomId;
            }
        }
    }

    private static void AddDoor(
        RoomNode roomNode,
        Vector2Int doorCell,
        bool[,] doorMap)
    {
        roomNode.m_doorCells.Add(doorCell);
        doorMap[doorCell.x, doorCell.y] = true;
    }

    private static void CreateCoarseNodes(
        int width,
        bool[,] protectedWallMap,
        out List<Vector2Int> coarseNodes,
        out Dictionary<Vector2Int, int> coarseNodeIndices)
    {
        coarseNodes = new List<Vector2Int>();
        coarseNodeIndices =
            new Dictionary<Vector2Int, int>();

        for (int y = s_coarseFirstCoordinate;
             y < width - 1;
             y += s_coarseStep)
        {
            for (int x = s_coarseFirstCoordinate;
                 x < width - 1;
                 x += s_coarseStep)
            {
                if (protectedWallMap[x, y])
                {
                    continue;
                }

                Vector2Int node = new Vector2Int(x, y);

                coarseNodeIndices.Add(
                    node,
                    coarseNodes.Count);

                coarseNodes.Add(node);
            }
        }
    }

    private static HashSet<Vector2Int> CreateTerminalNodes(
        int width,
        List<RoomNode> roomNodes)
    {
        HashSet<Vector2Int> result =
            new HashSet<Vector2Int>();

        int center = width / 2;

        result.Add(new Vector2Int(center, center + 4));
        result.Add(new Vector2Int(center - 4, center));
        result.Add(new Vector2Int(center, center - 4));
        result.Add(new Vector2Int(center + 4, center));

        result.Add(new Vector2Int(center, width - 3));
        result.Add(new Vector2Int(2, center));
        result.Add(new Vector2Int(center, 2));
        result.Add(new Vector2Int(width - 3, center));

        for (int roomIndex = 1;
             roomIndex < roomNodes.Count;
             roomIndex++)
        {
            RoomNode roomNode = roomNodes[roomIndex];

            if (roomNode.m_doorCells.Count != 1)
            {
                continue;
            }

            Vector2Int door = roomNode.m_doorCells[0];

            Vector2Int direction = new Vector2Int(
                Math.Sign(door.x - roomNode.m_center.x),
                Math.Sign(door.y - roomNode.m_center.y));

            result.Add(roomNode.m_center + direction * 3);
        }

        return result;
    }

    private static void ValidateTerminalNodes(
        HashSet<Vector2Int> terminalNodes,
        Dictionary<Vector2Int, int> coarseNodeIndices)
    {
        foreach (Vector2Int terminalNode in terminalNodes)
        {
            if (coarseNodeIndices.ContainsKey(terminalNode) == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 粗い格子に存在しない終端があります。 terminal="
                    + terminalNode);
            }
        }
    }

    private static List<CoarseEdgeOrbit> CreateAllUsableCoarseEdgeOrbits(
        int width,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        Dictionary<Vector2Int, int> coarseNodeIndices)
    {
        Dictionary<string, CoarseEdgeOrbit> orbitMap =
            new Dictionary<string, CoarseEdgeOrbit>();

        Vector2Int[] directions =
        {
            Vector2Int.right * s_coarseStep,
            Vector2Int.up * s_coarseStep
        };

        foreach (KeyValuePair<Vector2Int, int> pair
            in coarseNodeIndices)
        {
            Vector2Int node = pair.Key;

            for (int directionIndex = 0;
                 directionIndex < directions.Length;
                 directionIndex++)
            {
                Vector2Int neighbor = node
                    + directions[directionIndex];

                if (coarseNodeIndices.ContainsKey(neighbor) == false)
                {
                    continue;
                }

                AddCoarseEdgeOrbitIfUsable(
                    node,
                    neighbor,
                    width,
                    protectedWallMap,
                    doorMap,
                    coarseNodeIndices,
                    orbitMap);
            }
        }

        List<CoarseEdgeOrbit> result =
            new List<CoarseEdgeOrbit>();

        foreach (KeyValuePair<string, CoarseEdgeOrbit> pair
            in orbitMap)
        {
            result.Add(pair.Value);
        }

        return result;
    }

    private static void AddCoarseEdgeOrbitIfUsable(
        Vector2Int sourceA,
        Vector2Int sourceB,
        int width,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        Dictionary<Vector2Int, int> coarseNodeIndices,
        Dictionary<string, CoarseEdgeOrbit> orbitMap)
    {
        List<CoarseEdge> orbitEdges =
            new List<CoarseEdge>();

        for (int rotation = 0; rotation < 4; rotation++)
        {
            CoarseEdge edge = new CoarseEdge(
                Rotate(sourceA, width, rotation),
                Rotate(sourceB, width, rotation));

            if (coarseNodeIndices.ContainsKey(edge.m_a) == false
                || coarseNodeIndices.ContainsKey(edge.m_b) == false)
            {
                return;
            }

            if (CanCarveCoarseEdge(
                    edge,
                    protectedWallMap,
                    doorMap) == false)
            {
                return;
            }

            if (ContainsCoarseEdge(
                    orbitEdges,
                    edge) == false)
            {
                orbitEdges.Add(edge);
            }
        }

        string key = GetCoarseEdgeOrbitKey(orbitEdges);

        if (orbitMap.ContainsKey(key))
        {
            return;
        }

        CoarseEdgeOrbit orbit = new CoarseEdgeOrbit();
        orbit.m_key = key;
        orbit.m_edges = orbitEdges;

        orbitMap.Add(key, orbit);
    }

    private static bool CanCarveCoarseEdge(
        CoarseEdge edge,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        int deltaX = edge.m_b.x - edge.m_a.x;
        int deltaY = edge.m_b.y - edge.m_a.y;

        if (deltaX != 0 && deltaY != 0)
        {
            return false;
        }

        int stepX = Math.Sign(deltaX);
        int stepY = Math.Sign(deltaY);
        int length = Mathf.Abs(deltaX) + Mathf.Abs(deltaY);

        for (int step = 0; step <= length; step++)
        {
            int x = edge.m_a.x + stepX * step;
            int y = edge.m_a.y + stepY * step;

            if (protectedWallMap[x, y]
                && doorMap[x, y] == false)
            {
                return false;
            }
        }

        return true;
    }

    private static List<CoarseEdge> CreateRandomFourFoldCoarseGraph(
        System.Random random,
        Dictionary<Vector2Int, int> coarseNodeIndices,
        List<CoarseEdgeOrbit> edgeOrbits)
    {
        if (coarseNodeIndices.Count == 0)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 粗い格子ノードがありません。");
        }

        ShuffleList(random, edgeOrbits);

        DisjointSet disjointSet = new DisjointSet(
            coarseNodeIndices.Count);

        HashSet<string> selectedOrbitKeys =
            new HashSet<string>();

        List<CoarseEdge> selectedEdges =
            new List<CoarseEdge>();

        for (int orbitIndex = 0;
             orbitIndex < edgeOrbits.Count;
             orbitIndex++)
        {
            CoarseEdgeOrbit orbit = edgeOrbits[orbitIndex];

            if (CanAddOrbitWithoutCycle(
                    orbit,
                    coarseNodeIndices,
                    disjointSet))
            {
                AddOrbit(
                    orbit,
                    coarseNodeIndices,
                    disjointSet,
                    selectedEdges);

                selectedOrbitKeys.Add(orbit.m_key);
            }
        }

        int safetyCount = 0;

        while (disjointSet.ComponentCount > 1
            && safetyCount < edgeOrbits.Count)
        {
            bool added = false;

            for (int orbitIndex = 0;
                 orbitIndex < edgeOrbits.Count;
                 orbitIndex++)
            {
                CoarseEdgeOrbit orbit = edgeOrbits[orbitIndex];

                if (selectedOrbitKeys.Contains(orbit.m_key))
                {
                    continue;
                }

                if (WouldOrbitReduceComponents(
                        orbit,
                        coarseNodeIndices,
                        disjointSet))
                {
                    AddOrbit(
                        orbit,
                        coarseNodeIndices,
                        disjointSet,
                        selectedEdges);

                    selectedOrbitKeys.Add(orbit.m_key);
                    added = true;
                    break;
                }
            }

            if (added == false)
            {
                break;
            }

            safetyCount++;
        }

        if (disjointSet.ComponentCount > 1)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 粗い格子を連結できませんでした。");
        }

        List<CoarseEdgeOrbit> loopCandidates =
            new List<CoarseEdgeOrbit>();

        for (int orbitIndex = 0;
             orbitIndex < edgeOrbits.Count;
             orbitIndex++)
        {
            CoarseEdgeOrbit orbit = edgeOrbits[orbitIndex];

            if (selectedOrbitKeys.Contains(orbit.m_key) == false)
            {
                loopCandidates.Add(orbit);
            }
        }

        ShuffleList(random, loopCandidates);

        int loopOrbitCount = random.Next(
            s_minExtraLoopOrbitCount,
            s_maxExtraLoopOrbitCount + 1);

        for (int loopIndex = 0;
             loopIndex < loopOrbitCount
             && loopIndex < loopCandidates.Count;
             loopIndex++)
        {
            AddOrbit(
                loopCandidates[loopIndex],
                coarseNodeIndices,
                disjointSet,
                selectedEdges);
        }

        return selectedEdges;
    }

    private static bool CanAddOrbitWithoutCycle(
        CoarseEdgeOrbit orbit,
        Dictionary<Vector2Int, int> coarseNodeIndices,
        DisjointSet disjointSet)
    {
        DisjointSet temporary = disjointSet.Clone();

        for (int edgeIndex = 0;
             edgeIndex < orbit.m_edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = orbit.m_edges[edgeIndex];

            int a = coarseNodeIndices[edge.m_a];
            int b = coarseNodeIndices[edge.m_b];

            if (temporary.Union(a, b) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static bool WouldOrbitReduceComponents(
        CoarseEdgeOrbit orbit,
        Dictionary<Vector2Int, int> coarseNodeIndices,
        DisjointSet disjointSet)
    {
        for (int edgeIndex = 0;
             edgeIndex < orbit.m_edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = orbit.m_edges[edgeIndex];

            int a = coarseNodeIndices[edge.m_a];
            int b = coarseNodeIndices[edge.m_b];

            if (disjointSet.Find(a) != disjointSet.Find(b))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddOrbit(
        CoarseEdgeOrbit orbit,
        Dictionary<Vector2Int, int> coarseNodeIndices,
        DisjointSet disjointSet,
        List<CoarseEdge> selectedEdges)
    {
        for (int edgeIndex = 0;
             edgeIndex < orbit.m_edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = orbit.m_edges[edgeIndex];

            int a = coarseNodeIndices[edge.m_a];
            int b = coarseNodeIndices[edge.m_b];

            disjointSet.Union(a, b);
            selectedEdges.Add(edge);
        }
    }

    private static List<CoarseEdge> PruneUnnecessaryBranches(
        List<CoarseEdge> sourceEdges,
        List<Vector2Int> coarseNodes,
        HashSet<Vector2Int> terminalNodes)
    {
        HashSet<Vector2Int> removedNodes =
            new HashSet<Vector2Int>();

        bool removedAtLeastOneNode = true;

        while (removedAtLeastOneNode)
        {
            removedAtLeastOneNode = false;

            Dictionary<Vector2Int, int> degreeMap =
                CreateDegreeMap(
                    sourceEdges,
                    coarseNodes,
                    removedNodes);

            List<Vector2Int> nodesToRemove =
                new List<Vector2Int>();

            for (int nodeIndex = 0;
                 nodeIndex < coarseNodes.Count;
                 nodeIndex++)
            {
                Vector2Int node = coarseNodes[nodeIndex];

                if (removedNodes.Contains(node)
                    || terminalNodes.Contains(node))
                {
                    continue;
                }

                if (degreeMap[node] <= 1)
                {
                    nodesToRemove.Add(node);
                }
            }

            for (int index = 0;
                 index < nodesToRemove.Count;
                 index++)
            {
                removedNodes.Add(nodesToRemove[index]);
                removedAtLeastOneNode = true;
            }
        }

        List<CoarseEdge> result =
            new List<CoarseEdge>();

        for (int edgeIndex = 0;
             edgeIndex < sourceEdges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = sourceEdges[edgeIndex];

            if (removedNodes.Contains(edge.m_a)
                || removedNodes.Contains(edge.m_b))
            {
                continue;
            }

            result.Add(edge);
        }

        return result;
    }

    private static Dictionary<Vector2Int, int> CreateDegreeMap(
        List<CoarseEdge> edges,
        List<Vector2Int> coarseNodes,
        HashSet<Vector2Int> removedNodes)
    {
        Dictionary<Vector2Int, int> result =
            new Dictionary<Vector2Int, int>();

        for (int index = 0;
             index < coarseNodes.Count;
             index++)
        {
            Vector2Int node = coarseNodes[index];

            if (removedNodes.Contains(node) == false)
            {
                result.Add(node, 0);
            }
        }

        for (int edgeIndex = 0;
             edgeIndex < edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = edges[edgeIndex];

            if (removedNodes.Contains(edge.m_a)
                || removedNodes.Contains(edge.m_b))
            {
                continue;
            }

            result[edge.m_a]++;
            result[edge.m_b]++;
        }

        return result;
    }

    private static void ValidateCoarseGraphConnection(
        List<CoarseEdge> edges,
        List<Vector2Int> coarseNodes,
        HashSet<Vector2Int> terminalNodes)
    {
        Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap =
            CreateAdjacencyMap(edges, coarseNodes);

        Vector2Int start = Vector2Int.zero;
        bool foundStart = false;

        foreach (Vector2Int terminalNode in terminalNodes)
        {
            start = terminalNode;
            foundStart = true;
            break;
        }

        if (foundStart == false)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 終端ノードがありません。");
        }

        HashSet<Vector2Int> visitedNodes =
            CreateVisitedNodes(
                start,
                adjacencyMap);

        foreach (Vector2Int terminalNode in terminalNodes)
        {
            if (visitedNodes.Contains(terminalNode) == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 枝削除後に接続されていない終端があります。 terminal="
                    + terminalNode);
            }
        }
    }

    /*
     * 中央部屋へ接続する4ノードを除外しても、
     * 上・左・下・右の入口ノードが全て連結していることを検査する。
     *
     * これにより、中央だけを経由する4本の独立ルートを不採用にする。
     */
    private static void ValidatePreCenterSharedRoutes(
        int width,
        List<CoarseEdge> edges)
    {
        int center = width / 2;

        Vector2Int[] entrances =
        {
            new Vector2Int(center, width - 3),
            new Vector2Int(2, center),
            new Vector2Int(center, 2),
            new Vector2Int(width - 3, center)
        };

        HashSet<Vector2Int> centerGateNodes =
            new HashSet<Vector2Int>();

        centerGateNodes.Add(
            new Vector2Int(center, center + 4));

        centerGateNodes.Add(
            new Vector2Int(center - 4, center));

        centerGateNodes.Add(
            new Vector2Int(center, center - 4));

        centerGateNodes.Add(
            new Vector2Int(center + 4, center));

        List<Vector2Int> nodes = CreateNodesFromEdges(edges);

        Dictionary<Vector2Int, List<Vector2Int>> preCenterAdjacencyMap =
            CreateAdjacencyMapExcludingNodes(
                edges,
                nodes,
                centerGateNodes);

        HashSet<Vector2Int> visited =
            CreateVisitedNodes(
                entrances[0],
                preCenterAdjacencyMap);

        for (int entranceIndex = 1;
             entranceIndex < entrances.Length;
             entranceIndex++)
        {
            if (visited.Contains(entrances[entranceIndex]) == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 中央以外で入口間が接続されていません。 "
                    + "中央のみが遭遇地点になる迷路を除外しました。");
            }
        }

        Dictionary<Vector2Int, int> distanceFromCenterGates =
            CreateDistanceMapFromMultipleStarts(
                centerGateNodes,
                CreateAdjacencyMap(edges, nodes));

        bool foundSharedJunction = false;

        foreach (KeyValuePair<Vector2Int, List<Vector2Int>> pair
            in preCenterAdjacencyMap)
        {
            Vector2Int node = pair.Key;

            if (pair.Value.Count < 3)
            {
                continue;
            }

            int distanceFromCenter;

            if (distanceFromCenterGates.TryGetValue(
                    node,
                    out distanceFromCenter) == false)
            {
                continue;
            }

            if (distanceFromCenter
                < s_minimumSharedJunctionDistanceFromCenter)
            {
                continue;
            }

            foundSharedJunction = true;
            break;
        }

        if (foundSharedJunction == false)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 中央外に十分離れた共有分岐がありません。 "
                + "中央だけが混戦地点になる迷路を除外しました。");
        }
    }

    private static Dictionary<Vector2Int, List<Vector2Int>>
        CreateAdjacencyMapExcludingNodes(
            List<CoarseEdge> edges,
            List<Vector2Int> nodes,
            HashSet<Vector2Int> excludedNodes)
    {
        Dictionary<Vector2Int, List<Vector2Int>> result =
            new Dictionary<Vector2Int, List<Vector2Int>>();

        for (int index = 0;
             index < nodes.Count;
             index++)
        {
            Vector2Int node = nodes[index];

            if (excludedNodes.Contains(node) == false)
            {
                result.Add(
                    node,
                    new List<Vector2Int>());
            }
        }

        for (int edgeIndex = 0;
             edgeIndex < edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = edges[edgeIndex];

            if (excludedNodes.Contains(edge.m_a)
                || excludedNodes.Contains(edge.m_b))
            {
                continue;
            }

            if (result.ContainsKey(edge.m_a) == false
                || result.ContainsKey(edge.m_b) == false)
            {
                continue;
            }

            result[edge.m_a].Add(edge.m_b);
            result[edge.m_b].Add(edge.m_a);
        }

        return result;
    }

    private static Dictionary<Vector2Int, int>
        CreateDistanceMapFromMultipleStarts(
            HashSet<Vector2Int> starts,
            Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap)
    {
        Dictionary<Vector2Int, int> distanceMap =
            new Dictionary<Vector2Int, int>();

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        foreach (Vector2Int start in starts)
        {
            if (adjacencyMap.ContainsKey(start) == false)
            {
                continue;
            }

            if (distanceMap.ContainsKey(start))
            {
                continue;
            }

            distanceMap.Add(start, 0);
            queue.Enqueue(start);
        }

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            int currentDistance = distanceMap[current];

            List<Vector2Int> neighbors = adjacencyMap[current];

            for (int index = 0;
                 index < neighbors.Count;
                 index++)
            {
                Vector2Int next = neighbors[index];

                if (distanceMap.ContainsKey(next))
                {
                    continue;
                }

                distanceMap.Add(
                    next,
                    currentDistance + 1);

                queue.Enqueue(next);
            }
        }

        return distanceMap;
    }

    private static void ValidateCenterRouteQuality(
        int width,
        List<CoarseEdge> edges)
    {
        List<Vector2Int> nodes =
            CreateNodesFromEdges(edges);

        Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap =
            CreateAdjacencyMap(edges, nodes);

        int center = width / 2;

        Vector2Int entrance = new Vector2Int(
            center,
            width - 3);

        HashSet<Vector2Int> goals =
            new HashSet<Vector2Int>();

        goals.Add(new Vector2Int(center, center + 4));
        goals.Add(new Vector2Int(center - 4, center));
        goals.Add(new Vector2Int(center, center - 4));
        goals.Add(new Vector2Int(center + 4, center));

        List<Vector2Int> path =
            FindShortestPathToAnyGoal(
                entrance,
                goals,
                adjacencyMap);

        if (path == null || path.Count < 2)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 入口から中央への経路がありません。");
        }

        int stepCount = path.Count - 1;
        int turnCount = CountTurns(path);
        int maximumStraightRun = GetMaximumStraightRun(path);

        if (stepCount < s_minimumEntranceToCenterStepCount)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 中央への経路が短すぎます。 steps="
                + stepCount);
        }

        if (turnCount < s_minimumEntranceToCenterTurnCount)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 中央への経路が直線的すぎます。 turns="
                + turnCount);
        }

        if (maximumStraightRun > s_maximumStraightRunOnCenterPath)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 中央への経路に長い直線があります。 run="
                + maximumStraightRun);
        }
    }

    private static List<Vector2Int> CreateNodesFromEdges(
        List<CoarseEdge> edges)
    {
        HashSet<Vector2Int> nodeSet =
            new HashSet<Vector2Int>();

        for (int edgeIndex = 0;
             edgeIndex < edges.Count;
             edgeIndex++)
        {
            nodeSet.Add(edges[edgeIndex].m_a);
            nodeSet.Add(edges[edgeIndex].m_b);
        }

        List<Vector2Int> result =
            new List<Vector2Int>();

        foreach (Vector2Int node in nodeSet)
        {
            result.Add(node);
        }

        return result;
    }

    private static Dictionary<Vector2Int, List<Vector2Int>> CreateAdjacencyMap(
        List<CoarseEdge> edges,
        List<Vector2Int> nodes)
    {
        Dictionary<Vector2Int, List<Vector2Int>> result =
            new Dictionary<Vector2Int, List<Vector2Int>>();

        for (int index = 0;
             index < nodes.Count;
             index++)
        {
            if (result.ContainsKey(nodes[index]) == false)
            {
                result.Add(
                    nodes[index],
                    new List<Vector2Int>());
            }
        }

        for (int edgeIndex = 0;
             edgeIndex < edges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = edges[edgeIndex];

            if (result.ContainsKey(edge.m_a) == false)
            {
                result.Add(
                    edge.m_a,
                    new List<Vector2Int>());
            }

            if (result.ContainsKey(edge.m_b) == false)
            {
                result.Add(
                    edge.m_b,
                    new List<Vector2Int>());
            }

            result[edge.m_a].Add(edge.m_b);
            result[edge.m_b].Add(edge.m_a);
        }

        return result;
    }

    private static HashSet<Vector2Int> CreateVisitedNodes(
        Vector2Int start,
        Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap)
    {
        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        if (adjacencyMap.ContainsKey(start) == false)
        {
            return visited;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            List<Vector2Int> neighbors = adjacencyMap[current];

            for (int index = 0;
                 index < neighbors.Count;
                 index++)
            {
                if (visited.Add(neighbors[index]))
                {
                    queue.Enqueue(neighbors[index]);
                }
            }
        }

        return visited;
    }

    private static List<Vector2Int> FindShortestPathToAnyGoal(
        Vector2Int start,
        HashSet<Vector2Int> goals,
        Dictionary<Vector2Int, List<Vector2Int>> adjacencyMap)
    {
        if (adjacencyMap.ContainsKey(start) == false)
        {
            return null;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        HashSet<Vector2Int> visited =
            new HashSet<Vector2Int>();

        Dictionary<Vector2Int, Vector2Int> parentMap =
            new Dictionary<Vector2Int, Vector2Int>();

        visited.Add(start);
        queue.Enqueue(start);

        Vector2Int foundGoal = Vector2Int.zero;
        bool found = false;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            if (goals.Contains(current))
            {
                foundGoal = current;
                found = true;
                break;
            }

            List<Vector2Int> neighbors = adjacencyMap[current];

            for (int index = 0;
                 index < neighbors.Count;
                 index++)
            {
                Vector2Int next = neighbors[index];

                if (visited.Add(next))
                {
                    parentMap.Add(next, current);
                    queue.Enqueue(next);
                }
            }
        }

        if (found == false)
        {
            return null;
        }

        List<Vector2Int> result =
            new List<Vector2Int>();

        Vector2Int cursor = foundGoal;
        result.Add(cursor);

        while (cursor != start)
        {
            cursor = parentMap[cursor];
            result.Add(cursor);
        }

        result.Reverse();

        return result;
    }

    private static int CountTurns(
        List<Vector2Int> path)
    {
        if (path.Count < 3)
        {
            return 0;
        }

        int result = 0;

        Vector2Int previousDirection =
            GetCoarseDirection(
                path[0],
                path[1]);

        for (int index = 2;
             index < path.Count;
             index++)
        {
            Vector2Int direction =
                GetCoarseDirection(
                    path[index - 1],
                    path[index]);

            if (direction != previousDirection)
            {
                result++;
            }

            previousDirection = direction;
        }

        return result;
    }

    private static int GetMaximumStraightRun(
        List<Vector2Int> path)
    {
        if (path.Count < 2)
        {
            return 0;
        }

        int maximumRun = 1;
        int currentRun = 1;

        Vector2Int previousDirection =
            GetCoarseDirection(
                path[0],
                path[1]);

        for (int index = 2;
             index < path.Count;
             index++)
        {
            Vector2Int direction =
                GetCoarseDirection(
                    path[index - 1],
                    path[index]);

            if (direction == previousDirection)
            {
                currentRun++;
            }
            else
            {
                currentRun = 1;
            }

            maximumRun = Mathf.Max(
                maximumRun,
                currentRun);

            previousDirection = direction;
        }

        return maximumRun;
    }

    private static Vector2Int GetCoarseDirection(
        Vector2Int from,
        Vector2Int to)
    {
        return new Vector2Int(
            Math.Sign(to.x - from.x),
            Math.Sign(to.y - from.y));
    }

    private static void CarveCentralRoomConnections(
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        int center = width / 2;

        Vector2Int[] doors =
        {
            new Vector2Int(center, center + 3),
            new Vector2Int(center - 3, center),
            new Vector2Int(center, center - 3),
            new Vector2Int(center + 3, center)
        };

        Vector2Int[] outerNodes =
        {
            new Vector2Int(center, center + 4),
            new Vector2Int(center - 4, center),
            new Vector2Int(center, center - 4),
            new Vector2Int(center + 4, center)
        };

        for (int index = 0;
             index < doors.Length;
             index++)
        {
            CarveCorridorLine(
                doors[index],
                outerNodes[index],
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap);
        }
    }

    private static void CarveSmallRoomConnections(
        List<RoomNode> roomNodes,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        for (int roomIndex = 1;
             roomIndex < roomNodes.Count;
             roomIndex++)
        {
            RoomNode roomNode = roomNodes[roomIndex];

            if (roomNode.m_doorCells.Count != 1)
            {
                continue;
            }

            Vector2Int door = roomNode.m_doorCells[0];

            Vector2Int direction = new Vector2Int(
                Math.Sign(door.x - roomNode.m_center.x),
                Math.Sign(door.y - roomNode.m_center.y));

            Vector2Int outsideNode = roomNode.m_center
                + direction * 3;

            CarveCorridorLine(
                door,
                outsideNode,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap);
        }
    }

    private static void CarveCoarseGraph(
        List<CoarseEdge> selectedEdges,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        for (int edgeIndex = 0;
             edgeIndex < selectedEdges.Count;
             edgeIndex++)
        {
            CoarseEdge edge = selectedEdges[edgeIndex];

            CarveCorridorLine(
                edge.m_a,
                edge.m_b,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap);
        }
    }

    private static void CreateOuterEntranceLines(
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        int center = width / 2;
        int innerEdge = width - 3;

        Vector2Int[] entranceCells =
        {
            new Vector2Int(center, width - 1),
            new Vector2Int(0, center),
            new Vector2Int(center, 0),
            new Vector2Int(width - 1, center)
        };

        Vector2Int[] innerNodes =
        {
            new Vector2Int(center, innerEdge),
            new Vector2Int(2, center),
            new Vector2Int(center, 2),
            new Vector2Int(innerEdge, center)
        };

        for (int index = 0;
             index < entranceCells.Length;
             index++)
        {
            CarveCorridorLine(
                entranceCells[index],
                innerNodes[index],
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap);
        }
    }

    private static int CreateAdditionalRoomSetsInEmptySpace(
        System.Random random,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        int targetSetCount = random.Next(
            s_minAdditionalRoomSetCount,
            s_maxAdditionalRoomSetCount + 1);

        int createdSetCount = 0;

        for (int setIndex = 0;
             setIndex < targetSetCount;
             setIndex++)
        {
            AdditionalRoomPlan sourcePlan;

            if (TryFindAdditionalRoomPlan(
                    random,
                    width,
                    openMap,
                    roomMap,
                    protectedWallMap,
                    out sourcePlan) == false)
            {
                break;
            }

            CreateFourFoldAdditionalRoomSet(
                sourcePlan,
                width,
                ref nextRoomId,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap,
                roomNodes);

            createdSetCount++;
        }

        return createdSetCount;
    }

    private static bool TryFindAdditionalRoomPlan(
        System.Random random,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap,
        out AdditionalRoomPlan result)
    {
        int center = width / 2;

        List<Vector2Int> roomCenters =
            new List<Vector2Int>();

        for (int y = center + 4;
             y <= width - 7;
             y += 2)
        {
            for (int x = center + 5;
                 x <= width - 6;
                 x += 2)
            {
                roomCenters.Add(new Vector2Int(x, y));
            }
        }

        ShuffleList(random, roomCenters);

        Vector2Int[] directions =
        {
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down
        };

        for (int centerIndex = 0;
             centerIndex < roomCenters.Count;
             centerIndex++)
        {
            List<Vector2Int> shuffledDirections =
                new List<Vector2Int>(directions);

            ShuffleList(random, shuffledDirections);

            for (int directionIndex = 0;
                 directionIndex < shuffledDirections.Count;
                 directionIndex++)
            {
                AdditionalRoomPlan plan;

                if (TryCreateAdditionalRoomPlan(
                        roomCenters[centerIndex],
                        shuffledDirections[directionIndex],
                        width,
                        openMap,
                        roomMap,
                        protectedWallMap,
                        out plan)
                    && CanCreateFourFoldAdditionalRoomSet(
                        plan,
                        width,
                        openMap,
                        roomMap,
                        protectedWallMap))
                {
                    result = plan;
                    return true;
                }
            }
        }

        result = null;
        return false;
    }

    private static bool TryCreateAdditionalRoomPlan(
        Vector2Int roomCenter,
        Vector2Int doorDirection,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap,
        out AdditionalRoomPlan result)
    {
        Vector2Int door = roomCenter
            + doorDirection * 2;

        List<Vector2Int> corridorCells =
            new List<Vector2Int>();

        int center = width / 2;

        for (int y = center + 1;
             y < width - 1;
             y++)
        {
            for (int x = center + 1;
                 x < width - 1;
                 x++)
            {
                if (openMap[x, y]
                    && roomMap[x, y] == false)
                {
                    corridorCells.Add(new Vector2Int(x, y));
                }
            }
        }

        for (int targetIndex = 0;
             targetIndex < corridorCells.Count;
             targetIndex++)
        {
            List<Vector2Int> path;

            if (TryCreateOneTurnPath(
                    door,
                    doorDirection,
                    corridorCells[targetIndex],
                    out path) == false)
            {
                continue;
            }

            if (CanUseAdditionalConnectorPath(
                    path,
                    openMap,
                    roomMap,
                    protectedWallMap))
            {
                result = new AdditionalRoomPlan();
                result.m_roomCenter = roomCenter;
                result.m_doorDirection = doorDirection;
                result.m_connectorPath = path;

                return true;
            }
        }

        result = null;
        return false;
    }

    private static bool TryCreateOneTurnPath(
        Vector2Int door,
        Vector2Int firstDirection,
        Vector2Int target,
        out List<Vector2Int> path)
    {
        path = new List<Vector2Int>();

        Vector2Int elbow;

        if (firstDirection.x != 0)
        {
            if ((target.x - door.x) * firstDirection.x < 0)
            {
                return false;
            }

            elbow = new Vector2Int(
                target.x,
                door.y);
        }
        else
        {
            if ((target.y - door.y) * firstDirection.y < 0)
            {
                return false;
            }

            elbow = new Vector2Int(
                door.x,
                target.y);
        }

        AddLineCells(
            door,
            elbow,
            path,
            true);

        AddLineCells(
            elbow,
            target,
            path,
            false);

        return path.Count >= 2;
    }

    private static void AddLineCells(
        Vector2Int start,
        Vector2Int end,
        List<Vector2Int> result,
        bool includeStart)
    {
        int stepX = Math.Sign(end.x - start.x);
        int stepY = Math.Sign(end.y - start.y);
        int length = Mathf.Abs(end.x - start.x)
            + Mathf.Abs(end.y - start.y);

        int startStep = includeStart ? 0 : 1;

        for (int step = startStep;
             step <= length;
             step++)
        {
            Vector2Int cell = new Vector2Int(
                start.x + stepX * step,
                start.y + stepY * step);

            if (result.Count == 0
                || result[result.Count - 1] != cell)
            {
                result.Add(cell);
            }
        }
    }

    private static bool CanUseAdditionalConnectorPath(
        List<Vector2Int> path,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap)
    {
        if (path.Count < 2)
        {
            return false;
        }

        for (int index = 0;
             index < path.Count;
             index++)
        {
            Vector2Int cell = path[index];

            bool isDoor = index == 0;
            bool isExistingCorridor =
                index == path.Count - 1;

            if (isExistingCorridor)
            {
                if (openMap[cell.x, cell.y] == false
                    || roomMap[cell.x, cell.y])
                {
                    return false;
                }

                continue;
            }

            if (isDoor)
            {
                continue;
            }

            if (openMap[cell.x, cell.y]
                || roomMap[cell.x, cell.y]
                || protectedWallMap[cell.x, cell.y])
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanCreateFourFoldAdditionalRoomSet(
        AdditionalRoomPlan sourcePlan,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap)
    {
        for (int rotation = 0;
             rotation < 4;
             rotation++)
        {
            Vector2Int roomCenter = Rotate(
                sourcePlan.m_roomCenter,
                width,
                rotation);

            if (CanReserveAdditionalRoomArea(
                    roomCenter,
                    width,
                    openMap,
                    roomMap,
                    protectedWallMap) == false)
            {
                return false;
            }

            List<Vector2Int> rotatedPath =
                RotatePath(
                    sourcePlan.m_connectorPath,
                    width,
                    rotation);

            if (CanUseAdditionalConnectorPath(
                    rotatedPath,
                    openMap,
                    roomMap,
                    protectedWallMap) == false)
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanReserveAdditionalRoomArea(
        Vector2Int roomCenter,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap)
    {
        int halfSize = s_smallRoomSize / 2;

        for (int y = roomCenter.y - halfSize - s_roomWallMargin;
             y <= roomCenter.y + halfSize + s_roomWallMargin;
             y++)
        {
            for (int x = roomCenter.x - halfSize - s_roomWallMargin;
                 x <= roomCenter.x + halfSize + s_roomWallMargin;
                 x++)
            {
                if (x <= 0
                    || x >= width - 1
                    || y <= 0
                    || y >= width - 1)
                {
                    return false;
                }

                if (openMap[x, y]
                    || roomMap[x, y]
                    || protectedWallMap[x, y])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static List<Vector2Int> RotatePath(
        List<Vector2Int> sourcePath,
        int width,
        int rotation)
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

        for (int index = 0;
             index < sourcePath.Count;
             index++)
        {
            result.Add(
                Rotate(
                    sourcePath[index],
                    width,
                    rotation));
        }

        return result;
    }

    private static void CreateFourFoldAdditionalRoomSet(
        AdditionalRoomPlan sourcePlan,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        for (int rotation = 0;
             rotation < 4;
             rotation++)
        {
            Vector2Int roomCenter = Rotate(
                sourcePlan.m_roomCenter,
                width,
                rotation);

            Vector2Int doorDirection = RotateDirection(
                sourcePlan.m_doorDirection,
                rotation);

            Vector2Int doorCell = roomCenter
                + doorDirection * 2;

            RoomNode roomNode = new RoomNode();
            roomNode.m_roomId = nextRoomId;
            roomNode.m_center = roomCenter;

            nextRoomId++;

            int halfSize = s_smallRoomSize / 2;

            CreateRoom(
                roomNode,
                roomCenter.x - halfSize,
                roomCenter.y - halfSize,
                s_smallRoomSize,
                s_smallRoomSize,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap);

            AddDoor(
                roomNode,
                doorCell,
                doorMap);

            List<Vector2Int> connectorPath =
                RotatePath(
                    sourcePlan.m_connectorPath,
                    width,
                    rotation);

            for (int cellIndex = 0;
                 cellIndex < connectorPath.Count;
                 cellIndex++)
            {
                Vector2Int connectorCell =
                    connectorPath[cellIndex];

                CarveCorridorCell(
                    connectorCell.x,
                    connectorCell.y,
                    openMap,
                    roomMap,
                    roomIdMap,
                    protectedWallMap,
                    doorMap);
            }

            roomNodes.Add(roomNode);
        }
    }

    private static void CarveCorridorLine(
        Vector2Int start,
        Vector2Int end,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        if (start.x != end.x
            && start.y != end.y)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 斜めの通路を作ろうとしました。 start="
                + start
                + " end="
                + end);
        }

        int stepX = Math.Sign(end.x - start.x);
        int stepY = Math.Sign(end.y - start.y);
        int length = Mathf.Abs(end.x - start.x)
            + Mathf.Abs(end.y - start.y);

        for (int step = 0;
             step <= length;
             step++)
        {
            CarveCorridorCell(
                start.x + stepX * step,
                start.y + stepY * step,
                openMap,
                roomMap,
                roomIdMap,
                protectedWallMap,
                doorMap);
        }
    }

    private static void CarveCorridorCell(
        int x,
        int y,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap)
    {
        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            throw new InvalidOperationException(
                "MazeGenerator: マップ外へ通路を作ろうとしました。 x="
                + x
                + " y="
                + y);
        }

        if (roomMap[x, y])
        {
            return;
        }

        if (protectedWallMap[x, y]
            && doorMap[x, y] == false)
        {
            throw new InvalidOperationException(
                "MazeGenerator: 指定扉以外の予約壁を開こうとしました。 x="
                + x
                + " y="
                + y);
        }

        openMap[x, y] = true;
        roomMap[x, y] = false;
        roomIdMap[x, y] = -1;
    }

    private static void ValidateGeneratedMap(
        int width,
        int height,
        bool[,] openMap,
        List<RoomNode> roomNodes)
    {
        Vector2Int center = new Vector2Int(
            width / 2,
            height / 2);

        bool[,] reachableMap = CreateReachableMap(
            center,
            openMap);

        Vector2Int[] entrances =
        {
            new Vector2Int(width / 2, height - 1),
            new Vector2Int(0, height / 2),
            new Vector2Int(width / 2, 0),
            new Vector2Int(width - 1, height / 2)
        };

        for (int index = 0;
             index < entrances.Length;
             index++)
        {
            Vector2Int entrance = entrances[index];

            if (reachableMap[entrance.x, entrance.y] == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 中央から到達できない入口があります。 entrance="
                    + entrance);
            }
        }

        for (int roomIndex = 0;
             roomIndex < roomNodes.Count;
             roomIndex++)
        {
            RoomNode roomNode = roomNodes[roomIndex];

            if (reachableMap[
                    roomNode.m_center.x,
                    roomNode.m_center.y] == false)
            {
                throw new InvalidOperationException(
                    "MazeGenerator: 中央から到達できない部屋があります。 roomId="
                    + roomNode.m_roomId);
            }

            for (int doorIndex = 0;
                 doorIndex < roomNode.m_doorCells.Count;
                 doorIndex++)
            {
                Vector2Int door = roomNode.m_doorCells[doorIndex];

                if (reachableMap[door.x, door.y] == false)
                {
                    throw new InvalidOperationException(
                        "MazeGenerator: 接続されていない部屋の扉があります。 roomId="
                        + roomNode.m_roomId);
                }
            }
        }
    }

    private static bool[,] CreateReachableMap(
        Vector2Int start,
        bool[,] openMap)
    {
        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        bool[,] reachableMap = new bool[width, height];

        if (openMap[start.x, start.y] == false)
        {
            return reachableMap;
        }

        Queue<Vector2Int> queue =
            new Queue<Vector2Int>();

        reachableMap[start.x, start.y] = true;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            TryVisitCell(
                current.x + 1,
                current.y,
                openMap,
                reachableMap,
                queue);

            TryVisitCell(
                current.x - 1,
                current.y,
                openMap,
                reachableMap,
                queue);

            TryVisitCell(
                current.x,
                current.y + 1,
                openMap,
                reachableMap,
                queue);

            TryVisitCell(
                current.x,
                current.y - 1,
                openMap,
                reachableMap,
                queue);
        }

        return reachableMap;
    }

    private static void TryVisitCell(
        int x,
        int y,
        bool[,] openMap,
        bool[,] reachableMap,
        Queue<Vector2Int> queue)
    {
        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        if (openMap[x, y] == false
            || reachableMap[x, y])
        {
            return;
        }

        reachableMap[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    private static bool ContainsCoarseEdge(
        List<CoarseEdge> edges,
        CoarseEdge target)
    {
        for (int index = 0;
             index < edges.Count;
             index++)
        {
            if (AreSameCoarseEdge(
                    edges[index],
                    target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool AreSameCoarseEdge(
        CoarseEdge a,
        CoarseEdge b)
    {
        return GetCoarseEdgeKey(a)
            == GetCoarseEdgeKey(b);
    }

    private static string GetCoarseEdgeOrbitKey(
        List<CoarseEdge> edges)
    {
        List<string> keys =
            new List<string>();

        for (int index = 0;
             index < edges.Count;
             index++)
        {
            keys.Add(
                GetCoarseEdgeKey(edges[index]));
        }

        keys.Sort();

        return string.Join(
            "|",
            keys.ToArray());
    }

    private static string GetCoarseEdgeKey(
        CoarseEdge edge)
    {
        string a = edge.m_a.x + "," + edge.m_a.y;
        string b = edge.m_b.x + "," + edge.m_b.y;

        if (string.CompareOrdinal(a, b) > 0)
        {
            string temporary = a;
            a = b;
            b = temporary;
        }

        return a + "-" + b;
    }

    private static Vector2Int Rotate(
        Vector2Int position,
        int width,
        int rotation)
    {
        switch (rotation)
        {
            case 0:
                return position;

            case 1:
                return new Vector2Int(
                    width - 1 - position.y,
                    position.x);

            case 2:
                return new Vector2Int(
                    width - 1 - position.x,
                    width - 1 - position.y);

            default:
                return new Vector2Int(
                    position.y,
                    width - 1 - position.x);
        }
    }

    private static Vector2Int RotateDirection(
        Vector2Int direction,
        int rotation)
    {
        switch (rotation)
        {
            case 0:
                return direction;

            case 1:
                return new Vector2Int(
                    -direction.y,
                    direction.x);

            case 2:
                return new Vector2Int(
                    -direction.x,
                    -direction.y);

            default:
                return new Vector2Int(
                    direction.y,
                    -direction.x);
        }
    }

    private static void ShuffleList<T>(
        System.Random random,
        List<T> list)
    {
        for (int index = list.Count - 1;
             index > 0;
             index--)
        {
            int swapIndex = random.Next(
                0,
                index + 1);

            T temporary = list[index];
            list[index] = list[swapIndex];
            list[swapIndex] = temporary;
        }
    }

    private static void ApplyMapsToMazeData(
        MazeData mazeData,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap)
    {
        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                MazeCellData cell = mazeData.m_cells[x, y];

                cell.m_openDirections = MazeDirection.None;
                cell.m_isCentralHall = false;
                cell.m_isRoom = false;
                cell.m_roomId = -1;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (openMap[x, y] == false)
                {
                    continue;
                }

                MazeCellData cell = mazeData.m_cells[x, y];

                cell.m_isRoom = roomMap[x, y];
                cell.m_roomId = roomMap[x, y]
                    ? roomIdMap[x, y]
                    : -1;

                if (x + 1 < width && openMap[x + 1, y])
                {
                    cell.m_openDirections |= MazeDirection.East;
                }

                if (x - 1 >= 0 && openMap[x - 1, y])
                {
                    cell.m_openDirections |= MazeDirection.West;
                }

                if (y + 1 < height && openMap[x, y + 1])
                {
                    cell.m_openDirections |= MazeDirection.North;
                }

                if (y - 1 >= 0 && openMap[x, y - 1])
                {
                    cell.m_openDirections |= MazeDirection.South;
                }
            }
        }
    }

    private static void OpenOuterEntrance(
        MazeData mazeData,
        int x,
        int y,
        MazeDirection direction)
    {
        MazeCellData cell = mazeData.m_cells[x, y];

        if (cell == null || cell.IsBlockedCell())
        {
            return;
        }

        cell.m_openDirections |= direction;
    }

    private static void MarkCentralHallCells(
    MazeData mazeData)
    {
        if (mazeData == null)
        {
            return;
        }

        int centerX = mazeData.m_width / 2;
        int centerY = mazeData.m_height / 2;
        int halfSize = s_centerRoomSize / 2;

        for (int y = centerY - halfSize;
             y <= centerY + halfSize;
             y++)
        {
            for (int x = centerX - halfSize;
                 x <= centerX + halfSize;
                 x++)
            {
                MazeCellData cell = mazeData.GetCell(x, y);

                if (cell != null)
                {
                    cell.m_isCentralHall = true;
                }
            }
        }
    }


    /*
     * 既存通路と最大3セルの掘削を組み合わせ、
     * 長方形の共有部屋をC4対称に作る。
     *
     * 前回版は0～2組をランダムに選んで終わっていた。
     * 今回は、作成可能な候補がなくなるまで繰り返す。
     */
    private static int PromoteClosedLoopAreasToRooms(
        System.Random random,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        List<RoomNode> roomNodes)
    {
        int promotedSetCount = 0;

        while (promotedSetCount < s_maxLoopRoomPromotionSetCount)
        {
            List<Vector2Int> roomCells;

            if (TryFindBestClosedLoopRoomCells(
                    random,
                    width,
                    openMap,
                    roomMap,
                    protectedWallMap,
                    doorMap,
                    out roomCells) == false)
            {
                break;
            }

            PromoteFourFoldClosedLoopRoomSet(
                roomCells,
                width,
                ref nextRoomId,
                openMap,
                roomMap,
                roomIdMap,
                roomNodes);

            promotedSetCount++;
        }

        return promotedSetCount;
    }

    /*
     * 北東象限から、最も小さく・少ない掘削で作れる
     * 長方形共有部屋の候補を探す。
     *
     * 3x3～5x5の全ての長方形を対象にするため、
     * 前回見落としていた4x4、4x5、5x4、5x5も対象になる。
     */
    private static bool TryFindBestClosedLoopRoomCells(
        System.Random random,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        out List<Vector2Int> result)
    {
        int center = width / 2;

        int bestScore = int.MaxValue;

        List<List<Vector2Int>> bestCandidates =
            new List<List<Vector2Int>>();

        for (int roomHeight = s_minLoopRoomPromotionRoomSize;
             roomHeight <= s_maxLoopRoomPromotionRoomSize;
             roomHeight++)
        {
            for (int roomWidth = s_minLoopRoomPromotionRoomSize;
                 roomWidth <= s_maxLoopRoomPromotionRoomSize;
                 roomWidth++)
            {
                /*
                 * 中心軸の右上だけを走査する。
                 * 90度回転先を含めて4部屋が別々になる範囲に限定する。
                 */
                for (int minY = center + 1;
                     minY <= width - roomHeight - 1;
                     minY++)
                {
                    for (int minX = center + 1;
                         minX <= width - roomWidth - 1;
                         minX++)
                    {
                        List<Vector2Int> cells =
                            CreateRectangleCells(
                                minX,
                                minY,
                                roomWidth,
                                roomHeight);

                        int digCellCount;

                        if (CanPromoteFourFoldClosedLoopRoomSet(
                                cells,
                                width,
                                openMap,
                                roomMap,
                                protectedWallMap,
                                doorMap,
                                out digCellCount) == false)
                        {
                            continue;
                        }

                        /*
                         * まず小さい部屋を優先する。
                         * 同じ面積なら、掘るセルが少ない候補を優先する。
                         *
                         * 小さい候補から使うことで、無関係に大きな領域を
                         * 青い部屋へ吸収しすぎない。
                         */
                        int area = roomWidth * roomHeight;
                        int score = area * 10 + digCellCount;

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestCandidates.Clear();
                            bestCandidates.Add(cells);
                        }
                        else if (score == bestScore)
                        {
                            bestCandidates.Add(cells);
                        }
                    }
                }
            }
        }

        if (bestCandidates.Count == 0)
        {
            result = null;
            return false;
        }

        /*
         * 同じ優先度の候補からはランダムに選ぶ。
         * 大枠の公平性はC4対称で保ちつつ、毎回同じ配置にはしない。
         */
        result = bestCandidates[random.Next(bestCandidates.Count)];
        return true;
    }

    private static List<Vector2Int> CreateRectangleCells(
        int minX,
        int minY,
        int roomWidth,
        int roomHeight)
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

        for (int y = minY;
             y < minY + roomHeight;
             y++)
        {
            for (int x = minX;
                 x < minX + roomWidth;
                 x++)
            {
                result.Add(new Vector2Int(x, y));
            }
        }

        return result;
    }

    /*
     * 元候補と、その90度回転先3つ全てが
     * 同時に長方形共有部屋へ昇格できることを確認する。
     */
    private static bool CanPromoteFourFoldClosedLoopRoomSet(
        List<Vector2Int> sourceCells,
        int width,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        out int digCellCount)
    {
        digCellCount = 0;

        for (int rotation = 0;
             rotation < 4;
             rotation++)
        {
            List<Vector2Int> rotatedCells =
                RotatePath(
                    sourceCells,
                    width,
                    rotation);

            int rotatedDigCellCount;

            if (CanPromoteClosedLoopRoom(
                    rotatedCells,
                    openMap,
                    roomMap,
                    protectedWallMap,
                    doorMap,
                    out rotatedDigCellCount) == false)
            {
                return false;
            }

            /*
             * C4対称なので通常は各回転先で同数になるが、
             * 判定としては最も多い掘削数を採用しておく。
             */
            digCellCount = Mathf.Max(
                digCellCount,
                rotatedDigCellCount);
        }

        return true;
    }

    /*
     * 候補矩形を共有部屋にできるか調べる。
     *
     * 条件:
     * - 完成後の部屋は長方形。
     * - 矩形外周は、元から通常通路である。
     * - 矩形内部は通常通路または壁である。
     * - 掘る壁セルは1～3セルだけ。
     * - 既存部屋、予約壁、扉は含めない。
     *
     * 重要:
     * 元から通路だったセルも、完成後は同じroomIdの部屋になる。
     */
    private static bool CanPromoteClosedLoopRoom(
        List<Vector2Int> cells,
        bool[,] openMap,
        bool[,] roomMap,
        bool[,] protectedWallMap,
        bool[,] doorMap,
        out int digCellCount)
    {
        digCellCount = 0;

        if (cells == null
            || cells.Count == 0)
        {
            return false;
        }

        int width = openMap.GetLength(0);
        int height = openMap.GetLength(1);

        int minX;
        int maxX;
        int minY;
        int maxY;

        GetCellBounds(
            cells,
            out minX,
            out maxX,
            out minY,
            out maxY);

        int roomWidth = maxX - minX + 1;
        int roomHeight = maxY - minY + 1;

        if (roomWidth * roomHeight != cells.Count)
        {
            return false;
        }

        if (minX <= 0
            || maxX >= width - 1
            || minY <= 0
            || maxY >= height - 1)
        {
            return false;
        }

        for (int cellIndex = 0;
             cellIndex < cells.Count;
             cellIndex++)
        {
            Vector2Int cell = cells[cellIndex];

            if (roomMap[cell.x, cell.y]
                || protectedWallMap[cell.x, cell.y]
                || doorMap[cell.x, cell.y])
            {
                return false;
            }

            bool isBoundaryCell =
                cell.x == minX
                || cell.x == maxX
                || cell.y == minY
                || cell.y == maxY;

            /*
             * 外周は既存通路でなければならない。
             * ここを掘ることは許可しないため、
             * 通路ループを部屋へ昇格する性質を維持できる。
             */
            if (isBoundaryCell)
            {
                if (openMap[cell.x, cell.y] == false)
                {
                    return false;
                }

                continue;
            }

            /*
             * 内部の壁だけを掘る。
             * 内部に元からある通路は、そのまま部屋セルへ昇格する。
             */
            if (openMap[cell.x, cell.y] == false)
            {
                digCellCount++;
            }
        }

        if (digCellCount <= 0
            || digCellCount > s_maxLoopRoomPromotionCellCount)
        {
            return false;
        }

        return true;
    }

    /*
     * C4対称の候補4つを、それぞれ別roomIdの共有部屋にする。
     *
     * 今回掘った壁だけでなく、候補矩形内に元からあった通路も
     * 全てroomMap=trueへ変更する。
     */
    private static void PromoteFourFoldClosedLoopRoomSet(
        List<Vector2Int> sourceCells,
        int width,
        ref int nextRoomId,
        bool[,] openMap,
        bool[,] roomMap,
        int[,] roomIdMap,
        List<RoomNode> roomNodes)
    {
        for (int rotation = 0;
             rotation < 4;
             rotation++)
        {
            List<Vector2Int> roomCells =
                RotatePath(
                    sourceCells,
                    width,
                    rotation);

            int minX;
            int maxX;
            int minY;
            int maxY;

            GetCellBounds(
                roomCells,
                out minX,
                out maxX,
                out minY,
                out maxY);

            RoomNode roomNode = new RoomNode();
            roomNode.m_roomId = nextRoomId;
            roomNode.m_center = new Vector2Int(
                minX + (maxX - minX) / 2,
                minY + (maxY - minY) / 2);

            nextRoomId++;

            for (int cellIndex = 0;
                 cellIndex < roomCells.Count;
                 cellIndex++)
            {
                Vector2Int cell = roomCells[cellIndex];

                openMap[cell.x, cell.y] = true;
                roomMap[cell.x, cell.y] = true;
                roomIdMap[cell.x, cell.y] =
                    roomNode.m_roomId;
            }

            roomNodes.Add(roomNode);
        }
    }

    private static void GetCellBounds(
        List<Vector2Int> cells,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        minX = int.MaxValue;
        maxX = int.MinValue;
        minY = int.MaxValue;
        maxY = int.MinValue;

        for (int index = 0;
             index < cells.Count;
             index++)
        {
            Vector2Int cell = cells[index];

            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxY = Mathf.Max(maxY, cell.y);
        }
    }

}