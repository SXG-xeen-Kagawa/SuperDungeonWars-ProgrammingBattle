# Class Index

- GeneratedAt: 2026-09-21 12:21:05
- ScanRoots: Assets/Dungeon/Program, Assets/Dungeon/ParticipantApi, Assets/Dungeon/Participants, Assets/Dungeon/Editor
- TotalCount: 100
- MonoBehaviourCount: 31
- NonMonoBehaviourCount: 69

## MonoBehaviours

### Folder: Assets/Dungeon/ParticipantApi

#### ComCharacterBase

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/ParticipantApi/ComCharacterBase.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods: なし
- OtherMethods:
  - `KnownMapView SXG_GetKnownMap()`
  - `bool SXG_SetMoveTargetCell(Vector2Int targetCell)`
  - `bool SXG_SetMoveTargetWorld(Vector3 targetWorld)`
  - `void SXG_Stop()`
  - `void SXG_OnCharacterThink()`
  - `void SXG_OnCellReached(Vector2Int cellPosition)`
  - `int SXG_ChooseAttackTargetIndex(IReadOnlyList<VisibleCharacterData> attackableCharacters)`
  - `bool SXG_ShouldPickUpTreasure(TreasureType treasureType)`
  - `void SXG_OnTreasureExported(TreasureType treasureType)`
- SignatureReferencedTypes:
  - `KnownMapView`
  - `TreasureType`
  - `VisibleCharacterData`
- BodyReferencedTypes:
  - `PartyMemberOrder`

#### ComPartyBase

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/ParticipantApi/ComPartyBase.cs`
- Fields:
  - `[SerializeField] private ComCharacterBase m_memberPrefabs`
  - `[SerializeField] private string m_creatorDisplayName`
  - `[SerializeField] private string m_teamDisplayName`
  - `[SerializeField] private string m_teamSimpleDescription`
  - `[SerializeField] private Sprite m_teamIconSprite`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods: なし
- OtherMethods:
  - `KnownMapView SXG_GetKnownMap()`
  - `IReadOnlyList<VisibleCharacterData> SXG_GetVisibleCharacters()`
  - `IReadOnlyList<VisibleTreasureData> SXG_GetVisibleTreasures()`
  - `bool SXG_TryGetReturnEntranceCell(out Vector2Int returnEntranceCell)`
  - `int SXG_GetMemberCount()`
  - `bool SXG_TryGetMemberInfo(int memberIndex, out PartyMemberInfo memberInfo)`
  - `bool SXG_SetMemberMoveTargetCell(int memberIndex, Vector2Int targetCell)`
  - `bool SXG_SetMemberMoveTargetWorld(int memberIndex, Vector3 targetWorld)`
  - `void SXG_StopMember(int memberIndex)`
  - `void SXG_StopAllMembers()`
  - `void SXG_OnPartyThink()`
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `KnownMapView`
  - `PartyMemberInfo`
  - `VisibleCharacterData`
  - `VisibleTreasureData`
- BodyReferencedTypes:
  - `PartyMemberOrder`

### Folder: Assets/Dungeon/Program/Game/Battle

#### BattlePrototypeGameController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Battle/BattlePrototypeGameController.cs`
- Fields:
  - `[SerializeField] private MazeRenderer m_mazeRenderer`
  - `[SerializeField] private Transform m_partyRoot`
  - `[SerializeField] private ComPartyBase[] m_partyPrefabs`
  - `[SerializeField] private ExplorerAgent m_runtimeCharacterPrefab`
  - `[SerializeField] private MazeGenerationSettings m_mazeGenerationSettings`
  - `[SerializeField] private float m_mazeCellSize`
  - `[SerializeField] private KnownMapDebugTextView m_knownMapDebugTextView`
  - `[SerializeField] private PartyMovementAvoidanceSystem m_partyMovementAvoidanceSystem`
  - `[SerializeField] private TreasureChestSpawner m_treasureChestSpawner`
  - `[SerializeField] private BattleSpectatorCameraController m_battleSpectatorCameraController`
  - `[SerializeField] private BattleSpectatorFocusIndicatorView m_battleSpectatorFocusIndicatorView`
  - `[SerializeField] private BattleSpectatorFocusSettings m_spectatorFocusSettings`
  - `[SerializeField] private CharacterMannequinReplacer m_characterMannequinReplacer`
  - `[SerializeField] private BattleGameRuleData m_battleGameRuleData`
  - `private ExplorerAgent m_runtimeCharacterPrefab`
  - `private MazeGenerationSettings m_mazeGenerationSettings`
  - `... (23 more)`
- Properties: なし
- Events:
  - `public event BattleEndedDelegate BattleEnded`
- Delegates:
  - `public delegate void BattleEndedDelegate(BattleResultData battleResult)`
- UnityEvents:
  - `void Awake()`
  - `void Start()`
  - `void Update()`
  - `void OnDestroy()`
- PublicMethods:
  - `IReadOnlyList<TreasureChest> GetExportedTreasureChestsTeamIndex(int systemTeamIndex)`
  - `void PrepareBattle()`
  - `void StartBattle()`
  - `BattleResultData FinalizeBattleResult()`
  - `void RefreshSpectatorFocusByDeveloperSetting()`
- OtherMethods:
  - `void UpdateVisibleWorldData()`
  - `void RefreshDebugView()`
  - `List<Vector2Int> FindCentralHallCells()`
  - `void SpawnAndRegisterPartyMembers(ComPartyBase party, int teamIndex, Vector2Int startCell)`
  - `void SetInitialMemberPosition(ComCharacterBase member, int memberIndex, Vector2Int startCell)`
  - `void ApplyMemberTeamColor(ExplorerAgent runtimeCharacter, ComCharacterBase member, int teamIndex, int memberIndex)`
  - `void ClearParties()`
  - `Vector2Int[] FindBattleStartCells()`
  - `Vector2Int FindBorderStartCellFromLeft()`
  - `Vector2Int FindBorderStartCellFromRight()`
  - `Vector2Int FindBorderStartCellFromBottom()`
  - `Vector2Int FindBorderStartCellFromTop()`
  - `void AddFallbackBorderStartCells(List<Vector2Int> result)`
  - `void TryAddStartCell(List<Vector2Int> result, Vector2Int cell)`
  - `bool IsValidStartCell(int x, int y)`
  - `bool HasAllTreasuresExported()`
  - `... (8 more)`
- SignatureReferencedTypes:
  - `BattleCombatSystem`
  - `BattleDeveloperSpectatorMode`
  - `BattleEndReason`
  - `BattleGameRuleData`
  - `BattleParticipantSystem`
  - `BattleResultData`
  - `BattleSpectatorCameraController`
  - `BattleSpectatorFocusIndicatorView`
  - `BattleSpectatorFocusSelector`
  - `BattleSpectatorFocusSettings`
  - `BattleTreasureSystem`
  - `BattleVisibleWorldSystem`
  - `CharacterMannequinReplacer`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `ExplorerAgent`
  - `KnownMapDebugTextView`
  - `MazeData`
  - `MazeGenerationSettings`
  - `MazeRenderer`
  - `... (3 more)`
- BodyReferencedTypes:
  - `BattleDeveloperSpectatorSettings`
  - `BattleGameConstants`
  - `BattleResultCalculator`
  - `CharacterTeamColorController`
  - `ExportedTreasureResult`
  - `FocusReason`
  - `FocusTargetType`
  - `ITreasureRuntime`
  - `KnownMapData`
  - `MazeCellData`
  - `MazeGenerator`
  - `ParticipantMazeBounds`
  - `TeamBattleResult`
  - `TreasureType`

#### BattlePrototypeGameController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Battle/BattlePrototypeGameController_Debug.cs`
- Fields:
  - `private int m_debugSelectedPartyIndex`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `KnownMapData GetDebugKnownMapData()`
  - `Vector2Int[] GetDebugCurrentCells()`
  - `int GetDebugSelectedPartyIndex()`
- OtherMethods:
  - `void HandleDebugKnownMapSelection(Keyboard keyboard)`
  - `void SetDebugSelectedPartyIndex(int partyIndex)`
- SignatureReferencedTypes:
  - `KnownMapData`
- BodyReferencedTypes: なし

#### BattlePrototypeSceneFlowController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Battle/BattlePrototypeSceneFlowController.cs`
- Fields:
  - `[SerializeField] private BattlePrototypeGameController m_battleGameController`
  - `[SerializeField] private BattleInGameHud m_battleInGameHud`
  - `[SerializeField] private BattleResultPresentation m_battleResultPresentation`
  - `[SerializeField] private BattleTeamIntroductionPresentation m_battleTeamIntroductionPresentation`
  - `[SerializeField] private BattleStartCountdownPresentation m_battleStartCountdownPresentation`
  - `[SerializeField] private BattleEndPresentation m_battleEndPresentation`
  - `private BattlePrototypeGameController m_battleGameController`
  - `private BattleInGameHud m_battleInGameHud`
  - `private BattleResultPresentation m_battleResultPresentation`
  - `private BattleTeamIntroductionPresentation m_battleTeamIntroductionPresentation`
  - `private BattleStartCountdownPresentation m_battleStartCountdownPresentation`
  - `private BattleEndPresentation m_battleEndPresentation`
  - `private bool m_isBattleStartCountdownCompleted`
  - `private bool m_isBattleStartCountdownInputAccepted`
  - `private bool m_isBattleEndPresentationProceedRequested`
  - `private SceneFlow m_sceneFlow`
  - `... (3 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void OnEnable()`
  - `void OnDisable()`
  - `void Start()`
- PublicMethods: なし
- OtherMethods:
  - `void OnBattleEnded(BattleResultData battleResult)`
  - `void ChangeSceneFlow(SceneFlow nextSceneFlow)`
  - `IEnumerator CoSceneInitialize()`
  - `IEnumerator CoSceneTeamIntroduction()`
  - `IEnumerator CoSceneCountDown()`
  - `IEnumerator CoScenePlaying()`
  - `IEnumerator CoSceneBattleEndWait()`
  - `IEnumerator CoSceneResult()`
  - `IEnumerator CoWaitForResultSequence()`
  - `IEnumerator CoSceneFinish()`
  - `void SetBattleHudVisible(bool isVisible)`
  - `void HideBattleTeamIntroductionPresentation()`
  - `void ShowBattleTeamIntroductionPresentation()`
  - `void HideBattleResultPresentation()`
  - `void ShowBattleResultPresentation(BattleResultData battleResult)`
  - `void RefreshDebugTimeScale()`
  - `... (11 more)`
- SignatureReferencedTypes:
  - `BattleEndPresentation`
  - `BattleInGameHud`
  - `BattlePrototypeGameController`
  - `BattleResultData`
  - `BattleResultPresentation`
  - `BattleStartCountdownPresentation`
  - `BattleTeamIntroductionPresentation`
  - `SceneFlow`
- BodyReferencedTypes:
  - `BattleEndReason`
  - `MazeThemeTransitionController`

#### PartyMovementAvoidanceSystem

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Battle/PartyMovementAvoidanceSystem.cs`
- Fields: なし
- Properties:
  - `public PartyMovementAvoidanceSystem Instance`
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void OnDestroy()`
- PublicMethods:
  - `Vector3 GetAdjustedMoveDirection(ExplorerAgent agent, Vector3 desiredDirection, float moveDistance)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ExplorerAgent`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Character

#### CharacterAnimationController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Character/CharacterAnimationController.cs`
- Fields:
  - `[SerializeField] private string m_resultIdleStateName`
  - `[SerializeField] private string m_resultVictoryStateName`
  - `[SerializeField] private string m_resultDefeatStateName`
  - `[SerializeField] private string m_resultTreasureReactStateName`
  - `[SerializeField] private string m_introductionIdleStateNamePrefix`
  - `[SerializeField] private int m_introductionIdleStateCount`
  - `[SerializeField] private string m_normalLocomotionStateName`
  - `private int s_runSpeedParameterHash`
  - `private int s_isCarryingParameterHash`
  - `private int s_punchTriggerHash`
  - `private int s_lowKickTriggerHash`
  - `private int s_isKnockedOutHash`
  - `private int s_deliveryThrowTriggerHash`
  - `private string m_resultIdleStateName`
  - `private string m_resultVictoryStateName`
  - `private string m_resultDefeatStateName`
  - `... (11 more)`
- Properties: なし
- Events: なし
- Delegates:
  - `public delegate void DeliveryThrowReleaseHandler(ComCharacterBase character)`
- UnityEvents:
  - `void Awake()`
  - `void Update()`
  - `void LateUpdate()`
- PublicMethods:
  - `void PlayPunch()`
  - `void PlayLowKick()`
  - `void PlayDeliveryThrow()`
  - `void CancelDeliveryThrow()`
  - `void SetKnockedOut(bool isKnockedOut)`
  - `void BeginResultPresentation()`
  - `void PlayResultIdle()`
  - `void PlayResultVictory()`
  - `void PlayResultDefeat()`
  - `void PlayResultTreasureReact()`
  - `void EndResultPresentation()`
  - `void BeginTeamPreviewPresentation()`
  - `void PlayTeamPreviewIdle()`
  - `void PlayTeamPreviewIntroductionRandomIdle()`
  - `void PlayTeamPreviewVictory()`
  - `void PlayTeamPreviewDefeat()`
  - `... (3 more)`
- OtherMethods:
  - `void BindCharacter(ComCharacterBase character)`
  - `void PlayResultState(string stateName)`
  - `void CreateTreasureCarryAnchor()`
  - `void CacheHumanoidHandBones()`
  - `void UpdateTreasureCarryAnchor()`
  - `void PlayNormalLocomotionState()`
- SignatureReferencedTypes:
  - `ComCharacterBase`
- BodyReferencedTypes: なし

#### CharacterMannequinReplacer

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Character/CharacterMannequinReplacer.cs`
- Fields:
  - `[SerializeField] private string m_mannequinRootName`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool ReplaceMannequin(ComCharacterBase character, Transform systemMannequin)`
- OtherMethods:
  - `bool DoesSubmittedMannequinContainSystemTree(Transform submittedMannequin, Transform systemMannequin)`
  - `void TransferAdditionalObjects(Transform submittedCurrent, Transform systemCurrent, Transform submittedMannequin, Transform systemMannequin, ComCharacterBase character)`
  - `void CopyAccessoryObject(Transform submittedAccessory, Transform destinationParent, Transform submittedMannequin, Transform systemMannequin, ComCharacterBase character)`
  - `void RemapSkinnedMeshBones(Transform submittedAccessoryRoot, Transform copiedAccessoryRoot, Transform submittedMannequin, Transform systemMannequin)`
  - `Transform FindRemappedBone(Transform submittedBone, Transform submittedAccessoryRoot, Transform copiedAccessoryRoot, Transform submittedMannequin, Transform systemMannequin)`
  - `void DisableAccessoryPhysicsAndScripts(GameObject copiedAccessory)`
  - `Transform FindDescendantByName(Transform root, string objectName)`
  - `void CollectTransforms(Transform root, List<Transform> result)`
  - `Transform FindByRelativePath(Transform root, string relativePath)`
  - `string GetRelativePath(Transform root, Transform target)`
  - `bool IsSameOrChildOf(Transform target, Transform parent)`
- SignatureReferencedTypes:
  - `ComCharacterBase`
- BodyReferencedTypes: なし

#### CharacterRagdollController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Character/CharacterRagdollController.cs`
- Fields:
  - `[SerializeField] private Animator m_animator`
  - `[SerializeField] private Transform m_ragdollRoot`
  - `[SerializeField] private Transform m_hips`
  - `[SerializeField] private Rigidbody m_impulseBody`
  - `[SerializeField] private AnimationClip m_getUpFaceUpClip`
  - `[SerializeField] private AnimationClip m_getUpFaceDownClip`
  - `[SerializeField] private float m_recoveryBlendDuration`
  - `[SerializeField] private float m_attackForwardImpulseMultiplier`
  - `[SerializeField] private float m_attackUpwardImpulse`
  - `[SerializeField] private float m_attackForwardVelocityChange`
  - `[SerializeField] private float m_attackUpwardVelocityChange`
  - `[SerializeField] private float m_attackSpinVelocityChange`
  - `private float m_attackForwardVelocityChange`
  - `private float m_attackUpwardVelocityChange`
  - `private float m_attackSpinVelocityChange`
  - `private int s_getUpFaceUpTriggerHash`
  - `... (16 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void FixedUpdate()`
- PublicMethods:
  - `void KnockOut(Vector3 attackForwardDirection, Vector3 attackerWorldPosition, float impulsePower)`
  - `void ForceEndRagdollForResultPresentation()`
  - `void Recover()`
  - `void OnGetUpAnimationFinished()`
- OtherMethods:
  - `void BindExplorerAgent(ExplorerAgent explorerAgent)`
  - `void BindCharacter(ComCharacterBase character)`
  - `void InterruptRecovery()`
  - `Vector3 FindApproximateHitPosition(Rigidbody hitBody, Vector3 attackerWorldPosition)`
  - `Rigidbody FindClosestRagdollBody(Vector3 attackerWorldPosition)`
  - `IEnumerator RecoverCoroutine()`
  - `void FinishRecoveryWithoutAnimation()`
  - `AnimationClip SelectGetUpClip(out int getUpTriggerHash)`
  - `void SetRagdollPhysicsEnabled(bool isEnabled)`
  - `void CaptureCurrentRagdollPose(List<BonePose> poseList)`
  - `void CaptureCurrentRagdollWorldPose(List<WorldBonePose> poseList)`
  - `void ApplyWorldPose(List<WorldBonePose> poseList)`
  - `int GetTransformDepth(Transform transform)`
  - `void CaptureAnimationStartPose(AnimationClip clip, List<BonePose> poseList)`
  - `void ApplyBlendedPose(float rate)`
  - `void ApplyPose(List<BonePose> poseList)`
- SignatureReferencedTypes:
  - `BonePose`
  - `ComCharacterBase`
  - `ExplorerAgent`
  - `WorldBonePose`
- BodyReferencedTypes: なし

#### CharacterTeamColorController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Character/CharacterTeamColorController.cs`
- Fields:
  - `private int s_baseColorPropertyId`
  - `private int s_colorPropertyId`
  - `private SkinnedMeshRenderer m_skinnedMeshRenderer`
  - `private MaterialPropertyBlock m_materialPropertyBlock`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods:
  - `void Initialize(int teamIndex, int memberIndex)`
- OtherMethods:
  - `bool EnsureComponents()`
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `CharacterTeamColorConstants`

### Folder: Assets/Dungeon/Program/Game/Debug

#### KnownMapDebugTextView

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Debug/KnownMapDebugTextView.cs`
- Fields:
  - `[SerializeField] private TextMeshProUGUI m_textMeshProUGUI`
  - `private BattlePrototypeGameController m_battleGameController`
  - `private KnownMapData m_knownMapData`
  - `private StringBuilder m_stringBuilder`
  - `private string m_teamName`
  - `private char[] m_partyName`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void OnDestroy()`
- PublicMethods:
  - `void BindBattleGameController(BattlePrototypeGameController battleGameController)`
  - `void BindKnownMapData(KnownMapData knownMapData, string teamName)`
  - `void RefreshView()`
- OtherMethods:
  - `void OnKnownMapUpdated()`
  - `void RefreshText()`
  - `char GetCellChar(Vector2Int cellPos, bool hasSingleCurrentCell, Vector2Int singleCurrentCell, Vector2Int[] partyCurrentCells)`
- SignatureReferencedTypes:
  - `BattlePrototypeGameController`
  - `KnownMapData`
- BodyReferencedTypes:
  - `KnownCellData`
  - `KnownCellViewType`

### Folder: Assets/Dungeon/Program/Game/Maze

#### ExplorerAgent

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour, ICharacterRuntime
- Path: `Assets/Dungeon/Program/Game/Maze/ExplorerAgent.cs`
- Fields:
  - `[SerializeField] private float m_stopDistance`
  - `[SerializeField] private float m_rotateSpeed`
  - `[SerializeField] private Rigidbody m_rigidbody`
  - `[SerializeField] private CapsuleCollider m_capsuleCollider`
  - `[SerializeField] private CharacterAnimationController m_characterAnimationController`
  - `[SerializeField] private CharacterRagdollController m_characterRagdollController`
  - `[SerializeField] private Transform m_systemMannequinTransform`
  - `public ExplorerAgentCellReachedHandler m_onCellReached`
  - `public ExplorerAgentMoveTargetReachedHandler m_onMoveTargetReached`
  - `private float m_moveSpeed`
  - `private CharacterAnimationController m_characterAnimationController`
  - `private CharacterRagdollController m_characterRagdollController`
  - `private Transform m_systemMannequinTransform`
  - `private CharacterTeamColorController[] m_characterTeamColorControllers`
  - `private Collider[] m_runtimeColliders`
  - `private MazeData m_mazeData`
  - `... (20 more)`
- Properties: なし
- Events: なし
- Delegates:
  - `public delegate void ExplorerAgentCellReachedHandler(ExplorerAgent agent, Vector2Int cellPosition)`
  - `public delegate void ExplorerAgentMoveTargetReachedHandler(ExplorerAgent agent)`
- UnityEvents:
  - `void Reset()`
  - `void Awake()`
  - `void FixedUpdate()`
- PublicMethods:
  - `void BindCharacter(ComCharacterBase character)`
  - `void SetPartyMemberIndex(int partyMemberIndex)`
  - `void SetMoveSpeed(float moveSpeed)`
  - `void BindMazeData(MazeData mazeData)`
  - `void Initialize(MazeData mazeData, Vector2Int startCell)`
  - `void Initialize(MazeData mazeData, Vector2Int startCell, float bodyHeight)`
  - `bool TryMoveToCell(Vector2Int targetCell)`
  - `bool TryMoveToWorld(Vector3 targetWorld)`
  - `void StopMove()`
  - `void SetPositionToWorld(Vector3 worldPosition)`
  - `void SetRotationToWorld(Quaternion worldRotation)`
  - `void UpdateRagdollWorldPosition(Vector3 worldPosition)`
  - `void ApplyKnockback(Vector3 direction, float knockbackSpeed)`
  - `void BeginRagdoll()`
  - `void SetRagdollRecoveryPosition(Vector3 worldPosition)`
  - `void EndRagdoll()`
- OtherMethods:
  - `ComCharacterBase GetCharacter()`
  - `Transform GetRuntimeTransform()`
  - `CharacterAnimationController GetCharacterAnimationController()`
  - `CharacterRagdollController GetCharacterRagdollController()`
  - `Transform GetSystemMannequinTransform()`
  - `CharacterTeamColorController[] GetCharacterTeamColorControllers()`
  - `Collider[] GetRuntimeColliders()`
  - `bool TryMoveToWorldInternal(Vector3 targetWorld, bool isCellTarget, Vector2Int targetCell)`
  - `void CompleteMoveTarget()`
  - `bool IsWorldPositionInsideCell(Vector3 worldPosition, Vector2Int cellPosition)`
  - `void SetPositionToCell(Vector2Int cell)`
  - `float GetSpawnY()`
  - `void UpdateCurrentCell()`
  - `Vector2Int FindNearestCell(Vector3 worldPosition)`
  - `bool IsWorldPositionInsideMaze(Vector3 worldPosition)`
  - `void SynchronizeCurrentCellAfterKnockback()`
  - `... (1 more)`
- SignatureReferencedTypes:
  - `CharacterAnimationController`
  - `CharacterRagdollController`
  - `CharacterTeamColorController`
  - `ComCharacterBase`
  - `MazeData`
- BodyReferencedTypes:
  - `MazeCellData`

#### ExplorerAgent

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour, ICharacterRuntime
- Path: `Assets/Dungeon/Program/Game/Maze/ExplorerAgent_Debug.cs`
- Fields:
  - `[SerializeField] private bool m_showMoveTargetGizmo`
  - `[SerializeField] private float m_moveTargetGizmoRadius`
  - `[SerializeField] private bool m_showPartyMemberIndexGizmo`
  - `[SerializeField] private bool m_showCharacterStateGizmo`
  - `[SerializeField] private bool m_showWorldPositionInGizmo`
  - `[SerializeField] private Vector3 m_partyMemberIndexGizmoOffset`
  - `private bool m_showCharacterStateGizmo`
  - `private bool m_showWorldPositionInGizmo`
  - `private Vector3 m_partyMemberIndexGizmoOffset`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void OnDrawGizmos()`
- PublicMethods: なし
- OtherMethods:
  - `void DrawMoveTargetGizmo(Vector3 currentPosition)`
  - `void DrawPartyMemberIndexGizmo(Vector3 currentPosition)`
  - `Color GetCharacterStateGizmoColor(bool isKnockedOut, bool isActionLocked)`
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `ComCharacterBase`

#### MazeRenderer

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Maze/MazeRenderer.cs`
- Fields:
  - `[SerializeField] private GameObject m_floorPrefab`
  - `[SerializeField] private GameObject m_solidBlockPrefab`
  - `[SerializeField] private float m_floorHeight`
  - `[SerializeField] private float m_solidBlockHeight`
  - `[SerializeField] private float m_outerWallThickness`
  - `[SerializeField] private float m_outerWallOutset`
  - `[SerializeField] private float m_wallCollisionHeight`
  - `[SerializeField] private Material m_mazeSurfaceMaterial`
  - `[SerializeField] private Material m_outerBoundaryMaterial`
  - `public GameObject m_floorObject`
  - `public GameObject m_solidBlockObject`
  - `private List<Mesh> m_runtimeMeshes`
  - `private Mesh m_corridorFloorMesh`
  - `private Mesh m_roomFloorMesh`
  - `private Mesh m_centralHallFloorMesh`
  - `private Mesh m_solidBlockMesh`
  - `... (5 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `MazeData GetMazeData()`
  - `void Build(MazeData mazeData)`
  - `void BindKnownMapData(KnownMapData knownMapData)`
  - `void Clear()`
- OtherMethods:
  - `void OnKnownMapUpdated()`
  - `void RefreshAllVisibility()`
  - `void RefreshCellVisibility(int x, int y)`
  - `GameObject CreateFloor(MazeCellData cell, Vector3 cellWorld)`
  - `void CreateOuterBoundaryWalls()`
  - `void CreateOuterWall(string wallName, Vector3 worldPosition, Vector3 worldScale)`
  - `GameObject CreateSolidBlock(MazeCellData cell, Vector3 cellWorld)`
  - `void SetActiveSafe(GameObject targetObject, bool isActive)`
  - `void ApplyMazeWallLayer(GameObject wallObject)`
  - `void ApplyLayerRecursively(Transform targetTransform, int layer)`
  - `void ExtendWallBoxColliderUpward(GameObject wallObject)`
  - `void CreateRuntimeMeshes()`
  - `Mesh CreateAndRegisterRuntimeBoxMesh(string meshName, Vector3 size, RuntimeBoxMeshFactory.SurfaceType topSurfaceType, RuntimeBoxMeshFactory.SurfaceType sideSurfaceType, RuntimeBoxMeshFactory.SurfaceType bottomSurfaceType)`
  - `GameObject CreateMazeSurfaceObject(GameObject prefab, string objectName, Mesh mesh, Vector3 worldPosition, bool isWall)`
  - `void ClearRuntimeMeshes()`
- SignatureReferencedTypes:
  - `CellVisualSet`
  - `KnownMapData`
  - `MazeCellData`
  - `MazeData`
  - `RuntimeBoxMeshFactory`
  - `SurfaceType`
- BodyReferencedTypes:
  - `KnownCellData`
  - `KnownCellViewType`

### Folder: Assets/Dungeon/Program/Game/Presentation/Hud

#### BattleInGameHud

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleInGameHud.cs`
- Fields:
  - `[SerializeField] private BattlePrototypeGameController m_battleGameController`
  - `[SerializeField] private TextMeshProUGUI m_remainingTimeText`
  - `[SerializeField] private Transform m_teamCardsRoot`
  - `[SerializeField] private BattleInGameHudTeamCard m_teamCardPrefab`
  - `[SerializeField] private Color m_normalRemainingTimeColor`
  - `[SerializeField] private Color m_cautionRemainingTimeColor`
  - `[SerializeField] private Color m_warningRemainingTimeColor`
  - `[SerializeField] private Color m_criticalRemainingTimeColor`
  - `[SerializeField] private Color m_timeUpFlashColor`
  - `[SerializeField] private TextMeshProUGUI m_centralChestStatusText`
  - `[SerializeField] private Color m_centralChestStatusStarColor`
  - `[SerializeField] private Color m_centralChestStatusBaseColor`
  - `[SerializeField] private Color m_centralChestStatusUnsecuredColor`
  - `private BattlePrototypeGameController m_battleGameController`
  - `private Transform m_teamCardsRoot`
  - `private BattleInGameHudTeamCard m_teamCardPrefab`
  - `... (18 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void Update()`
  - `void OnDestroy()`
- PublicMethods:
  - `void SetPresentationVisible(bool isVisible)`
- OtherMethods:
  - `void RefreshView()`
  - `void RefreshRemainingTime()`
  - `void RefreshRemainingTimeColor(float remainingSeconds)`
  - `float GetFlashRate(float flashSpeed)`
  - `void RefreshTeamCards()`
  - `void EnsureTeamCardCount(int requiredCount)`
  - `BattleInGameHudTeamCard CreateTeamCard()`
  - `void ClearTeamCards()`
  - `void RefreshCentralChestStatus()`
  - `void ApplyCentralChestStatusText(CentralChestStatusKind statusKind, int partyIndex)`
  - `TreasureChest FindCentralChest()`
  - `int FindOwnerPartyIndex(ComCharacterBase ownerCharacter)`
  - `int FindExportedTreasurePartyIndex(TreasureChest treasureChest)`
  - `ComPartyBase GetPartyByIndex(int partyIndex)`
  - `string GetPartyColorCode(int partyIndex)`
  - `string GetPartyDisplayName(ComPartyBase party)`
- SignatureReferencedTypes:
  - `BattleInGameHudTeamCard`
  - `BattlePrototypeGameController`
  - `CentralChestStatusKind`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `TreasureChest`
- BodyReferencedTypes:
  - `CharacterTeamColorConstants`
  - `ITreasureRuntime`
  - `TreasureType`

#### BattleInGameHudTeamCard

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleInGameHudTeamCard.cs`
- Fields:
  - `[SerializeField] private Image m_teamColorBarImage`
  - `[SerializeField] private Image m_teamIconImage`
  - `[SerializeField] private Image m_teamIconFrameImage`
  - `[SerializeField] private TextMeshProUGUI m_teamNameText`
  - `[SerializeField] private TextMeshProUGUI m_availableCountText`
  - `[SerializeField] private Image[] m_memberStateImages`
  - `[SerializeField] private Transform m_exportedTreasureRoot`
  - `[SerializeField] private Image m_smallTreasureIconPrefab`
  - `[SerializeField] private Image m_centralTreasureIconPrefab`
  - `[SerializeField] private Color m_knockedOutColor`
  - `[SerializeField] private bool m_centerExportedTreasures`
  - `[SerializeField] private float m_treasureIconSpacing`
  - `[SerializeField] private float m_treasureDropHeight`
  - `[SerializeField] private float m_treasureDropDuration`
  - `[SerializeField] private float m_treasureDropRotationDegrees`
  - `private bool m_centerExportedTreasures`
  - `... (9 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Update()`
- PublicMethods:
  - `void Refresh(ComPartyBase party, int systemTeamIndex)`
- OtherMethods:
  - `void UpdateTreasureDropAnimations()`
  - `void ApplyTeamIdentity(ComPartyBase party, Color teamColor)`
  - `void RefreshMemberStates(ComPartyBase party, Color teamColor)`
  - `void RefreshExportedTreasures(IReadOnlyList<ITreasureRuntime> exportedTreasures)`
  - `void CreateTreasureIcon(TreasureChest treasureChest, Image prefab)`
  - `void LayoutTreasureIcons()`
  - `float GetTreasureIconWidth(Image icon)`
  - `bool HasTreasureIcon(TreasureChest treasureChest)`
  - `void ClearTreasureIcons()`
- SignatureReferencedTypes:
  - `ComPartyBase`
  - `ITreasureRuntime`
  - `TreasureChest`
  - `TreasureIconVisual`
- BodyReferencedTypes:
  - `CharacterTeamColorConstants`
  - `ComCharacterBase`
  - `TreasureType`

#### BattleMazeMapView

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleMazeMapView.cs`
- Fields:
  - `[SerializeField] private BattlePrototypeGameController m_battleGameController`
  - `[SerializeField] private RectTransform m_squareArea`
  - `[SerializeField] private RectTransform m_cellLayer`
  - `[SerializeField] private RectTransform m_treasureMarkerLayer`
  - `[SerializeField] private RectTransform m_characterMarkerLayer`
  - `[SerializeField] private RectTransform m_fogLayer`
  - `[SerializeField] private Color m_blockedCellColor`
  - `[SerializeField] private Color m_corridorCellColor`
  - `[SerializeField] private Color m_roomCellColor`
  - `[SerializeField] private Color m_centralHallCellColor`
  - `[SerializeField] private Color m_outerWallColor`
  - `[SerializeField] private Color m_unknownFogColor`
  - `[SerializeField] private Shader m_flowingFogShader`
  - `[SerializeField] private Color m_flowingFogBaseColor`
  - `[SerializeField] private Color m_flowingFogCloudColor`
  - `[SerializeField] private Vector2 m_flowingFogDirection`
  - `... (44 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void LateUpdate()`
  - `void OnDestroy()`
- PublicMethods:
  - `void RefreshFogVisibilityByDeveloperSetting()`
  - `bool TryWorldPositionToNormalizedPosition(Vector3 worldPosition, out Vector2 normalizedPosition)`
  - `bool TryGetWorldPositionOnMap(Vector3 worldPosition, out Vector3 mapWorldPosition)`
- OtherMethods:
  - `void OnRectTransformDimensionsChange()`
  - `void Rebuild(MazeData mazeData)`
  - `void ClearView()`
  - `void BuildMazeCells()`
  - `void SubscribeKnownMapData()`
  - `void UnsubscribeKnownMapData()`
  - `void OnKnownCellUpdated(int x, int y, KnownCellData knownCellData)`
  - `void BuildFlowingFog()`
  - `void RefreshAllFogCells()`
  - `void RefreshFogCell(int x, int y)`
  - `void ApplyFogVisibilityMask()`
  - `void ReleaseFlowingFogResources()`
  - `bool IsObservedByAnyParty(int x, int y)`
  - `bool IsObservedByAllPartiesOr(int x, int y)`
  - `bool IsObservedByPartyIndex(int partyIndex, int x, int y)`
  - `Color GetCellColor(MazeCellData cell)`
  - `... (19 more)`
- SignatureReferencedTypes:
  - `BattlePrototypeGameController`
  - `CharacterMarkerVisual`
  - `KnownCellData`
  - `KnownMapData`
  - `MazeCellData`
  - `MazeData`
- BodyReferencedTypes:
  - `BattleDeveloperSpectatorSettings`
  - `CharacterTeamColorConstants`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `TreasureChest`
  - `TreasureType`

#### BattleStartCountdownPresentation

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleStartCountdownPresentation.cs`
- Fields:
  - `[SerializeField] private Image m_dimBackgroundImage`
  - `[SerializeField] private Image m_countdownImage`
  - `[SerializeField] private TextMeshProUGUI m_messageText`
  - `[SerializeField] private Sprite m_countdown3Sprite`
  - `[SerializeField] private Sprite m_countdown2Sprite`
  - `[SerializeField] private Sprite m_countdown1Sprite`
  - `[SerializeField] private Sprite m_explorationStartSprite`
  - `[SerializeField] private string m_waitingMessage`
  - `[SerializeField] private float m_countdownDisplayDuration`
  - `[SerializeField] private float m_countdownEntryDuration`
  - `[SerializeField] private float m_explorationStartDisplayDuration`
  - `[SerializeField] private float m_fadeOutDuration`
  - `[SerializeField] private float m_countdownInitialScale`
  - `private string m_waitingMessage`
  - `private float m_countdownDisplayDuration`
  - `private float m_countdownEntryDuration`
  - `... (9 more)`
- Properties: なし
- Events: なし
- Delegates:
  - `public delegate bool IsCountdownStartRequestedDelegate()`
  - `public delegate void CountdownCompletedDelegate()`
- UnityEvents:
  - `void Awake()`
  - `void Update()`
  - `void OnDisable()`
- PublicMethods:
  - `void Show(IsCountdownStartRequestedDelegate isCountdownStartRequested, CountdownCompletedDelegate countdownCompletedCallback)`
  - `void Hide()`
- OtherMethods:
  - `IEnumerator CoPlayCountdown()`
  - `IEnumerator CoShowCountdownSprite(Sprite sprite)`
  - `void ShowSpriteImmediately(Sprite sprite)`
  - `IEnumerator CoFadeOutAndHide()`
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/Result

#### BattleEndPresentation

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleEndPresentation.cs`
- Fields:
  - `[SerializeField] private CanvasGroup m_canvasGroup`
  - `[SerializeField] private RectTransform m_endContentRoot`
  - `[SerializeField] private Image m_endImage`
  - `[SerializeField] private TextMeshProUGUI m_endReasonText`
  - `[SerializeField] private string m_timeLimitReachedText`
  - `[SerializeField] private string m_allTreasuresExportedText`
  - `[SerializeField] private float m_showFadeInDuration`
  - `[SerializeField] private float m_showEntryDuration`
  - `[SerializeField] private float m_showSettleDuration`
  - `[SerializeField] private float m_showInitialScale`
  - `[SerializeField] private float m_showImpactScale`
  - `[SerializeField] private float m_fadeOutDuration`
  - `private CanvasGroup m_canvasGroup`
  - `private RectTransform m_endContentRoot`
  - `private Image m_endImage`
  - `private TextMeshProUGUI m_endReasonText`
  - `... (13 more)`
- Properties: なし
- Events: なし
- Delegates:
  - `public delegate bool IsProceedRequestedDelegate()`
  - `public delegate void ProceedRequestedDelegate()`
- UnityEvents:
  - `void Awake()`
  - `void Update()`
  - `void OnDisable()`
- PublicMethods:
  - `void Show(BattleEndReason endReason, IsProceedRequestedDelegate isProceedRequested, ProceedRequestedDelegate proceedRequestedCallback)`
  - `void Hide()`
- OtherMethods:
  - `IEnumerator CoShowAndWaitForProceed()`
  - `IEnumerator CoCloseByProceed()`
  - `IEnumerator CoFadeCanvasGroup(float fromAlpha, float toAlpha, float duration)`
  - `IEnumerator CoScaleEndContent(Vector3 fromScale, Vector3 toScale, float duration)`
  - `string GetEndReasonText(BattleEndReason endReason)`
- SignatureReferencedTypes:
  - `BattleEndReason`
- BodyReferencedTypes: なし

#### BattleResultLightEffectRotation

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleResultLightEffectRotation.cs`
- Fields:
  - `[SerializeField] private float m_rotationSpeed`
  - `private float m_rotationSpeed`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void OnEnable()`
  - `void Update()`
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### BattleResultPresentation

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleResultPresentation.cs`
- Fields:
  - `[SerializeField] private BattleResultTeamLane m_teamLanePrefab`
  - `[SerializeField] private RectTransform m_teamLanesRoot`
  - `[SerializeField] private BattleTeamPreviewController m_battleTeamPreviewController`
  - `[SerializeField] private float m_resultStartDelay`
  - `[SerializeField] private float m_totalCoinCountUpDuration`
  - `[SerializeField] private float m_treasureRevealInterval`
  - `[SerializeField] private float m_treasureOpenRevealDelay`
  - `[SerializeField] private float m_resultFinishHoldDuration`
  - `[SerializeField] private float m_centralChestPreOpenDelay`
  - `private int TeamCount`
  - `private BattleResultTeamLane m_teamLanePrefab`
  - `private RectTransform m_teamLanesRoot`
  - `private BattleTeamPreviewController m_battleTeamPreviewController`
  - `private float m_resultStartDelay`
  - `private float m_totalCoinCountUpDuration`
  - `private float m_treasureRevealInterval`
  - `... (7 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods:
  - `void Show(BattleResultData battleResult, IReadOnlyList<ComPartyBase> spawnedParties)`
  - `void StartResultSequence(BattleResultData battleResult)`
  - `void Hide()`
- OtherMethods:
  - `IEnumerator CoPlayResultSequence(BattleResultData battleResult)`
  - `IEnumerator CoPlayTreasureType(BattleResultData battleResult, TreasureType treasureType, int[] currentTotalValues, int[] revealedTreasureCounts, bool[] isDefeatPoseApplied)`
  - `IEnumerator CoApplyTreasureResult(BattleResultData battleResult, int teamIndex, ExportedTreasureResult treasureResult, int[] currentTotalValues, int[] revealedTreasureCounts, bool[] isDefeatPoseApplied)`
  - `void UpdateProvisionalRanks(int[] currentTotalValues)`
  - `void ApplyDefeatPoseForEliminatedTeams(BattleResultData battleResult, int[] currentTotalValues, int[] revealedTreasureCounts, bool[] isDefeatPoseApplied)`
  - `int GetProvisionalRank(int teamIndex, int[] currentTotalValues)`
  - `bool IsDefeatPoseApplied(bool[] isDefeatPoseApplied, int teamIndex)`
  - `void ApplyFinalResult(BattleResultData battleResult, int[] currentTotalValues, bool[] isDefeatPoseApplied)`
  - `int GetMaximumTreasureCountByType(BattleResultData battleResult, TreasureType treasureType)`
  - `int GetTreasureCountByType(TeamBattleResult teamResult, TreasureType treasureType)`
  - `ExportedTreasureResult GetTreasureByTypeAndOrder(TeamBattleResult teamResult, TreasureType treasureType, int treasureOrder)`
  - `BattleResultTeamLane GetTeamLaneTeamIndex(int teamIndex)`
  - `void StopResultSequence()`
  - `void DestroySpawnedTeamLanes()`
  - `TeamBattleResult FindTeamResultByTeamIndex(BattleResultData battleResult, int teamIndex)`
  - `ComPartyBase GetPartyByTeamIndex(IReadOnlyList<ComPartyBase> spawnedParties, int teamIndex)`
  - `... (1 more)`
- SignatureReferencedTypes:
  - `BattleResultData`
  - `BattleResultTeamLane`
  - `BattleTeamPreviewController`
  - `ComPartyBase`
  - `ExportedTreasureResult`
  - `TeamBattleResult`
  - `TreasureType`
- BodyReferencedTypes:
  - `BattleTeamPreviewPose`

#### BattleResultTeamLane

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleResultTeamLane.cs`
- Fields:
  - `[SerializeField] private Image m_teamColorFrameImage`
  - `[SerializeField] private Sprite[] m_teamColorFrameSprites`
  - `[SerializeField] private Image m_teamIconBaseImage`
  - `[SerializeField] private Image m_teamIconImage`
  - `[SerializeField] private TextMeshProUGUI m_teamNameText`
  - `[SerializeField] private Sprite[] m_teamNameBaseSprites`
  - `[SerializeField] private Image m_teamNameBaseImage`
  - `[SerializeField] private Image m_rankImage`
  - `[SerializeField] private Sprite[] m_rankSprites`
  - `[SerializeField] private TextMeshProUGUI m_totalCoinText`
  - `[SerializeField] private TextMeshProUGUI m_addedCoinText`
  - `[SerializeField] private RectTransform m_addedCoinPopupRoot`
  - `[SerializeField] private float m_addedCoinPopupViewportUpOffset`
  - `[SerializeField] private float m_addedCoinPopupRiseDistance`
  - `[SerializeField] private float m_addedCoinPopupPopDuration`
  - `[SerializeField] private float m_addedCoinPopupHoldDuration`
  - `... (44 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Initialize(int systemTeamIndex, ComPartyBase party, TeamBattleResult teamResult)`
  - `void SetPreviewTexture(RenderTexture renderTexture)`
  - `void SetRank(int rank)`
  - `void SetTotalCoin(int totalCoin)`
  - `void SetGoldTowerScaleReferenceTotal(int maximumBattleTotalCoin)`
  - `IEnumerator CoShowAddedCoinPopup(int addedCoin, Camera previewCamera, Vector3 treasureWorldPosition)`
  - `void ShowAddedCoin(int addedCoin)`
  - `void HideAddedCoin()`
  - `void SetLightEffectVisible(bool isVisible)`
- OtherMethods:
  - `void CacheGoldTowerBaseLayout()`
  - `void UpdateGoldTowerPresentation()`
- SignatureReferencedTypes:
  - `ComPartyBase`
  - `TeamBattleResult`
- BodyReferencedTypes:
  - `CharacterTeamColorConstants`

#### BattleTeamPreviewController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewController.cs`
- Fields:
  - `[SerializeField] private BattleTeamPreviewStage m_teamPreviewStagePrefab`
  - `[SerializeField] private BattleTeamPreviewStage m_introductionPreviewStagePrefab`
  - `[SerializeField] private TreasureChest m_resultTreasureChestPrefab`
  - `[SerializeField] private Vector3 m_stageBaseLocalPosition`
  - `[SerializeField] private float m_stageLocalSpacing`
  - `[SerializeField] private string[] m_teamPreviewLayerNames`
  - `[SerializeField] private Vector3 m_resultTreasureBaseLocalPosition`
  - `[SerializeField] private float m_resultTreasureHorizontalRange`
  - `[SerializeField] private int m_resultTreasurePlacementCandidateCount`
  - `[SerializeField] private int m_resultTreasurePlacementRetryCount`
  - `[SerializeField] private float m_resultTreasureCastStartHeight`
  - `[SerializeField] private float m_resultTreasureDropStartClearance`
  - `[SerializeField] private float m_resultTreasureSettleTimeout`
  - `[SerializeField] private float m_resultTreasureSettleHoldSeconds`
  - `[SerializeField] private float m_resultTreasureSettleLinearSpeed`
  - `[SerializeField] private float m_resultTreasureSettleAngularSpeed`
  - `... (82 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void OnDestroy()`
- PublicMethods:
  - `void Show(IReadOnlyList<ComPartyBase> spawnedParties, BattleTeamPreviewPose previewPose)`
  - `void Show(IReadOnlyList<ComPartyBase> spawnedParties, BattleResultData battleResult, BattleTeamPreviewPose previewPose)`
  - `void SetPreviewPose(BattleTeamPreviewPose previewPose)`
  - `void SetPreviewPoseTeamIndex(int systemTeamIndex, BattleTeamPreviewPose previewPose)`
  - `RenderTexture GetPreviewTextureTeamIndex(int systemTeamIndex)`
  - `int GetResultTreasureIdByTypeAndOrder(int teamIndex, TreasureType treasureType, int treasureOrder)`
  - `bool TryGetResultTreasurePresentationPosition(int treasureId, out Camera previewCamera, out Vector3 treasureWorldPosition)`
  - `bool SetResultTreasureOpened(int treasureId, bool isOpened)`
  - `bool StartResultTreasureGlow(int treasureId)`
  - `bool SetResultTreasureCoinsVisible(int treasureId, bool isVisible)`
  - `bool ThrowAwayResultTreasure(int treasureId)`
  - `bool PlayResultTreasureGoldCoinFountain(int treasureId, int addedScore)`
  - `void Hide(bool restoreMembers)`
- OtherMethods:
  - `void PlayResultTreasureOpenEffect(TreasureChest resultTreasureChest)`
  - `GameObject InstantiateResultTreasureEffect(GameObject effectPrefab, TreasureChest resultTreasureChest)`
  - `void StopResultTreasureGlow(int treasureId)`
  - `TreasureChest FindResultTreasureChest(int treasureId)`
  - `void DestroyAllResultTreasureGlowEffects()`
  - `IEnumerator CoPlayResultTreasureGoldCoinFountain(GameObject goldCoinFountainEffect, int treasureId, int addedScore)`
  - `void CreatePreviewStages(BattleTeamPreviewPose previewPose)`
  - `BattleTeamPreviewStage GetPreviewStagePrefab(BattleTeamPreviewPose previewPose)`
  - `RenderTexture CreateRuntimeRenderTexture(BattleTeamPreviewStage previewStage, int teamIndex)`
  - `void CreateResultTreasureChests(int teamIndex, TeamBattleResult teamResult, BattleTeamPreviewStage previewStage)`
  - `void PlaceResultTreasureChests(int teamIndex, BattleTeamPreviewStage previewStage, int placementVersion)`
  - `void PlaceSingleResultTreasure(ResultTreasureSpawnData spawnData, BattleTeamPreviewStage previewStage, bool isCentralChest, int normalChestOrder, int placementVersion)`
  - `bool TryFindResultTreasureDropPosition(TreasureChest treasureChest, BattleTeamPreviewStage previewStage, int teamIndex, int treasureIndex, bool isCentralChest, int normalChestOrder, out Vector3 dropWorldPosition)`
  - `Vector3 GetCentralChestHorizontalOffset(BattleTeamPreviewStage previewStage, Vector3 baseWorldPosition)`
  - `Vector3 GetResultTreasureRearCandidateHorizontalOffset(BattleTeamPreviewStage previewStage, Vector3 baseWorldPosition, int teamIndex, int treasureIndex, int candidateIndex)`
  - `bool IsTooCloseToCentralChest(int teamIndex, Vector3 candidateWorldPosition)`
  - `... (24 more)`
- SignatureReferencedTypes:
  - `BattleResultData`
  - `BattleTeamPreviewPose`
  - `BattleTeamPreviewStage`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `GameObjectLayerRestoreData`
  - `MemberRestoreData`
  - `ResultTreasureSpawnData`
  - `TeamBattleResult`
  - `TreasureChest`
  - `TreasureType`
- BodyReferencedTypes:
  - `CharacterAnimationController`
  - `CharacterRagdollController`
  - `ExplorerAgent`
  - `ExportedTreasureResult`
  - `PartyMemberOrder`

#### BattleTeamPreviewStage

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewStage.cs`
- Fields:
  - `[SerializeField] private Camera m_previewCamera`
  - `[SerializeField] private Transform[] m_memberAnchors`
  - `private Camera m_previewCamera`
  - `private Transform[] m_memberAnchors`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `Transform GetMemberAnchor(int memberIndex)`
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/SceneTransition

#### MazeThemeTransitionController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/SceneTransition/MazeThemeTransitionController.cs`
- Fields:
  - `[SerializeField] private RawImage m_themeRawImage`
  - `[SerializeField] private Texture m_themeTexture`
  - `[SerializeField] private Shader m_transitionShader`
  - `[SerializeField] private float m_transitionDuration`
  - `[SerializeField] private float m_fadeOutHoldDuration`
  - `[SerializeField] private int m_cellSizePixels`
  - `[SerializeField] private int m_randomSeed`
  - `private string RESOURCE_PREFAB_PATH`
  - `private MazeThemeTransitionController s_instance`
  - `private bool s_isCreatingInstance`
  - `private RawImage m_themeRawImage`
  - `private Texture m_themeTexture`
  - `private Shader m_transitionShader`
  - `private float m_transitionDuration`
  - `private float m_fadeOutHoldDuration`
  - `private int m_cellSizePixels`
  - `... (10 more)`
- Properties: なし
- Events: なし
- Delegates:
  - `public delegate void FadeCompletedDelegate()`
- UnityEvents:
  - `void Awake()`
  - `void OnDestroy()`
- PublicMethods:
  - `void FadeIn(FadeCompletedDelegate onCompleted = null)`
  - `void FadeOut(FadeCompletedDelegate onCompleted = null)`
  - `void TransitionToScene(string sceneName)`
  - `void ShowImmediately()`
  - `void HideImmediately()`
- OtherMethods:
  - `void ResetStaticState()`
  - `void CreateBeforeFirstSceneLoad()`
  - `void EnsureInstance()`
  - `void ValidateReferences()`
  - `void CreateMaterial()`
  - `void StartTransition(bool isFadeIn, FadeCompletedDelegate onCompleted)`
  - `IEnumerator PlayTransitionCoroutine(bool isFadeIn, FadeCompletedDelegate onCompleted)`
  - `IEnumerator TransitionToSceneCoroutine(string sceneName)`
  - `void StopCurrentTransition()`
  - `void PrepareGrid()`
  - `void BuildRevealOrder()`
  - `void AddVisitedCell(Vector2Int cell, bool[,] visitedCells, bool[,] queuedCells)`
  - `void TryAddFrontierCell(Vector2Int cell, bool[,] visitedCells, bool[,] queuedCells)`
  - `bool IsValidCell(Vector2Int cell)`
  - `void FillMask(float value)`
  - `void ApplyCellsUntil(int targetCellCount, bool isFadeIn)`
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/Spectator

#### BattleSpectatorCameraController

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorCameraController.cs`
- Fields:
  - `[SerializeField] private ComCharacterBase m_initialFocusCharacter`
  - `[SerializeField] private TreasureChest m_initialFocusTreasureChest`
  - `[SerializeField] private float m_characterFocusHeight`
  - `[SerializeField] private float m_characterDistance`
  - `[SerializeField] private float m_characterHeight`
  - `[SerializeField] private float m_characterSideOffset`
  - `[SerializeField] private float m_treasureFocusHeight`
  - `[SerializeField] private float m_treasureDistance`
  - `[SerializeField] private float m_treasureHeight`
  - `[SerializeField] private float m_treasureSideOffset`
  - `[SerializeField] private float m_positionSmoothTime`
  - `[SerializeField] private float m_rotationSmoothSpeed`
  - `[SerializeField] private float m_warpFocusDistance`
  - `[SerializeField] private float m_nearFocusPositionSmoothTime`
  - `[SerializeField] private float m_nearFocusRotationReleaseDistance`
  - `[SerializeField] private LayerMask m_wallLayerMask`
  - `... (34 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void LateUpdate()`
  - `void OnDrawGizmosSelected()`
- PublicMethods:
  - `void SetFocusCharacter(ComCharacterBase character)`
  - `void SetFocusCharacter(ComCharacterBase character, Vector3 desiredBackwardDirection)`
  - `void SetFocusTreasureChest(TreasureChest treasureChest)`
  - `void SetFocusTreasureChest(TreasureChest treasureChest, Vector3 desiredBackwardDirection)`
  - `void ClearFocus()`
  - `void SetFixedBackwardDirection(Vector3 backwardDirection)`
- OtherMethods:
  - `bool BeginFocusTransition(Vector3 nextFocusPosition)`
  - `bool TryGetCurrentFocusPosition(out Vector3 currentFocusPosition)`
  - `bool TryGetFocusData(out Vector3 focusPosition, out Vector3 backwardDirection, out float cameraDistance, out float cameraHeight, out float sideOffset)`
  - `bool TryGetCharacterFocusData(out Vector3 focusPosition, out Vector3 backwardDirection, out float cameraDistance, out float cameraHeight, out float sideOffset)`
  - `bool TryGetTreasureChestFocusData(out Vector3 focusPosition, out Vector3 backwardDirection, out float cameraDistance, out float cameraHeight, out float sideOffset)`
  - `void UpdateStableDirectionFromCharacter(ComCharacterBase character)`
  - `Vector3 CalculateDesiredCameraPosition(Vector3 focusPosition, Vector3 backwardDirection, float cameraDistance, float cameraHeight, float sideOffset)`
  - `Vector3 ResolveWallCollision(Vector3 focusPosition, Vector3 desiredCameraPosition)`
  - `void UpdateCameraTransform(Vector3 focusPosition, Vector3 targetCameraPosition)`
  - `void InitializeCameraPositionIfNeeded()`
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `FocusTargetType`
  - `TreasureChest`
- BodyReferencedTypes: なし

#### BattleSpectatorFocusIndicatorView

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusIndicatorView.cs`
- Fields:
  - `[SerializeField] private BattleMazeMapView m_battleMazeMapView`
  - `[SerializeField] private RectTransform m_spectatorRootRectTransform`
  - `[SerializeField] private Image m_spectatorFrameImage`
  - `[SerializeField] private BattleSpectatorFocusTailGraphic m_tailGraphic`
  - `[SerializeField] private float m_tailAlpha`
  - `[SerializeField] private float m_tailBaseHalfHeight`
  - `[SerializeField] private float m_tailRootCenterPull`
  - `[SerializeField] private float m_tailMinimumDiagonalOffset`
  - `[SerializeField] private Color m_noFocusFrameColor`
  - `private BattleMazeMapView m_battleMazeMapView`
  - `private RectTransform m_spectatorRootRectTransform`
  - `private Image m_spectatorFrameImage`
  - `private BattleSpectatorFocusTailGraphic m_tailGraphic`
  - `private float m_tailAlpha`
  - `private float m_tailBaseHalfHeight`
  - `private float m_tailRootCenterPull`
  - `... (7 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void LateUpdate()`
- PublicMethods:
  - `void SetFocusCharacter(ComCharacterBase character, int systemTeamIndex)`
  - `void SetFocusTreasureChest(TreasureChest treasureChest, int systemTeamIndex)`
  - `void ClearFocus()`
- OtherMethods:
  - `void RefreshFrameColor()`
  - `void RefreshTail()`
  - `bool TryGetFocusWorldPosition(out Vector3 focusWorldPosition)`
  - `bool TryConvertWorldToTailLocalPosition(Vector3 worldPosition, out Vector2 localPosition)`
  - `void ClearTail()`
- SignatureReferencedTypes:
  - `BattleMazeMapView`
  - `BattleSpectatorFocusTailGraphic`
  - `ComCharacterBase`
  - `TreasureChest`
- BodyReferencedTypes:
  - `CharacterTeamColorConstants`

### Folder: Assets/Dungeon/Program/Game/Presentation/TeamIntroduction

#### BattleTeamIntroductionPresentation

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/TeamIntroduction/BattleTeamIntroductionPresentation.cs`
- Fields:
  - `[SerializeField] private BattleTeamPreviewController m_battleTeamPreviewController`
  - `[SerializeField] private BattleTeamIntroductionTeamPanel[] m_teamPanels`
  - `private int TeamCount`
  - `private BattleTeamPreviewController m_battleTeamPreviewController`
  - `private BattleTeamIntroductionTeamPanel[] m_teamPanels`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods:
  - `void Show(IReadOnlyList<ComPartyBase> spawnedParties)`
  - `void Hide()`
- OtherMethods:
  - `BattleTeamIntroductionTeamPanel GetTeamPanelTeamIndex(int teamIndex)`
  - `ComPartyBase GetPartyTeamIndex(IReadOnlyList<ComPartyBase> spawnedParties, int teamIndex)`
- SignatureReferencedTypes:
  - `BattleTeamIntroductionTeamPanel`
  - `BattleTeamPreviewController`
  - `ComPartyBase`
- BodyReferencedTypes:
  - `BattleTeamPreviewPose`

#### BattleTeamIntroductionTeamPanel

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Presentation/TeamIntroduction/BattleTeamIntroductionTeamPanel.cs`
- Fields:
  - `[SerializeField] private RawImage m_previewRawImage`
  - `[SerializeField] private Image m_teamIconImage`
  - `[SerializeField] private TMP_Text m_creatorNameText`
  - `[SerializeField] private TMP_Text m_teamNameText`
  - `[SerializeField] private TMP_Text m_teamDescriptionText`
  - `private RawImage m_previewRawImage`
  - `private Image m_teamIconImage`
  - `private TMP_Text m_creatorNameText`
  - `private TMP_Text m_teamNameText`
  - `private TMP_Text m_teamDescriptionText`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Set(ComPartyBase party, RenderTexture previewTexture)`
  - `void Clear()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ComPartyBase`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Treasure

#### TreasureChest

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour, ITreasureRuntime
- Path: `Assets/Dungeon/Program/Game/Treasure/TreasureChest.cs`
- Fields:
  - `[SerializeField] private Collider m_pickupTrigger`
  - `[SerializeField] private GameObject m_visualRoot`
  - `[SerializeField] private GameObject m_chestCloseModelRoot`
  - `[SerializeField] private GameObject m_chestOpenModelRoot`
  - `[SerializeField] private GameObject m_resultCoinsObject`
  - `[SerializeField] private MeshRenderer m_chestCloseMeshRenderer`
  - `[SerializeField] private MeshRenderer m_chestOpenMeshRenderer`
  - `[SerializeField] private Texture2D m_standardChestTexture`
  - `[SerializeField] private Texture2D m_centralChestTexture`
  - `[SerializeField] private Rigidbody m_dropRigidbody`
  - `[SerializeField] private Collider m_physicsCollider`
  - `[SerializeField] private float m_dropHorizontalImpulse`
  - `[SerializeField] private float m_dropUpwardImpulse`
  - `[SerializeField] private float m_dropTorqueImpulse`
  - `[SerializeField] private float m_dropMaxPhysicsSeconds`
  - `[SerializeField] private float m_dropSettleSpeed`
  - `... (31 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
  - `void Update()`
  - `void LateUpdate()`
  - `void FixedUpdate()`
  - `void OnTriggerEnter(Collider other)`
  - `void OnTriggerStay(Collider other)`
- PublicMethods:
  - `void Initialize(int treasureId, TreasurePlacement placement, MazeData mazeData)`
  - `void SetBattleTreasureSystem(BattleTreasureSystem battleTreasureSystem)`
  - `bool IsPickupAvailable()`
  - `bool AttachToCharacter(ComCharacterBase ownerCharacter)`
  - `bool DetachAndDrop(ComCharacterBase ownerCharacter, Vector3 dropDirection, float pickupLockSeconds)`
  - `bool ThrowForExport(ComCharacterBase ownerCharacter, Vector3 throwDirection, float horizontalImpulse, float upwardImpulse, float torqueImpulse, float visibleSeconds)`
  - `bool Export(ComCharacterBase ownerCharacter)`
  - `void SetResultPresentationVisible(bool isVisible)`
  - `void SetOpenedForResultPresentation(bool isOpened)`
  - `void SetResultCoinsVisible(bool isVisible)`
  - `void ThrowAwayForResultPresentation(Vector3 throwDirection, float horizontalSpeed, float upwardSpeed, float torqueImpulse, float hideAfterSeconds)`
  - `void InitializeForResultPresentation(int treasureId, TreasureType treasureType)`
  - `bool TryGetResultPlacementColliderInfo(out Vector3 colliderCenterOffset, out Vector3 colliderExtents, out float castRadius)`
  - `void BeginResultPlacementPhysics()`
  - `void FinishResultPlacementPhysics()`
  - `void CancelResultPlacementPhysics()`
  - `... (1 more)`
- OtherMethods:
  - `void SetDropPhysicsEnabled(bool isEnabled)`
  - `void TryRequestPickupByCollider(Collider other)`
  - `void ApplyTextureByTreasureType(TreasureType treasureType)`
  - `void ApplyTextureToChestMeshRenderer(MeshRenderer chestMeshRenderer, Texture2D texture, string modelName)`
  - `void RefreshCurrentCellPosition()`
  - `void SetPickupTriggerEnabled(bool isEnabled)`
  - `void SetVisualVisible(bool isVisible)`
  - `void IgnoreOwnerPhysicsCollisions(ComCharacterBase ownerCharacter)`
  - `void RestoreOwnerPhysicsCollisions()`
- SignatureReferencedTypes:
  - `BattleTreasureSystem`
  - `ComCharacterBase`
  - `MazeData`
  - `TreasurePlacement`
  - `TreasureType`
- BodyReferencedTypes:
  - `CharacterAnimationController`
  - `ExplorerAgent`
  - `ICharacterRuntime`
  - `TreasurePresentationConstants`

#### TreasureChestSpawner

- Kind: class
- Namespace: (global)
- BaseTypes: MonoBehaviour
- Path: `Assets/Dungeon/Program/Game/Treasure/TreasureChestSpawner.cs`
- Fields:
  - `[SerializeField] private TreasureChest m_treasureChestPrefab`
  - `[SerializeField] private Transform m_treasureRoot`
  - `private List<TreasureChest> m_spawnedTreasureChests`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Build(MazeData mazeData)`
  - `void Clear()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `MazeData`
  - `TreasureChest`
- BodyReferencedTypes:
  - `TreasurePlacement`
  - `TreasurePlacementPlanner`

## Non-MonoBehaviours

### Folder: Assets/Dungeon/ParticipantApi

#### KnownCellInfo

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/KnownCellInfo.cs`
- Fields: なし
- Properties:
  - `public Vector2Int CellPosition`
  - `public bool IsObserved`
  - `public bool IsRoom`
  - `public bool IsCentralHall`
  - `public int RoomId`
  - `public MazeDirection KnownOpenDirections`
  - `public bool IsWalkable`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsOpen(MazeDirection direction)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `MazeDirection`
- BodyReferencedTypes: なし

#### KnownMapView

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/KnownMapView.cs`
- Fields:
  - `private KnownMapData m_knownMapData`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsInside(Vector2Int cellPosition)`
  - `bool TryGetCell(Vector2Int cellPosition, out KnownCellInfo cellInfo)`
  - `bool IsObserved(Vector2Int cellPosition)`
  - `bool IsWalkable(Vector2Int cellPosition)`
  - `bool CanMove(Vector2Int from, Vector2Int to)`
  - `bool TryGetOpenNeighbor(Vector2Int from, Vector2Int direction, out Vector2Int neighbor)`
- OtherMethods:
  - `bool TryGetDirection(Vector2Int from, Vector2Int to, out MazeDirection direction)`
  - `MazeDirection GetOppositeDirection(MazeDirection direction)`
- SignatureReferencedTypes:
  - `KnownCellInfo`
  - `KnownMapData`
  - `MazeDirection`
- BodyReferencedTypes:
  - `KnownCellData`

#### PartyMemberInfo

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/PartyMemberInfo.cs`
- Fields: なし
- Properties:
  - `public int CharacterId`
  - `public int MemberIndex`
  - `public Vector2Int CurrentCell`
  - `public Vector3 WorldPosition`
  - `public bool IsMoving`
  - `public bool IsKnockedOut`
  - `public bool IsActionLocked`
  - `public bool HasTreasure`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### TreasureType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/TreasureType.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/ParticipantApi/Internal

#### ComCharacterBase

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/ComCharacterBase.Internal.cs`
- Fields:
  - `private ICharacterRuntime m_characterRuntime`
  - `private ComPartyBase m_ownerParty`
  - `private KnownMapData m_knownMapData`
  - `private KnownMapView m_knownMapView`
  - `private int m_memberIndex`
  - `private PartyMemberOrder m_currentOrder`
  - `private ITreasureRuntime m_carriedTreasure`
  - `private int m_characterId`
  - `private bool m_isKnockedOut`
  - `private float m_actionLockEndTime`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods:
  - `void SetCharacterId(int characterId)`
  - `void Initialize(ComPartyBase ownerParty, KnownMapData knownMapData, int memberIndex, ICharacterRuntime characterRuntime)`
  - `void Think()`
  - `void SetOrder(PartyMemberOrder order)`
  - `void ApplyOrder()`
  - `ICharacterRuntime GetExplorerAgent()`
  - `int GetCharacterId()`
  - `int GetMemberIndex()`
  - `Vector2Int GetCurrentCell()`
  - `Vector3 GetWorldPosition()`
  - `bool IsMoving()`
  - `bool IsKnockedOut()`
  - `bool IsActionLocked()`
  - `bool HasTreasure()`
  - `ITreasureRuntime GetCarriedTreasure()`
  - `void NotifyCellReached(Vector2Int cellPosition)`
  - `... (8 more)`
- SignatureReferencedTypes:
  - `ComPartyBase`
  - `ICharacterRuntime`
  - `ITreasureRuntime`
  - `KnownMapData`
  - `KnownMapView`
  - `PartyMemberOrder`
  - `TreasureType`
  - `VisibleCharacterData`
- BodyReferencedTypes:
  - `PartyMemberOrderType`

#### ComPartyBase

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/ComPartyBase.Internal.cs`
- Fields:
  - `private ParticipantMazeBounds m_mazeBounds`
  - `private KnownMapData m_knownMapData`
  - `private KnownMapView m_knownMapView`
  - `private List<ComCharacterBase> m_members`
  - `private bool m_isStarted`
  - `private Vector2Int m_returnEntranceCell`
  - `private bool m_hasReturnEntranceCell`
  - `private List<Vector2Int> m_exportEntranceCells`
  - `private List<ITreasureRuntime> m_exportedTreasures`
  - `private VisibleWorldData m_visibleWorldData`
  - `private Sprite s_defaultTeamIconImage`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods:
  - `ComCharacterBase GetMemberPrefab()`
  - `void Initialize(ParticipantMazeBounds mazeBounds, KnownMapData knownMapData, Vector2Int[] entranceCellsByRelativeTeamIndex)`
  - `void RegisterMembers(ComCharacterBase[] members)`
  - `void StartParty()`
  - `void Think()`
  - `bool TryGetMember(int memberIndex, out ComCharacterBase member)`
  - `Vector2Int[] GetMemberCurrentCells()`
  - `KnownMapData GetKnownMapData()`
  - `VisibleWorldData GetVisibleWorldData()`
  - `IReadOnlyList<ITreasureRuntime> GetExportedTreasureChests()`
  - `bool TryGetReturnEntranceCell(out Vector2Int returnEntranceCell)`
  - `void SetReturnEntranceCell(Vector2Int returnEntranceCell)`
  - `bool IsExportEntranceCell(Vector2Int cellPosition)`
  - `void AddExportedTreasure(ITreasureRuntime treasure)`
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `ITreasureRuntime`
  - `KnownMapData`
  - `KnownMapView`
  - `ParticipantMazeBounds`
  - `VisibleWorldData`
- BodyReferencedTypes: なし

#### ICharacterRuntime

- Kind: interface
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/ParticipantRuntimeContracts.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `ComCharacterBase`

#### ITreasureRuntime

- Kind: interface
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/ParticipantRuntimeContracts.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `TreasureType`

#### KnownCellData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/KnownMapData.cs`
- Fields:
  - `public bool m_isObserved`
  - `public bool m_isRoom`
  - `public bool m_isCentralHall`
  - `public int m_roomId`
  - `public MazeDirection m_knownOpenDirections`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsBlockedCell()`
  - `bool IsOpen(MazeDirection direction)`
  - `KnownCellViewType GetViewType()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `KnownCellViewType`
  - `MazeDirection`
- BodyReferencedTypes: なし

#### KnownCellViewType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/KnownMapData.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### KnownMapData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/KnownMapData.cs`
- Fields:
  - `public int m_width`
  - `public int m_height`
  - `public KnownCellData[,] m_cells`
  - `private List<Vector2Int> m_centralHallCells`
- Properties: なし
- Events:
  - `public event KnownCellUpdatedHandler m_onKnownCellUpdated`
  - `public event KnownMapUpdatedHandler m_onKnownMapUpdated`
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `KnownCellData GetCell(int x, int y)`
  - `bool IsObserved(int x, int y)`
  - `Vector2Int[] GetCentralHallCells()`
  - `void SetCentralHallCells(List<Vector2Int> CentralHallCells)`
  - `void ObserveCentralHallCells(MazeData mazeData)`
  - `void ObserveCell(MazeCellData sourceCell)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `KnownCellData`
  - `MazeCellData`
  - `MazeData`
- BodyReferencedTypes: なし

#### MazeCellData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/MazeCellData.cs`
- Fields:
  - `public int m_x`
  - `public int m_y`
  - `public MazeDirection m_openDirections`
  - `public bool m_isCentralHall`
  - `public bool m_isRoom`
  - `public int m_roomId`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsOpen(MazeDirection direction)`
  - `bool IsBlockedCell()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `MazeDirection`
- BodyReferencedTypes: なし

#### MazeData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/MazeData.cs`
- Fields:
  - `public int m_width`
  - `public int m_height`
  - `public MazeCellData[,] m_cells`
  - `private float m_cellSize`
  - `public float CellSize`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `MazeCellData GetCell(int x, int y)`
  - `bool IsInside(int x, int y)`
  - `Vector2Int GetCenterCellPosition()`
  - `Vector3 CellToWorld(int x, int y)`
  - `bool TryWorldToCell(Vector3 worldPosition, out Vector2Int cellPosition)`
  - `bool TryWorldToCell(Vector3 worldPosition, int width, int height, float cellSize, out Vector2Int cellPosition)`
  - `List<MazeCellData> GetRoomCells(int roomId)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `MazeCellData`
- BodyReferencedTypes: なし

#### MazeDirection

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/MazeCellData.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### ParticipantMazeBounds

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/ParticipantRuntimeContracts.cs`
- Fields:
  - `private int m_width`
  - `private int m_height`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsInside(Vector2Int cellPosition)`
  - `bool IsInside(int x, int y)`
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### PartyMemberOrder

- Kind: struct
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/PartyMemberOrder.cs`
- Fields:
  - `public PartyMemberOrderType m_orderType`
  - `public Vector2Int m_targetCell`
  - `public Vector3 m_targetWorld`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `PartyMemberOrder CreateMoveToCell(Vector2Int targetCell)`
  - `PartyMemberOrder CreateMoveToWorld(Vector3 targetWorld)`
  - `PartyMemberOrder CreateStop()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `PartyMemberOrderType`
- BodyReferencedTypes: なし

#### PartyMemberOrderType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/PartyMemberOrder.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### VisibleCharacterData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/VisibleWorldData.cs`
- Fields: なし
- Properties:
  - `public int CharacterId`
  - `public int TeamIndex`
  - `public Vector3 WorldPosition`
  - `public Vector2Int CellPosition`
  - `public Vector3 Forward`
  - `public bool HasTreasure`
  - `public bool IsKnockedOut`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### VisibleTreasureData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/VisibleWorldData.cs`
- Fields: なし
- Properties:
  - `public int TreasureId`
  - `public Vector3 WorldPosition`
  - `public Vector2Int CellPosition`
  - `public TreasureType TreasureType`
  - `public bool IsCarried`
  - `public int OwnerCharacterId`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureType`
- BodyReferencedTypes: なし

#### VisibleWorldData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/ParticipantApi/Internal/VisibleWorldData.cs`
- Fields:
  - `private List<VisibleCharacterData> m_visibleCharacters`
  - `private List<VisibleTreasureData> m_visibleTreasures`
  - `private ReadOnlyCollection<VisibleCharacterData> m_readOnlyVisibleCharacters`
  - `private ReadOnlyCollection<VisibleTreasureData> m_readOnlyVisibleTreasures`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool TryGetVisibleCharacter(int characterId, out VisibleCharacterData visibleCharacter)`
  - `bool TryGetVisibleTreasure(int treasureId, out VisibleTreasureData visibleTreasure)`
- OtherMethods:
  - `void Clear()`
  - `void AddCharacter(VisibleCharacterData visibleCharacter)`
  - `void AddTreasure(VisibleTreasureData visibleTreasure)`
- SignatureReferencedTypes:
  - `VisibleCharacterData`
  - `VisibleTreasureData`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/BattleRules

#### BattleGameConstants

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/BattleRules/BattleGameConstants.cs`
- Fields:
  - `public int PartyMemberCount`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Battle

#### BattleEndReason

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/BattleEndReason.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### BattleGameRuleData

- Kind: class
- Namespace: (global)
- BaseTypes: ScriptableObject
- Path: `Assets/Dungeon/Program/Game/Battle/BattleGameRuleData.cs`
- Fields:
  - `[SerializeField] private TreasureValueRange[] m_treasureValueRanges`
  - `private float m_battleDurationSeconds`
  - `private float m_normalMoveSpeed`
  - `private float m_carryingTreasureMoveSpeed`
  - `private float m_attackRange`
  - `private float m_attackStartRange`
  - `private float m_attackAngleDegrees`
  - `private int m_attackAnimationFrameRate`
  - `private int m_attackHitFrame`
  - `private float m_attackCooldownSeconds`
  - `private float m_knockoutSeconds`
  - `private float m_knockbackSpeed`
  - `private float m_treasureScatterMinDistance`
  - `private float m_treasureScatterMaxDistance`
  - `private float m_treasurePickupLockSeconds`
  - `private float m_attackMotionLockSeconds`
  - `... (21 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool TryGetTreasureValueRange(TreasureType treasureType, out int minValue, out int maxValue)`
  - `int GetRandomTreasureValue(TreasureType treasureType)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureType`
  - `TreasureValueRange`
- BodyReferencedTypes: なし

#### BattleVisibleWorldSystem

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/BattleVisibleWorldSystem.cs`
- Fields:
  - `private HashSet<int> m_visibleCharacterIds`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void UpdateVisibleWorldData(MazeData mazeData, List<ComPartyBase> parties, IReadOnlyList<TreasureChest> treasureChests)`
- OtherMethods:
  - `void RegisterCharacterTeamIndices(List<ComPartyBase> parties)`
  - `void UpdatePartyVisibleWorldData(MazeData mazeData, List<ComPartyBase> parties, ComPartyBase observerParty, int observerSystemTeamIndex, IReadOnlyList<TreasureChest> treasureChests)`
  - `void AddVisibleCharacters(MazeData mazeData, List<ComPartyBase> parties, ComPartyBase observerParty, int observerSystemTeamIndex, VisibleWorldData visibleWorldData)`
  - `void AddVisibleCharacter(ComCharacterBase character, int visibleTeamIndex, VisibleWorldData visibleWorldData)`
  - `void AddVisibleTreasures(MazeData mazeData, ComPartyBase observerParty, IReadOnlyList<TreasureChest> treasureChests, VisibleWorldData visibleWorldData)`
  - `bool IsCharacterVisibleToParty(MazeData mazeData, ComPartyBase observerParty, ComCharacterBase targetCharacter)`
  - `bool IsCellVisibleToParty(MazeData mazeData, ComPartyBase observerParty, Vector2Int targetCellPosition)`
  - `bool CanSeeCell(MazeData mazeData, Vector2Int observerCellPosition, Vector2Int targetCellPosition)`
  - `bool CanSeeStraightLine(MazeData mazeData, Vector2Int observerCellPosition, Vector2Int targetCellPosition, MazeDirection direction)`
  - `int GetRelativeTeamIndex(int targetSystemTeamIndex, int observerSystemTeamIndex, int partyCount)`
  - `Vector2Int GetDirectionOffset(MazeDirection direction)`
  - `MazeDirection GetOppositeDirection(MazeDirection direction)`
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `ComPartyBase`
  - `MazeData`
  - `MazeDirection`
  - `TreasureChest`
  - `VisibleWorldData`
- BodyReferencedTypes:
  - `MazeCellData`
  - `TreasureType`
  - `VisibleCharacterData`
  - `VisibleTreasureData`

#### SceneFlow

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/BattlePrototypeSceneFlowController.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### TreasureValueRange

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/BattleGameRuleData.cs`
- Fields:
  - `public TreasureType m_treasureType`
  - `public int m_minValue`
  - `public int m_maxValue`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureType`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Battle/Result

#### BattleResultCalculator

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/Result/BattleResultCalculator.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `BattleResultData CreateResult(List<ComPartyBase> parties, BattleGameRuleData battleGameRuleData)`
- OtherMethods:
  - `void AdjustPositiveTotalValues(BattleResultData result, BattleGameRuleData battleGameRuleData)`
  - `void GetTotalRange(TeamBattleResult teamResult, BattleGameRuleData battleGameRuleData, out int minTotal, out int maxTotal)`
  - `int FindUnusedNearestTotal(int originalTotal, int minTotal, int maxTotal, HashSet<int> usedTotals)`
  - `void ApplyTotalValue(TeamBattleResult teamResult, BattleGameRuleData battleGameRuleData, int targetTotal)`
  - `void AssignRanks(BattleResultData result)`
  - `void SelectTournamentRepresentative(BattleResultData result)`
- SignatureReferencedTypes:
  - `BattleGameRuleData`
  - `BattleResultData`
  - `ComPartyBase`
  - `TeamBattleResult`
- BodyReferencedTypes:
  - `ExportedTreasureResult`
  - `ITreasureRuntime`
  - `TreasureType`

#### BattleResultData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/Result/BattleResultData.cs`
- Fields:
  - `private List<TeamBattleResult> m_teamResults`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void AddTeamResult(TeamBattleResult teamResult)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TeamBattleResult`
- BodyReferencedTypes: なし

#### ExportedTreasureResult

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/Result/BattleResultData.cs`
- Fields: なし
- Properties:
  - `public int TreasureId`
  - `public TreasureType TreasureType`
  - `public int Value`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void SetValue(int value)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureType`
- BodyReferencedTypes: なし

#### TeamBattleResult

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/Result/BattleResultData.cs`
- Fields:
  - `private List<ExportedTreasureResult> m_treasures`
- Properties:
  - `public int TeamIndex`
  - `public int TotalValue`
  - `public int Rank`
  - `public bool IsTournamentRepresentative`
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void AddTreasure(ExportedTreasureResult treasure)`
  - `void RefreshTotalValue()`
  - `void SetRank(int rank)`
  - `void SetTournamentRepresentative(bool isRepresentative)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ExportedTreasureResult`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Battle/System

#### AttackFocusTargetState

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/System/BattleCombatSystem.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### BattleCombatSystem

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/System/BattleCombatSystem.cs`
- Fields:
  - `public ComCharacterBase m_attacker`
  - `public ComCharacterBase m_target`
  - `public float m_hitTime`
  - `private float s_attackFocusScoreHoldSeconds`
  - `private List<PendingAttack> m_pendingAttacks`
  - `private List<PendingAttack> m_dueAttacks`
  - `private BattleGameRuleData m_ruleData`
  - `private BattleTreasureSystem m_battleTreasureSystem`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Initialize(BattleGameRuleData ruleData, IReadOnlyList<ComPartyBase> parties, BattleTreasureSystem battleTreasureSystem)`
  - `void Shutdown()`
  - `void UpdateSystem()`
  - `bool IsCharacterAttacking(ComCharacterBase character)`
  - `bool TryGetAttackFocusScore(ComCharacterBase attacker, out float score)`
  - `bool HasEnemyInAttackArea(ComCharacterBase attacker)`
- OtherMethods:
  - `AttackFocusTargetState GetAttackFocusTargetState(ComCharacterBase target)`
  - `bool IsCharacterAttackFocusScoreHeld(ComCharacterBase character)`
  - `void RegisterCharacter(ComPartyBase party, int teamIndex, ComCharacterBase character)`
  - `void RecoverKnockedOutCharacters()`
  - `void RequestAttacksFromParticipants()`
  - `bool CanStartAttack(ComCharacterBase attacker)`
  - `List<VisibleCharacterData> CreateAttackCandidates(ComCharacterBase attacker)`
  - `bool TryFindCandidateTarget(IReadOnlyList<VisibleCharacterData> candidates, int selectedCandidateIndex, out ComCharacterBase target)`
  - `void StartAttack(ComCharacterBase attacker, ComCharacterBase target)`
  - `void ResolveDueAttacks()`
  - `bool CanHitAtResolveTime(PendingAttack attack, Dictionary<ComCharacterBase, bool> wasKnockedOut)`
  - `void KnockOutCharacter(ComCharacterBase attacker, ComCharacterBase target)`
  - `bool HasPendingAttack(ComCharacterBase attacker)`
  - `bool IsInAttackStartArea(ComCharacterBase attacker, ComCharacterBase target)`
  - `bool IsInAttackArea(ComCharacterBase attacker, ComCharacterBase target)`
  - `bool IsInAttackArea(ComCharacterBase attacker, ComCharacterBase target, float attackRange)`
  - `... (3 more)`
- SignatureReferencedTypes:
  - `AttackFocusTargetState`
  - `BattleGameRuleData`
  - `BattleTreasureSystem`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `PendingAttack`
  - `VisibleCharacterData`
- BodyReferencedTypes:
  - `CharacterAnimationController`
  - `CharacterRagdollController`
  - `ExplorerAgent`
  - `VisibleWorldData`

#### BattleParticipantSystem

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/System/BattleParticipantSystem.cs`
- Fields:
  - `private List<ComPartyBase> m_parties`
  - `private MazeData m_mazeData`
  - `private BattleTreasureSystem m_battleTreasureSystem`
  - `private BattleGameRuleData m_battleGameRuleData`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Initialize(MazeData mazeData, IReadOnlyList<ComPartyBase> parties, BattleTreasureSystem battleTreasureSystem, BattleGameRuleData battleGameRuleData)`
  - `void Shutdown()`
  - `void UpdateSystem()`
- OtherMethods:
  - `void RegisterCharacter(ComPartyBase party, ComCharacterBase character)`
  - `void ThinkParticipants()`
  - `void TryExportTreasuresAtEntrance()`
  - `void ApplyParticipantOrders()`
  - `void OnAgentCellReached(ExplorerAgent agent, Vector2Int cellPosition)`
  - `void ApplyMoveSpeed(ExplorerAgent agent, ComCharacterBase character)`
- SignatureReferencedTypes:
  - `BattleGameRuleData`
  - `BattleTreasureSystem`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `ExplorerAgent`
  - `MazeData`
- BodyReferencedTypes:
  - `ExplorerSensor`

#### BattleTreasureSystem

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/System/BattleTreasureSystem.cs`
- Fields:
  - `private List<TreasureChest> m_treasureChests`
  - `private float s_exportFocusScoreHoldSeconds`
  - `private MazeData m_mazeData`
  - `private BattleGameRuleData m_ruleData`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void Initialize(MazeData mazeData, BattleGameRuleData ruleData, IReadOnlyList<ComPartyBase> parties, IReadOnlyList<TreasureChest> treasureChests)`
  - `void Shutdown()`
  - `void TryRequestPickUpTreasure(TreasureChest treasureChest, ComCharacterBase character)`
  - `void TryExportTreasure(ComCharacterBase character)`
  - `void CancelDeliveryThrow(ComCharacterBase character)`
  - `bool TryGetExportFocusTreasureType(ComCharacterBase character, out TreasureType exportedTreasureType)`
  - `void DropCarriedTreasure(ComCharacterBase character)`
  - `void DropCarriedTreasure(ComCharacterBase character, Vector3 dropDirection)`
- OtherMethods:
  - `void OnDeliveryThrowRelease(ComCharacterBase character)`
  - `void RegisterParties(IReadOnlyList<ComPartyBase> parties)`
  - `void RegisterTreasureChests(IReadOnlyList<TreasureChest> treasureChests)`
  - `Vector3 FindTreasureDropPosition(Vector3 centerPosition)`
- SignatureReferencedTypes:
  - `BattleGameRuleData`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `MazeData`
  - `TreasureChest`
  - `TreasureType`
- BodyReferencedTypes:
  - `CharacterAnimationController`
  - `ExplorerAgent`
  - `MazeCellData`

#### PendingAttack

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Battle/System/BattleCombatSystem.cs`
- Fields:
  - `public ComCharacterBase m_attacker`
  - `public ComCharacterBase m_target`
  - `public float m_hitTime`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ComCharacterBase`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Character

#### BonePose

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Character/CharacterRagdollController.cs`
- Fields:
  - `public Transform m_transform`
  - `public Vector3 m_localPosition`
  - `public Quaternion m_localRotation`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### CharacterTeamColorConstants

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Character/CharacterTeamColorConstants.cs`
- Fields:
  - `private Color[] s_teamBaseColors`
  - `private float[] s_memberBrightnessMultipliers`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `Color GetCharacterColor(int teamIndex, int memberIndex)`
  - `Color GetTeamBaseColor(int systemTeamIndex)`
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### WorldBonePose

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Character/CharacterRagdollController.cs`
- Fields:
  - `public Transform m_transform`
  - `public Vector3 m_worldPosition`
  - `public Quaternion m_worldRotation`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Data

#### BattlePartyRegistry

- Kind: class
- Namespace: (global)
- BaseTypes: ScriptableObject
- Path: `Assets/Dungeon/Program/Game/Data/BattlePartyRegistry.cs`
- Fields:
  - `[SerializeField] private ComPartyBase[] m_partyPrefabs`
  - `private ComPartyBase[] m_partyPrefabs`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void RegisterPartyPrefabToFrontByEditor(ComPartyBase partyPrefab)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ComPartyBase`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Debug

#### BattleDeveloperSpectatorMode

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Debug/BattleDeveloperSpectatorSettings.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### BattleDeveloperSpectatorSettings

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Debug/BattleDeveloperSpectatorSettings.cs`
- Fields:
  - `public string EditorPrefsModeKey`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `BattleDeveloperSpectatorMode GetMode()`
  - `void SetMode(BattleDeveloperSpectatorMode mode)`
  - `int GetRestrictedSystemTeamIndex()`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `BattleDeveloperSpectatorMode`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Maze

#### AdditionalRoomPlan

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `public Vector2Int m_roomCenter`
  - `public Vector2Int m_doorDirection`
  - `public List<Vector2Int> m_connectorPath`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### CellVisualSet

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeRenderer.cs`
- Fields:
  - `public GameObject m_floorObject`
  - `public GameObject m_solidBlockObject`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### CoarseEdge

- Kind: struct
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `public Vector2Int m_a`
  - `public Vector2Int m_b`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### CoarseEdgeOrbit

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `public string m_key`
  - `public List<CoarseEdge> m_edges`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `CoarseEdge`
- BodyReferencedTypes: なし

#### DisjointSet

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `private int[] m_parent`
  - `private int[] m_rank`
  - `private int m_componentCount`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `DisjointSet Clone()`
  - `int Find(int value)`
  - `bool Union(int a, int b)`
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### ExplorerSensor

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/ExplorerSensor.cs`
- Fields:
  - `private MazeData m_mazeData`
  - `private KnownMapData m_knownMapData`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `void ObserveFromCell(Vector2Int currentCellPosition)`
- OtherMethods:
  - `void ObserveRoom(int roomId)`
  - `void ObserveAdjacentOutsideCells(MazeCellData roomCell, int roomId)`
  - `void TryObserveOutside(int x, int y, int roomId)`
  - `void ObserveStraightLine(int startX, int startY, Vector2Int direction)`
- SignatureReferencedTypes:
  - `KnownMapData`
  - `MazeCellData`
  - `MazeData`
- BodyReferencedTypes: なし

#### MazeGenerationSettings

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `public int m_width`
  - `public int m_height`
  - `public bool m_useRandomSeed`
  - `public int m_seed`
  - `public int m_randomRoomCountMin`
  - `public int m_randomRoomCountMax`
  - `public int m_roomMinWidth`
  - `public int m_roomMaxWidth`
  - `public int m_roomMinHeight`
  - `public int m_roomMaxHeight`
  - `public int m_roomMargin`
  - `public int m_extraConnectionCount`
  - `public bool m_reduceCorridorPlaza`
  - `public int m_corridorPlazaReductionPassCount`
  - `public bool m_cleanupRoomAdjacentCorridors`
  - `public int m_roomAdjacentCleanupPassCount`
  - `... (8 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### MazeGenerator

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `private int s_minimumMapSize`
  - `private int s_centerRoomSize`
  - `private int s_smallRoomSize`
  - `private int s_roomWallMargin`
  - `private int s_initialSmallRoomSetCount`
  - `private int s_minAdditionalRoomSetCount`
  - `private int s_maxAdditionalRoomSetCount`
  - `private int s_maxLoopRoomPromotionSetCount`
  - `private int s_maxLoopRoomPromotionCellCount`
  - `private int s_minLoopRoomPromotionRoomSize`
  - `private int s_maxLoopRoomPromotionRoomSize`
  - `private int s_minExtraLoopOrbitCount`
  - `private int s_maxExtraLoopOrbitCount`
  - `private int s_coarseFirstCoordinate`
  - `private int s_coarseStep`
  - `private int s_maxGenerationAttemptCount`
  - `... (18 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `DisjointSet Clone()`
  - `int Find(int value)`
  - `bool Union(int a, int b)`
  - `MazeData Generate(MazeGenerationSettings settings, float cellSize)`
- OtherMethods:
  - `MazeData GenerateInternal(MazeGenerationSettings settings, float cellSize, System.Random random, int usedSeed, int attemptIndex)`
  - `MazeGenerationSettings NormalizeSettings(MazeGenerationSettings settings)`
  - `System.Random CreateRandom(MazeGenerationSettings settings, out int usedSeed, out string seedSource)`
  - `int GetRandomPreapprovedMazeSeed()`
  - `int CreateNonDeterministicSeed()`
  - `void InitializeRoomIdMap(int[,] roomIdMap)`
  - `void CreateCentralRoom(int width, ref int nextRoomId, bool[,] openMap, bool[,] roomMap, int[,] roomIdMap, bool[,] protectedWallMap, bool[,] doorMap, List<RoomNode> roomNodes)`
  - `void CreateInitialSmallRoomSets(System.Random random, int width, ref int nextRoomId, bool[,] openMap, bool[,] roomMap, int[,] roomIdMap, bool[,] protectedWallMap, bool[,] doorMap, List<RoomNode> roomNodes)`
  - `bool TryFindInitialSmallRoomSetPlacement(System.Random random, int width, bool[,] protectedWallMap, out Vector2Int sourceCenter)`
  - `bool CanReserveFourFoldRoomSet(Vector2Int sourceCenter, int width, bool[,] protectedWallMap)`
  - `bool CanReserveRoomArea(Vector2Int center, int roomSize, int width, bool[,] protectedWallMap)`
  - `void CreateFourFoldSmallRoomSet(Vector2Int sourceCenter, int width, ref int nextRoomId, bool[,] openMap, bool[,] roomMap, int[,] roomIdMap, bool[,] protectedWallMap, bool[,] doorMap, List<RoomNode> roomNodes)`
  - `void CreateRoom(RoomNode roomNode, int minX, int minY, int roomWidth, int roomHeight, bool[,] openMap, bool[,] roomMap, int[,] roomIdMap, bool[,] protectedWallMap)`
  - `void AddDoor(RoomNode roomNode, Vector2Int doorCell, bool[,] doorMap)`
  - `void CreateCoarseNodes(int width, bool[,] protectedWallMap, out List<Vector2Int> coarseNodes, out Dictionary<Vector2Int, int> coarseNodeIndices)`
  - `HashSet<Vector2Int> CreateTerminalNodes(int width, List<RoomNode> roomNodes)`
  - `... (53 more)`
- SignatureReferencedTypes:
  - `AdditionalRoomPlan`
  - `CoarseEdge`
  - `CoarseEdgeOrbit`
  - `DisjointSet`
  - `MazeData`
  - `MazeDirection`
  - `MazeGenerationSettings`
  - `RoomNode`
- BodyReferencedTypes:
  - `MazeCellData`

#### RoomNode

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/MazeGenerator.cs`
- Fields:
  - `public int m_roomId`
  - `public Vector2Int m_center`
  - `public List<Vector2Int> m_doorCells`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### RuntimeBoxMeshFactory

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/RuntimeBoxMeshFactory.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `Mesh CreateBoxMesh(string meshName, Vector3 size, SurfaceType topSurfaceType, SurfaceType sideSurfaceType, SurfaceType bottomSurfaceType)`
- OtherMethods:
  - `void AddFace(Vector3[] vertices, Vector3[] normals, Vector2[] uvs, Vector2[] surfaceTypes, int[] triangles, ref int vertexIndex, ref int triangleIndex, Vector3 vertex0, Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 normal, SurfaceType surfaceType)`
- SignatureReferencedTypes:
  - `SurfaceType`
- BodyReferencedTypes: なし

#### SurfaceType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Maze/RuntimeBoxMeshFactory.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/Hud

#### CentralChestStatusKind

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleInGameHud.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### CharacterMarkerVisual

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleMazeMapView.cs`
- Fields:
  - `public RectTransform m_rootRectTransform`
  - `public GameObject m_knockedOutCrossObject`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### TreasureIconVisual

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Hud/BattleInGameHudTeamCard.cs`
- Fields:
  - `public TreasureChest m_treasureChest`
  - `public Image m_iconImage`
  - `public Vector2 m_targetPosition`
  - `public float m_animationStartTime`
  - `public bool m_isDropAnimating`
  - `public float m_startRotationZ`
  - `public float m_rotationDirection`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureChest`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/Result

#### BattleTeamPreviewPose

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewController.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### GameObjectLayerRestoreData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewController.cs`
- Fields:
  - `public GameObject GameObject`
  - `public int Layer`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### MemberRestoreData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewController.cs`
- Fields:
  - `public ComCharacterBase Member`
  - `public int SystemTeamIndex`
  - `public Vector3 WorldPosition`
  - `public Quaternion WorldRotation`
  - `public TreasureChest CarriedTreasureChest`
  - `public List<GameObjectLayerRestoreData> GameObjectLayerRestoreDataList`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `GameObjectLayerRestoreData`
  - `TreasureChest`
- BodyReferencedTypes: なし

#### ResultTreasureSpawnData

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Result/BattleTeamPreviewController.cs`
- Fields:
  - `public int TeamIndex`
  - `public int TreasureIndex`
  - `public TreasureType TreasureType`
  - `public TreasureChest TreasureChest`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureChest`
  - `TreasureType`
- BodyReferencedTypes: なし

### Folder: Assets/Dungeon/Program/Game/Presentation/Spectator

#### BattleSpectatorFocusSelector

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusSelector.cs`
- Fields:
  - `private float s_carryingCentralChestBaseScore`
  - `private float s_carryingTreasureBaseScore`
  - `private float s_treasurePickupScoreBonus`
  - `private float s_treasurePickupScoreHoldSeconds`
  - `private float s_treasureNearExportScoreBonus`
  - `private int s_treasureNearExportCellDistance`
  - `private float s_attackingTreasureCarrierScore`
  - `private float s_attackingNormalTargetScore`
  - `private float s_attackingUnavailableTargetScore`
  - `private float s_enemyInAttackAreaScore`
  - `private float s_nearEnemyBaseScore`
  - `private float s_centralChestScore`
  - `private float s_nearMazeCenterBaseScore`
  - `private float s_maxNearbyInfluenceScore`
  - `private BattleSpectatorFocusSettings m_settings`
  - `private List<FocusCandidate> m_focusCandidates`
  - `... (18 more)`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool TrySelectFocus(MazeData mazeData, IReadOnlyList<ComPartyBase> parties, IReadOnlyList<TreasureChest> treasureChests, BattleCombatSystem battleCombatSystem, BattleTreasureSystem battleTreasureSystem, int restrictedSystemTeamIndex, out FocusTargetType selectedTargetType, out ComCharacterBase selectedCharacter, out TreasureChest selectedTreasureChest, out int selectedSystemTeamIndex, out FocusReason selectedReason)`
  - `void ClearCurrentFocus()`
  - `bool IsSameTarget(FocusCandidate other)`
  - `int GetStableTargetId()`
  - `FocusCandidate CreateNone()`
  - `FocusCandidate CreateCharacter(ComCharacterBase character, int systemTeamIndex, FocusReason reason, float primaryScore)`
  - `FocusCandidate CreateTreasureChest(TreasureChest treasureChest, FocusReason reason, float score)`
- OtherMethods:
  - `void BuildFocusCandidates(MazeData mazeData, IReadOnlyList<ComPartyBase> parties, IReadOnlyList<TreasureChest> treasureChests, BattleCombatSystem battleCombatSystem, BattleTreasureSystem battleTreasureSystem, int restrictedSystemTeamIndex)`
  - `FocusCandidate EvaluateCharacterCandidate(ComCharacterBase character, int systemTeamIndex, IReadOnlyList<ComPartyBase> parties, MazeData mazeData, Vector3 mazeCenterWorld, BattleCombatSystem battleCombatSystem, BattleTreasureSystem battleTreasureSystem)`
  - `float EvaluateCarryingTreasureScore(ComCharacterBase character, int systemTeamIndex, IReadOnlyList<ComPartyBase> parties, MazeData mazeData, TreasureChest treasureChest)`
  - `float GetTreasurePickupScoreBonus(TreasureChest treasureChest)`
  - `bool TryGetNearestExportEntranceCellDistance(Vector2Int sourceCell, ComPartyBase party, MazeData mazeData, out int nearestDistance)`
  - `void ApplyNearbyCharacterInfluence()`
  - `void EvaluateCentralChestCandidates(IReadOnlyList<TreasureChest> treasureChests)`
  - `FocusCandidate FindBestCandidate()`
  - `bool TryFindCurrentCandidate(out FocusCandidate currentCandidate)`
  - `bool TryFindRotationCandidate(FocusCandidate currentCandidate, float bestScore, out FocusCandidate rotationCandidate)`
  - `bool TryGetNearestEnemyDistance(ComCharacterBase sourceCharacter, int sourceSystemTeamIndex, IReadOnlyList<ComPartyBase> parties, out float nearestDistance)`
  - `bool IsCharacterAvailable(ComCharacterBase character)`
  - `Vector3 GetMazeCenterWorld(MazeData mazeData)`
  - `void TryReplaceBestCandidate(FocusCandidate candidate, ref FocusCandidate bestCandidate)`
  - `bool HasCurrentFocus()`
  - `bool IsSameTarget(FocusCandidate candidate)`
  - `... (4 more)`
- SignatureReferencedTypes:
  - `BattleCombatSystem`
  - `BattleSpectatorFocusSettings`
  - `BattleTreasureSystem`
  - `ComCharacterBase`
  - `ComPartyBase`
  - `FocusCandidate`
  - `FocusReason`
  - `FocusTargetType`
  - `MazeData`
  - `TreasureChest`
- BodyReferencedTypes:
  - `TreasureType`

#### BattleSpectatorFocusSettings

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusSettings.cs`
- Fields:
  - `[SerializeField] private float m_minimumFocusSeconds`
  - `[SerializeField] private float m_switchCooldownSeconds`
  - `[SerializeField] private float m_requiredScoreDifference`
  - `[SerializeField] private float m_nearEnemyDistance`
  - `[SerializeField] private float m_maximumFocusSeconds`
  - `[SerializeField] private float m_rotationScoreRange`
  - `[SerializeField] private float m_nearbyCharacterInfluenceDistance`
  - `private float m_minimumFocusSeconds`
  - `private float m_switchCooldownSeconds`
  - `private float m_requiredScoreDifference`
  - `private float m_nearEnemyDistance`
  - `private float m_maximumFocusSeconds`
  - `private float m_rotationScoreRange`
  - `private float m_nearbyCharacterInfluenceDistance`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### BattleSpectatorFocusTailGraphic

- Kind: class
- Namespace: (global)
- BaseTypes: MaskableGraphic
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusTailGraphic.cs`
- Fields:
  - `private Vector2 m_baseTopPosition`
  - `private Vector2 m_baseBottomPosition`
  - `private Vector2 m_tipPosition`
  - `private bool m_hasTriangle`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents:
  - `void Awake()`
- PublicMethods:
  - `void SetTriangle(Vector2 baseTopPosition, Vector2 baseBottomPosition, Vector2 tipPosition, Color triangleColor)`
  - `void ClearTriangle()`
- OtherMethods:
  - `void OnPopulateMesh(VertexHelper vertexHelper)`
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### FocusCandidate

- Kind: struct
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusSelector.cs`
- Fields:
  - `public FocusTargetType m_targetType`
  - `public ComCharacterBase m_character`
  - `public TreasureChest m_treasureChest`
  - `public int m_systemTeamIndex`
  - `public FocusReason m_reason`
  - `public float m_primaryScore`
  - `public float m_score`
  - `public float m_nearbyInfluenceScore`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `bool IsSameTarget(FocusCandidate other)`
  - `int GetStableTargetId()`
  - `FocusCandidate CreateNone()`
  - `FocusCandidate CreateCharacter(ComCharacterBase character, int systemTeamIndex, FocusReason reason, float primaryScore)`
  - `FocusCandidate CreateTreasureChest(TreasureChest treasureChest, FocusReason reason, float score)`
- OtherMethods: なし
- SignatureReferencedTypes:
  - `ComCharacterBase`
  - `FocusReason`
  - `FocusTargetType`
  - `TreasureChest`
- BodyReferencedTypes: なし

#### FocusReason

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusSelector.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

#### FocusTargetType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorCameraController.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `TreasureChest`

#### FocusTargetType

- Kind: enum
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Presentation/Spectator/BattleSpectatorFocusSelector.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes:
  - `TreasureChest`

### Folder: Assets/Dungeon/Program/Game/Treasure

#### TreasurePlacement

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Treasure/TreasurePlacement.cs`
- Fields:
  - `public TreasureType m_treasureType`
  - `public int m_roomId`
  - `public Vector2Int m_cellPosition`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes:
  - `TreasureType`
- BodyReferencedTypes: なし

#### TreasurePlacementPlanner

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Treasure/TreasurePlacementPlanner.cs`
- Fields: なし
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods:
  - `List<TreasurePlacement> CreatePlacements(MazeData mazeData)`
- OtherMethods:
  - `Vector2Int FindRoomCenterCell(List<MazeCellData> roomCells)`
- SignatureReferencedTypes:
  - `MazeCellData`
  - `MazeData`
  - `TreasurePlacement`
- BodyReferencedTypes:
  - `TreasureType`

#### TreasurePresentationConstants

- Kind: class
- Namespace: (global)
- BaseTypes: -
- Path: `Assets/Dungeon/Program/Game/Treasure/TreasurePresentationConstants.cs`
- Fields:
  - `public Vector3 s_carryLocalPosition`
  - `public Quaternion s_carryLocalRotation`
- Properties: なし
- Events: なし
- Delegates: なし
- UnityEvents: なし
- PublicMethods: なし
- OtherMethods: なし
- SignatureReferencedTypes: なし
- BodyReferencedTypes: なし

