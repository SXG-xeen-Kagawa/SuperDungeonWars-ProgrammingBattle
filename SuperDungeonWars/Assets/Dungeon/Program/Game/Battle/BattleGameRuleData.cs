using System;
using UnityEngine;

[Serializable]
public class TreasureValueRange
{
    public TreasureType m_treasureType;
    public int m_minValue;
    public int m_maxValue;
}

[CreateAssetMenu(
    fileName = "BattleGameRuleData",
    menuName = "MazeExplore/Battle Game Rule Data")]
public class BattleGameRuleData : ScriptableObject
{
    [SerializeField, Min(1.0f)]
    private float m_battleDurationSeconds = 120.0f;

    [SerializeField, Min(0.1f)]
    private float m_normalMoveSpeed = 3.0f;

    [SerializeField, Min(0.1f)]
    private float m_carryingTreasureMoveSpeed = 2.0f;

    [SerializeField, Min(0.1f)]
    private float m_attackRange = 1.4f;

    [SerializeField, Min(0.1f)]
    private float m_attackStartRange = 0.9f;

    [SerializeField, Range(1.0f, 360.0f)]
    private float m_attackAngleDegrees = 120.0f;

    [SerializeField, Min(1)]
    private int m_attackAnimationFrameRate = 30;

    [SerializeField, Min(0)]
    private int m_attackHitFrame = 6;

    [SerializeField, Min(0.0f)]
    private float m_attackCooldownSeconds = 0.7f;

    [SerializeField, Min(0.1f)]
    private float m_knockoutSeconds = 3.0f;

    [SerializeField, Min(0.0f)]
    private float m_knockbackSpeed = 3.5f;

    [SerializeField, Min(0.0f)]
    private float m_treasureScatterMinDistance = 0.8f;

    [SerializeField, Min(0.0f)]
    private float m_treasureScatterMaxDistance = 1.6f;

    [SerializeField, Min(0.0f)]
    private float m_treasurePickupLockSeconds = 1.5f;

    [SerializeField, Min(0.0f)]
    private float m_attackMotionLockSeconds = 0.7f;

    [SerializeField, Min(0.0f)]
    private float m_knockoutRecoveryMoveLockSeconds = 0.5f;


    [SerializeField, Min(0.1f)]
    private float m_deliveryThrowLockSeconds = 1.2f;

    [SerializeField, Min(0.0f)]
    private float m_deliveryThrowHorizontalImpulse = 4.5f;

    [SerializeField, Min(0.0f)]
    private float m_deliveryThrowUpwardImpulse = 2.5f;

    [SerializeField, Min(0.0f)]
    private float m_deliveryThrowTorqueImpulse = 3.0f;

    [SerializeField, Min(0.0f)]
    private float m_deliveryTreasureVisibleSeconds = 1.2f;


    [SerializeField]
    private TreasureValueRange[] m_treasureValueRanges;

    public float BattleDurationSeconds => m_battleDurationSeconds;
    public float NormalMoveSpeed => m_normalMoveSpeed;
    public float CarryingTreasureMoveSpeed => m_carryingTreasureMoveSpeed;
    public float AttackRange => m_attackRange;
    public float AttackStartRange => m_attackStartRange;
    public float AttackAngleDegrees => m_attackAngleDegrees;
    public float AttackCooldownSeconds => m_attackCooldownSeconds;
    public float KnockoutSeconds => m_knockoutSeconds;
    public float KnockbackSpeed => m_knockbackSpeed;
    public float TreasureScatterMinDistance => m_treasureScatterMinDistance;
    public float TreasureScatterMaxDistance => m_treasureScatterMaxDistance;
    public float TreasurePickupLockSeconds => m_treasurePickupLockSeconds;

    public float AttackMotionLockSeconds => m_attackMotionLockSeconds;

    public float KnockoutRecoveryMoveLockSeconds => m_knockoutRecoveryMoveLockSeconds;



    public float AttackHitDelaySeconds
    {
        get
        {
            return m_attackAnimationFrameRate > 0
                ? (float)m_attackHitFrame / m_attackAnimationFrameRate
                : 0.0f;
        }
    }


    public float DeliveryThrowLockSeconds
    {
        get { return m_deliveryThrowLockSeconds; }
    }

    public float DeliveryThrowHorizontalImpulse
    {
        get { return m_deliveryThrowHorizontalImpulse; }
    }

    public float DeliveryThrowUpwardImpulse
    {
        get { return m_deliveryThrowUpwardImpulse; }
    }

    public float DeliveryThrowTorqueImpulse
    {
        get { return m_deliveryThrowTorqueImpulse; }
    }

    public float DeliveryTreasureVisibleSeconds
    {
        get { return m_deliveryTreasureVisibleSeconds; }
    }



    public bool TryGetTreasureValueRange(
        TreasureType treasureType,
        out int minValue,
        out int maxValue)
    {
        if (m_treasureValueRanges != null)
        {
            for (int i = 0; i < m_treasureValueRanges.Length; i++)
            {
                TreasureValueRange valueRange =
                    m_treasureValueRanges[i];

                if (valueRange == null
                    || valueRange.m_treasureType != treasureType)
                {
                    continue;
                }

                minValue = Mathf.Min(
                    valueRange.m_minValue,
                    valueRange.m_maxValue);

                maxValue = Mathf.Max(
                    valueRange.m_minValue,
                    valueRange.m_maxValue);

                return true;
            }
        }

        minValue = 0;
        maxValue = 0;
        return false;
    }

    public int GetRandomTreasureValue(
        TreasureType treasureType)
    {
        int minValue;
        int maxValue;

        if (!TryGetTreasureValueRange(
            treasureType,
            out minValue,
            out maxValue))
        {
            return 0;
        }

        return UnityEngine.Random.Range(minValue, maxValue + 1);
    }
}