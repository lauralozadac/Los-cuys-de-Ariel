using UnityEngine;
using UnityEngine.Events;

// Pasos 3a, 3b y 3c: condimentar el pez.
// Va en cada frasco (salero, achote, Maggi).
//
// El condimento cuenta como aplicado cuando el frasco está BOCA ABAJO y su boca
// está dentro de la zona sobre el plato durante "Required Time" segundos.
// No necesita conexiones de eventos.
public class Condiment : MonoBehaviour
{
    public string condimentName = "Sal";
    [Tooltip("El paso de este condimento (ej. Paso_3a_Sal)")]
    public KitchenStep step;

    [Header("Dónde se echa")]
    [Tooltip("Box Collider sobre el plato del pez. Puede ser el mismo para los 3 frascos.")]
    public BoxCollider seasoningZone;
    [Tooltip("Punto en la boca del frasco. Si lo dejas vacío, usa el centro del frasco.")]
    public Transform spout;
    [Tooltip("Hacia dónde apunta la boca del frasco en sus ejes locales (normalmente arriba = Y)")]
    public Vector3 localOpeningAxis = Vector3.up;

    [Header("Inclinación")]
    [Tooltip("Ángulo a partir del cual el frasco está 'boca abajo' (grados). 90 = acostado.")]
    public float pourAngle = 100f;
    [Tooltip("Segundos echando condimento para que cuente")]
    public float requiredTime = 1f;

    [Header("Efectos (opcionales)")]
    [Tooltip("Partículas que caen mientras el frasco está inclinado")]
    public ParticleSystem particles;
    public AudioSource audioSource;
    public AudioClip doneSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onSeasoned;

    public bool IsDone { get; private set; }

    private float timer;

    void Start()
    {
        if (spout == null) spout = transform;
        if (particles != null) particles.Stop();
        if (debugLogs && seasoningZone == null)
            Debug.LogWarning($"[Condimento {condimentName}] Falta asignar la zona sobre el plato.", this);
    }

    void Update()
    {
        bool active = step == null || step.IsActive;
        bool pouring = !IsDone && active && IsTilted();

        // Partículas mientras está inclinado (solo en su turno)
        if (particles != null)
        {
            if (pouring && !particles.isPlaying) particles.Play();
            else if (!pouring && particles.isPlaying) particles.Stop();
        }

        if (!pouring) return;
        if (seasoningZone != null && !InsideZone(spout.position)) return;

        timer += Time.deltaTime;
        if (timer >= requiredTime) Finish();
    }

    private bool IsTilted()
    {
        Vector3 opening = transform.TransformDirection(localOpeningAxis.normalized);
        return Vector3.Angle(opening, Vector3.up) > pourAngle;
    }

    private bool InsideZone(Vector3 worldPoint)
    {
        Vector3 p = seasoningZone.transform.InverseTransformPoint(worldPoint) - seasoningZone.center;
        Vector3 h = seasoningZone.size * 0.5f;
        return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
    }

    private void Finish()
    {
        IsDone = true;
        if (particles != null) particles.Stop();
        if (audioSource != null && doneSound != null) audioSource.PlayOneShot(doneSound);
        if (debugLogs) Debug.Log($"[Condimento] ¡{condimentName} aplicado!", this);

        onSeasoned?.Invoke();
        if (step != null) step.Complete();
    }
}