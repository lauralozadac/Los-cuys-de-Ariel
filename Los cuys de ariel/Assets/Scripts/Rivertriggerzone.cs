using UnityEngine;
using UnityEngine.Events;

// Zona invisible a lo largo del río. Cuando el bote entra, reproduce un audio
// (opcional) y dispara un evento (ej. FishGroup.StartAll).
// Requiere: un Collider con "Is Trigger" activado en este objeto, y que el bote
// tenga un Collider + Rigidbody (Is Kinematic) en el objeto con BoatController.
[RequireComponent(typeof(Collider))]
public class RiverTriggerZone : MonoBehaviour
{
    [Header("Audio (opcional)")]
    public AudioSource audioSource;   // déjalo vacío si esta zona no tiene audio
    public AudioClip clip;

    [Header("Comportamiento")]
    [Tooltip("Si está activo, la zona solo se dispara la primera vez")]
    public bool triggerOnce = false;

    [Header("Diagnóstico")]
    [Tooltip("Muestra en la consola todo lo que entra a la zona. Desactívalo cuando funcione.")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onBoatEnter;    // ej. FishGroup.StartAll
    public UnityEvent onBoatExit;     // ej. FishGroup.StopAll

    private bool hasTriggered = false;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        if (!debugLogs) return;

        var col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning($"[Zona {name}] El collider NO tiene 'Is Trigger' activado. Actívalo.", this);

        var boat = FindFirstObjectByType<MoveController>();
        if (boat == null)
        {
            Debug.LogWarning($"[Zona {name}] No encontré ningún BoatController en la escena.", this);
        }
        else
        {
            var rb = boat.GetComponentInParent<Rigidbody>();
            if (rb == null) rb = boat.GetComponent<Rigidbody>();
            if (rb == null)
                Debug.LogWarning($"[Zona {name}] El bote '{boat.name}' no tiene Rigidbody en su mismo objeto. " +
                                 "Agrégale uno con Is Kinematic activado.", boat);

            var cols = boat.GetComponentsInChildren<Collider>();
            if (cols.Length == 0)
                Debug.LogWarning($"[Zona {name}] El bote '{boat.name}' no tiene ningún Collider.", boat);
        }

        if (onBoatEnter.GetPersistentEventCount() == 0)
            Debug.LogWarning($"[Zona {name}] 'On Boat Enter' no tiene nada conectado.", this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (debugLogs) Debug.Log($"[Zona {name}] Entró: {other.name}", other);

        if (!IsBoat(other)) return;
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        if (debugLogs) Debug.Log($"[Zona {name}] ¡Bote detectado! Disparando On Boat Enter.", this);

        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);

        onBoatEnter?.Invoke();
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsBoat(other)) return;
        if (debugLogs) Debug.Log($"[Zona {name}] El bote salió. Disparando On Boat Exit.", this);
        onBoatExit?.Invoke();
    }

    private bool IsBoat(Collider other)
    {
        return other.GetComponentInParent<MoveController>() != null;
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.center, box.size);
    }
}