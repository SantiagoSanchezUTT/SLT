using UnityEngine;

public class PlayerCarCollision : MonoBehaviour
{
    public float baseImpactForce = 15f;
    public float maxImpactForce = 60f; // seguridad física (nuevo)
    public LayerMask trafficLayer;

    void OnCollisionEnter(Collision collision)
    {
        // ¿Es un coche NPC?
        if ((trafficLayer.value & (1 << collision.gameObject.layer)) == 0)
            return;

        CarPhysicsHandler npcHandler = collision.gameObject.GetComponent<CarPhysicsHandler>();
        if (npcHandler == null) return;

        // ===============================
        // 1. Tomar contacto más válido
        // ===============================
        ContactPoint contact = collision.GetContact(0);

        // Normal apuntando desde NPC hacia afuera
        Vector3 impactDirection = -contact.normal;

        // Evitar fuerzas hacia abajo
        impactDirection.y = Mathf.Abs(impactDirection.y * 0.25f);

        impactDirection.Normalize();

        // ===============================
        // 2. Fuerza basada en velocidad
        // ===============================
        float relativeSpeed = collision.relativeVelocity.magnitude;

        Vector3 finalForce =
            impactDirection * baseImpactForce * Mathf.Clamp(relativeSpeed, 0f, maxImpactForce);

        // ===============================
        // 3. Aplicar explosión realista
        // ===============================
        npcHandler.ActivateExplosion(finalForce, contact.point);
    }
}