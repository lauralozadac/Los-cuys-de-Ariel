using System.Collections;
using UnityEngine;

// Pone un objeto (ej. el Canvas del menú) frente a la mirada del jugador,
// a la altura de sus ojos, sin importar si está de pie o sentado.
// Si el jugador gira mucho la cabeza, el objeto lo sigue con suavidad.
//
// Va en el objeto PADRE del Canvas, en la raíz de la Hierarchy (no hijo de la cámara).
public class HeadFollower : MonoBehaviour
{
    [Tooltip("La cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Transform head;

    [Header("Posición")]
    [Tooltip("Distancia al frente del jugador (m)")]
    public float distance = 1.5f;
    [Tooltip("Altura respecto a los ojos (m). 0 = a la altura de la mirada.")]
    public float heightOffset = 0f;

    [Header("Seguimiento")]
    [Tooltip("Sigue al jugador si gira la cabeza. Desactívalo para que solo se ubique al inicio.")]
    public bool follow = true;
    [Tooltip("Cuántos grados debe girar la cabeza para que el objeto la siga")]
    public float recenterAngle = 40f;
    public float followSpeed = 3f;

    [Header("Inicio")]
    [Tooltip("Espera antes de ubicarse, para que las gafas ya tengan la posición de la cabeza (s)")]
    public float startDelay = 0.3f;

    private bool ready;
    private bool needsRecenter;

    IEnumerator Start()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;

        // Las gafas tardan unos frames en reportar dónde está la cabeza
        yield return new WaitForSeconds(startDelay);
        SnapToTarget();
        ready = true;
    }

    void LateUpdate()
    {
        if (!ready || !follow) return;
        if (head == null)
        {
            if (Camera.main == null) return;
            head = Camera.main.transform;
        }

        Vector3 forward = FlatForward();
        Vector3 desired = head.position + forward * distance + Vector3.up * heightOffset;

        Vector3 toObj = transform.position - head.position;
        toObj.y = 0f;
        if (Vector3.Angle(forward, toObj) > recenterAngle || Mathf.Abs(transform.position.y - desired.y) > 0.4f)
            needsRecenter = true;

        if (needsRecenter)
        {
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            if ((transform.position - desired).sqrMagnitude < 0.0004f) needsRecenter = false;
        }

        FaceHead();
    }

    // Para volver a ubicarlo frente al jugador desde un evento
    public void SnapToTarget()
    {
        if (head == null) return;
        transform.position = head.position + FlatForward() * distance + Vector3.up * heightOffset;
        FaceHead();
        needsRecenter = false;
    }

    private Vector3 FlatForward()
    {
        Vector3 f = head.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) f = head.up;
        f.y = 0f;
        return f.normalized;
    }

    private void FaceHead()
    {
        Vector3 look = transform.position - head.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look, Vector3.up);   // el Canvas se lee de frente
    }
}