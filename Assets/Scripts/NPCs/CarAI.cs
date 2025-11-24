using UnityEngine;

public class CarAI : MonoBehaviour
{
    [Header("Velocidad")]
    public float speed = 12f;
    public float rotationSpeed = 4f;

    [Header("Ruta actual")]
    public WaypointPath currentPath;
    public int currentIndex = 0;

    [Header("Distancia mínima para pasar al siguiente punto")]
    public float waypointReachDistance = 3f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        // Asegurar que no haya movimiento raro al activarse
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        if (currentPath == null || currentPath.transform.childCount == 0)
            return;

        MoveAlongPath();
    }

    // ============================================================
    // LÓGICA PRINCIPAL DE IA
    // ============================================================
    void MoveAlongPath()
    {
        Vector3 target = currentPath.GetWaypointPosition(currentIndex);

        // Dirección hacia el waypoint
        Vector3 direction = (target - transform.position);
        Vector3 flatDirection = new Vector3(direction.x, 0f, direction.z).normalized;

        if (flatDirection.sqrMagnitude > 0.01f)
        {
            // Rotación suave hacia la ruta
            Quaternion targetRot = Quaternion.LookRotation(flatDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );
        }

        // Movimiento
        transform.position += transform.forward * speed * Time.deltaTime;

        // Cambiar al siguiente punto
        float dist = Vector3.Distance(transform.position, target);
        if (dist < waypointReachDistance)
        {
            currentIndex++;

            // Si llegó al final, buscar si hay rutas conectadas
            if (currentIndex >= currentPath.transform.childCount)
            {
                TryConnectToNextPath();
            }
        }
    }

    // ============================================================
    // CAMBIO AUTOMÁTICO A OTRA RUTA (USANDO nextConnectedPaths)
    // ============================================================
    void TryConnectToNextPath()
    {
        if (currentPath.nextConnectedPaths != null &&
            currentPath.nextConnectedPaths.Count > 0)
        {
            // Elegir una ruta conectada al azar
            WaypointPath next = currentPath.nextConnectedPaths[
                Random.Range(0, currentPath.nextConnectedPaths.Count)
            ];

            if (next != null && next.transform.childCount > 0)
            {
                currentPath = next;
                currentIndex = 0;
                return;
            }
        }

        // Si no hay rutas conectadas → Desactivar y reciclar auto
        gameObject.SetActive(false);
    }

    // ============================================================
    // MÉTODO QUE USA EL TRAFFICMANAGER PARA COLOCARLO
    // ============================================================
    public void SetupRoute(WaypointPath path, int startIndex)
    {
        currentPath = path;
        currentIndex = startIndex;
    }
}