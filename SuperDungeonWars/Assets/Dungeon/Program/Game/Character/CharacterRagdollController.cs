using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CharacterRagdollController : MonoBehaviour
{
    [SerializeField] private Animator m_animator;
    [SerializeField] private Transform m_ragdollRoot;
    [SerializeField] private Transform m_hips;
    [SerializeField] private Rigidbody m_impulseBody;
    [SerializeField] private AnimationClip m_getUpFaceUpClip;
    [SerializeField] private AnimationClip m_getUpFaceDownClip;
    [SerializeField] private float m_recoveryBlendDuration = 0.2f;

    [SerializeField] private float m_attackForwardImpulseMultiplier = 1.5f;
    [SerializeField] private float m_attackUpwardImpulse = 0.25f;

    [SerializeField]
    private float m_attackForwardVelocityChange = 8.0f;

    [SerializeField]
    private float m_attackUpwardVelocityChange = 1.2f;

    [SerializeField]
    private float m_attackSpinVelocityChange = 2.5f;

    private static readonly int s_getUpFaceUpTriggerHash =
        Animator.StringToHash("GetUpFaceUp");

    private static readonly int s_getUpFaceDownTriggerHash =
        Animator.StringToHash("GetUpFaceDown");

    private ExplorerAgent m_explorerAgent;
    private ComCharacterBase m_character;
    private Rigidbody[] m_ragdollBodies;
    private Collider[] m_ragdollColliders;

    private readonly List<BonePose> m_ragdollPoseList = new();
    private readonly List<WorldBonePose> m_ragdollWorldPoseList = new();
    private readonly List<BonePose> m_getUpStartPoseList = new();

    private bool m_isRagdollActive;
    private bool m_isWaitingForGetUpAnimationFinished;
    private Coroutine m_recoveryCoroutine;

    public bool IsRagdollActive
    {
        get { return m_isRagdollActive; }
    }

    public bool IsRecovering
    {
        get
        {
            return m_recoveryCoroutine != null
                || m_isWaitingForGetUpAnimationFinished;
        }
    }

    private sealed class WorldBonePose
    {
        public Transform m_transform;
        public Vector3 m_worldPosition;
        public Quaternion m_worldRotation;
    }

    private sealed class BonePose
    {
        public Transform m_transform;
        public Vector3 m_localPosition;
        public Quaternion m_localRotation;
    }

    internal void BindExplorerAgent(
        ExplorerAgent explorerAgent)
    {
        if (explorerAgent == null)
        {
            Debug.LogError(
                "CharacterRagdollController.BindExplorerAgent: "
                + "explorerAgent が null です。",
                this);

            return;
        }

        m_explorerAgent = explorerAgent;
    }

    internal void BindCharacter(
        ComCharacterBase character)
    {
        if (character == null)
        {
            Debug.LogError(
                "CharacterRagdollController.BindCharacter: "
                + "character が null です。",
                this);

            return;
        }

        m_character = character;
    }

    private void Awake()
    {
        m_explorerAgent = GetComponentInParent<ExplorerAgent>();

        if (m_explorerAgent == null)
        {
            Debug.LogError(
                "CharacterRagdollController: 親階層に ExplorerAgent がありません。",
                this);
        }

        if (m_ragdollRoot == null)
        {
            Debug.LogError(
                "CharacterRagdollController: Ragdoll Root が未設定です。",
                this);

            return;
        }

        m_ragdollBodies =
            m_ragdollRoot.GetComponentsInChildren<Rigidbody>(true);

        m_ragdollColliders =
            m_ragdollRoot.GetComponentsInChildren<Collider>(true);

        SetRagdollPhysicsEnabled(false);
    }

    private void FixedUpdate()
    {
        if (!m_isRagdollActive ||
            m_explorerAgent == null ||
            m_hips == null)
        {
            return;
        }

        m_explorerAgent.UpdateRagdollWorldPosition(
            m_hips.position);
    }

    public void KnockOut(
        Vector3 attackForwardDirection,
        Vector3 attackerWorldPosition,
        float impulsePower)
    {
        _ = impulsePower;

        if (!m_isRagdollActive &&
            m_animator == null)
        {
            Debug.LogWarning(
                "[CharacterRagdollController.KnockOutIgnored] "
                + $"character={name} "
                + "reason=AnimatorMissing "
                + $"time={Time.time:F2}",
                this);

            return;
        }

        if (!m_isRagdollActive)
        {
            m_isRagdollActive = true;
            m_isWaitingForGetUpAnimationFinished = false;

            if (m_explorerAgent != null)
            {
                m_explorerAgent.BeginRagdoll();
            }

            m_animator.enabled = false;
            SetRagdollPhysicsEnabled(true);
        }
        else if (IsRecovering)
        {
            InterruptRecovery();
        }

        if (m_explorerAgent != null &&
            m_hips != null)
        {
            m_explorerAgent.UpdateRagdollWorldPosition(
                m_hips.position);
        }

        attackForwardDirection.y = 0.0f;

        if (attackForwardDirection.sqrMagnitude <= 0.0001f)
        {
            attackForwardDirection = transform.forward;
            attackForwardDirection.y = 0.0f;
        }

        if (attackForwardDirection.sqrMagnitude <= 0.0001f)
        {
            attackForwardDirection = Vector3.forward;
        }

        attackForwardDirection.Normalize();

        Vector3 launchVelocity =
            attackForwardDirection
            * m_attackForwardVelocityChange;

        launchVelocity.y =
            m_attackUpwardVelocityChange;

        for (int i = 0; i < m_ragdollBodies.Length; i++)
        {
            Rigidbody body = m_ragdollBodies[i];

            if (body == null)
            {
                continue;
            }

            body.linearVelocity = launchVelocity;
            body.angularVelocity = Vector3.zero;
        }

        Rigidbody hitBody = FindClosestRagdollBody(
            attackerWorldPosition);

        if (hitBody == null)
        {
            hitBody = m_impulseBody;
        }

        if (hitBody == null)
        {
            return;
        }

        Vector3 hitPosition = FindApproximateHitPosition(
            hitBody,
            attackerWorldPosition);

        hitBody.AddForceAtPosition(
            attackForwardDirection
            * m_attackForwardVelocityChange
            * 0.6f,
            hitPosition,
            ForceMode.VelocityChange);

        Vector3 spinAxis = Vector3.Cross(
            Vector3.up,
            attackForwardDirection);

        if (spinAxis.sqrMagnitude > 0.0001f)
        {
            hitBody.AddTorque(
                spinAxis.normalized
                * m_attackSpinVelocityChange,
                ForceMode.VelocityChange);
        }
    }

    private void InterruptRecovery()
    {
        Debug.Log(
            "[CharacterRagdollController.InterruptRecovery] "
            + $"character={name} "
            + $"instanceId={GetInstanceID()} "
            + $"wasBlending={m_recoveryCoroutine != null} "
            + $"wasPlayingGetUp={m_isWaitingForGetUpAnimationFinished} "
            + $"time={Time.time:F2}",
            this);

        if (m_recoveryCoroutine != null)
        {
            StopCoroutine(m_recoveryCoroutine);
            m_recoveryCoroutine = null;
        }

        m_isWaitingForGetUpAnimationFinished = false;

        if (m_animator != null)
        {
            m_animator.ResetTrigger(
                s_getUpFaceUpTriggerHash);

            m_animator.ResetTrigger(
                s_getUpFaceDownTriggerHash);

            m_animator.enabled = false;
        }

        SetRagdollPhysicsEnabled(true);
        Physics.SyncTransforms();

        if (m_explorerAgent != null)
        {
            m_explorerAgent.BeginRagdoll();

            if (m_hips != null)
            {
                m_explorerAgent.UpdateRagdollWorldPosition(
                    m_hips.position);
            }
        }
    }

    private Vector3 FindApproximateHitPosition(
        Rigidbody hitBody,
        Vector3 attackerWorldPosition)
    {
        if (hitBody == null)
        {
            return attackerWorldPosition;
        }

        Collider[] colliders =
            hitBody.GetComponentsInChildren<Collider>(true);

        Vector3 closestPosition =
            hitBody.worldCenterOfMass;

        float closestSqrDistance =
            (closestPosition - attackerWorldPosition).sqrMagnitude;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            if (collider == null)
            {
                continue;
            }

            Vector3 candidatePosition =
                collider.ClosestPoint(attackerWorldPosition);

            float sqrDistance =
                (candidatePosition - attackerWorldPosition)
                .sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance = sqrDistance;
            closestPosition = candidatePosition;
        }

        return closestPosition;
    }

    private Rigidbody FindClosestRagdollBody(
        Vector3 attackerWorldPosition)
    {
        if (m_ragdollBodies == null
            || m_ragdollBodies.Length <= 0)
        {
            return null;
        }

        Rigidbody closestBody = null;
        float closestSqrDistance = float.MaxValue;

        for (int i = 0; i < m_ragdollBodies.Length; i++)
        {
            Rigidbody body = m_ragdollBodies[i];

            if (body == null)
            {
                continue;
            }

            Vector3 bodyPosition = body.worldCenterOfMass;

            float sqrDistance =
                (bodyPosition - attackerWorldPosition).sqrMagnitude;

            if (sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            closestSqrDistance = sqrDistance;
            closestBody = body;
        }

        return closestBody;
    }

    public void ForceEndRagdollForResultPresentation()
    {
        if (m_recoveryCoroutine != null)
        {
            StopCoroutine(m_recoveryCoroutine);
            m_recoveryCoroutine = null;
        }

        SetRagdollPhysicsEnabled(false);

        m_isWaitingForGetUpAnimationFinished = false;
        m_isRagdollActive = false;

        if (m_animator != null)
        {
            m_animator.enabled = true;
            m_animator.Rebind();
            m_animator.Update(0.0f);

            m_animator.ResetTrigger(
                s_getUpFaceUpTriggerHash);

            m_animator.ResetTrigger(
                s_getUpFaceDownTriggerHash);
        }

        if (m_explorerAgent != null)
        {
            m_explorerAgent.EndRagdoll();
        }
    }

    public void Recover()
    {
        if (!m_isRagdollActive
            || m_recoveryCoroutine != null
            || m_isWaitingForGetUpAnimationFinished)
        {
            return;
        }

        m_recoveryCoroutine = StartCoroutine(
            RecoverCoroutine());
    }

    private IEnumerator RecoverCoroutine()
    {
        int getUpTriggerHash;

        AnimationClip getUpClip = SelectGetUpClip(
            out getUpTriggerHash);

        if (getUpClip == null)
        {
            FinishRecoveryWithoutAnimation();
            yield break;
        }

        Vector3 ragdollHipsPosition = m_hips.position;

        CaptureCurrentRagdollPose(m_ragdollPoseList);
        CaptureCurrentRagdollWorldPose(m_ragdollWorldPoseList);

        CaptureAnimationStartPose(
            getUpClip,
            m_getUpStartPoseList);

        SetRagdollPhysicsEnabled(false);

        ApplyPose(m_getUpStartPoseList);

        if (m_explorerAgent != null)
        {
            Vector3 rootPosition =
                m_explorerAgent.transform.position;

            Vector3 hipsOffset =
                ragdollHipsPosition - m_hips.position;

            rootPosition.x += hipsOffset.x;
            rootPosition.z += hipsOffset.z;

            m_explorerAgent.SetRagdollRecoveryPosition(
                rootPosition);
        }

        ApplyWorldPose(m_ragdollWorldPoseList);
        CaptureCurrentRagdollPose(m_ragdollPoseList);

        float elapsedTime = 0.0f;

        while (elapsedTime < m_recoveryBlendDuration)
        {
            elapsedTime += Time.deltaTime;

            float rate = Mathf.Clamp01(
                elapsedTime / m_recoveryBlendDuration);

            ApplyBlendedPose(rate);

            yield return null;
        }

        ApplyPose(m_getUpStartPoseList);

        m_animator.enabled = true;

        m_animator.ResetTrigger(s_getUpFaceUpTriggerHash);
        m_animator.ResetTrigger(s_getUpFaceDownTriggerHash);

        m_isWaitingForGetUpAnimationFinished = true;

        m_animator.SetTrigger(getUpTriggerHash);

        m_recoveryCoroutine = null;
    }

    private void FinishRecoveryWithoutAnimation()
    {
        SetRagdollPhysicsEnabled(false);

        m_isWaitingForGetUpAnimationFinished = false;

        if (m_animator != null)
        {
            m_animator.enabled = true;
        }

        if (m_explorerAgent != null)
        {
            m_explorerAgent.EndRagdoll();
        }

        m_isRagdollActive = false;
        m_recoveryCoroutine = null;
    }

    private AnimationClip SelectGetUpClip(
        out int getUpTriggerHash)
    {
        float faceUpDot = Vector3.Dot(
            m_hips.up,
            Vector3.up);

        if (faceUpDot >= 0.0f)
        {
            getUpTriggerHash = s_getUpFaceUpTriggerHash;
            return m_getUpFaceUpClip;
        }

        getUpTriggerHash = s_getUpFaceDownTriggerHash;
        return m_getUpFaceDownClip;
    }

    private void SetRagdollPhysicsEnabled(bool isEnabled)
    {
        foreach (Rigidbody body in m_ragdollBodies)
        {
            if (body == null)
            {
                continue;
            }

            if (isEnabled)
            {
                body.isKinematic = false;
                body.useGravity = true;

                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            else
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }

                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        foreach (Collider collider in m_ragdollColliders)
        {
            if (collider == null)
            {
                continue;
            }

            collider.enabled = isEnabled;
        }
    }

    private void CaptureCurrentRagdollPose(
        List<BonePose> poseList)
    {
        poseList.Clear();

        foreach (Rigidbody body in m_ragdollBodies)
        {
            poseList.Add(new BonePose
            {
                m_transform = body.transform,
                m_localPosition = body.transform.localPosition,
                m_localRotation = body.transform.localRotation,
            });
        }
    }

    private void CaptureCurrentRagdollWorldPose(
        List<WorldBonePose> poseList)
    {
        poseList.Clear();

        foreach (Rigidbody body in m_ragdollBodies)
        {
            if (body == null)
            {
                continue;
            }

            poseList.Add(new WorldBonePose
            {
                m_transform = body.transform,
                m_worldPosition = body.transform.position,
                m_worldRotation = body.transform.rotation,
            });
        }
    }

    private void ApplyWorldPose(
        List<WorldBonePose> poseList)
    {
        poseList.Sort(
            (left, right) =>
            GetTransformDepth(left.m_transform).CompareTo(
                GetTransformDepth(right.m_transform)));

        foreach (WorldBonePose pose in poseList)
        {
            if (pose.m_transform == null)
            {
                continue;
            }

            pose.m_transform.SetPositionAndRotation(
                pose.m_worldPosition,
                pose.m_worldRotation);
        }

        Physics.SyncTransforms();
    }

    private static int GetTransformDepth(
        Transform transform)
    {
        int depth = 0;
        Transform current = transform;

        while (current != null)
        {
            depth++;
            current = current.parent;
        }

        return depth;
    }

    private void CaptureAnimationStartPose(
        AnimationClip clip,
        List<BonePose> poseList)
    {
        clip.SampleAnimation(gameObject, 0.0f);

        poseList.Clear();

        foreach (Rigidbody body in m_ragdollBodies)
        {
            poseList.Add(new BonePose
            {
                m_transform = body.transform,
                m_localPosition = body.transform.localPosition,
                m_localRotation = body.transform.localRotation,
            });
        }

        ApplyPose(m_ragdollPoseList);
    }

    private void ApplyBlendedPose(float rate)
    {
        int poseCount = Mathf.Min(
            m_ragdollPoseList.Count,
            m_getUpStartPoseList.Count);

        for (int i = 0; i < poseCount; i++)
        {
            BonePose ragdollPose = m_ragdollPoseList[i];
            BonePose getUpPose = m_getUpStartPoseList[i];

            ragdollPose.m_transform.localPosition =
                Vector3.Lerp(
                    ragdollPose.m_localPosition,
                    getUpPose.m_localPosition,
                    rate);

            ragdollPose.m_transform.localRotation =
                Quaternion.Slerp(
                    ragdollPose.m_localRotation,
                    getUpPose.m_localRotation,
                    rate);
        }
    }

    private static void ApplyPose(List<BonePose> poseList)
    {
        foreach (BonePose pose in poseList)
        {
            pose.m_transform.localPosition =
                pose.m_localPosition;

            pose.m_transform.localRotation =
                pose.m_localRotation;
        }
    }

    public void OnGetUpAnimationFinished()
    {
        if (!m_isRagdollActive ||
            !m_isWaitingForGetUpAnimationFinished)
        {
            return;
        }

        if (m_character != null &&
            m_character.IsKnockedOut())
        {
            InterruptRecovery();
            return;
        }

        m_isWaitingForGetUpAnimationFinished = false;

        if (m_explorerAgent != null)
        {
            m_explorerAgent.EndRagdoll();
        }

        m_isRagdollActive = false;
    }
}