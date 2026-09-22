using UnityEngine;

public sealed class BattleResultLightEffectRotation : MonoBehaviour
{
    [SerializeField]
    private float m_rotationSpeed =
        24.0f;

    private void OnEnable()
    {
        transform.localRotation =
            Quaternion.identity;
    }

    private void Update()
    {
        transform.Rotate(
            0.0f,
            0.0f,
            m_rotationSpeed
            * Time.unscaledDeltaTime,
            Space.Self);
    }
}