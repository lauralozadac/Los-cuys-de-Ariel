using UnityEngine;
using UnityEngine.Events;

// Lleva el estado del balde: si tiene el pez o no.
// Va en el objeto BALDE. Escucha los eventos de la snap zone del balde.
//
// Eventos de la snap zone (Interactable Unity Event Wrapper apuntando al Snap Interactable):
//   When Select   -> BucketController.OnFishSnapped
//   When Unselect -> BucketController.OnFishUnsnapped
//
// Además controla cuándo se puede agarrar el balde (solo al final del recorrido).
public class BucketController : MonoBehaviour
{
    [Header("Snap zone")]
    [Tooltip("El objeto de la snap zone (donde encaja el pez). Si lo dejas vacío, usa este objeto.")]
    public Transform snapPoint;
    [Tooltip("Distancia máxima para identificar qué pez encajó (m)")]
    public float detectRadius = 0.5f;
    [Tooltip("Hace al pez hijo de la snap zone para que se mueva con el balde y el bote")]
    public bool parentFishOnSnap = true;

    [Header("Agarre del balde")]
    [Tooltip("Los objetos de agarre del balde (los que creó 'Add Grab Interaction'). " +
             "Se apagan al inicio y se encienden al final del recorrido.")]
    public GameObject[] grabInteractionObjects;
    public bool grabbableAtStart = false;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip fishInSound;
    public AudioClip fishOutSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    [Tooltip("Para el contador de la UI más adelante. Recibe 0 o 1.")]
    public UnityEvent<int> onFishCountChanged;
    public UnityEvent onFishIn;
    public UnityEvent onFishOut;

    public FishCatchable CurrentFish { get; private set; }
    public bool HasFish => CurrentFish != null;

    private FishCatchable[] allFish;

    void Start()
    {
        if (snapPoint == null) snapPoint = transform;
        allFish = FindObjectsByType<FishCatchable>(FindObjectsSortMode.None);
        SetGrabbable(grabbableAtStart);

        if (debugLogs && allFish.Length == 0)
            Debug.LogWarning("[Balde] No encontré peces con FishCatchable en la escena.", this);
    }

    // ---------- Eventos de la snap zone ----------

    public void OnFishSnapped()
    {
        FishCatchable fish = FindClosestFish();
        if (fish == null)
        {
            if (debugLogs) Debug.LogWarning("[Balde] La snap zone tomó algo, pero no encontré un pez cerca.", this);
            return;
        }

        CurrentFish = fish;
        fish.SetInBucket(parentFishOnSnap ? snapPoint : null);

        Play(fishInSound);
        if (debugLogs) Debug.Log($"[Balde] ¡Pez en el balde! ({fish.name})", this);
        onFishIn?.Invoke();
        onFishCountChanged?.Invoke(1);
    }

    public void OnFishUnsnapped()
    {
        if (CurrentFish == null) return;

        FishCatchable fish = CurrentFish;
        CurrentFish = null;
        fish.RemovedFromBucket();

        Play(fishOutSound);
        if (debugLogs) Debug.Log("[Balde] Sacaron el pez del balde.", this);
        onFishOut?.Invoke();
        onFishCountChanged?.Invoke(0);
    }

    // ---------- Agarre del balde ----------

    public void SetGrabbable(bool value)
    {
        if (grabInteractionObjects == null) return;
        foreach (var go in grabInteractionObjects)
            if (go != null) go.SetActive(value);
    }

    public void EnableGrab() => SetGrabbable(true);

    // ---------- Utilidades ----------

    private FishCatchable FindClosestFish()
    {
        FishCatchable best = null;
        float bestDist = detectRadius;
        foreach (var f in allFish)
        {
            if (f == null) continue;
            float d = Vector3.Distance(f.transform.position, snapPoint.position);
            if (d <= bestDist) { bestDist = d; best = f; }
        }
        return best;
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}