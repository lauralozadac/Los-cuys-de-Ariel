using UnityEngine;
using UnityEngine.Events;

// Paso 5i: mezclar en la paila con la cuchara de palo.
// Va en la paila.
//
//   - Mientras la punta de la cuchara se MUEVE dentro de la zona de la paila, suma tiempo.
//   - A los 4 segundos: los ingredientes sueltos se apagan y aparece el "guiso"
//     (un objeto hijo de la paila que empieza apagado). Se completa el paso.
//
// El guiso puede ser algo simple por ahora (un cilindro aplanado con color de sofrito)
// y luego se reemplaza por el modelo de los diseñadores: solo hay que cambiar "Mixed Result".
// No necesita conexiones de eventos.
public class StirStation : MonoBehaviour
{
    [Tooltip("El paso de mezclar (ej. Paso_5i_Mezclar)")]
    public KitchenStep step;

    [Header("Zona y cuchara")]
    [Tooltip("Box Collider sobre la paila. Puede ser Zona_Condimento_Paila.")]
    public BoxCollider stirZone;
    [Tooltip("Objeto vacío en la punta de la cuchara de palo")]
    public Transform spoonTip;
    [Tooltip("Velocidad mínima de la punta para que cuente como mezclar (m/s)")]
    public float minSpeed = 0.15f;
    [Tooltip("Segundos mezclando")]
    public float requiredTime = 4f;

    [Header("Resultado")]
    [Tooltip("Los ingredientes que cayeron en la paila (cebolla, tomate, pimentón, ajo...). Se apagan al mezclar.")]
    public GameObject[] hideWhenMixed;
    [Tooltip("El guiso (hijo de la paila). Empieza apagado y aparece al mezclar.")]
    public GameObject mixedResult;

    [Header("Sonido (opcional)")]
    [Tooltip("AudioSource 3D en la paila. Si tiene un clip en loop, suena mientras se mezcla.")]
    public AudioSource stirLoop;
    public AudioSource audioSource;
    public AudioClip doneSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onMixed;

    public bool IsDone { get; private set; }
    public float Progress01 => Mathf.Clamp01(timer / requiredTime);

    private float timer;
    private Vector3 lastPos;

    void Start()
    {
        if (mixedResult != null) mixedResult.SetActive(false);
        if (spoonTip != null) lastPos = spoonTip.position;
        if (stirLoop != null) { stirLoop.loop = true; stirLoop.Stop(); }

        if (debugLogs)
        {
            if (stirZone == null) Debug.LogWarning("[Paila] Falta asignar la zona de mezclar.", this);
            if (spoonTip == null) Debug.LogWarning("[Paila] Falta asignar la punta de la cuchara.", this);
        }
    }

    void Update()
    {
        if (IsDone || spoonTip == null) return;

        Vector3 p = spoonTip.position;
        float speed = (p - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPos = p;

        bool active = step == null || step.IsActive;
        bool stirring = active && speed >= minSpeed && Inside(stirZone, p);

        if (stirLoop != null)
        {
            if (stirring && !stirLoop.isPlaying) stirLoop.Play();
            else if (!stirring && stirLoop.isPlaying) stirLoop.Stop();
        }

        if (!stirring) return;
        timer += Time.deltaTime;
        if (timer >= requiredTime) Finish();
    }

    private void Finish()
    {
        IsDone = true;
        if (stirLoop != null) stirLoop.Stop();

        foreach (var go in hideWhenMixed)
            if (go != null) go.SetActive(false);
        if (mixedResult != null) mixedResult.SetActive(true);

        if (audioSource != null && doneSound != null) audioSource.PlayOneShot(doneSound);
        if (debugLogs) Debug.Log("[Paila] ¡Guiso mezclado!", this);

        onMixed?.Invoke();
        if (step != null) step.Complete();
    }

    private static bool Inside(BoxCollider zone, Vector3 worldPoint)
    {
        if (zone == null) return false;
        Vector3 p = zone.transform.InverseTransformPoint(worldPoint) - zone.center;
        Vector3 h = zone.size * 0.5f;
        return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
    }
}