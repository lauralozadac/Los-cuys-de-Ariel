using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Pasos 2a y 2b: poner el pez en la tabla y cortarlo.
//
// Va en un objeto "Zona_Corte" con un Box Collider (Is Trigger) que cubra el pez
// sobre la tabla, un poco más alto que el pez.
//
//   - Cuando el pez encaja en la snap zone de la tabla -> se completa el paso 2a.
//   - Cada vez que el cuchillo (en la mano) BAJA dentro de la zona, cuenta un corte.
//   - Al llegar a los cortes necesarios, el pez entero desaparece, aparece el pez
//     cortado y se completa el paso 2b.
//
// Conexiones (Interactable Unity Event Wrapper de la snap zone de la tabla):
//   When Select   -> FishCuttingBoard.OnFishPlaced
//   When Unselect -> FishCuttingBoard.OnFishRemoved
[RequireComponent(typeof(Collider))]
public class FishCuttingBoard : MonoBehaviour
{
    [Header("Pez")]
    [Tooltip("El pez entero (el que se lavó)")]
    public GameObject wholeFish;
    [Tooltip("El pez cortado, ya ubicado sobre la tabla donde queda el pez entero. Empieza apagado.")]
    public GameObject cutFish;

    [Header("Corte")]
    public int cutsRequired = 3;
    [Tooltip("Velocidad mínima hacia abajo para que cuente como corte (m/s)")]
    public float minDownSpeed = 0.3f;
    [Tooltip("Tiempo mínimo entre un corte y otro (s)")]
    public float cutCooldown = 0.35f;
    [Tooltip("Solo corta si el cuchillo está en la mano")]
    public bool requireHeldKnife = true;

    [Header("Pasos")]
    public KitchenStep placeStep;   // 2a: poner el pez en la tabla
    public KitchenStep cutStep;     // 2b: cortar el pez

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip cutSound;
    public AudioClip finishSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onFishPlacedOnBoard;
    public UnityEvent<int> onCut;      // recibe el número de cortes hechos
    public UnityEvent onFishCut;

    public bool IsFishOnBoard { get; private set; }
    public bool IsCut { get; private set; }
    public int Cuts { get; private set; }

    private float lastCutTime = -99f;
    private readonly Dictionary<Knife, int> knifeColliders = new Dictionary<Knife, int>();

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (cutFish != null) cutFish.SetActive(false);
    }

    // ---------- Snap zone de la tabla ----------

    public void OnFishPlaced()
    {
        IsFishOnBoard = true;
        if (debugLogs) Debug.Log("[Tabla] Pez en la tabla.", this);
        onFishPlacedOnBoard?.Invoke();
        if (placeStep != null) placeStep.Complete();
    }

    public void OnFishRemoved()
    {
        if (IsCut) return;   // al cortarlo apagamos el pez entero y eso también "lo saca"
        IsFishOnBoard = false;
        if (debugLogs) Debug.Log("[Tabla] Sacaron el pez de la tabla.", this);
    }

    // ---------- Detección del cuchillo ----------

    void OnTriggerEnter(Collider other)
    {
        Knife knife = other.GetComponentInParent<Knife>();
        if (knife == null) return;

        knifeColliders.TryGetValue(knife, out int n);
        knifeColliders[knife] = n + 1;
        if (n > 0) return;   // ese cuchillo ya estaba adentro (otro de sus colliders)

        TryCut(knife);
    }

    void OnTriggerExit(Collider other)
    {
        Knife knife = other.GetComponentInParent<Knife>();
        if (knife == null) return;

        if (knifeColliders.TryGetValue(knife, out int n))
            knifeColliders[knife] = Mathf.Max(0, n - 1);
    }

    private void TryCut(Knife knife)
    {
        if (IsCut) return;
        if (!IsFishOnBoard)
        {
            if (debugLogs) Debug.Log("[Tabla] El cuchillo entró pero no hay pez en la tabla.", this);
            return;
        }
        if (cutStep != null && !cutStep.IsActive) return;
        if (requireHeldKnife && !knife.IsHeld) return;

        float downSpeed = -knife.Velocity.y;
        if (downSpeed < minDownSpeed)
        {
            if (debugLogs) Debug.Log($"[Tabla] Corte muy suave ({downSpeed:0.00} m/s), no cuenta.", this);
            return;
        }
        if (Time.time - lastCutTime < cutCooldown) return;

        lastCutTime = Time.time;
        Cuts++;
        Play(cutSound);
        if (debugLogs) Debug.Log($"[Tabla] ¡Corte {Cuts}/{cutsRequired}!", this);
        onCut?.Invoke(Cuts);

        if (Cuts >= cutsRequired) FinishCut();
    }

    private void FinishCut()
    {
        IsCut = true;
        if (wholeFish != null) wholeFish.SetActive(false);
        if (cutFish != null) cutFish.SetActive(true);

        Play(finishSound);
        if (debugLogs) Debug.Log("[Tabla] ¡Pez cortado!", this);
        onFishCut?.Invoke();
        if (cutStep != null) cutStep.Complete();
    }

    private void Play(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }
}
