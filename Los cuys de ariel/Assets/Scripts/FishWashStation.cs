using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Paso 1: lavar el pez sumergiéndolo en el agua del lavaplatos.
//
// Va en un objeto "Agua_Lavaplatos" con un Box Collider (Is Trigger) que cubra
// el volumen del agua: desde la superficie hasta el fondo del lavaplatos.
//
//   - Mientras el pez está dentro del agua, el lavado avanza
//     (sirve si lo deja en la snap zone o si lo sostiene sumergido con la mano).
//   - A los 3 segundos queda limpio: brilla un poco, suena algo y se completa el paso.
//   - Si lo saca antes de tiempo, el conteo vuelve a empezar (se puede desactivar).
//
// No necesita conexiones: detecta el pez solo.
[RequireComponent(typeof(Collider))]
public class FishWashStation : MonoBehaviour
{
    [Header("Pez")]
    [Tooltip("El objeto raíz del pez de la cocina")]
    public Transform fish;

    [Header("Lavado")]
    public float washDuration = 3f;
    [Tooltip("Si el pez sale del agua antes de tiempo, el conteo vuelve a cero")]
    public bool resetIfRemoved = true;
    [Tooltip("Opcional: el paso de la cocina. Si lo asignas, el lavado solo cuenta " +
             "cuando este paso está activo y al terminar lo completa.")]
    public KitchenStep step;

    [Header("Pez limpio")]
    [Range(0f, 1f)] public float targetMetallic = 0.4f;
    [Range(0f, 1f)] public float targetSmoothness = 0.8f;
    [Tooltip("Tiempo en que el pez pasa de normal a brillante (s)")]
    public float shineTransition = 0.6f;

    [Header("Sonido (opcional)")]
    [Tooltip("AudioSource 3D en el lavaplatos")]
    public AudioSource audioSource;
    public AudioClip splashSound;    // el pez entra al agua
    public AudioClip washedSound;    // el pez quedó limpio

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onFishEnterWater;
    public UnityEvent onFishExitWater;
    public UnityEvent onFishWashed;

    public bool IsFishInWater => fishCollidersInside > 0;
    public bool IsWashed { get; private set; }
    public float Progress01 => Mathf.Clamp01(progress / washDuration);

    private int fishCollidersInside;
    private float progress;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (debugLogs && fish == null)
            Debug.LogWarning("[Lavaplatos] Falta asignar el pez.", this);
    }

    void Update()
    {
        if (IsWashed || !IsFishInWater) return;
        if (step != null && !step.IsActive) return;

        progress += Time.deltaTime;
        if (progress >= washDuration) FinishWash();
    }

    // ---------- Detección del pez en el agua ----------

    void OnTriggerEnter(Collider other)
    {
        if (!IsFish(other)) return;

        fishCollidersInside++;
        if (fishCollidersInside > 1) return;   // ya estaba adentro (otro de sus colliders)

        if (debugLogs) Debug.Log("[Lavaplatos] El pez entró al agua.", this);
        if (!IsWashed) Play(splashSound);
        onFishEnterWater?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsFish(other)) return;

        fishCollidersInside = Mathf.Max(0, fishCollidersInside - 1);
        if (fishCollidersInside > 0) return;   // todavía queda parte del pez adentro

        if (!IsWashed && resetIfRemoved && progress > 0f)
        {
            if (debugLogs) Debug.Log($"[Lavaplatos] Lo sacaron antes de tiempo ({progress:0.0}s). Vuelve a empezar.", this);
            progress = 0f;
        }
        else if (debugLogs)
        {
            Debug.Log("[Lavaplatos] El pez salió del agua.", this);
        }
        onFishExitWater?.Invoke();
    }

    private bool IsFish(Collider other)
    {
        return fish != null && (other.transform == fish || other.transform.IsChildOf(fish));
    }

    // ---------- Lavado ----------

    private void FinishWash()
    {
        IsWashed = true;
        if (debugLogs) Debug.Log("[Lavaplatos] ¡Pez lavado! Ya se puede sacar.", this);

        Play(washedSound);
        if (fish != null) StartCoroutine(Shine());

        onFishWashed?.Invoke();
        if (step != null) step.Complete();
    }

    // Sube un poco el Metallic y el Smoothness de los materiales del pez
    private IEnumerator Shine()
    {
        var mats = new List<Material>();
        var startMetal = new List<float>();
        var startSmooth = new List<float>();

        foreach (var r in fish.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var m in r.materials)   // copia propia: no cambia otros objetos con el mismo material
            {
                mats.Add(m);
                startMetal.Add(m.HasProperty("_Metallic") ? m.GetFloat("_Metallic") : 0f);
                startSmooth.Add(m.HasProperty("_Smoothness") ? m.GetFloat("_Smoothness") : 0f);
            }
        }

        float t = 0f;
        while (t < shineTransition)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / shineTransition);
            for (int i = 0; i < mats.Count; i++)
            {
                if (mats[i].HasProperty("_Metallic"))
                    mats[i].SetFloat("_Metallic", Mathf.Lerp(startMetal[i], targetMetallic, k));
                if (mats[i].HasProperty("_Smoothness"))
                    mats[i].SetFloat("_Smoothness", Mathf.Lerp(startSmooth[i], targetSmoothness, k));
            }
            yield return null;
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}