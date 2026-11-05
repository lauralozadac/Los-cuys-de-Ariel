using UnityEngine;
using UnityEngine.Events;

// Paso 4a: quitar la tapa de la olla.
// Va en la tapa (TAPA OLLA). Cuando la tapa se aleja de su lugar más de
// "Lift Distance", cuenta como quitada. No necesita conexiones de eventos.
public class PotLid : MonoBehaviour
{
    [Tooltip("El paso de quitar la tapa (ej. Paso_4a_Tapa)")]
    public KitchenStep removeStep;
    [Tooltip("Qué tanto hay que alejar la tapa de la olla para que cuente (m)")]
    public float liftDistance = 0.15f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onLidRemoved;

    public bool IsRemoved { get; private set; }
    // Dónde estaba la tapa al inicio (para el paso de volver a taparla, más adelante)
    public Vector3 RestPosition { get; private set; }

    void Start()
    {
        RestPosition = transform.position;
    }

    void Update()
    {
        if (IsRemoved) return;
        if (removeStep != null && !removeStep.IsActive) return;

        if (Vector3.Distance(transform.position, RestPosition) > liftDistance)
        {
            IsRemoved = true;
            if (debugLogs) Debug.Log("[Olla] Tapa quitada.", this);
            onLidRemoved?.Invoke();
            if (removeStep != null) removeStep.Complete();
        }
    }
}