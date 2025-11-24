using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TrafficManager : MonoBehaviour
{
    public static TrafficManager Instance;

    [Header("Configuración Global")]
    public Transform player;
    public LayerMask trafficLayer;
    public GameObject[] carPrefabs;
    public int maxCars = 400;

    [Header("Ajuste de Densidad")]
    [Range(0.0f, 1.0f)]
    public float trafficDensity = 0.5f;

    [Header("Control de Espaciado")]
    [Tooltip("Distancia mínima obligatoria (en metros) entre vehículos al spawnear. Ej: 5.0m")]
    public float minVehicleSpacing = 5.0f; // ¡NUEVA VARIABLE CLAVE!

    [Header("Zonas de Aparición")]
    public float spawnRadiusMin = 40f;
    public float spawnRadiusMax = 100f;
    public float despawnDistance = 130f;

    private List<CarAI> trafficPool = new List<CarAI>();
    private List<WaypointPath> allRoutes = new List<WaypointPath>();
    private float timer = 0f;

    // -------------------------------------------------------------
    // AWAKE & START
    // -------------------------------------------------------------
    void Awake()
    {
        if (Instance == null) Instance = this;
        allRoutes = new List<WaypointPath>(FindObjectsOfType<WaypointPath>());
    }

    void Start()
    {
        if (carPrefabs.Length == 0)
        {
            Debug.LogError("❌ ERROR: No has asignado Prefabs de autos en el TrafficManager.");
            return;
        }

        for (int i = 0; i < maxCars; i++)
        {
            GameObject prefab = carPrefabs[UnityEngine.Random.Range(0, carPrefabs.Length)];
            GameObject obj = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            obj.transform.SetParent(transform);
            obj.SetActive(false);

            CarAI ai = obj.GetComponent<CarAI>();
            if (ai != null)
                trafficPool.Add(ai);
        }

        // 🚀 Spawn inicial masivo
        InitialMassSpawn();
    }

    // -------------------------------------------------------------
    // UPDATE & MANAGE TRAFFIC
    // -------------------------------------------------------------
    void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer > 0.5f)
        {
            timer = 0f;
            ManageTraffic();
        }
    }

    void ManageTraffic()
    {
        // 1. Reciclar autos que se alejaron demasiado
        for (int i = 0; i < trafficPool.Count; i++)
        {
            CarAI car = trafficPool[i];

            if (car.gameObject.activeInHierarchy)
            {
                if (Vector3.Distance(player.position, car.transform.position) > despawnDistance)
                {
                    car.gameObject.SetActive(false);
                }
            }
        }

        // 2. Control Dinámico de Densidad
        int activeCount = trafficPool.Count(c => c.gameObject.activeInHierarchy);
        int densityTarget = Mathf.FloorToInt(maxCars * trafficDensity);

        if (activeCount < densityTarget)
        {
            float spawnChance = 0.5f + (densityTarget - activeCount) / (float)maxCars;

            if (UnityEngine.Random.value < spawnChance)
            {
                SpawnCarNearPlayer();
            }
        }
    }

    // -------------------------------------------------------------
    // SPAWN INICIAL MASIVO Y DISTRIBUIDO
    // -------------------------------------------------------------
    void InitialMassSpawn()
    {
        int spawned = 0;
        int targetSpawnCount = Mathf.FloorToInt(maxCars * trafficDensity);

        // Recolectar todos los posibles nodos (sin cambios)
        List<(WaypointPath route, int index)> spawnPoints = new List<(WaypointPath, int)>();

        foreach (var r in allRoutes)
        {
            if (r.transform.childCount < 2) continue;

            for (int i = 0; i < r.transform.childCount - 1; i++)
            {
                if (UnityEngine.Random.value < trafficDensity)
                    spawnPoints.Add((r, i));
            }
        }

        spawnPoints = spawnPoints.OrderBy(x => UnityEngine.Random.value).ToList();

        foreach (var point in spawnPoints)
        {
            if (spawned >= maxCars || spawned >= targetSpawnCount) break;

            CarAI car = trafficPool[spawned];
            if (car == null) continue;

            Transform start = point.route.transform.GetChild(point.index);

            // --- VERIFICACIÓN DE ESPACIO UNIFORME ---
            if (Physics.CheckSphere(start.position, minVehicleSpacing, trafficLayer)) // Usa la nueva distancia
                continue;

            // No cerca del jugador
            float d = Vector3.Distance(player.position, start.position);
            if (d < spawnRadiusMin) continue;

            // Posición
            Vector3 pos = start.position;
            pos.y += 0.2f;

            Quaternion rot = Quaternion.LookRotation((point.route.transform.GetChild(point.index + 1).position - start.position).normalized);

            car.transform.position = pos;
            car.transform.rotation = rot;
            car.gameObject.SetActive(true);
            car.SetupRoute(point.route, point.index);

            spawned++;
        }

        Debug.Log($"🚗 Spawn inicial completado: {spawned} autos distribuidos (Densidad: {trafficDensity * 100}%).");
    }

    // -------------------------------------------------------------
    // SPAWN DINÁMICO CERCA DEL JUGADOR
    // -------------------------------------------------------------
    void SpawnCarNearPlayer()
    {
        CarAI candidate = trafficPool.FirstOrDefault(c => !c.gameObject.activeInHierarchy);
        if (candidate == null) return;

        var validRoutes = allRoutes.Where(r =>
        {
            if (r.transform.childCount < 2) return false;

            float d = Vector3.Distance(player.position, r.transform.GetChild(0).position);
            return d > spawnRadiusMin && d < spawnRadiusMax;

        }).ToList();

        if (validRoutes.Count == 0) return;

        var route = validRoutes[UnityEngine.Random.Range(0, validRoutes.Count)];
        int nodeIndex = UnityEngine.Random.Range(0, route.transform.childCount - 1);

        Transform startNode = route.transform.GetChild(nodeIndex);
        Transform nextNode = route.transform.GetChild(nodeIndex + 1);

        // --- VERIFICACIÓN DE ESPACIO UNIFORME ---
        if (Physics.CheckSphere(startNode.position, minVehicleSpacing, trafficLayer)) // Usa la nueva distancia
            return;

        // Posición
        Vector3 spawnPos = startNode.position;
        spawnPos.y += 0.2f;

        Quaternion lookRot = Quaternion.LookRotation((nextNode.position - startNode.position).normalized);

        candidate.transform.position = spawnPos;
        candidate.transform.rotation = lookRot;
        candidate.gameObject.SetActive(true);

        candidate.SetupRoute(route, nodeIndex);
    }

    // -------------------------------------------------------------
    // RUTA MÁS CERCANA (para la IA)
    // -------------------------------------------------------------
    public WaypointPath GetClosestRoute(Vector3 pos, WaypointPath ignore)
    {
        WaypointPath best = null;
        float minDst = Mathf.Infinity;

        foreach (var r in allRoutes)
        {
            if (r == ignore || r.transform.childCount == 0) continue;

            float d = Vector3.Distance(pos, r.transform.GetChild(0).position);

            if (d < minDst && d < 60f)
            {
                best = r;
            }
        }

        return best;
    }
}