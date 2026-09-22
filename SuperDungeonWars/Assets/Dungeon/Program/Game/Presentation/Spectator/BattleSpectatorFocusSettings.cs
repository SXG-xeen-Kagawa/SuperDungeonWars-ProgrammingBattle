using System;
using UnityEngine;

[Serializable]
public sealed class BattleSpectatorFocusSettings
{
    [SerializeField]
    private float m_minimumFocusSeconds = 3.0f;

    [SerializeField]
    private float m_switchCooldownSeconds = 1.0f;

    [SerializeField]
    private float m_requiredScoreDifference = 100.0f;

    [SerializeField]
    private float m_nearEnemyDistance = 4.0f;

    [SerializeField]
    private float m_maximumFocusSeconds = 10.0f;

    [SerializeField]
    private float m_rotationScoreRange = 100.0f;

    [SerializeField]
    private float m_nearbyCharacterInfluenceDistance = 5.0f;

    public float MinimumFocusSeconds
    {
        get { return m_minimumFocusSeconds; }
    }

    public float SwitchCooldownSeconds
    {
        get { return m_switchCooldownSeconds; }
    }

    public float RequiredScoreDifference
    {
        get { return m_requiredScoreDifference; }
    }

    public float NearEnemyDistance
    {
        get { return m_nearEnemyDistance; }
    }

    public float MaximumFocusSeconds
    {
        get { return m_maximumFocusSeconds; }
    }

    public float RotationScoreRange
    {
        get { return m_rotationScoreRange; }
    }

    public float NearbyCharacterInfluenceDistance
    {
        get { return m_nearbyCharacterInfluenceDistance; }
    }
}