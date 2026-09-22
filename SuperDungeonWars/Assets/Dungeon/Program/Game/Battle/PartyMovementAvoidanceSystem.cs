using UnityEngine;

public class PartyMovementAvoidanceSystem : MonoBehaviour
{
    public static PartyMovementAvoidanceSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public Vector3 GetAdjustedMoveDirection(
        ExplorerAgent agent,
        Vector3 desiredDirection,
        float moveDistance)
    {
        // 現時点では、味方への停止・譲り・横回避を行わない。
        // Rigidbody + CapsuleCollider の通常の物理衝突だけを観察する。
        return desiredDirection;
    }
}