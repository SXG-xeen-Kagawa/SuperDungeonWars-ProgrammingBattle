using UnityEngine;

/// <summary>
/// プロバト観戦用の専用カメラ制御。
/// キャラクターまたは宝箱を手動指定して追従し、
/// MazeWall Layerに対するSphereCastで簡易的な壁めり込み回避を行う。
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleSpectatorCameraController : MonoBehaviour
{
    private enum FocusTargetType
    {
        None,
        Character,
        TreasureChest,
    }

    [Header("Focus Target")]
    [SerializeField]
    private ComCharacterBase m_initialFocusCharacter;

    [SerializeField]
    private TreasureChest m_initialFocusTreasureChest;

    [Header("Character Camera")]
    [SerializeField]
    private float m_characterFocusHeight = 0.9f;

    [SerializeField]
    private float m_characterDistance = 6.5f;

    [SerializeField]
    private float m_characterHeight = 4.5f;

    [SerializeField]
    private float m_characterSideOffset = 1.2f;

    [Header("Treasure Chest Camera")]
    [SerializeField]
    private float m_treasureFocusHeight = 0.25f;

    [SerializeField]
    private float m_treasureDistance = 5.5f;

    [SerializeField]
    private float m_treasureHeight = 3.5f;

    [SerializeField]
    private float m_treasureSideOffset = 0.8f;

    [Header("Smoothing")]
    [SerializeField]
    [Min(0.01f)]
    private float m_positionSmoothTime = 0.18f;

    [SerializeField]
    [Min(0.0f)]
    private float m_rotationSmoothSpeed = 10.0f;

    [Header("Focus Switching")]
    [SerializeField]
    [Min(0.01f)]
    private float m_warpFocusDistance = 7.0f;

    [SerializeField]
    [Min(0.01f)]
    private float m_nearFocusPositionSmoothTime = 0.45f;

    [SerializeField]
    [Min(0.001f)]
    private float m_nearFocusRotationReleaseDistance = 0.08f;


    [Header("Wall Avoidance")]
    [SerializeField]
    private LayerMask m_wallLayerMask;

    [SerializeField]
    [Min(0.01f)]
    private float m_cameraCollisionRadius = 0.25f;

    [SerializeField]
    [Min(0.0f)]
    private float m_wallPadding = 0.15f;

    [SerializeField]
    [Min(0.01f)]
    private float m_minimumCollisionDistance = 0.2f;

    [Header("Idle")]
    [SerializeField]
    private bool m_keepCurrentPositionWhenNoFocus = true;




    [SerializeField]
    private bool m_enableWallAvoidance = false;


    private FocusTargetType m_focusTargetType;
    private ComCharacterBase m_focusCharacter;
    private TreasureChest m_focusTreasureChest;

    private Vector3 m_positionVelocity;
    private Vector3 m_lastStableBackwardDirection = Vector3.back;
    private bool m_isCameraPositionInitialized;
    private bool m_shouldWarpOnNextUpdate;
    private bool m_keepCurrentRotationDuringFocusTransition;


    /// <summary>
    /// 現在キャラクターを追従中かどうかを返す。
    /// </summary>
    public bool IsFocusingCharacter
    {
        get { return m_focusTargetType == FocusTargetType.Character; }
    }

    /// <summary>
    /// 現在宝箱を追従中かどうかを返す。
    /// </summary>
    public bool IsFocusingTreasureChest
    {
        get { return m_focusTargetType == FocusTargetType.TreasureChest; }
    }

    private void Awake()
    {
        ClearFocus();

        if (m_initialFocusCharacter != null)
        {
            SetFocusCharacter(m_initialFocusCharacter);
        }
        else if (m_initialFocusTreasureChest != null)
        {
            SetFocusTreasureChest(m_initialFocusTreasureChest);
        }
    }

    private void LateUpdate()
    {
        if (!TryGetFocusData(
                out Vector3 focusPosition,
                out Vector3 backwardDirection,
                out float cameraDistance,
                out float cameraHeight,
                out float sideOffset))
        {
            return;
        }

        Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(
            focusPosition,
            backwardDirection,
            cameraDistance,
            cameraHeight,
            sideOffset
        );

        Vector3 collisionAdjustedCameraPosition = m_enableWallAvoidance
            ? ResolveWallCollision(focusPosition, desiredCameraPosition)
            : desiredCameraPosition;

        UpdateCameraTransform(focusPosition, collisionAdjustedCameraPosition);
    }

    /// <summary>
    /// 指定したキャラクターを追従対象にする。
    /// 既存互換用。切替時のカメラ後方方向は変更しない。
    /// </summary>
    public void SetFocusCharacter(ComCharacterBase character)
    {
        SetFocusCharacter(
            character,
            m_lastStableBackwardDirection
        );
    }

    /// <summary>
    /// 指定したキャラクターを追従対象にする。
    /// 遠距離の切替時だけ、指定したカメラ後方方向を採用する。
    /// </summary>
    public void SetFocusCharacter(
        ComCharacterBase character,
        Vector3 desiredBackwardDirection)
    {
        if (character == null)
        {
            ClearFocus();
            return;
        }

        if (m_focusTargetType == FocusTargetType.Character
            && m_focusCharacter == character)
        {
            return;
        }

        Vector3 nextFocusPosition = character.GetWorldPosition()
            + Vector3.up * m_characterFocusHeight;

        bool shouldWarp = BeginFocusTransition(nextFocusPosition);

        m_focusTargetType = FocusTargetType.Character;
        m_focusCharacter = character;
        m_focusTreasureChest = null;

        if (shouldWarp)
        {
            SetFixedBackwardDirection(desiredBackwardDirection);
        }

        InitializeCameraPositionIfNeeded();
    }



    /// <summary>
    /// 指定した宝箱を追従対象にする。
    /// 既存互換用。切替時のカメラ後方方向は変更しない。
    /// </summary>
    public void SetFocusTreasureChest(TreasureChest treasureChest)
    {
        SetFocusTreasureChest(
            treasureChest,
            m_lastStableBackwardDirection
        );
    }

    /// <summary>
    /// 指定した宝箱を追従対象にする。
    /// 遠距離の切替時だけ、指定したカメラ後方方向を採用する。
    /// </summary>
    public void SetFocusTreasureChest(
        TreasureChest treasureChest,
        Vector3 desiredBackwardDirection)
    {
        if (treasureChest == null || treasureChest.IsExported)
        {
            ClearFocus();
            return;
        }

        if (m_focusTargetType == FocusTargetType.TreasureChest
            && m_focusTreasureChest == treasureChest)
        {
            return;
        }

        Vector3 nextFocusPosition = treasureChest.transform.position
            + Vector3.up * m_treasureFocusHeight;

        bool shouldWarp = BeginFocusTransition(nextFocusPosition);

        m_focusTargetType = FocusTargetType.TreasureChest;
        m_focusCharacter = null;
        m_focusTreasureChest = treasureChest;

        if (shouldWarp)
        {
            SetFixedBackwardDirection(desiredBackwardDirection);
        }

        InitializeCameraPositionIfNeeded();
    }


    /// <summary>
    /// 追従対象を解除する。
    /// </summary>
    public void ClearFocus()
    {
        m_focusTargetType = FocusTargetType.None;
        m_focusCharacter = null;
        m_focusTreasureChest = null;
        m_positionVelocity = Vector3.zero;

        if (!m_keepCurrentPositionWhenNoFocus)
        {
            m_isCameraPositionInitialized = false;
        }
    }

    public void SnapToCurrentFocus()
    {
        if (!TryGetFocusData(
                out Vector3 focusPosition,
                out Vector3 backwardDirection,
                out float cameraDistance,
                out float cameraHeight,
                out float sideOffset))
        {
            return;
        }

        Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(
            focusPosition,
            backwardDirection,
            cameraDistance,
            cameraHeight,
            sideOffset
        );

        Vector3 collisionAdjustedCameraPosition = m_enableWallAvoidance
            ? ResolveWallCollision(focusPosition, desiredCameraPosition)
            : desiredCameraPosition;

        m_shouldWarpOnNextUpdate = true;
        m_keepCurrentRotationDuringFocusTransition = false;

        UpdateCameraTransform(
            focusPosition,
            collisionAdjustedCameraPosition);
    }


    private bool BeginFocusTransition(Vector3 nextFocusPosition)
    {
        bool shouldWarp = !m_isCameraPositionInitialized;

        if (!shouldWarp
            && TryGetCurrentFocusPosition(out Vector3 currentFocusPosition))
        {
            Vector3 difference =
                nextFocusPosition - currentFocusPosition;

            difference.y = 0.0f;

            float warpDistanceSqr =
                m_warpFocusDistance * m_warpFocusDistance;

            shouldWarp = difference.sqrMagnitude >= warpDistanceSqr;
        }

        m_shouldWarpOnNextUpdate = shouldWarp;

        m_keepCurrentRotationDuringFocusTransition =
            !shouldWarp && m_isCameraPositionInitialized;

        /*
         * 切替前の対象を追うために残っている速度を引き継がない。
         * 近距離切替でも、いったん新対象へ素直に寄り始める。
         */
        m_positionVelocity = Vector3.zero;

        return shouldWarp;
    }

    private bool TryGetCurrentFocusPosition(
        out Vector3 currentFocusPosition)
    {
        currentFocusPosition = Vector3.zero;

        switch (m_focusTargetType)
        {
            case FocusTargetType.Character:
                if (m_focusCharacter == null
                    || !m_focusCharacter.gameObject.activeInHierarchy)
                {
                    return false;
                }

                currentFocusPosition = m_focusCharacter.GetWorldPosition()
                    + Vector3.up * m_characterFocusHeight;

                return true;

            case FocusTargetType.TreasureChest:
                if (m_focusTreasureChest == null
                    || m_focusTreasureChest.IsExported
                    || !m_focusTreasureChest.gameObject.activeInHierarchy)
                {
                    return false;
                }

                currentFocusPosition =
                    m_focusTreasureChest.transform.position
                    + Vector3.up * m_treasureFocusHeight;

                return true;

            default:
                return false;
        }
    }



    private bool TryGetFocusData(
        out Vector3 focusPosition,
        out Vector3 backwardDirection,
        out float cameraDistance,
        out float cameraHeight,
        out float sideOffset)
    {
        focusPosition = Vector3.zero;
        backwardDirection = m_lastStableBackwardDirection;
        cameraDistance = 0.0f;
        cameraHeight = 0.0f;
        sideOffset = 0.0f;

        switch (m_focusTargetType)
        {
            case FocusTargetType.Character:
                return TryGetCharacterFocusData(
                    out focusPosition,
                    out backwardDirection,
                    out cameraDistance,
                    out cameraHeight,
                    out sideOffset
                );

            case FocusTargetType.TreasureChest:
                return TryGetTreasureChestFocusData(
                    out focusPosition,
                    out backwardDirection,
                    out cameraDistance,
                    out cameraHeight,
                    out sideOffset
                );

            default:
                return false;
        }
    }

    private bool TryGetCharacterFocusData(
        out Vector3 focusPosition,
        out Vector3 backwardDirection,
        out float cameraDistance,
        out float cameraHeight,
        out float sideOffset)
    {
        focusPosition = Vector3.zero;
        backwardDirection = m_lastStableBackwardDirection;
        cameraDistance = m_characterDistance;
        cameraHeight = m_characterHeight;
        sideOffset = m_characterSideOffset;

        if (m_focusCharacter == null
            || !m_focusCharacter.gameObject.activeInHierarchy)
        {
            ClearFocus();
            return false;
        }

        //UpdateStableDirectionFromCharacter(m_focusCharacter);

        focusPosition = m_focusCharacter.GetWorldPosition()
            + Vector3.up * m_characterFocusHeight;

        backwardDirection = m_lastStableBackwardDirection;
        return true;
    }

    private bool TryGetTreasureChestFocusData(
        out Vector3 focusPosition,
        out Vector3 backwardDirection,
        out float cameraDistance,
        out float cameraHeight,
        out float sideOffset)
    {
        focusPosition = Vector3.zero;
        backwardDirection = m_lastStableBackwardDirection;
        cameraDistance = m_treasureDistance;
        cameraHeight = m_treasureHeight;
        sideOffset = m_treasureSideOffset;

        if (m_focusTreasureChest == null
            || m_focusTreasureChest.IsExported
            || !m_focusTreasureChest.gameObject.activeInHierarchy)
        {
            ClearFocus();
            return false;
        }

        /*
         * 宝箱単独時は、最後に安定していたカメラ後方方向を使う。
         * 所持中の宝箱なら、持ち主の向きを使うことで自然な斜め後方視点にする。
         */
        //ComCharacterBase ownerCharacter = m_focusTreasureChest.OwnerCharacter;
        //if (ownerCharacter != null && ownerCharacter.gameObject.activeInHierarchy)
        //{
        //    //UpdateStableDirectionFromCharacter(ownerCharacter);
        //}

        focusPosition = m_focusTreasureChest.transform.position
            + Vector3.up * m_treasureFocusHeight;

        backwardDirection = m_lastStableBackwardDirection;
        return true;
    }

    private void UpdateStableDirectionFromCharacter(ComCharacterBase character)
    {
        Vector3 characterForward = character.transform.forward;
        characterForward.y = 0.0f;

        if (characterForward.sqrMagnitude < 0.0001f)
        {
            return;
        }

        m_lastStableBackwardDirection = -characterForward.normalized;
    }

    private Vector3 CalculateDesiredCameraPosition(
        Vector3 focusPosition,
        Vector3 backwardDirection,
        float cameraDistance,
        float cameraHeight,
        float sideOffset)
    {
        Vector3 normalizedBackwardDirection = backwardDirection;
        normalizedBackwardDirection.y = 0.0f;

        if (normalizedBackwardDirection.sqrMagnitude < 0.0001f)
        {
            normalizedBackwardDirection = Vector3.back;
        }

        normalizedBackwardDirection.Normalize();

        Vector3 sideDirection = Vector3.Cross(Vector3.up, normalizedBackwardDirection);
        Vector3 horizontalOffset = normalizedBackwardDirection * cameraDistance
            + sideDirection * sideOffset;

        return focusPosition
            + horizontalOffset
            + Vector3.up * cameraHeight;
    }

    private Vector3 ResolveWallCollision(
        Vector3 focusPosition,
        Vector3 desiredCameraPosition)
    {
        Vector3 cameraOffset = desiredCameraPosition - focusPosition;
        float desiredDistance = cameraOffset.magnitude;

        if (desiredDistance <= 0.0001f)
        {
            return desiredCameraPosition;
        }

        Vector3 cameraDirection = cameraOffset / desiredDistance;

        if (!Physics.SphereCast(
                focusPosition,
                m_cameraCollisionRadius,
                cameraDirection,
                out RaycastHit hit,
                desiredDistance,
                m_wallLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return desiredCameraPosition;
        }

        float safeDistance = Mathf.Max(
            m_minimumCollisionDistance,
            hit.distance - m_wallPadding
        );

        safeDistance = Mathf.Min(safeDistance, desiredDistance);

        return focusPosition + cameraDirection * safeDistance;
    }

    private void UpdateCameraTransform(
        Vector3 focusPosition,
        Vector3 targetCameraPosition)
    {
        bool shouldSnapRotation = false;

        if (!m_isCameraPositionInitialized
            || m_shouldWarpOnNextUpdate)
        {
            /*
             * 初期配置および遠距離切替では、
             * カメラが通路を横断して移動する演出を作らず即時移動する。
             */
            transform.position = targetCameraPosition;

            m_positionVelocity = Vector3.zero;
            m_isCameraPositionInitialized = true;
            m_shouldWarpOnNextUpdate = false;

            shouldSnapRotation = true;
        }
        else
        {
            float positionSmoothTime =
                m_keepCurrentRotationDuringFocusTransition
                ? m_nearFocusPositionSmoothTime
                : m_positionSmoothTime;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetCameraPosition,
                ref m_positionVelocity,
                positionSmoothTime
            );
        }

        Vector3 lookDirection = focusPosition - transform.position;
        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(
            lookDirection.normalized,
            Vector3.up
        );

        if (shouldSnapRotation)
        {
            transform.rotation = targetRotation;
            return;
        }

        /*
         * 近距離の対象切替中は、カメラの首振りを発生させない。
         * カメラ位置が新しい配置へ十分近づいた時点で通常追従へ戻す。
         */
        if (m_keepCurrentRotationDuringFocusTransition)
        {
            float releaseDistanceSqr =
                m_nearFocusRotationReleaseDistance
                * m_nearFocusRotationReleaseDistance;

            Vector3 positionDifference =
                targetCameraPosition - transform.position;

            if (positionDifference.sqrMagnitude
                > releaseDistanceSqr)
            {
                return;
            }

            m_keepCurrentRotationDuringFocusTransition = false;
        }

        float rotationT = 1.0f - Mathf.Exp(
            -m_rotationSmoothSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationT
        );
    }


    private void InitializeCameraPositionIfNeeded()
    {
        if (m_isCameraPositionInitialized)
        {
            return;
        }

        m_positionVelocity = Vector3.zero;
    }


    /// <summary>
    /// カメラを置く水平方向を設定する。
    /// 例：チームの開始地点（出口）側から中央方向を見たい場合、
    /// チームがダンジョンへ進入する方向の反対を渡す。
    /// </summary>
    public void SetFixedBackwardDirection(Vector3 backwardDirection)
    {
        backwardDirection.y = 0.0f;

        if (backwardDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        m_lastStableBackwardDirection = backwardDirection.normalized;
    }



#if GAME_DEBUG
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!TryGetFocusData(
                out Vector3 focusPosition,
                out Vector3 backwardDirection,
                out float cameraDistance,
                out float cameraHeight,
                out float sideOffset))
        {
            return;
        }

        Vector3 desiredCameraPosition = CalculateDesiredCameraPosition(
            focusPosition,
            backwardDirection,
            cameraDistance,
            cameraHeight,
            sideOffset
        );

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(focusPosition, desiredCameraPosition);
        Gizmos.DrawWireSphere(
            desiredCameraPosition,
            m_cameraCollisionRadius
        );
    }
#endif
}