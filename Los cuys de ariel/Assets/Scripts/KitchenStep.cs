using UnityEngine;
using UnityEngine.Events;

// Un paso de la cocina (ej. "Lavar el pez", "Cortar el pez").
// El KitchenStepManager los activa en orden. Cuando el paso se cumple,
// algo llama a Complete() y el manager pasa al siguiente.
//
// Pon uno por cada paso del storyboard en un objeto vacío (ej. "Paso_1_Lavar").
public class KitchenStep : MonoBehaviour
{
    public string stepName = "Paso";

    [Header("Audios (opcionales)")]
    public AudioClip startClip;      // instrucción al empezar el paso
    public AudioClip completeClip;   // felicitación al terminarlo

    [Header("Objetos")]
    [Tooltip("Se apagan al inicio y se encienden cuando empieza este paso")]
    public GameObject[] enableOnStart;
    [Tooltip("Se apagan cuando el paso se completa")]
    public GameObject[] disableOnComplete;

    [Header("Eventos")]
    public UnityEvent onStepStart;
    public UnityEvent onStepComplete;

    public bool IsActive { get; private set; }
    public bool IsDone { get; private set; }

    private KitchenStepManager manager;

    void Awake()
    {
        foreach (var go in enableOnStart)
            if (go != null) go.SetActive(false);
    }

    public void Begin(KitchenStepManager owner)
    {
        manager = owner;
        IsActive = true;

        foreach (var go in enableOnStart)
            if (go != null) go.SetActive(true);

        if (startClip != null && NarrationManager.Instance != null)
            NarrationManager.Instance.Enqueue(startClip);

        if (manager != null && manager.debugLogs)
            Debug.Log($"[Cocina] Empieza: {stepName}", this);

        onStepStart?.Invoke();
    }

    // Llamar cuando el paso se cumple (desde un script o un evento del Inspector)
    public void Complete()
    {
        if (!IsActive || IsDone) return;
        IsDone = true;
        IsActive = false;

        if (completeClip != null && NarrationManager.Instance != null)
            NarrationManager.Instance.Enqueue(completeClip);

        foreach (var go in disableOnComplete)
            if (go != null) go.SetActive(false);

        if (manager != null && manager.debugLogs)
            Debug.Log($"[Cocina] Completado: {stepName}", this);

        onStepComplete?.Invoke();
        if (manager != null) manager.OnStepCompleted(this);
    }
}