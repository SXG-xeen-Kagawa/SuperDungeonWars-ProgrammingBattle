using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public partial class ExplorerAgent : MonoBehaviour, ICharacterRuntime
{
    public delegate void ExplorerAgentCellReachedHandler(
        ExplorerAgent agent,
        Vector2Int cellPosition);

    public delegate void ExplorerAgentMoveTargetReachedHandler(
        ExplorerAgent agent);

    public ExplorerAgentCellReachedHandler m_onCellReached;
    public ExplorerAgentMoveTargetReachedHandler m_onMoveTargetReached;

    private float m_moveSpeed = 3.0f;

    [SerializeField] private float m_stopDistance = 0.05f;
    [SerializeField] private float m_rotateSpeed = 720.0f;
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private CapsuleCollider m_capsuleCollider;

    [SerializeField]
    private CharacterAnimationController m_characterAnimationController;

    [SerializeField]
    private CharacterRagdollController m_characterRagdollController;

    [SerializeField]
    private Transform m_systemMannequinTransform;

    private CharacterTeamColorController[] m_characterTeamColorControllers;

    private Collider[] m_runtimeColliders;

    private MazeData m_mazeData;
    private ComCharacterBase m_character;

    private bool m_hasMoveTarget;
    private bool m_isCellTarget;
    private Vector3 m_targetWorldPosition;
    private Vector2Int m_targetCellPosition;
    private Vector2Int m_currentCell;
    private float m_spawnBodyHeight = 0.5f;

    private int m_partyMemberIndex = -1;

    private bool m_isRagdollPaused;
    private bool m_hasRagdollWorldPosition;
    private Vector3 m_ragdollWorldPosition;

    private bool m_isSynchronizingCellAfterKnockback;

    public Vector2Int CurrentCell => m_currentCell;
    public bool HasMoveTarget => m_hasMoveTarget;
    public bool IsMoving => m_hasMoveTarget;

    public Vector3 WorldPosition
    {
        get
        {
            if (m_isRagdollPaused &&
                m_hasRagdollWorldPosition)
            {
                return m_ragdollWorldPosition;
            }

            return m_rigidbody != null
                ? m_rigidbody.position
                : transform.position;
        }
    }

    public Vector3 MoveTargetWorldPosition => m_targetWorldPosition;
    public MazeData MazeData => m_mazeData;
    public int PartyMemberIndex => m_partyMemberIndex;
    public bool IsCellTarget => m_isCellTarget;
    public Vector2Int TargetCellPosition => m_targetCellPosition;

    public float CapsuleRadius
    {
        get
        {
            if (m_capsuleCollider == null)
            {
                return 0.25f;
            }

            Vector3 scale = transform.lossyScale;

            float horizontalScale = Mathf.Max(
                Mathf.Abs(scale.x),
                Mathf.Abs(scale.z));

            return m_capsuleCollider.radius * horizontalScale;
        }
    }



    public void BindCharacter(
        ComCharacterBase character)
    {
        if (character == null)
        {
            Debug.LogError(
                $"[ExplorerAgent.BindCharacter] " +
                $"character is null. agent={name}",
                this);

            return;
        }

        if (m_character != null &&
            m_character != character)
        {
            Debug.LogWarning(
                $"[ExplorerAgent.BindCharacter] " +
                $"Bound character is replaced. " +
                $"agent={name} " +
                $"previous={m_character.name} " +
                $"next={character.name}",
                this);
        }

        m_character = character;

        if (m_characterAnimationController != null)
        {
            m_characterAnimationController.BindCharacter(
                character);
        }

        if (m_characterRagdollController != null)
        {
            m_characterRagdollController.BindExplorerAgent(
                this);

            m_characterRagdollController.BindCharacter(
                character);
        }
    }

    internal ComCharacterBase GetCharacter()
    {
        return m_character;
    }

    internal Transform GetRuntimeTransform()
    {
        return transform;
    }

    internal CharacterAnimationController GetCharacterAnimationController()
    {
        return m_characterAnimationController;
    }

    internal CharacterRagdollController GetCharacterRagdollController()
    {
        return m_characterRagdollController;
    }

    internal Transform GetSystemMannequinTransform()
    {
        return m_systemMannequinTransform;
    }

    internal CharacterTeamColorController[] GetCharacterTeamColorControllers()
    {
        return m_characterTeamColorControllers;
    }

    internal Collider[] GetRuntimeColliders()
    {
        return m_runtimeColliders;
    }

    public void SetPartyMemberIndex(
        int partyMemberIndex)
    {
        m_partyMemberIndex = partyMemberIndex;
    }

    public void SetMoveSpeed(
        float moveSpeed)
    {
        m_moveSpeed = Mathf.Max(0.0f, moveSpeed);
    }

    private void Reset()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_capsuleCollider = GetComponent<CapsuleCollider>();
    }

    private void Awake()
    {
        if (m_rigidbody == null)
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }

        if (m_capsuleCollider == null)
        {
            m_capsuleCollider = GetComponent<CapsuleCollider>();
        }

        m_characterTeamColorControllers = GetComponentsInChildren<CharacterTeamColorController>(true);
        m_runtimeColliders = GetComponentsInChildren<Collider>(true);

        m_rigidbody.useGravity = true;
        m_rigidbody.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        m_rigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        m_rigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (m_isRagdollPaused)
        {
            return;
        }

        if (m_mazeData == null)
        {
            return;
        }

        if (m_hasMoveTarget == false)
        {
            SynchronizeCurrentCellAfterKnockback();
            return;
        }

        Vector3 currentPosition = m_rigidbody.position;
        Vector3 targetPosition = m_targetWorldPosition;
        targetPosition.y = currentPosition.y;

        if (m_isCellTarget &&
            IsWorldPositionInsideCell(
                currentPosition,
                m_targetCellPosition))
        {
            CompleteMoveTarget();
            return;
        }

        Vector3 delta = targetPosition - currentPosition;
        delta.y = 0.0f;

        float distance = delta.magnitude;

        if (m_isCellTarget == false &&
            distance <= m_stopDistance)
        {
            CompleteMoveTarget();
            return;
        }

        Vector3 direction = delta / distance;

        float moveDistance = Mathf.Min(
            m_moveSpeed * Time.fixedDeltaTime,
            distance);

        Vector3 nextPosition =
            currentPosition +
            direction * moveDistance;

        nextPosition.y = currentPosition.y;

        m_rigidbody.MovePosition(nextPosition);

        Quaternion targetRotation = Quaternion.LookRotation(
            direction,
            Vector3.up);

        Quaternion nextRotation = Quaternion.RotateTowards(
            m_rigidbody.rotation,
            targetRotation,
            m_rotateSpeed * Time.fixedDeltaTime);

        m_rigidbody.MoveRotation(nextRotation);
    }

    public void BindMazeData(
        MazeData mazeData)
    {
        m_mazeData = mazeData;
    }

    public void Initialize(
        MazeData mazeData,
        Vector2Int startCell)
    {
        Initialize(
            mazeData,
            startCell,
            0.5f);
    }

    public void Initialize(
        MazeData mazeData,
        Vector2Int startCell,
        float bodyHeight)
    {
        m_mazeData = mazeData;
        m_spawnBodyHeight = bodyHeight;

        SetPositionToCell(startCell);
        StopMove();

        Debug.Log(
            $"[ExplorerAgent.Initialize] name={name} " +
            $"startCell={startCell} currentCell={m_currentCell} " +
            $"world={WorldPosition}");
    }

    public bool TryMoveToCell(
        Vector2Int targetCell)
    {
        if (m_mazeData == null ||
            m_mazeData.IsInside(
                targetCell.x,
                targetCell.y) == false)
        {
            Debug.LogWarning(
                $"[ExplorerAgent.TryMoveToCell: InvalidCell] " +
                $"agent={name} " +
                $"currentCell={m_currentCell} " +
                $"targetCell={targetCell}",
                this);

            return false;
        }

        MazeCellData cell = m_mazeData.GetCell(
            targetCell.x,
            targetCell.y);

        if (cell == null ||
            cell.IsBlockedCell())
        {
            Debug.LogWarning(
                $"[ExplorerAgent.TryMoveToCell: BlockedCell] " +
                $"agent={name} " +
                $"currentCell={m_currentCell} " +
                $"targetCell={targetCell}",
                this);

            return false;
        }

        if (m_currentCell == targetCell)
        {
            m_hasMoveTarget = false;
            return true;
        }

        Vector3 targetWorld = m_mazeData.CellToWorld(
            targetCell.x,
            targetCell.y);

        return TryMoveToWorldInternal(
            targetWorld,
            true,
            targetCell);
    }

    public bool TryMoveToWorld(
        Vector3 targetWorld)
    {
        if (m_mazeData != null &&
            IsWorldPositionInsideMaze(targetWorld) == false)
        {
            Debug.LogWarning(
                $"[ExplorerAgent.TryMoveToWorld: OutsideMaze] " +
                $"agent={name} " +
                $"currentCell={m_currentCell} " +
                $"currentWorld={WorldPosition} " +
                $"targetWorld={targetWorld}",
                this);
        }

        return TryMoveToWorldInternal(
            targetWorld,
            false,
            Vector2Int.zero);
    }

    public void StopMove()
    {
        m_hasMoveTarget = false;
    }

    public void SetPositionToWorld(
        Vector3 worldPosition)
    {
        if (m_rigidbody != null)
        {
            m_rigidbody.position = worldPosition;

            if (!m_rigidbody.isKinematic)
            {
                m_rigidbody.linearVelocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            transform.position = worldPosition;
        }

        m_targetWorldPosition = worldPosition;
    }

    public void SetRotationToWorld(
        Quaternion worldRotation)
    {
        if (m_rigidbody != null)
        {
            m_rigidbody.rotation = worldRotation;
            return;
        }

        transform.rotation = worldRotation;
    }

    public void UpdateRagdollWorldPosition(
        Vector3 worldPosition)
    {
        if (!m_isRagdollPaused)
        {
            return;
        }

        m_ragdollWorldPosition = worldPosition;
        m_hasRagdollWorldPosition = true;

        if (m_mazeData == null)
        {
            return;
        }

        Vector2Int cellPosition;

        if (!m_mazeData.TryWorldToCell(
                worldPosition,
                out cellPosition))
        {
            return;
        }

        MazeCellData cell = m_mazeData.GetCell(
            cellPosition.x,
            cellPosition.y);

        if (cell == null ||
            cell.IsBlockedCell())
        {
            return;
        }

        if (m_currentCell == cellPosition)
        {
            return;
        }

        m_currentCell = cellPosition;

        if (m_onCellReached != null)
        {
            m_onCellReached(
                this,
                m_currentCell);
        }
    }

    private bool TryMoveToWorldInternal(
        Vector3 targetWorld,
        bool isCellTarget,
        Vector2Int targetCell)
    {
        if (m_isRagdollPaused)
        {
            return false;
        }

        if (m_mazeData != null &&
            IsWorldPositionInsideMaze(targetWorld) == false)
        {
            Debug.LogError(
                $"[ExplorerAgent.InvalidMoveTarget] " +
                $"agent={name} " +
                $"currentCell={m_currentCell} " +
                $"currentWorld={WorldPosition} " +
                $"isCellTarget={isCellTarget} " +
                $"targetCell={targetCell} " +
                $"targetWorld={targetWorld}",
                this);

            return false;
        }

        m_targetWorldPosition = targetWorld;
        m_targetWorldPosition.y = 0.0f;

        m_isCellTarget = isCellTarget;
        m_targetCellPosition = targetCell;
        m_hasMoveTarget = true;

        return true;
    }

    private void CompleteMoveTarget()
    {
        m_hasMoveTarget = false;

        if (m_isCellTarget)
        {
            m_currentCell = m_targetCellPosition;

            if (m_onCellReached != null)
            {
                m_onCellReached(
                    this,
                    m_currentCell);
            }
        }

        if (m_onMoveTargetReached != null)
        {
            m_onMoveTargetReached(this);
        }
    }

    private bool IsWorldPositionInsideCell(
        Vector3 worldPosition,
        Vector2Int cellPosition)
    {
        if (m_mazeData == null ||
            m_mazeData.IsInside(
                cellPosition.x,
                cellPosition.y) == false)
        {
            return false;
        }

        Vector3 cellCenter = m_mazeData.CellToWorld(
            cellPosition.x,
            cellPosition.y);

        float halfCellSize =
            m_mazeData.CellSize * 0.5f;

        return
            worldPosition.x >= cellCenter.x - halfCellSize &&
            worldPosition.x <= cellCenter.x + halfCellSize &&
            worldPosition.z >= cellCenter.z - halfCellSize &&
            worldPosition.z <= cellCenter.z + halfCellSize;
    }

    private void SetPositionToCell(
        Vector2Int cell)
    {
        if (m_mazeData == null)
        {
            return;
        }

        Vector3 world = m_mazeData.CellToWorld(
            cell.x,
            cell.y);

        world.y = GetSpawnY();

        m_rigidbody.position = world;
        m_rigidbody.linearVelocity = Vector3.zero;
        m_rigidbody.angularVelocity = Vector3.zero;

        m_targetWorldPosition = world;
        m_currentCell = cell;
    }

    private float GetSpawnY()
    {
        if (m_spawnBodyHeight > 0.0f)
        {
            return m_spawnBodyHeight;
        }

        if (m_capsuleCollider != null)
        {
            return
                m_capsuleCollider.center.y +
                m_capsuleCollider.height * 0.5f;
        }

        return 0.5f;
    }

    private void UpdateCurrentCell()
    {
        if (m_mazeData == null)
        {
            return;
        }

        m_currentCell = FindNearestCell(
            WorldPosition);
    }

    private Vector2Int FindNearestCell(
        Vector3 worldPosition)
    {
        Vector2Int bestCell = m_currentCell;
        float bestSqrDistance = float.MaxValue;
        bool hasFoundCell = false;

        for (int y = 0; y < m_mazeData.m_height; y++)
        {
            for (int x = 0; x < m_mazeData.m_width; x++)
            {
                MazeCellData cell = m_mazeData.GetCell(
                    x,
                    y);

                if (cell == null ||
                    cell.IsBlockedCell())
                {
                    continue;
                }

                Vector3 cellWorld = m_mazeData.CellToWorld(
                    x,
                    y);

                Vector3 delta = cellWorld - worldPosition;
                delta.y = 0.0f;

                float sqrDistance = delta.sqrMagnitude;

                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestCell = new Vector2Int(x, y);
                    hasFoundCell = true;
                }
            }
        }

        return hasFoundCell
            ? bestCell
            : m_currentCell;
    }

    private bool IsWorldPositionInsideMaze(
        Vector3 worldPosition)
    {
        if (m_mazeData == null)
        {
            return true;
        }

        Vector3 firstCellWorld = m_mazeData.CellToWorld(
            0,
            0);

        Vector3 lastCellWorld = m_mazeData.CellToWorld(
            m_mazeData.m_width - 1,
            m_mazeData.m_height - 1);

        float halfCellSize =
            m_mazeData.CellSize * 0.5f;

        float minX = Mathf.Min(
            firstCellWorld.x,
            lastCellWorld.x) - halfCellSize;

        float maxX = Mathf.Max(
            firstCellWorld.x,
            lastCellWorld.x) + halfCellSize;

        float minZ = Mathf.Min(
            firstCellWorld.z,
            lastCellWorld.z) - halfCellSize;

        float maxZ = Mathf.Max(
            firstCellWorld.z,
            lastCellWorld.z) + halfCellSize;

        return
            worldPosition.x >= minX &&
            worldPosition.x <= maxX &&
            worldPosition.z >= minZ &&
            worldPosition.z <= maxZ;
    }

    public void ApplyKnockback(
        Vector3 direction,
        float knockbackSpeed)
    {
        StopMove();

        direction.y = 0.0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
            direction.y = 0.0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        direction.Normalize();

        if (m_rigidbody != null)
        {
            Vector3 velocity =
                direction *
                Mathf.Max(0.0f, knockbackSpeed);

            velocity.y = m_rigidbody.linearVelocity.y;
            m_rigidbody.linearVelocity = velocity;
        }

        m_isSynchronizingCellAfterKnockback = true;
    }

    private void SynchronizeCurrentCellAfterKnockback()
    {
        if (!m_isSynchronizingCellAfterKnockback ||
            m_mazeData == null ||
            m_rigidbody == null)
        {
            return;
        }

        Vector3 horizontalVelocity =
            m_rigidbody.linearVelocity;

        horizontalVelocity.y = 0.0f;

        if (horizontalVelocity.sqrMagnitude > 0.01f)
        {
            return;
        }

        m_isSynchronizingCellAfterKnockback = false;

        Vector2Int cellPosition;

        if (!m_mazeData.TryWorldToCell(
                m_rigidbody.position,
                out cellPosition))
        {
            return;
        }

        MazeCellData cell = m_mazeData.GetCell(
            cellPosition.x,
            cellPosition.y);

        if (cell == null ||
            cell.IsBlockedCell())
        {
            return;
        }

        if (m_currentCell == cellPosition)
        {
            return;
        }

        m_currentCell = cellPosition;

        if (m_onCellReached != null)
        {
            m_onCellReached(
                this,
                m_currentCell);
        }
    }

    public void BeginRagdoll()
    {
        StopMove();

        m_ragdollWorldPosition =
            m_rigidbody != null
                ? m_rigidbody.position
                : transform.position;

        m_hasRagdollWorldPosition = true;
        m_isRagdollPaused = true;
        m_isSynchronizingCellAfterKnockback = false;

        if (m_rigidbody != null)
        {
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.isKinematic = true;
        }

        if (m_capsuleCollider != null)
        {
            m_capsuleCollider.enabled = false;
        }
    }

    public void SetRagdollRecoveryPosition(
        Vector3 worldPosition)
    {
        if (!m_isRagdollPaused)
        {
            return;
        }

        worldPosition.y = transform.position.y;
        transform.position = worldPosition;

        if (m_rigidbody != null)
        {
            m_rigidbody.position = worldPosition;
        }

        Physics.SyncTransforms();

        m_targetWorldPosition = worldPosition;
    }

    public void EndRagdoll()
    {
        if (!m_isRagdollPaused)
        {
            return;
        }

        if (m_rigidbody != null)
        {
            m_rigidbody.isKinematic = false;
            m_rigidbody.useGravity = true;
            m_rigidbody.linearVelocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
        }

        if (m_capsuleCollider != null)
        {
            m_capsuleCollider.enabled = true;
        }

        m_isRagdollPaused = false;
        m_hasRagdollWorldPosition = false;

        SynchronizeCurrentCellAfterRagdoll();
    }

    private void SynchronizeCurrentCellAfterRagdoll()
    {
        if (m_mazeData == null)
        {
            return;
        }

        Vector2Int previousCell = m_currentCell;
        m_currentCell = FindNearestCell(WorldPosition);

        if (previousCell != m_currentCell &&
            m_onCellReached != null)
        {
            m_onCellReached(
                this,
                m_currentCell);
        }
    }
}