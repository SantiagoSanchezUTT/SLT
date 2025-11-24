using UnityEngine;
using System.Collections.Generic;
// NO AGREGUES "using System;" AQUÍ, ESO CAUSA LA AMBIGÜEDAD

[ExecuteInEditMode]
public class AutoTrafficNetwork : MonoBehaviour
{
    [Header("Ajustes de Precisión")]
    [Tooltip("Distancia máxima para conectar dos caminos.")]
    public float connectionRadius = 12.0f;

    [Tooltip("Ángulo máximo permitido entre calles.")]
    public float maxAngleDiff = 60.0f;

    [Header("Herramientas")]
    [Tooltip("Haz clic derecho en el título del script y elige 'Conectar Rutas Ahora'")]
    public bool mostrarAyuda = true;

    // ESTA ES LA FUNCIÓN DEL BOTÓN (Context Menu)
    [ContextMenu("🔴 CONECTAR RUTAS AHORA")]
    public void ConectarTodo()
    {
        LimpiarConexiones();
        ConnectRoutesStrictly();
    }

    void LimpiarConexiones()
    {
        // Usamos explícitamente UnityEngine.Object para evitar conflictos con System.Object
        WaypointPath[] allPaths = UnityEngine.Object.FindObjectsOfType<WaypointPath>();

        foreach (var path in allPaths)
        {
            if (path.nextConnectedPaths == null)
                path.nextConnectedPaths = new List<WaypointPath>();

            path.nextConnectedPaths.Clear();
        }
        UnityEngine.Debug.Log($"🧹 Limpieza completada: Se revisaron {allPaths.Length} rutas.");
    }

    void ConnectRoutesStrictly()
    {
        WaypointPath[] allPaths = UnityEngine.Object.FindObjectsOfType<WaypointPath>();
        int connectionsMade = 0;

        UnityEngine.Debug.Log("🔄 Iniciando algoritmo de conexión...");

        foreach (WaypointPath pathA in allPaths)
        {
            // Validaciones de seguridad
            if (pathA == null) continue;
            if (pathA.transform.childCount < 2) continue;

            // Datos de la Ruta A (El final de la calle)
            Transform endNodeA = pathA.transform.GetChild(pathA.transform.childCount - 1);
            Transform preEndA = pathA.transform.GetChild(pathA.transform.childCount - 2);

            // Vector de dirección de salida
            UnityEngine.Vector3 dirA = (endNodeA.position - preEndA.position).normalized;

            foreach (WaypointPath pathB in allPaths)
            {
                // Validaciones para Ruta B
                if (pathB == null) continue;
                if (pathA == pathB) continue; // No conectarse a sí mismo
                if (pathB.transform.childCount < 2) continue;

                // Datos de la Ruta B (El inicio de la calle)
                Transform startNodeB = pathB.transform.GetChild(0);

                // --- FILTRO 1: DISTANCIA ---
                float dist = UnityEngine.Vector3.Distance(endNodeA.position, startNodeB.position);
                if (dist > connectionRadius) continue;

                // --- FILTRO 2: ALINEACIÓN (Flujo) ---
                Transform postStartB = pathB.transform.GetChild(1);
                UnityEngine.Vector3 dirB = (postStartB.position - startNodeB.position).normalized;

                // Si el ángulo es muy abierto, es que la calle va en otro sentido
                if (UnityEngine.Vector3.Angle(dirA, dirB) > maxAngleDiff) continue;

                // --- FILTRO 3: POSICIÓN RELATIVA ---
                // ¿Está la calle B realmente "enfrente" de la A?
                UnityEngine.Vector3 dirToB = (startNodeB.position - endNodeA.position).normalized;
                if (UnityEngine.Vector3.Angle(dirA, dirToB) > 60) continue;

                // --- CONECTAR ---
                // Nos aseguramos de no duplicar
                if (!pathA.nextConnectedPaths.Contains(pathB))
                {
                    pathA.nextConnectedPaths.Add(pathB);
                    connectionsMade++;
                }
            }
        }

        UnityEngine.Debug.Log($"✅ ¡ÉXITO! Se generaron {connectionsMade} conexiones de tráfico.");
    }
}