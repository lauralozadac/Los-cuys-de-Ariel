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

    [Header("Guía para el jugador")]
    [Tooltip("Texto que aparece en el panel de instrucciones al empezar el paso")]
    [TextArea] public string instruction = "";
    [Tooltip("Lo que hay que AGARRAR: brilla (pulso + contorno) mientras el paso está activo")]
    public GameObject[] highlightObjects;
    [Tooltip("Marcadores (TargetMarker) de DÓNDE dejarlo: aparecen solo durante el paso")]
    public GameObject[] targetMarkers;

    [Header("Audios (opcionales)")]
    public AudioClip startClip;      // instrucción al empezar el paso
    public AudioClip completeClip;   // felicitación al terminarlo

    [Header("Objetos")]
    [Tooltip("Se apagan al inicio y se encienden cuando empieza este paso")]
    public GameObject[] enableOnStart;
    [Tooltip("Se apagan cuando el paso se completa")]
    public GameObject[] disableOnComplete;

    [Header("Agarre por turno")]
    [Tooltip("Objetos que NO se pueden agarrar hasta que empiece este paso. Siguen visibles.")]
    public GameObject[] grabbableFromThisStep;
    [Tooltip("Objetos que dejan de poderse agarrar cuando este paso se completa")]
    public GameObject[] lockOnComplete;
    [Tooltip("Activado: solo bloquea el agarre A DISTANCIA (de cerca con la mano se pueden agarrar). " +
             "Desactivado: bloquea todo agarre.")]
    public bool onlyDistanceGrab = true;

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

        foreach (var go in targetMarkers)
            if (go != null) go.SetActive(false);

        foreach (var go in grabbableFromThisStep)
            SetGrab(go, false, onlyDistanceGrab);
    }

    public void Begin(KitchenStepManager owner)
    {
        manager = owner;
        IsActive = true;

        foreach (var go in enableOnStart)
            if (go != null) go.SetActive(true);

        foreach (var go in grabbableFromThisStep)
            SetGrab(go, true, onlyDistanceGrab);

        // Guía: texto, brillo y marcadores
        if (!string.IsNullOrWhiteSpace(instruction) && InstructionPanel.Instance != null)
            InstructionPanel.Instance.Show(instruction);
        foreach (var go in highlightObjects)
            if (go != null) Highlighter.Get(go).StartHighlight();
        foreach (var go in targetMarkers)
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

        // Apagar la guía de este paso
        foreach (var go in highlightObjects)
            if (go != null) Highlighter.Get(go).StopHighlight();
        foreach (var go in targetMarkers)
            if (go != null) go.SetActive(false);

        if (completeClip != null && NarrationManager.Instance != null)
            NarrationManager.Instance.Enqueue(completeClip);

        foreach (var go in disableOnComplete)
            if (go != null) go.SetActive(false);

        foreach (var go in lockOnComplete)
            SetGrab(go, false, onlyDistanceGrab);

        if (manager != null && manager.debugLogs)
            Debug.Log($"[Cocina] Completado: {stepName}", this);

        onStepComplete?.Invoke();
        if (manager != null) manager.OnStepCompleted(this);
    }

    // Prende o apaga los componentes de agarre del Interaction SDK de un objeto.
    // No toca las snap zones, los Poke ni los wrappers de eventos.
    public static void SetGrab(GameObject go, bool enabled, bool onlyDistance)
    {
        if (go == null) return;

        foreach (var b in go.GetComponentsInChildren<Behaviour>(true))
        {
            if (b == null) continue;
            System.Type t = b.GetType();
            if (t.Namespace == null || !t.Namespace.StartsWith("Oculus.Interaction")) continue;

            string n = t.Name;
            bool isGrab = n.EndsWith("GrabInteractable");   // HandGrab, Grab, DistanceGrab, DistanceHandGrab
            if (!isGrab) continue;
            if (onlyDistance && !n.StartsWith("Distance")) continue;

            b.enabled = enabled;
        }
    }
}