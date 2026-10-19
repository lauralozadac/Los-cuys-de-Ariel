using UnityEngine;

// Deja de dibujar ciertas capas cuando están lejos de la cámara del jugador.
// Ideal para vegetación: el jugador solo necesita ver las plantas cercanas.
//
// Uso: pon la vegetación en una capa (ej. "Vegetacion") y agrega este script
// a cualquier objeto de la escena.
public class LayerCullDistances : MonoBehaviour
{
    [System.Serializable]
    public class LayerDistance
    {
        public string layerName = "Vegetacion";
        [Tooltip("Distancia máxima a la que se dibuja esta capa (m)")]
        public float distance = 40f;
    }

    [Tooltip("Cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Camera targetCamera;
    public LayerDistance[] layers = { new LayerDistance() };
    [Tooltip("Mide la distancia como esfera alrededor del jugador. En VR evita que " +
             "las plantas aparezcan y desaparezcan al girar la cabeza.")]
    public bool sphericalCulling = true;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Debug.LogWarning("[Culling] No encontré la cámara del jugador.", this);
            return;
        }

        float[] distances = new float[32];   // 0 = usar la distancia normal de la cámara
        foreach (var l in layers)
        {
            int index = LayerMask.NameToLayer(l.layerName);
            if (index < 0)
            {
                Debug.LogWarning($"[Culling] No existe la capa '{l.layerName}'. Créala en Tags and Layers.", this);
                continue;
            }
            distances[index] = l.distance;
        }

        targetCamera.layerCullDistances = distances;
        targetCamera.layerCullSpherical = sphericalCulling;
    }
}