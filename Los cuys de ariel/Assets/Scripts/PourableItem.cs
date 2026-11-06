using System.Collections;
using UnityEngine;

// Un pedazo que se puede voltear de un plato a la olla.
// No hace falta ponerlo a mano: PourablePlate lo agrega solo a cada pedazo.
//
// IMPORTANTE: los pedazos NO deben tener Grab Interaction propia (solo el plato).
public class PourableItem : MonoBehaviour
{
    private enum State { OnPlate, Falling, InPot }
    private State state = State.OnPlate;

    private PourablePlate plate;
    private Transform homeParent;
    private Vector3 homeLocalPos;
    private Quaternion homeLocalRot;
    private Vector3 homeLocalScale;
    private Rigidbody rb;
    private float returnDelay = 4f;
    private float timer;

    public bool IsInPot => state == State.InPot;

    // Guarda dónde está el pedazo en el plato (posición, rotación y escala) para poder devolverlo
    public void Init(PourablePlate owner, float delay)
    {
        plate = owner;
        returnDelay = delay;
        homeParent = transform.parent;
        homeLocalPos = transform.localPosition;
        homeLocalRot = transform.localRotation;
        homeLocalScale = transform.localScale;
    }

    void Update()
    {
        if (state != State.Falling) return;
        timer += Time.deltaTime;
        if (timer >= returnDelay) ReturnToPlate();
    }

    // Se suelta del plato y cae con gravedad
    public void Release()
    {
        if (state != State.OnPlate) return;

        EnsurePhysics();
        transform.SetParent(null, true);   // ya no se mueve con el plato
        rb.isKinematic = false;
        rb.useGravity = true;

        state = State.Falling;
        timer = 0f;
    }

    // Llamado por la olla cuando el pedazo entra
    public void EnterPot(PotContents pot)
    {
        if (state != State.Falling) return;
        state = State.InPot;
        StartCoroutine(SettleInPot(pot));
    }

    private IEnumerator SettleInPot(PotContents pot)
    {
        yield return new WaitForSeconds(pot.settleTime);   // lo dejamos caer al fondo
        Freeze();   // queda quieto donde cayó (sin cambiar de padre, para no deformarlo)
        if (plate != null) plate.OnItemInPot(this);
    }

    private void ReturnToPlate()
    {
        Freeze();
        transform.SetParent(homeParent, false);
        transform.localPosition = homeLocalPos;
        transform.localRotation = homeLocalRot;
        transform.localScale = homeLocalScale;   // vuelve con su tamaño original
        state = State.OnPlate;
        if (plate != null) plate.OnItemReturned(this);
    }

    private void EnsurePhysics()
    {
        // Si el pedazo no tiene collider, le ponemos uno que se ajusta a su malla
        if (GetComponentInChildren<Collider>() == null)
            gameObject.AddComponent<BoxCollider>();

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Freeze()
    {
        if (rb == null) return;
        if (!rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity = false;
    }
}