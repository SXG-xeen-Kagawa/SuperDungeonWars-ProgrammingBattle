using System.Collections.Generic;
using UnityEngine;

public class TreasureChest : MonoBehaviour, ITreasureRuntime
{
    private static readonly int s_mainTexturePropertyId =
        Shader.PropertyToID("_MainTex");

    private static readonly int s_baseMapPropertyId =
        Shader.PropertyToID("_BaseMap");

    [SerializeField] private Collider m_pickupTrigger;
    [SerializeField] private GameObject m_visualRoot;

    [SerializeField]
    private GameObject m_chestCloseModelRoot;

    [SerializeField]
    private GameObject m_chestOpenModelRoot;

    [SerializeField]
    private GameObject m_resultCoinsObject;

    [SerializeField]
    private MeshRenderer m_chestCloseMeshRenderer;

    [SerializeField]
    private MeshRenderer m_chestOpenMeshRenderer;

    [SerializeField]
    private Texture2D m_standardChestTexture;

    [SerializeField]
    private Texture2D m_centralChestTexture;

    [SerializeField] private Rigidbody m_dropRigidbody;
    [SerializeField] private Collider m_physicsCollider;

    [SerializeField] private float m_dropHorizontalImpulse = 2.2f;
    [SerializeField] private float m_dropUpwardImpulse = 1.1f;
    [SerializeField] private float m_dropTorqueImpulse = 1.6f;
    [SerializeField] private float m_dropMaxPhysicsSeconds = 2.0f;
    [SerializeField] private float m_dropSettleSpeed = 0.15f;
    [SerializeField] private float m_dropSettleAngularSpeed = 0.8f;

    private bool m_isDropping;
    private float m_dropPhysicsEndTime;
    private float m_exportHideTime = -1.0f;

    private MaterialPropertyBlock m_materialPropertyBlock;

    private MazeData m_mazeData;
    private TreasurePlacement m_placement;
    private BattleTreasureSystem m_battleTreasureSystem;
    private ComCharacterBase m_ownerCharacter;

    private Vector2Int m_currentCellPosition;
    private bool m_isPickedUp;
    private bool m_isExported;
    private int m_treasureId = -1;
    private float m_pickupAvailableTime;
    private float m_pickedUpTime = -1.0f;

    private readonly List<Collider> m_ignoredOwnerColliders =
        new List<Collider>();

    public int TreasureId => m_treasureId;
    public TreasurePlacement Placement => m_placement;
    public Vector3 WorldPosition => transform.position;
    public bool IsPickedUp => m_isPickedUp;
    public bool IsExported => m_isExported;
    public ComCharacterBase OwnerCharacter => m_ownerCharacter;

    public TreasureType TreasureType
    {
        get
        {
            return m_placement != null
                ? m_placement.m_treasureType
                : TreasureType.SmallChest;
        }
    }

    public Vector2Int CellPosition
    {
        get
        {
            RefreshCurrentCellPosition();
            return m_currentCellPosition;
        }
    }

    public float PickedUpTime
    {
        get { return m_pickedUpTime; }
    }

    private void Awake()
    {
        m_materialPropertyBlock = new MaterialPropertyBlock();

        SetDropPhysicsEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = true;
        }

        SetOpenedForResultPresentation(false);
    }

    public void Initialize(
        int treasureId,
        TreasurePlacement placement,
        MazeData mazeData)
    {
        if (treasureId < 0
            || placement == null
            || mazeData == null)
        {
            Debug.LogError(
                "TreasureChest.Initialize: 引数が不正です。",
                this);

            return;
        }

        m_treasureId = treasureId;
        m_placement = placement;
        m_mazeData = mazeData;
        m_battleTreasureSystem = null;
        m_ownerCharacter = null;
        m_currentCellPosition = placement.m_cellPosition;
        m_isPickedUp = false;
        m_isExported = false;
        m_pickupAvailableTime = 0.0f;
        m_pickedUpTime = -1.0f;

        m_isDropping = false;
        m_exportHideTime = -1.0f;

        SetDropPhysicsEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = true;
        }

        SetOpenedForResultPresentation(false);

        ApplyTextureByTreasureType(placement.m_treasureType);

        SetPickupTriggerEnabled(true);
        SetVisualVisible(true);
    }

    public void SetBattleTreasureSystem(
        BattleTreasureSystem battleTreasureSystem)
    {
        m_battleTreasureSystem = battleTreasureSystem;
    }

    public bool IsPickupAvailable()
    {
        return !m_isPickedUp
            && !m_isExported
            && !m_isDropping
            && Time.time >= m_pickupAvailableTime;
    }

    public bool AttachToCharacter(
        ComCharacterBase ownerCharacter)
    {
        if (ownerCharacter == null
            || !IsPickupAvailable())
        {
            return false;
        }

        ICharacterRuntime characterRuntime =
            ownerCharacter.GetExplorerAgent();

        ExplorerAgent ownerAgent =
            characterRuntime as ExplorerAgent;

        if (ownerAgent == null)
        {
            Debug.LogWarning(
                "TreasureChest: Runtime ExplorerAgent が未設定です。",
                ownerCharacter);

            return false;
        }

        Transform runtimeTransform =
            ownerAgent.GetRuntimeTransform();

        if (runtimeTransform == null)
        {
            Debug.LogWarning(
                "TreasureChest: Runtime Character Transform が未設定です。",
                ownerCharacter);

            return false;
        }

        m_isPickedUp = true;
        m_ownerCharacter = ownerCharacter;
        m_pickupAvailableTime = 0.0f;
        m_pickedUpTime = Time.time;

        IgnoreOwnerPhysicsCollisions(ownerCharacter);

        SetPickupTriggerEnabled(false);
        SetDropPhysicsEnabled(false);

        CharacterAnimationController animationController =
            ownerAgent.GetCharacterAnimationController();

        Transform carryParent =
            animationController != null
                ? animationController.TreasureCarryAnchor
                : null;

        if (carryParent == null)
        {
            carryParent = runtimeTransform;
        }

        transform.SetParent(carryParent, false);

        transform.localPosition =
            TreasurePresentationConstants.s_carryLocalPosition;

        transform.localRotation =
            TreasurePresentationConstants.s_carryLocalRotation;

        SetVisualVisible(true);
        RefreshCurrentCellPosition();

        return true;
    }

    public bool DetachAndDrop(
        ComCharacterBase ownerCharacter,
        Vector3 dropDirection,
        float pickupLockSeconds)
    {
        if (!m_isPickedUp
            || m_isExported
            || ownerCharacter == null
            || m_ownerCharacter != ownerCharacter)
        {
            return false;
        }

        RestoreOwnerPhysicsCollisions();

        transform.SetParent(null, true);

        m_ownerCharacter = null;
        m_isPickedUp = false;
        m_isDropping = true;
        m_pickupAvailableTime =
            Time.time + Mathf.Max(0.0f, pickupLockSeconds);

        SetPickupTriggerEnabled(false);
        SetVisualVisible(true);

        Vector3 horizontalDirection = dropDirection;
        horizontalDirection.y = 0.0f;

        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            horizontalDirection = Random.insideUnitSphere;
            horizontalDirection.y = 0.0f;
        }

        horizontalDirection.Normalize();

        SetDropPhysicsEnabled(true);

        m_dropRigidbody.linearVelocity = Vector3.zero;
        m_dropRigidbody.angularVelocity = Vector3.zero;

        Vector3 impulse =
            horizontalDirection * m_dropHorizontalImpulse;

        impulse.y = m_dropUpwardImpulse;

        m_dropRigidbody.AddForce(
            impulse,
            ForceMode.Impulse);

        Vector3 torqueAxis =
            Vector3.Cross(horizontalDirection, Vector3.up);

        torqueAxis += Random.insideUnitSphere * 0.35f;

        m_dropRigidbody.AddTorque(
            torqueAxis.normalized * m_dropTorqueImpulse,
            ForceMode.Impulse);

        m_dropPhysicsEndTime =
            Time.time + m_dropMaxPhysicsSeconds;

        RefreshCurrentCellPosition();

        return true;
    }

    public bool ThrowForExport(
        ComCharacterBase ownerCharacter,
        Vector3 throwDirection,
        float horizontalImpulse,
        float upwardImpulse,
        float torqueImpulse,
        float visibleSeconds)
    {
        if (m_isExported
            || !m_isPickedUp
            || ownerCharacter == null
            || m_ownerCharacter != ownerCharacter
            || m_dropRigidbody == null)
        {
            return false;
        }

        RestoreOwnerPhysicsCollisions();

        transform.SetParent(null, true);

        m_ownerCharacter = null;
        m_isPickedUp = false;
        m_isDropping = false;
        m_isExported = true;
        m_pickupAvailableTime = float.PositiveInfinity;

        SetPickupTriggerEnabled(false);
        SetVisualVisible(true);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = false;
        }

        Vector3 horizontalDirection = throwDirection;
        horizontalDirection.y = 0.0f;

        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            horizontalDirection = transform.forward;
            horizontalDirection.y = 0.0f;
        }

        horizontalDirection.Normalize();

        SetDropPhysicsEnabled(true);

        m_dropRigidbody.linearVelocity = Vector3.zero;
        m_dropRigidbody.angularVelocity = Vector3.zero;

        Vector3 impulse =
            horizontalDirection * Mathf.Max(0.0f, horizontalImpulse);

        impulse.y = Mathf.Max(0.0f, upwardImpulse);

        m_dropRigidbody.AddForce(
            impulse,
            ForceMode.Impulse);

        Vector3 torqueAxis =
            Vector3.Cross(horizontalDirection, Vector3.up);

        torqueAxis += Random.insideUnitSphere * 0.35f;

        m_dropRigidbody.AddTorque(
            torqueAxis.normalized * Mathf.Max(0.0f, torqueImpulse),
            ForceMode.Impulse);

        m_exportHideTime =
            Time.time + Mathf.Max(0.0f, visibleSeconds);

        RefreshCurrentCellPosition();

        return true;
    }

    public bool Export(
        ComCharacterBase ownerCharacter)
    {
        if (m_isExported
            || !m_isPickedUp
            || ownerCharacter == null
            || m_ownerCharacter != ownerCharacter)
        {
            return false;
        }

        RestoreOwnerPhysicsCollisions();

        transform.SetParent(null, true);

        m_ownerCharacter = null;
        m_isPickedUp = false;
        m_isExported = true;
        m_pickupAvailableTime = float.PositiveInfinity;

        SetPickupTriggerEnabled(false);
        SetVisualVisible(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = false;
        }

        return true;
    }

    public void SetResultPresentationVisible(
        bool isVisible)
    {
        SetVisualVisible(isVisible);
    }

    public void SetOpenedForResultPresentation(
        bool isOpened)
    {
        if (m_chestCloseModelRoot != null)
        {
            m_chestCloseModelRoot.SetActive(!isOpened);
        }

        if (m_chestOpenModelRoot != null)
        {
            m_chestOpenModelRoot.SetActive(isOpened);
        }

        SetResultCoinsVisible(true);
    }

    public void SetResultCoinsVisible(
        bool isVisible)
    {
        if (m_resultCoinsObject == null)
        {
            return;
        }

        m_resultCoinsObject.SetActive(
            isVisible);
    }

    public void ThrowAwayForResultPresentation(
        Vector3 throwDirection,
        float horizontalSpeed,
        float upwardSpeed,
        float torqueImpulse,
        float hideAfterSeconds)
    {
        if (m_dropRigidbody == null)
        {
            SetVisualVisible(false);
            return;
        }

        m_isDropping = false;
        m_isPickedUp = false;
        m_isExported = true;
        m_ownerCharacter = null;
        m_pickupAvailableTime = float.PositiveInfinity;

        SetPickupTriggerEnabled(false);
        SetResultCoinsVisible(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = true;
        }

        Vector3 horizontalDirection = throwDirection;
        horizontalDirection.y = 0.0f;

        if (horizontalDirection.sqrMagnitude <= 0.0001f)
        {
            horizontalDirection = transform.forward;
            horizontalDirection.y = 0.0f;
        }

        horizontalDirection.Normalize();

        SetDropPhysicsEnabled(true);

        m_dropRigidbody.linearVelocity =
            horizontalDirection * Mathf.Max(0.0f, horizontalSpeed)
            + Vector3.up * Mathf.Max(0.0f, upwardSpeed);

        m_dropRigidbody.angularVelocity = Vector3.zero;

        Vector3 torqueAxis =
            Vector3.Cross(
                horizontalDirection,
                Vector3.up);

        torqueAxis += Random.insideUnitSphere * 0.35f;

        m_dropRigidbody.AddTorque(
            torqueAxis.normalized * torqueImpulse,
            ForceMode.Impulse);

        m_exportHideTime =
            Time.time
            + Mathf.Max(0.0f, hideAfterSeconds);
    }

    public void InitializeForResultPresentation(
        int treasureId,
        TreasureType treasureType)
    {
        RestoreOwnerPhysicsCollisions();

        m_treasureId = treasureId;
        m_placement = null;
        m_mazeData = null;
        m_battleTreasureSystem = null;
        m_ownerCharacter = null;

        m_currentCellPosition = Vector2Int.zero;
        m_isPickedUp = false;
        m_isExported = false;
        m_isDropping = false;

        m_pickupAvailableTime = float.PositiveInfinity;
        m_pickedUpTime = -1.0f;
        m_dropPhysicsEndTime = -1.0f;
        m_exportHideTime = -1.0f;

        SetDropPhysicsEnabled(false);
        SetPickupTriggerEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = false;
        }

        SetOpenedForResultPresentation(false);

        ApplyTextureByTreasureType(treasureType);
        SetVisualVisible(true);
    }

    private void Update()
    {
        if (m_exportHideTime < 0.0f
            || Time.time < m_exportHideTime)
        {
            return;
        }

        m_exportHideTime = -1.0f;

        SetDropPhysicsEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = false;
        }

        SetVisualVisible(false);
    }

    private void LateUpdate()
    {
        RefreshCurrentCellPosition();
    }

    private void FixedUpdate()
    {
        if (!m_isDropping
            || m_dropRigidbody == null)
        {
            return;
        }

        if (Time.time < m_dropPhysicsEndTime)
        {
            return;
        }

        m_isDropping = false;

        SetDropPhysicsEnabled(false);
        SetPickupTriggerEnabled(true);

        RefreshCurrentCellPosition();
    }

    private void SetDropPhysicsEnabled(bool isEnabled)
    {
        if (m_dropRigidbody == null)
        {
            return;
        }

        if (!isEnabled)
        {
            if (!m_dropRigidbody.isKinematic)
            {
                m_dropRigidbody.linearVelocity = Vector3.zero;
                m_dropRigidbody.angularVelocity = Vector3.zero;
            }

            m_dropRigidbody.useGravity = false;
            m_dropRigidbody.isKinematic = true;
            m_dropRigidbody.interpolation =
                RigidbodyInterpolation.None;

            return;
        }

        m_dropRigidbody.isKinematic = false;
        m_dropRigidbody.useGravity = true;
        m_dropRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRequestPickupByCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRequestPickupByCollider(other);
    }

    private void TryRequestPickupByCollider(Collider other)
    {
        if (!IsPickupAvailable()
            || other == null)
        {
            return;
        }

        ExplorerAgent agent =
            other.GetComponentInParent<ExplorerAgent>();

        if (agent == null)
        {
            return;
        }

        ComCharacterBase character =
            agent.GetCharacter();

        if (character == null)
        {
            return;
        }

        if (m_battleTreasureSystem != null)
        {
            m_battleTreasureSystem.TryRequestPickUpTreasure(
                this,
                character);
        }
    }

    private void ApplyTextureByTreasureType(
        TreasureType treasureType)
    {
        Texture2D texture =
            treasureType == TreasureType.CentralChest
                ? m_centralChestTexture
                : m_standardChestTexture;

        if (texture == null)
        {
            Debug.LogWarning(
                "TreasureChest: 宝箱テクスチャが未設定です。"
                + " TreasureType="
                + treasureType,
                this);

            return;
        }

        ApplyTextureToChestMeshRenderer(
            m_chestCloseMeshRenderer,
            texture,
            "chest_close");

        ApplyTextureToChestMeshRenderer(
            m_chestOpenMeshRenderer,
            texture,
            "chest_open");
    }

    private void ApplyTextureToChestMeshRenderer(
        MeshRenderer chestMeshRenderer,
        Texture2D texture,
        string modelName)
    {
        if (chestMeshRenderer == null)
        {
            Debug.LogWarning(
                "TreasureChest: "
                + modelName
                + " の MeshRenderer が未設定です。",
                this);

            return;
        }

        if (m_materialPropertyBlock == null)
        {
            m_materialPropertyBlock =
                new MaterialPropertyBlock();
        }

        Material material =
            chestMeshRenderer.sharedMaterial;

        if (material == null)
        {
            Debug.LogWarning(
                "TreasureChest: "
                + modelName
                + " の Material が未設定です。",
                this);

            return;
        }

        chestMeshRenderer.GetPropertyBlock(
            m_materialPropertyBlock);

        if (material.HasProperty(
            s_baseMapPropertyId))
        {
            m_materialPropertyBlock.SetTexture(
                s_baseMapPropertyId,
                texture);
        }

        if (material.HasProperty(
            s_mainTexturePropertyId))
        {
            m_materialPropertyBlock.SetTexture(
                s_mainTexturePropertyId,
                texture);
        }

        chestMeshRenderer.SetPropertyBlock(
            m_materialPropertyBlock);
    }

    private void RefreshCurrentCellPosition()
    {
        if (m_mazeData == null)
        {
            return;
        }

        Vector2Int cellPosition;

        if (m_mazeData.TryWorldToCell(
            transform.position,
            out cellPosition))
        {
            m_currentCellPosition = cellPosition;
        }
    }

    private void SetPickupTriggerEnabled(bool isEnabled)
    {
        if (m_pickupTrigger != null)
        {
            m_pickupTrigger.enabled = isEnabled;
        }
    }

    private void SetVisualVisible(bool isVisible)
    {
        if (m_visualRoot != null)
        {
            m_visualRoot.SetActive(isVisible);
            return;
        }

        Renderer[] renderers =
            GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = isVisible;
        }
    }

    private void IgnoreOwnerPhysicsCollisions(
        ComCharacterBase ownerCharacter)
    {
        RestoreOwnerPhysicsCollisions();

        if (ownerCharacter == null
            || m_physicsCollider == null)
        {
            return;
        }

        ICharacterRuntime characterRuntime =
            ownerCharacter.GetExplorerAgent();

        ExplorerAgent ownerAgent =
            characterRuntime as ExplorerAgent;

        Collider[] ownerColliders =
            ownerAgent != null
                ? ownerAgent.GetRuntimeColliders()
                : null;

        if (ownerColliders == null)
        {
            return;
        }

        for (int i = 0; i < ownerColliders.Length; i++)
        {
            Collider ownerCollider = ownerColliders[i];

            if (ownerCollider == null
                || ownerCollider.isTrigger)
            {
                continue;
            }

            Physics.IgnoreCollision(
                m_physicsCollider,
                ownerCollider,
                true);

            m_ignoredOwnerColliders.Add(ownerCollider);
        }
    }

    private void RestoreOwnerPhysicsCollisions()
    {
        if (m_physicsCollider == null)
        {
            m_ignoredOwnerColliders.Clear();
            return;
        }

        for (int i = 0; i < m_ignoredOwnerColliders.Count; i++)
        {
            Collider ownerCollider =
                m_ignoredOwnerColliders[i];

            if (ownerCollider == null)
            {
                continue;
            }

            Physics.IgnoreCollision(
                m_physicsCollider,
                ownerCollider,
                false);
        }

        m_ignoredOwnerColliders.Clear();
    }

    public bool TryGetResultPlacementColliderInfo(
        out Vector3 colliderCenterOffset,
        out Vector3 colliderExtents,
        out float castRadius)
    {
        colliderCenterOffset = Vector3.zero;
        colliderExtents = Vector3.zero;
        castRadius = 0.0f;

        if (m_physicsCollider == null)
        {
            return false;
        }

        bool wasEnabled = m_physicsCollider.enabled;

        m_physicsCollider.enabled = true;

        Physics.SyncTransforms();

        Bounds bounds = m_physicsCollider.bounds;

        colliderCenterOffset =
            bounds.center - transform.position;

        colliderExtents = bounds.extents;

        castRadius = Mathf.Max(
            bounds.extents.x,
            bounds.extents.z);

        m_physicsCollider.enabled = wasEnabled;

        return castRadius > 0.001f
            && colliderExtents.y > 0.001f;
    }

    public void BeginResultPlacementPhysics()
    {
        SetPickupTriggerEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = true;
        }

        SetDropPhysicsEnabled(true);

        if (m_dropRigidbody != null)
        {
            m_dropRigidbody.linearVelocity = Vector3.zero;
            m_dropRigidbody.angularVelocity = Vector3.zero;
        }
    }

    public void FinishResultPlacementPhysics()
    {
        SetDropPhysicsEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = true;
        }
    }

    public void CancelResultPlacementPhysics()
    {
        SetDropPhysicsEnabled(false);

        if (m_physicsCollider != null)
        {
            m_physicsCollider.enabled = false;
        }
    }

    public bool IsResultPlacementStable(
        float maximumLinearSpeed,
        float maximumAngularSpeed)
    {
        if (m_dropRigidbody == null)
        {
            return true;
        }

        return m_dropRigidbody.linearVelocity.sqrMagnitude
                <= maximumLinearSpeed * maximumLinearSpeed
            && m_dropRigidbody.angularVelocity.sqrMagnitude
                <= maximumAngularSpeed * maximumAngularSpeed;
    }
}