using UnityEngine;
using System.Collections;

public class CarWaterReset : MonoBehaviour
{
    [Header("Configuración General")]
    public string waterTag = "Water";
    public string roadTag = "Road";
    public float waitTime = 4.0f; // Aumenté el tiempo para disfrutar la flotación
    public float groundCheckDistance = 3.0f;

    [Header("Configuración de Flotación")]
    public float floatForce = 12f; // Fuerza hacia arriba (Contrarresta la gravedad)
    public float waterDrag = 2f;   // Resistencia del agua (para que no se mueva rápido)
    public float waterAngularDrag = 2f; // Resistencia al giro

    [Header("Referencias")]
    public UniversalCarController carController;
    public Rigidbody carRb;

    // Estado interno
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private bool isDrowning = false;
    private bool hasSafePosition = false;

    void Start()
    {
        if (carController == null) carController = GetComponent<UniversalCarController>();
        if (carRb == null) carRb = GetComponent<Rigidbody>();

        // Guardar primera posición por seguridad
        SaveCurrentPosition();
    }

    void Update()
    {
        // Guardar posición solo si estamos seguros en la carretera
        if (!isDrowning && IsCarStable() && IsTouchingRoad())
        {
            SaveCurrentPosition();
        }
    }

    // AQUI OCURRE LA MAGIA DE LA FLOTACIÓN
    void FixedUpdate()
    {
        if (isDrowning && carRb != null)
        {
            // 1. Aplicar fuerza hacia arriba para que flote
            // (La gravedad suele ser 9.81, así que una fuerza de 10-15 lo hace flotar)
            carRb.AddForce(Vector3.up * floatForce, ForceMode.Acceleration);

            // 2. Un poquito de rotación suave para simular oleaje
            // Hacemos que el coche se incline un poco al azar
            float waveX = Mathf.Sin(Time.time) * 0.5f;
            float waveZ = Mathf.Cos(Time.time * 0.8f) * 0.5f;
            carRb.AddTorque(new Vector3(waveX, 0, waveZ), ForceMode.Force);
        }
    }

    void SaveCurrentPosition()
    {
        lastSafePosition = transform.position;
        lastSafeRotation = transform.rotation;
        hasSafePosition = true;
    }

    bool IsCarStable()
    {
        if (Vector3.Dot(transform.up, Vector3.up) < 0.5f) return false;
        return true;
    }

    bool IsTouchingRoad()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, groundCheckDistance))
        {
            if (hit.collider.CompareTag(roadTag)) return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(waterTag) && !isDrowning)
        {
            StartCoroutine(DrownSequence());
        }
    }

    private IEnumerator DrownSequence()
    {
        isDrowning = true;

        // 1. APAGAR CONTROLES (Pero dejar el Rigidbody encendido para que flote)
        if (carController != null)
        {
            if (carController.engineSource != null) carController.engineSource.Stop();
            if (carController.brakeSource != null) carController.brakeSource.Stop();
            carController.enabled = false;
        }

        // 2. CONFIGURAR FÍSICAS DE AGUA
        float originalDrag = carRb.drag;
        float originalAngularDrag = carRb.angularDrag;

        carRb.drag = waterDrag;             // El agua te frena mucho
        carRb.angularDrag = waterAngularDrag; // Cuesta más girar en el agua

        // 3. FLOTAR UN RATO (Esperamos el tiempo configurado)
        yield return new WaitForSeconds(waitTime);

        // 4. RESTAURAR FÍSICAS ORIGINALES
        carRb.drag = originalDrag; // 0.05f o lo que tuvieras
        carRb.angularDrag = originalAngularDrag;
        carRb.velocity = Vector3.zero;
        carRb.angularVelocity = Vector3.zero;

        // 5. RESPAWN (Teletransportar)
        if (hasSafePosition)
        {
            transform.position = lastSafePosition + Vector3.up * 2.0f;
            transform.rotation = Quaternion.Euler(0, lastSafeRotation.eulerAngles.y, 0);
        }
        else
        {
            transform.position = new Vector3(0, 5, 0);
        }

        // 6. ENCENDER CONTROLES
        if (carController != null)
        {
            carController.enabled = true;
            if (carController.engineSource != null) carController.engineSource.Play();
            if (carController.brakeSource != null) carController.brakeSource.Play();
        }

        isDrowning = false;
    }
}