using UnityEngine;

public sealed class CharacterRagdollImpactSensor : MonoBehaviour
{
    private CharacterRagdollController m_controller;
    private Rigidbody m_body;

    internal void Bind(
        CharacterRagdollController controller,
        Rigidbody body)
    {
        m_controller = controller;
        m_body = body;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (m_controller == null || m_body == null)
        {
            return;
        }

        m_controller.OnRagdollImpact(m_body, collision);
    }
}
