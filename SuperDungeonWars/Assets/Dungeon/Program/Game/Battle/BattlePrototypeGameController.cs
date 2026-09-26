//#define PRINT_DEBUG_VIEW


using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class BattlePrototypeGameController : MonoBehaviour
{
    [SerializeField] private MazeRenderer m_mazeRenderer;
    [SerializeField] private Transform m_partyRoot;

    [SerializeField]
    private BattlePartyRegistry m_battlePartyRegistry;

    [SerializeField]
    private ExplorerAgent m_runtimeCharacterPrefab;

    [SerializeField]
    private MazeGenerationSettings m_mazeGenerationSettings =
        new MazeGenerationSettings();

    [SerializeField] private float m_mazeCellSize = 2.0f;
    [SerializeField] private KnownMapDebugTextView m_knownMapDebugTextView;

    [SerializeField]
    private PartyMovementAvoidanceSystem m_partyMovementAvoidanceSystem;

    [SerializeField] private TreasureChestSpawner m_treasureChestSpawner;

    [SerializeField]
    private BattleSpectatorCameraController m_battleSpectatorCameraController;

    [SerializeField]
    private BattleSpectatorFocusIndicatorView m_battleSpectatorFocusIndicatorView;

    [SerializeField]
    private BattleSpectatorFocusSettings m_spectatorFocusSettings = new();

    [SerializeField]
    private CharacterMannequinReplacer m_characterMannequinReplacer;

    private BattleSpectatorFocusSelector m_battleSpectatorFocusSelector;

    private int m_spectatorFocusedPartyIndex = -1;

    private BattleDeveloperSpectatorMode m_lastAppliedDeveloperSpectatorMode =
        (BattleDeveloperSpectatorMode)(-1);

    private MazeData m_mazeData;

    private readonly List<ComPartyBase> m_spawnedParties =
        new List<ComPartyBase>();

    private readonly BattleVisibleWorldSystem m_visibleWorldSystem =
        new BattleVisibleWorldSystem();

    private int m_nextCharacterId;

    [SerializeField]
    private BattleGameRuleData m_battleGameRuleData;

    private BattleResultData m_battleResult;
    private float m_battleElapsedSeconds;
    private bool m_isBattleRunning;

    private static readonly TreasureChest[] s_emptyTreasureChests =
        new TreasureChest[0];

    private readonly BattleTreasureSystem m_battleTreasureSystem =
        new BattleTreasureSystem();

    private readonly BattleParticipantSystem m_battleParticipantSystem =
        new BattleParticipantSystem();

    private readonly BattleCombatSystem m_battleCombatSystem =
        new BattleCombatSystem();

    private readonly List<Vector2Int> m_spawnedPartyStartCells =
        new List<Vector2Int>();

    public delegate void BattleEndedDelegate(BattleResultData battleResult);

    public event BattleEndedDelegate BattleEnded;

    public BattleResultData BattleResult
    {
        get { return m_battleResult; }
    }

    private BattleEndReason m_lastBattleEndReason;

    public BattleEndReason LastBattleEndReason
    {
        get { return m_lastBattleEndReason; }
    }

    public float RemainingBattleSeconds
    {
        get
        {
            if (m_battleGameRuleData == null)
            {
                return 0.0f;
            }

            return Mathf.Max(
                0.0f,
                m_battleGameRuleData.BattleDurationSeconds
                - m_battleElapsedSeconds);
        }
    }

    public bool IsBattleRunning
    {
        get { return m_isBattleRunning; }
    }

    public int SpawnedPartyCount
    {
        get { return m_spawnedParties.Count; }
    }

    public MazeData MazeData
    {
        get { return m_mazeData; }
    }

    public IReadOnlyList<ComPartyBase> SpawnedParties
    {
        get { return m_spawnedParties; }
    }

    public IReadOnlyList<TreasureChest> SpawnedTreasureChests
    {
        get
        {
            return m_treasureChestSpawner != null
                ? m_treasureChestSpawner.SpawnedTreasureChests
                : s_emptyTreasureChests;
        }
    }

    public int SpectatorFocusedPartyIndex
    {
        get { return m_spectatorFocusedPartyIndex; }
    }

    public void SnapSpectatorCameraToCurrentFocus()
    {
        if (m_battleSpectatorCameraController != null)
        {
            m_battleSpectatorCameraController.SnapToCurrentFocus();
        }
    }

    public IReadOnlyList<TreasureChest> GetExportedTreasureChestsTeamIndex(
            int systemTeamIndex)
    {
        if (systemTeamIndex < 0
            || systemTeamIndex >= m_spawnedParties.Count)
        {
            return s_emptyTreasureChests;
        }

        ComPartyBase party =
            m_spawnedParties[systemTeamIndex];

        if (party == null)
        {
            return s_emptyTreasureChests;
        }

        IReadOnlyList<ITreasureRuntime> exportedTreasures =
            party.GetExportedTreasureChests();

        if (exportedTreasures == null
            || exportedTreasures.Count <= 0)
        {
            return s_emptyTreasureChests;
        }

        List<TreasureChest> exportedTreasureChests =
            new List<TreasureChest>();

        for (int i = 0; i < exportedTreasures.Count; i++)
        {
            TreasureChest treasureChest =
                exportedTreasures[i] as TreasureChest;

            if (treasureChest != null)
            {
                exportedTreasureChests.Add(treasureChest);
            }
        }

        return exportedTreasureChests;
    }

    private static readonly Vector3[] s_memberSpawnOffsets =
    {
        new Vector3(-0.35f, 0.0f,  0.35f),
        new Vector3( 0.35f, 0.0f,  0.35f),
        new Vector3(-0.35f, 0.0f, -0.35f),
        new Vector3( 0.35f, 0.0f, -0.35f),
    };

    private void Awake()
    {
        if (m_partyMovementAvoidanceSystem == null)
        {
            m_partyMovementAvoidanceSystem =
                GetComponent<PartyMovementAvoidanceSystem>();
        }

        if (m_partyMovementAvoidanceSystem == null)
        {
            m_partyMovementAvoidanceSystem =
                gameObject.AddComponent<
                    PartyMovementAvoidanceSystem>();
        }

        if (m_characterMannequinReplacer == null)
        {
            m_characterMannequinReplacer =
                GetComponent<CharacterMannequinReplacer>();
        }

        if (m_characterMannequinReplacer == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "CharacterMannequinReplacer がありません。",
                this);
        }
    }

    private void Start()
    {
        //BuildAndStart();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        //if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        //{
        //    BuildAndStart();
        //    return;
        //}

        HandleDebugKnownMapSelection(keyboard);

        RefreshSpectatorFocusIfDeveloperSettingChanged();

        if (m_isBattleRunning)
        {
            m_battleElapsedSeconds += Time.deltaTime;

            if (m_battleGameRuleData != null
                && m_battleElapsedSeconds
                    >= m_battleGameRuleData.BattleDurationSeconds)
            {
                EndBattle(BattleEndReason.TimeLimitReached);
            }
            else
            {
                UpdateVisibleWorldData();

                m_battleParticipantSystem.UpdateSystem();

                m_battleCombatSystem.UpdateSystem();

                UpdateSpectatorFocus();

                if (HasAllTreasuresExported())
                {
                    EndBattle(BattleEndReason.AllTreasuresExported);
                }
            }
        }

#if PRINT_DEBUG_VIEW
        RefreshDebugView();
#endif
    }

    private void OnDestroy()
    {
        m_battleCombatSystem.Shutdown();
        m_battleParticipantSystem.Shutdown();
        m_battleTreasureSystem.Shutdown();
    }

    private void UpdateVisibleWorldData()
    {
        IReadOnlyList<TreasureChest> treasureChests = null;

        if (m_treasureChestSpawner != null)
        {
            treasureChests =
                m_treasureChestSpawner.SpawnedTreasureChests;
        }

        m_visibleWorldSystem.UpdateVisibleWorldData(
            m_mazeData,
            m_spawnedParties,
            treasureChests);
    }

#if PRINT_DEBUG_VIEW
    private void RefreshDebugView()
    {
        if (m_knownMapDebugTextView == null)
        {
            return;
        }

        if (m_spawnedParties.Count <= 0
            || m_spawnedParties[0] == null)
        {
            return;
        }

        m_knownMapDebugTextView.RefreshView();
    }
#endif


    public void PrepareBattle()
    {
        ClearParties();

        m_nextCharacterId = 0;
        m_battleResult = null;
        m_battleElapsedSeconds = 0.0f;
        m_isBattleRunning = false;
        m_debugSelectedPartyIndex = -1;
        m_spectatorFocusedPartyIndex = -1;

        m_lastAppliedDeveloperSpectatorMode =
            (BattleDeveloperSpectatorMode)(-1);

        m_lastBattleEndReason = BattleEndReason.None;

        if (m_mazeRenderer == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "m_mazeRenderer が未設定です。",
                this);

            return;
        }

        if (m_partyRoot == null)
        {
            m_partyRoot = transform;
        }

        if (m_battlePartyRegistry == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "m_battlePartyRegistry が未設定です。",
                this);

            return;
        }

        ComPartyBase[] partyPrefabs =
            m_battlePartyRegistry.PartyPrefabs;

        if (partyPrefabs == null || partyPrefabs.Length <= 0)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "BattlePartyRegistry の PartyPrefabs が未設定です。",
                m_battlePartyRegistry);

            return;
        }

        if (m_runtimeCharacterPrefab == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "m_runtimeCharacterPrefab が未設定です。",
                this);

            return;
        }

        m_mazeData = MazeGenerator.Generate(
            m_mazeGenerationSettings,
            m_mazeCellSize);

        m_mazeRenderer.Build(m_mazeData);
        m_mazeRenderer.BindKnownMapData(null);

        if (m_treasureChestSpawner != null)
        {
            m_treasureChestSpawner.Build(m_mazeData);
        }

        List<Vector2Int> centralHallCells = FindCentralHallCells();

        Vector2Int[] startCells = FindBattleStartCells();

        int count = Mathf.Min(
            partyPrefabs.Length,
            startCells.Length);

        for (int teamIndex = 0; teamIndex < count; teamIndex++)
        {
            ComPartyBase partyPrefab = partyPrefabs[teamIndex];

            if (partyPrefab == null)
            {
                continue;
            }

            Vector2Int startCell = startCells[teamIndex];

            ComPartyBase party = Instantiate(
                partyPrefab,
                m_partyRoot);

            KnownMapData knownMapData = new KnownMapData(
                m_mazeData.m_width,
                m_mazeData.m_height);

            knownMapData.SetCentralHallCells(
                centralHallCells);

            knownMapData.ObserveCentralHallCells(
                m_mazeData);

            Vector2Int[] entranceCellsByRelativeTeamIndex =
                CreateEntranceCellsByRelativeTeamIndex(
                    startCells,
                    teamIndex);

            party.Initialize(
                new ParticipantMazeBounds(
                    m_mazeData.m_width,
                    m_mazeData.m_height),
                knownMapData,
                entranceCellsByRelativeTeamIndex);

            SpawnAndRegisterPartyMembers(
                party,
                teamIndex,
                startCell);

            m_spawnedParties.Add(party);

            m_spawnedPartyStartCells.Add(startCell);
        }

        IReadOnlyList<TreasureChest> treasureChests =
            m_treasureChestSpawner != null
                ? m_treasureChestSpawner.SpawnedTreasureChests
                : s_emptyTreasureChests;

        m_battleTreasureSystem.Initialize(
            m_mazeData,
            m_battleGameRuleData,
            m_spawnedParties,
            treasureChests);

        m_battleParticipantSystem.Initialize(
            m_mazeData,
            m_spawnedParties,
            m_battleTreasureSystem,
            m_battleGameRuleData);

        m_battleCombatSystem.Initialize(
            m_battleGameRuleData,
            m_spawnedParties,
            m_battleTreasureSystem);

        UpdateVisibleWorldData();

        InitializeSpectatorFocus();

        if (m_knownMapDebugTextView != null)
        {
            m_knownMapDebugTextView.BindBattleGameController(this);
            SetDebugSelectedPartyIndex(0);
            m_knownMapDebugTextView.RefreshView();
        }
    }


    private List<Vector2Int> FindCentralHallCells()
    {
        List<Vector2Int> result =
            new List<Vector2Int>();

        if (m_mazeData == null)
        {
            return result;
        }

        for (int y = 0; y < m_mazeData.m_height; y++)
        {
            for (int x = 0; x < m_mazeData.m_width; x++)
            {
                MazeCellData cell = m_mazeData.GetCell(x, y);

                if (cell != null && cell.m_isCentralHall)
                {
                    result.Add(new Vector2Int(x, y));
                }
            }
        }

        return result;
    }

    private void SpawnAndRegisterPartyMembers(
        ComPartyBase party,
        int teamIndex,
        Vector2Int startCell)
    {
        if (party == null)
        {
            return;
        }

        List<ComCharacterBase> members =
            new List<ComCharacterBase>();

        for (int memberIndex = 0;
             memberIndex < BattleGameConstants.PartyMemberCount;
             memberIndex++)
        {
            ComCharacterBase memberPrefab = party.GetMemberPrefab();

            if (memberPrefab == null)
            {
                continue;
            }

            ExplorerAgent runtimeCharacter = Instantiate(
                m_runtimeCharacterPrefab,
                party.transform);

            runtimeCharacter.name =
                m_runtimeCharacterPrefab.name;

            runtimeCharacter.Initialize(
                m_mazeData,
                startCell);

            ComCharacterBase member = Instantiate(
                memberPrefab,
                runtimeCharacter.GetRuntimeTransform());

            member.transform.localPosition = Vector3.zero;
            member.transform.localRotation = Quaternion.identity;
            member.transform.localScale = Vector3.one;

            member.SetCharacterId(m_nextCharacterId);
            m_nextCharacterId++;

            member.Initialize(
                party,
                party.GetKnownMapData(),
                memberIndex,
                runtimeCharacter);

            if (m_characterMannequinReplacer != null)
            {
                m_characterMannequinReplacer.ReplaceMannequin(
                    member,
                    runtimeCharacter.GetSystemMannequinTransform());
            }

            SetInitialMemberPosition(
                member,
                memberIndex,
                startCell);

            ApplyMemberTeamColor(
                runtimeCharacter,
                member,
                teamIndex,
                memberIndex);

            members.Add(member);
        }

        party.RegisterMembers(members.ToArray());
    }

    private void SetInitialMemberPosition(
        ComCharacterBase member,
        int memberIndex,
        Vector2Int startCell)
    {
        if (member == null)
        {
            return;
        }

        ExplorerAgent agent =
            member.GetExplorerAgent() as ExplorerAgent;

        if (agent == null)
        {
            return;
        }

        Vector3 spawnWorld = m_mazeData.CellToWorld(
            startCell.x,
            startCell.y);

        if (memberIndex >= 0
            && memberIndex < s_memberSpawnOffsets.Length)
        {
            spawnWorld += s_memberSpawnOffsets[memberIndex];
        }

        agent.SetPartyMemberIndex(memberIndex);
        agent.SetPositionToWorld(spawnWorld);
    }

    private void ApplyMemberTeamColor(
        ExplorerAgent runtimeCharacter,
        ComCharacterBase member,
        int teamIndex,
        int memberIndex)
    {
        if (runtimeCharacter == null)
        {
            return;
        }

        CharacterTeamColorController[] colorControllers =
            runtimeCharacter.GetCharacterTeamColorControllers();

        if (colorControllers == null
            || colorControllers.Length <= 0)
        {
            Debug.LogWarning(
                (member != null ? member.name : runtimeCharacter.name)
                + " に Runtime Character 側の "
                + "CharacterTeamColorController がありません。",
                runtimeCharacter);

            return;
        }

        for (int i = 0; i < colorControllers.Length; i++)
        {
            CharacterTeamColorController colorController =
                colorControllers[i];

            if (colorController != null)
            {
                colorController.Initialize(
                    teamIndex,
                    memberIndex);
            }
        }
    }

    private void ClearParties()
    {
        m_spectatorFocusedPartyIndex = -1;

        m_battleCombatSystem.Shutdown();
        m_battleParticipantSystem.Shutdown();
        m_battleTreasureSystem.Shutdown();

        for (int i = 0; i < m_spawnedParties.Count; i++)
        {
            if (m_spawnedParties[i] != null)
            {
                Destroy(m_spawnedParties[i].gameObject);
            }
        }

        m_spawnedParties.Clear();
        m_spawnedPartyStartCells.Clear();

        if (m_battleSpectatorFocusSelector != null)
        {
            m_battleSpectatorFocusSelector.ClearCurrentFocus();
        }

        if (m_battleSpectatorFocusIndicatorView != null)
        {
            m_battleSpectatorFocusIndicatorView.ClearFocus();
        }
    }

    private Vector2Int[] FindBattleStartCells()
    {
        List<Vector2Int> result = new List<Vector2Int>();

        TryAddStartCell(result, FindBorderStartCellFromBottom());
        TryAddStartCell(result, FindBorderStartCellFromLeft());
        TryAddStartCell(result, FindBorderStartCellFromTop());
        TryAddStartCell(result, FindBorderStartCellFromRight());

        if (result.Count < 4)
        {
            AddFallbackBorderStartCells(result);
        }

        return result.ToArray();
    }

    private Vector2Int FindBorderStartCellFromLeft()
    {
        int yCenter = m_mazeData.m_height / 2;

        for (int offset = 0; offset < m_mazeData.m_height; offset++)
        {
            int y1 = yCenter + offset;

            if (IsValidStartCell(0, y1))
            {
                return new Vector2Int(0, y1);
            }

            int y2 = yCenter - offset;

            if (IsValidStartCell(0, y2))
            {
                return new Vector2Int(0, y2);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindBorderStartCellFromRight()
    {
        int x = m_mazeData.m_width - 1;
        int yCenter = m_mazeData.m_height / 2;

        for (int offset = 0; offset < m_mazeData.m_height; offset++)
        {
            int y1 = yCenter + offset;

            if (IsValidStartCell(x, y1))
            {
                return new Vector2Int(x, y1);
            }

            int y2 = yCenter - offset;

            if (IsValidStartCell(x, y2))
            {
                return new Vector2Int(x, y2);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindBorderStartCellFromBottom()
    {
        int xCenter = m_mazeData.m_width / 2;

        for (int offset = 0; offset < m_mazeData.m_width; offset++)
        {
            int x1 = xCenter + offset;

            if (IsValidStartCell(x1, 0))
            {
                return new Vector2Int(x1, 0);
            }

            int x2 = xCenter - offset;

            if (IsValidStartCell(x2, 0))
            {
                return new Vector2Int(x2, 0);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindBorderStartCellFromTop()
    {
        int y = m_mazeData.m_height - 1;
        int xCenter = m_mazeData.m_width / 2;

        for (int offset = 0; offset < m_mazeData.m_width; offset++)
        {
            int x1 = xCenter + offset;

            if (IsValidStartCell(x1, y))
            {
                return new Vector2Int(x1, y);
            }

            int x2 = xCenter - offset;

            if (IsValidStartCell(x2, y))
            {
                return new Vector2Int(x2, y);
            }
        }

        return new Vector2Int(-1, -1);
    }

    private void AddFallbackBorderStartCells(
        List<Vector2Int> result)
    {
        for (int y = 0; y < m_mazeData.m_height; y++)
        {
            TryAddStartCell(result, new Vector2Int(0, y));

            TryAddStartCell(
                result,
                new Vector2Int(m_mazeData.m_width - 1, y));
        }

        for (int x = 0; x < m_mazeData.m_width; x++)
        {
            TryAddStartCell(result, new Vector2Int(x, 0));

            TryAddStartCell(
                result,
                new Vector2Int(x, m_mazeData.m_height - 1));
        }
    }

    private void TryAddStartCell(
        List<Vector2Int> result,
        Vector2Int cell)
    {
        if (cell.x < 0 || cell.y < 0)
        {
            return;
        }

        for (int i = 0; i < result.Count; i++)
        {
            if (result[i] == cell)
            {
                return;
            }
        }

        if (IsValidStartCell(cell.x, cell.y))
        {
            result.Add(cell);
        }
    }

    private bool IsValidStartCell(int x, int y)
    {
        if (m_mazeData == null
            || m_mazeData.IsInside(x, y) == false)
        {
            return false;
        }

        MazeCellData cell = m_mazeData.GetCell(x, y);

        return cell != null && cell.IsBlockedCell() == false;
    }

    public void StartBattle()
    {
        if (m_isBattleRunning)
        {
            return;
        }

        if (m_battleGameRuleData == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "m_battleGameRuleData が未設定です。",
                this);

            return;
        }

        for (int i = 0; i < m_spawnedParties.Count; i++)
        {
            ComPartyBase party = m_spawnedParties[i];

            if (party != null)
            {
                party.StartParty();
            }
        }

        m_battleElapsedSeconds = 0.0f;
        m_isBattleRunning = true;

        Debug.Log(
            "試合開始。制限時間="
            + m_battleGameRuleData.BattleDurationSeconds
            + "秒");
    }

    private bool HasAllTreasuresExported()
    {
        if (m_treasureChestSpawner == null)
        {
            return false;
        }

        IReadOnlyList<TreasureChest> treasureChests =
            m_treasureChestSpawner.SpawnedTreasureChests;

        if (treasureChests == null
            || treasureChests.Count <= 0)
        {
            return false;
        }

        for (int i = 0; i < treasureChests.Count; i++)
        {
            TreasureChest treasureChest = treasureChests[i];

            if (treasureChest == null
                || !treasureChest.IsExported)
            {
                return false;
            }
        }

        return true;
    }

    private void EndBattle(BattleEndReason endReason)
    {
        if (!m_isBattleRunning)
        {
            return;
        }

        m_isBattleRunning = false;
        m_lastBattleEndReason = endReason;

        BattleResultData battleResult =
            FinalizeBattleResult();

        Debug.Log(
            "試合終了。理由="
            + GetBattleEndReasonText(endReason)
            + " / 経過時間="
            + m_battleElapsedSeconds.ToString("F1")
            + "秒");

        DebugLogBattleResult(battleResult);

        BattleEndedDelegate callback = BattleEnded;

        if (callback != null)
        {
            callback(battleResult);
        }
    }

    private static string GetBattleEndReasonText(
        BattleEndReason endReason)
    {
        switch (endReason)
        {
            case BattleEndReason.TimeLimitReached:
                return "制限時間到達";

            case BattleEndReason.AllTreasuresExported:
                return "全財宝を搬出";

            default:
                return "不明";
        }
    }

    private void DebugLogBattleResult(
        BattleResultData battleResult)
    {
        if (battleResult == null)
        {
            Debug.LogError(
                "試合結果を生成できなかったため、"
                + "結果ログを出力できません。",
                this);

            return;
        }

        Debug.Log("========== Battle Result ==========");

        for (int i = 0;
             i < battleResult.TeamResults.Count;
             i++)
        {
            TeamBattleResult teamResult =
                battleResult.TeamResults[i];

            System.Text.StringBuilder builder =
                new System.Text.StringBuilder();

            builder.Append("Team ");
            builder.Append(teamResult.TeamIndex);
            builder.Append(" / Rank ");
            builder.Append(teamResult.Rank);
            builder.Append(" / Score ");
            builder.Append(teamResult.TotalValue);
            builder.Append(" / Exported ");

            if (teamResult.Treasures.Count <= 0)
            {
                builder.Append("none");
            }
            else
            {
                for (int treasureIndex = 0;
                     treasureIndex < teamResult.Treasures.Count;
                     treasureIndex++)
                {
                    ExportedTreasureResult treasureResult =
                        teamResult.Treasures[treasureIndex];

                    if (treasureIndex > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(treasureResult.TreasureType);
                    builder.Append("(");
                    builder.Append(treasureResult.Value);
                    builder.Append(")");
                }
            }

            if (teamResult.IsTournamentRepresentative)
            {
                builder.Append(" / Representative");
            }

            Debug.Log(builder.ToString());
        }

        Debug.Log("===================================");
    }

    public BattleResultData FinalizeBattleResult()
    {
        if (m_battleResult != null)
        {
            return m_battleResult;
        }

        if (m_battleGameRuleData == null)
        {
            Debug.LogError(
                "BattlePrototypeGameController: "
                + "m_battleGameRuleData が未設定です。",
                this);

            return null;
        }

        m_battleResult = BattleResultCalculator.CreateResult(
            m_spawnedParties,
            m_battleGameRuleData);

        return m_battleResult;
    }

    private Vector2Int[] CreateEntranceCellsByRelativeTeamIndex(
        Vector2Int[] startCells,
        int ownSystemTeamIndex)
    {
        if (startCells == null
            || startCells.Length <= 0
            || ownSystemTeamIndex < 0
            || ownSystemTeamIndex >= startCells.Length)
        {
            return new Vector2Int[0];
        }

        Vector2Int[] result =
            new Vector2Int[startCells.Length];

        for (int relativeTeamIndex = 0;
             relativeTeamIndex < startCells.Length;
             relativeTeamIndex++)
        {
            int systemTeamIndex =
                (ownSystemTeamIndex + relativeTeamIndex)
                % startCells.Length;

            result[relativeTeamIndex] =
                startCells[systemTeamIndex];
        }

        return result;
    }

    private Vector3 GetSpectatorBackwardDirectionByPartyIndex(
        int partyIndex)
    {
        if (m_mazeData == null
            || partyIndex < 0
            || partyIndex >= m_spawnedPartyStartCells.Count)
        {
            return Vector3.back;
        }

        Vector2Int startCell =
            m_spawnedPartyStartCells[partyIndex];

        Vector3 startWorld = m_mazeData.CellToWorld(
            startCell.x,
            startCell.y);

        Vector3 mazeCenterWorld = m_mazeData.CellToWorld(
            m_mazeData.m_width / 2,
            m_mazeData.m_height / 2);

        Vector3 backwardDirection = startWorld - mazeCenterWorld;
        backwardDirection.y = 0.0f;

        if (backwardDirection.sqrMagnitude < 0.0001f)
        {
            return Vector3.back;
        }

        return backwardDirection.normalized;
    }

    private void InitializeSpectatorFocus()
    {
        if (m_spectatorFocusSettings == null)
        {
            m_spectatorFocusSettings =
                new BattleSpectatorFocusSettings();
        }

        if (m_battleSpectatorCameraController == null)
        {
            return;
        }

        m_battleSpectatorFocusSelector =
            new BattleSpectatorFocusSelector(
                m_spectatorFocusSettings);

        UpdateSpectatorFocus();
    }

    public void RefreshSpectatorFocusByDeveloperSetting()
    {
        m_lastAppliedDeveloperSpectatorMode =
            (BattleDeveloperSpectatorMode)(-1);

        RefreshSpectatorFocusIfDeveloperSettingChanged();
    }

    private void RefreshSpectatorFocusIfDeveloperSettingChanged()
    {
        BattleDeveloperSpectatorMode currentMode =
            BattleDeveloperSpectatorSettings.GetMode();

        if (currentMode
            == m_lastAppliedDeveloperSpectatorMode)
        {
            return;
        }

        m_lastAppliedDeveloperSpectatorMode =
            currentMode;

        if (m_battleSpectatorFocusSelector == null)
        {
            return;
        }

        m_battleSpectatorFocusSelector.ClearCurrentFocus();
        m_spectatorFocusedPartyIndex = -1;

        if (m_battleSpectatorCameraController != null)
        {
            m_battleSpectatorCameraController.ClearFocus();
        }

        if (m_battleSpectatorFocusIndicatorView != null)
        {
            m_battleSpectatorFocusIndicatorView.ClearFocus();
        }

        UpdateSpectatorFocus();
    }

    private void UpdateSpectatorFocus()
    {
        if (m_battleSpectatorCameraController == null
            || m_battleSpectatorFocusSelector == null)
        {
            return;
        }

        IReadOnlyList<TreasureChest> treasureChests =
            m_treasureChestSpawner != null
                ? m_treasureChestSpawner.SpawnedTreasureChests
                : s_emptyTreasureChests;

        BattleSpectatorFocusSelector.FocusTargetType targetType;
        ComCharacterBase character;
        TreasureChest treasureChest;
        int systemTeamIndex;
        BattleSpectatorFocusSelector.FocusReason reason;

        int restrictedSystemTeamIndex =
            BattleDeveloperSpectatorSettings
                .GetRestrictedSystemTeamIndex();

        bool hasFocusChanged =
            m_battleSpectatorFocusSelector.TrySelectFocus(
                m_mazeData,
                m_spawnedParties,
                treasureChests,
                m_battleCombatSystem,
                m_battleTreasureSystem,
                restrictedSystemTeamIndex,
                out targetType,
                out character,
                out treasureChest,
                out systemTeamIndex,
                out reason);

        if (!hasFocusChanged)
        {
            return;
        }

        /*
         * 観戦カメラは常にワールド南側から北側を見る。
         * 近距離のターゲット切替では CameraController 側が
         * 現在の向きを維持するため、ここで指定する方向は
         * 遠距離切替時だけ適用される。
         */
        Vector3 backwardDirection = Vector3.back;

        m_spectatorFocusedPartyIndex = systemTeamIndex;

        switch (targetType)
        {
            case BattleSpectatorFocusSelector.FocusTargetType.Character:
                m_battleSpectatorCameraController.SetFocusCharacter(
                    character,
                    backwardDirection);

                if (m_battleSpectatorFocusIndicatorView != null)
                {
                    m_battleSpectatorFocusIndicatorView
                        .SetFocusCharacter(
                            character,
                            systemTeamIndex);
                }
                break;

            case BattleSpectatorFocusSelector.FocusTargetType.TreasureChest:
                m_battleSpectatorCameraController.SetFocusTreasureChest(
                    treasureChest,
                    backwardDirection);

                if (m_battleSpectatorFocusIndicatorView != null)
                {
                    m_battleSpectatorFocusIndicatorView
                        .SetFocusTreasureChest(
                            treasureChest,
                            systemTeamIndex);
                }
                break;

            default:
                m_battleSpectatorCameraController.ClearFocus();

                if (m_battleSpectatorFocusIndicatorView != null)
                {
                    m_battleSpectatorFocusIndicatorView.ClearFocus();
                }
                break;
        }

#if GAME_DEBUG
        Debug.Log(
            "[Spectator] Focus="
            + targetType
            + " / Reason="
            + reason
            + " / Team="
            + systemTeamIndex
            + " / Score="
            + m_battleSpectatorFocusSelector.CurrentScore.ToString("F1"));
#endif
    }
}