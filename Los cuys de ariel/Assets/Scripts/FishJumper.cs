using UnityEngine;

// Ciclo de un pez saltador:
//   Esperando (escondido bajo el agua) -> Asomándose (se sacude en la superficie,
//   avisa al jugador) -> Saltando (arco por el aire) -> vuelve a Esperando.
// Repite hasta que el jugador lo agarre (OnGrabbed) o el grupo lo detenga (StopCycle).
// Si el jugador lo suelta fuera del balde, FishCatchable llama a ReturnToWater().
//
// Va en un objeto vacío PADRE; el modelo animado del pez va como HIJO (campo "Visual").
public class FishJumper : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El modelo del pez (hijo). Si lo dejas vacío, usa el primer hijo.")]
    public Transform visual;

    [Header("Inicio")]
    [Tooltip("Arranca solo al dar Play. Desactívalo si lo controla un FishGroup.")]
    public bool autoStart = true;
    [Tooltip("Retraso aleatorio antes del primer salto (s)")]
    public Vector2 startDelayRange = new Vector2(0f, 3f);

    [Header("Salto")]
    public float jumpDistance = 2f;
    public float jumpHeight = 1f;
    public float jumpDuration = 1.6f;
    [Tooltip("Alterna el sentido del salto (ida y vuelta sobre el mismo arco)")]
    public bool alternateDirection = true;

    [Header("Espera entre saltos")]
    public Vector2 waitRange = new Vector2(1.5f, 4f);
    public float hideDepth = 0.5f;
    [Tooltip("Oculta el pez mientras espera bajo el agua (útil si el agua es transparente)")]
    public bool hideWhileWaiting = true;

    [Header("Aviso antes de saltar")]
    [Tooltip("Tiempo que el pez se asoma y se sacude antes de saltar (s)")]
    public float anticipationTime = 0.8f;
    [Tooltip("Qué tanto queda bajo la superficie mientras se asoma (m)")]
    public float peekDepth = 0.1f;
    public float wiggleAngle = 20f;
    public float wiggleSpeed = 25f;

    [Header("Salpicadura (opcional)")]
    public ParticleSystem splashPrefab;
    public AudioClip splashSound;
    [Range(0f, 1f)] public float splashVolume = 0.7f;

    private enum State { Idle, Waiting, Anticipating, Jumping, Grabbed }
    private State state = State.Idle;

    private Vector3 startPoint, endPoint;   // arco actual
    private Vector3 homeStart, homeEnd;     // arco original (para volver al agua)
    private float timer;
    private float stateDuration;
    private bool stopRequested;
    private bool cycleEnabled;              // el grupo quiere que este pez salte
    private Renderer[] renderers;

    public bool IsGrabbed => state == State.Grabbed;
    public Vector3 HomePosition => homeStart;

    void Awake()
    {
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        renderers = visual != null ? visual.GetComponentsInChildren<Renderer>()
                                   : GetComponentsInChildren<Renderer>();

        // El punto donde lo colocas en la escena (sobre el agua) es el inicio del arco
        startPoint = homeStart = transform.position;
        endPoint = homeEnd = startPoint + Flat(transform.forward) * jumpDistance;
    }

    void Start()
    {
        if (autoStart) StartJumping();
        else GoIdle();
    }

    void Update()
    {
        if (state == State.Idle || state == State.Grabbed) return;
        timer += Time.deltaTime;

        switch (state)
        {
            case State.Waiting:
                if (timer >= stateDuration)
                {
                    if (stopRequested) { GoIdle(); return; }
                    EnterAnticipation();
                }
                break;

            case State.Anticipating:
                Vector3 dir = Flat(endPoint - startPoint);
                float wiggle = Mathf.Sin(timer * wiggleSpeed) * wiggleAngle;
                transform.position = startPoint + Vector3.down * peekDepth
                                     + Vector3.up * Mathf.Sin(timer * wiggleSpeed * 0.5f) * 0.03f;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up)
                                     * Quaternion.Euler(-15f, wiggle, 0f);
                if (timer >= stateDuration) EnterJump();
                break;

            case State.Jumping:
                float t = Mathf.Clamp01(timer / jumpDuration);
                Vector3 pos = Vector3.Lerp(startPoint, endPoint, t);
                pos.y += jumpHeight * 4f * t * (1f - t);

                Vector3 move = pos - transform.position;
                if (move.sqrMagnitude > 0.000001f)
                    transform.rotation = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.position = pos;

                if (t >= 1f)
                {
                    Splash(endPoint);
                    if (alternateDirection)
                    {
                        Vector3 tmp = startPoint; startPoint = endPoint; endPoint = tmp;
                    }
                    else
                    {
                        Vector3 step = endPoint - startPoint;
                        startPoint = endPoint; endPoint = startPoint + step;
                    }
                    EnterWaiting(Random.Range(waitRange.x, waitRange.y));
                }
                break;
        }
    }

    // ---------- API pública ----------

    public void StartJumping()
    {
        StartJumping(Random.Range(startDelayRange.x, startDelayRange.y));
    }

    public void StartJumping(float delay)
    {
        cycleEnabled = true;
        if (state == State.Grabbed) return;
        stopRequested = false;
        if (state == State.Idle) EnterWaiting(delay);
    }

    // Detiene el ciclo sin cortar un salto a la mitad (ej. cuando el bote sale de la zona)
    public void StopCycle()
    {
        cycleEnabled = false;
        if (state == State.Grabbed) return;
        stopRequested = true;
        if (state == State.Waiting) GoIdle();
    }

    // El jugador lo agarró: deja de saltar y queda visible en la mano
    public void OnGrabbed()
    {
        state = State.Grabbed;
        SetVisible(true);
    }

    // Compatibilidad con la versión anterior
    public void StopJumping() { OnGrabbed(); }

    // El pez se soltó fuera del balde: vuelve a su punto del río.
    // Si el grupo sigue activo, retoma los saltos.
    public void ReturnToWater()
    {
        startPoint = homeStart;
        endPoint = homeEnd;
        Splash(homeStart);
        GoIdle();
        if (cycleEnabled) EnterWaiting(Random.Range(waitRange.x, waitRange.y));
    }

    // ---------- Estados ----------

    private void EnterWaiting(float duration)
    {
        state = State.Waiting;
        timer = 0f;
        stateDuration = duration;
        transform.position = startPoint + Vector3.down * hideDepth;
        if (hideWhileWaiting) SetVisible(false);
    }

    private void EnterAnticipation()
    {
        state = State.Anticipating;
        timer = 0f;
        stateDuration = anticipationTime;
        SetVisible(true);
    }

    private void EnterJump()
    {
        state = State.Jumping;
        timer = 0f;
        transform.position = startPoint;
        Splash(startPoint);
    }

    private void GoIdle()
    {
        state = State.Idle;
        stopRequested = false;
        transform.position = startPoint + Vector3.down * hideDepth;
        if (hideWhileWaiting) SetVisible(false);
    }

    // ---------- Utilidades ----------

    private void SetVisible(bool v)
    {
        foreach (var r in renderers) if (r != null) r.enabled = v;
    }

    private void Splash(Vector3 pos)
    {
        if (splashPrefab != null)
        {
            var ps = Instantiate(splashPrefab, pos, Quaternion.identity);
            Destroy(ps.gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        if (splashSound != null)
            AudioSource.PlayClipAtPoint(splashSound, pos, splashVolume);
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 a = Application.isPlaying ? startPoint : transform.position;
        Vector3 b = Application.isPlaying ? endPoint
                  : transform.position + Flat(transform.forward) * jumpDistance;
        Gizmos.color = Color.cyan;
        Vector3 prev = a;
        for (int i = 1; i <= 20; i++)
        {
            float t = i / 20f;
            Vector3 p = Vector3.Lerp(a, b, t);
            p.y += jumpHeight * 4f * t * (1f - t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}