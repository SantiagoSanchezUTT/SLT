using UnityEngine;

// Asegura que este script siempre tenga acceso a estos dos componentes
[RequireComponent(typeof(Rigidbody), typeof(CarAI))]
public class CarPhysicsHandler : MonoBehaviour
{
    private Rigidbody rb;
    private CarAI ai;

    [Header("Configuración de Choque")]
    public float disablePhysicsTime = 4.0f; // Tiempo que el auto estará volando
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ai = GetComponent<CarAI>();
    }

    void OnEnable()
    {
        // Al salir del Pool o al final del choque, el auto es controlado por la IA
        ResetToAImode();
    }

    void Update()
    {
        if (!rb.isKinematic)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                // Regresa al pool; TrafficManager lo respawnea
                gameObject.SetActive(false);
            }
        }
    }

    // ============================================================
    //  ACTIVAR FÍSICA REAL POR EXPLOSIÓN / CHOQUE
    // ============================================================
    public void ActivateExplosion(Vector3 impactForce, Vector3 impactPoint)
    {
        if (!rb.isKinematic) return; // Ya está volando

        // 1. Apagar la IA
        ai.enabled = false;

        // 2. Activar física real
        rb.isKinematic = false;

        // 3. Limpiar velocidades
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 4. Aplicar fuerza de impacto
        rb.AddForceAtPosition(impactForce, impactPoint, ForceMode.Impulse);

        // 5. Timer
        timer = disablePhysicsTime;
    }

    // ============================================================
    //  REGRESAR A MODO IA EL AUTO
    // ============================================================
    private void ResetToAImode()
    {
        // 1. Primero limpia velocidades (permitido porque sigue NO kinematic)
        rb.velocity = Vector3.zero;

        // ⚠️ Este era el error >> hay que limpiar ANTES de isKinematic = true
        if (!rb.isKinematic)
            rb.angularVelocity = Vector3.zero;

        // 2. Ahora sí, volverlo cinemático
        rb.isKinematic = true;

        // 3. Reactivar IA
        ai.enabled = true;

        // 4. Reiniciar timer
        timer = 0;
    }
}