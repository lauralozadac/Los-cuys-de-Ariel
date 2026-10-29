using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Paso 1: lavar el pez en el lavaplatos.
//   - El agua se abre/cierra tocando la manigueta (Poke) cuando el jugador quiera.
//   - El lavado solo avanza si el pez está en la snap zone del lavaplatos Y el agua está abierta.
//   - A los 3 segundos el pez queda limpio (brilla un poco), el agua se cierra sola
//     y el paso se completa.
//
// Conexiones:
//   Manigueta (Poke)  When Select   -> FishWashStation.ToggleWater
//   Snap zone         When Select   -> FishWashStation.OnFishPlaced
//   Snap zone         When Unselect -> FishWashStation.OnFishRemoved
public class FishWashStation : MonoBehaviour
{
    [Header("Agua")]
    [Tooltip("El chorro de agua (se prende y apaga)")]
    public GameObject waterStream;
    [Tooltip("Opcional: sonido del agua corriendo (AudioSource 3D en el grifo)")]
    public AudioSource waterSound;

    [Header("Lavado")]
    public float washDuration = 3f;
    public bool closeWaterWhenWashed = true;
    [Tooltip("Opcional: el paso de la cocina. Si lo asignas, el lavado solo cuenta " +
             "cuando este paso está activo y al terminar lo completa.")]
    public KitchenStep step;

    [Header("Pez limpio")]
    [Tooltip("El objeto raíz del pez de la cocina")]
    public Transform fish;
    [Range(0f, 1f)] public float targetMetallic = 0.4f;
    [Range(0f, 1f)] public float targetSmoothness = 0.8f;
    [Tooltip("Tiempo en que el pez pasa de normal a brillante (s)")]
    public float shineTransition = 0.6f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onWaterOpened;
    public UnityEvent onWaterClosed;
    public UnityEvent onFishWashed;

    public bool IsWaterOn { get; private set; }
    public bool IsFishInSink { get; private set; }
    public bool IsWashed { get; private set; }

    private float progress;

    void Start()
    {
        IsWaterOn = false;
        if (waterStream != null) waterStream.SetActive(false);
        if (waterSound != null) { waterSound.loop = true; waterSound.Stop(); }
    }

    void Update()
    {
        if (IsWashed || !IsWaterOn || !IsFishInSink) return;
        if (step != null && !step.IsActive) return;

        progress += Time.deltaTime;
        if (progress >= washDuration) FinishWash();
    }

    // ---------- Agua ----------

    public void ToggleWater() => SetWater(!IsWaterOn);
    public void OpenWater() => SetWater(true);
    public void CloseWater() => SetWater(false);

    private void SetWater(bool on)
    {
        if (IsWaterOn == on) return;
        IsWaterOn = on;

        if (waterStream != null) waterStream.SetActive(on);
        if (waterSound != null)
        {
            if (on) waterSound.Play();
            else waterSound.Stop();
        }

        if (debugLogs) Debug.Log($"[Lavaplatos] Agua {(on ? "abierta" : "cerrada")}.", this);
        if (on) onWaterOpened?.Invoke();
        else onWaterClosed?.Invoke();
    }

    // ---------- Snap zone ----------

    public void OnFishPlaced()
    {
        IsFishInSink = true;
        if (debugLogs) Debug.Log("[Lavaplatos] Pez en el lavaplatos.", this);
    }

    public void OnFishRemoved()
    {
        IsFishInSink = false;
        if (debugLogs) Debug.Log("[Lavaplatos] Sacaron el pez del lavaplatos.", this);
    }

    // ---------- Lavado ----------

    private void FinishWash()
    {
        IsWashed = true;
        if (debugLogs) Debug.Log("[Lavaplatos] ¡Pez lavado!", this);

        if (fish != null) StartCoroutine(Shine());
        if (closeWaterWhenWashed) CloseWater();

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
}