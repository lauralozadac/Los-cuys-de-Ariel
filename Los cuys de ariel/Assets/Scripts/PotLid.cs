using UnityEngine;
using UnityEngine.Events;

// Quitar la tapa de la olla (pasos 4a y 6d).
// Va en la tapa (TAPA OLLA). Se pueden poner DOS de estos en la tapa:
// uno para el paso 4a y otro para el 6d.
//
// Cuando su paso empieza, guarda dónde está la tapa. Si el jugador la aleja
// más de "Lift Distance", cuenta como quitada. No necesita conexiones de eventos.
public class PotLid : MonoBehaviour
{
    [Tooltip("El paso de quitar la tapa (ej. Paso_4a_Tapa o Paso_6d_Destapar)")]
    public KitchenStep removeStep;
    [Tooltip("Qué tanto hay que alejar la tapa para que cuente (m)")]
    public float liftDistance = 0.15f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onLidRemoved;

    public bool IsRemoved { get; private set; }
    // Dónde estaba la tapa cuando empezó su paso
    public Vector3 RestPosition { get; private set; }

    private bool armed;

    void Update()
    {
        if (IsRemoved) return;

        if (removeStep != null && !removeStep.IsActive)
        {
            armed = false;
            return;
        }

        // Primer frame del paso: guardamos dónde está la tapa ahora
        if (!armed)
        {
            RestPosition = transform.position;
            armed = true;
            return;
        }

        if (Vector3.Distance(transform.position, RestPosition) > liftDistance)
        {
            IsRemoved = true;
            if (debugLogs) Debug.Log("[Olla] Tapa quitada.", this);
            onLidRemoved?.Invoke();
            if (removeStep != null) removeStep.Complete();
        }
    }
}