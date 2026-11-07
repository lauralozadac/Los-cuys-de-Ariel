using UnityEngine;
using UnityEngine.Events;

// Pasos 6e y 6f: servir con la cuchara honda.
// Va en la cuchara honda.
//
//   6e: cuando la punta de la cuchara entra en la olla, aparece el pescado en la cuchara.
//   6f: cuando la punta llega al plato, desaparece de la cuchara y aparece
//       el pescado cocinado organizado en el plato.
//
// No necesita conexiones de eventos.
public class ServingSpoon : MonoBehaviour
{
    [Tooltip("Objeto vacío en el centro de la parte honda de la cuchara")]
    public Transform tip;

    [Header("Zonas")]
    [Tooltip("El Box Collider del interior de la olla (Contenido_Olla)")]
    public BoxCollider potZone;
    [Tooltip("Box Collider sobre el plato final (Koe Koe)")]
    public BoxCollider plateZone;

    [Header("Comida")]
    [Tooltip("El pescado dentro de la cuchara (hijo de la cuchara). Empieza apagado.")]
    public GameObject spoonContents;
    [Tooltip("El pescado cocinado organizado en el plato. Empieza apagado.")]
    public GameObject servedFood;
    [Tooltip("Opcional: lo que se apaga en la olla al servir (ej. el pez cocinado dentro de la olla)")]
    public GameObject[] hideInPotWhenFilled;

    [Header("Pasos")]
    public KitchenStep fillStep;    // 6e: sacar de la olla
    public KitchenStep serveStep;   // 6f: servir en el plato

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip fillSound;
    public AudioClip serveSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onFilled;
    public UnityEvent onServed;

    public bool IsFilled { get; private set; }
    public bool IsServed { get; private set; }

    void Start()
    {
        if (tip == null) tip = transform;
        if (spoonContents != null) spoonContents.SetActive(false);
        if (servedFood != null) servedFood.SetActive(false);
    }

    void Update()
    {
        if (IsServed) return;
        Vector3 p = tip.position;

        if (!IsFilled)
        {
            if (fillStep != null && !fillStep.IsActive) return;
            if (Inside(potZone, p)) Fill();
        }
        else
        {
            if (serveStep != null && !serveStep.IsActive) return;
            if (Inside(plateZone, p)) Serve();
        }
    }

    private void Fill()
    {
        IsFilled = true;
        if (spoonContents != null) spoonContents.SetActive(true);
        foreach (var go in hideInPotWhenFilled)
            if (go != null) go.SetActive(false);

        Play(fillSound);
        if (debugLogs) Debug.Log("[Cuchara] Pescado en la cuchara.", this);
        onFilled?.Invoke();
        if (fillStep != null) fillStep.Complete();
    }

    private void Serve()
    {
        IsServed = true;
        if (spoonContents != null) spoonContents.SetActive(false);
        if (servedFood != null) servedFood.SetActive(true);

        Play(serveSound);
        if (debugLogs) Debug.Log("[Cuchara] ¡Pescado servido!", this);
        onServed?.Invoke();
        if (serveStep != null) serveStep.Complete();
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    private static bool Inside(BoxCollider zone, Vector3 worldPoint)
    {
        if (zone == null) return false;
        Vector3 p = zone.transform.InverseTransformPoint(worldPoint) - zone.center;
        Vector3 h = zone.size * 0.5f;
        return Mathf.Abs(p.x) <= h.x && Mathf.Abs(p.y) <= h.y && Mathf.Abs(p.z) <= h.z;
    }
}