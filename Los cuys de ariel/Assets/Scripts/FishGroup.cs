using UnityEngine;

// Controla un grupo de peces saltadores (FishJumper) como un solo tramo del río.
// Pon este script en un objeto vacío y los Pez_Saltador como hijos.
// Conecta StartAll / StopAll a los eventos On Boat Enter / On Boat Exit de un RiverTriggerZone
// para que los peces solo salten mientras el bote pasa por ese tramo.
public class FishGroup : MonoBehaviour
{
    [Tooltip("Si lo dejas vacío, toma automáticamente todos los FishJumper hijos")]
    public FishJumper[] fishes;

    [Tooltip("Arranca al dar Play (útil para probar sin el bote). " +
             "Desactívalo cuando lo controle un RiverTriggerZone.")]
    public bool startOnPlay = true;

    [Tooltip("Desfase máximo entre peces al arrancar (s). Más alto = saltos más repartidos.")]
    public float maxStagger = 3f;

    [Tooltip("Muestra en la consola cuándo arranca y se detiene el grupo")]
    public bool debugLogs = true;

    void Awake()
    {
        if (fishes == null || fishes.Length == 0)
            fishes = GetComponentsInChildren<FishJumper>();

        foreach (var f in fishes) if (f != null) f.autoStart = false;
    }

    void Start()
    {
        if (startOnPlay) StartAll();
    }

    public void StartAll()
    {
        if (debugLogs) Debug.Log($"[Grupo {name}] StartAll: {fishes.Length} peces empiezan a saltar.", this);

        for (int i = 0; i < fishes.Length; i++)
        {
            if (fishes[i] == null) continue;
            float baseDelay = fishes.Length > 1 ? maxStagger * i / (fishes.Length - 1) : 0f;
            fishes[i].StartJumping(baseDelay + Random.Range(0f, 0.5f));
        }
    }

    public void StopAll()
    {
        if (debugLogs) Debug.Log($"[Grupo {name}] StopAll: los peces dejan de saltar.", this);
        foreach (var f in fishes) if (f != null) f.StopCycle();
    }

    public int GrabbedCount()
    {
        int n = 0;
        foreach (var f in fishes) if (f != null && f.IsGrabbed) n++;
        return n;
    }
}