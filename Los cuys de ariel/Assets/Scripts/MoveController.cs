using UnityEngine;
using UnityEngine.Events;

// Mueve el bote por una ruta de waypoints de forma continua (sin paradas).
// Los eventos del recorrido (audios, peces, etc.) se manejan con RiverTriggerZone.
//
// NOTA: si en su proyecto este script se llama MoveController, basta con copiar
// el método JumpToWaypoint (abajo) dentro de MoveController.
public class MoveController : MonoBehaviour
{
    [Header("Ruta")]
    public Transform[] waypoints;
    public float moveSpeed = 2f;
    public float turnSpeed = 2f;
    [Tooltip("Distancia a la que se considera que llegó al waypoint")]
    public float arrivalThreshold = 0.1f;

    [Header("Eventos")]
    public UnityEvent onJourneyComplete; // Llega a la bahía

    private int currentIndex = 0;
    private bool isMoving = true;

    void Start()
    {
        if (waypoints.Length == 0) return;
        transform.position = waypoints[0].position;
        currentIndex = 1;
    }

    void Update()
    {
        if (!isMoving || currentIndex >= waypoints.Length) return;

        Transform target = waypoints[currentIndex];

        transform.position = Vector3.MoveTowards(
            transform.position, target.position, moveSpeed * Time.deltaTime);

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, target.position) < arrivalThreshold)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
            {
                isMoving = false;
                onJourneyComplete?.Invoke();
            }
        }
    }

    public void Pause()  { isMoving = false; }
    public void Resume() { if (currentIndex < waypoints.Length) isMoving = true; }

    // Pone el bote en un waypoint y sigue el recorrido desde ahí.
    // Lo usa el ExperienceManager para reiniciar desde el checkpoint.
    public void JumpToWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0) return;
        index = Mathf.Clamp(index, 0, waypoints.Length - 1);

        transform.position = waypoints[index].position;

        // Mira hacia el siguiente waypoint
        if (index + 1 < waypoints.Length)
        {
            Vector3 dir = waypoints[index + 1].position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        currentIndex = index + 1;
        isMoving = currentIndex < waypoints.Length;
    }
}