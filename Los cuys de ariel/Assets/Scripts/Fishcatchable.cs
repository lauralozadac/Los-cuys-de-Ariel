using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Conecta el agarre del Meta SDK con el pez saltador.
// Va en el mismo objeto que FishJumper y Grabbable (el padre Pez_Saltador).
//
// Eventos del agarre (Pointable Unity Event Wrapper apuntando al Grabbable):
//   When Select   -> FishCatchable.OnGrab
//   When Unselect -> FishCatchable.OnRelease
//
// - Al agarrarlo: deja de saltar.
// - Al soltarlo: si la snap zone del balde lo toma, se queda en el balde.
//   Si no, cae y después de unos segundos vuelve al río a seguir saltando.
[RequireComponent(typeof(Rigidbody))]
public class FishCatchable : MonoBehaviour
{
    [Header("Al soltarlo fuera del balde")]
    [Tooltip("Segundos hasta que el pez vuelve al río")]
    public float returnDelay = 2.5f;
    [Tooltip("Tiempo que espera para ver si la snap zone lo tomó (s)")]
    public float snapCheckDelay = 0.25f;
    [Tooltip("Si está activo, el pez cae con gravedad antes de volver al río")]
    public bool fallWhenReleased = true;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onGrabbed;
    public UnityEvent onReturnedToWater;

    private enum State { InWater, Held, Released, InBucket }
    private State state = State.InWater;

    private FishJumper jumper;
    private Rigidbody rb;
    private Transform originalParent;

    public bool IsHeld => state == State.Held;
    public bool IsInBucket => state == State.InBucket;

    void Awake()
    {
        jumper = GetComponent<FishJumper>();
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;

        // Mientras salta lo mueve el script, no la física
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    // ---------- Eventos del agarre ----------

    public void OnGrab()
    {
        StopAllCoroutines();
        state = State.Held;
        if (jumper != null) jumper.OnGrabbed();
        if (debugLogs) Debug.Log($"[Pez {name}] Agarrado.", this);
        onGrabbed?.Invoke();
    }

    public void OnRelease()
    {
        if (state != State.Held) return;   // si la snap zone ya lo tomó, no hacemos nada
        state = State.Released;
        StartCoroutine(ReleaseRoutine());
    }

    // ---------- Llamados por BucketController ----------

    public void SetInBucket(Transform newParent)
    {
        StopAllCoroutines();
        state = State.InBucket;
        StopPhysics();
        if (newParent != null) transform.SetParent(newParent, true);
        if (debugLogs) Debug.Log($"[Pez {name}] Quedó en el balde.", this);
    }

    public void RemovedFromBucket()
    {
        // Si lo sacan del balde es porque está en la mano
        if (transform.parent != originalParent) transform.SetParent(originalParent, true);
        if (state == State.InBucket) state = State.Held;
        if (debugLogs) Debug.Log($"[Pez {name}] Lo sacaron del balde.", this);
    }

    // ---------- Interno ----------

    private IEnumerator ReleaseRoutine()
    {
        // Damos un momento para que la snap zone lo tome
        yield return new WaitForSeconds(snapCheckDelay);
        if (state != State.Released) yield break;

        if (fallWhenReleased)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, returnDelay - snapCheckDelay));
        if (state != State.Released) yield break;

        ReturnToWater();
    }

    private void ReturnToWater()
    {
        StopPhysics();
        if (transform.parent != originalParent) transform.SetParent(originalParent, true);
        state = State.InWater;
        if (jumper != null) jumper.ReturnToWater();
        if (debugLogs) Debug.Log($"[Pez {name}] Volvió al río.", this);
        onReturnedToWater?.Invoke();
    }

    private void StopPhysics()
    {
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}
