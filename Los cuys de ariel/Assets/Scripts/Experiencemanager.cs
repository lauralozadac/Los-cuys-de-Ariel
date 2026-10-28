using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Controla el flujo de la experiencia:
//
//   Inicio            -> suenan los 3 diálogos seguidos (con una pausa entre ellos).
//   Salir zona pesca 1 -> audio "pescó" o "no pescó" según el balde.
//   Fin del recorrido:
//     - Con pez -> suena el audio final, se espera a que termine, fundido a negro
//                  y el jugador aparece en la casa (acto 2).
//     - Sin pez -> se reinicia DESDE EL CHECKPOINT (antes de la primera zona de pesca),
//                  sin repetir los diálogos del inicio.
//
// Conexiones:
//   Bote (On Journey Complete)            -> ExperienceManager.OnJourneyComplete
//   Zona de pesca 1 (On Boat Exit)        -> ExperienceManager.OnFirstFishingZoneExit
//   Zona antes de la pesca (On Boat Enter) -> NarrationManager.Enqueue (clip)
public class ExperienceManager : MonoBehaviour
{
    [Header("Referencias")]
    public BucketController bucket;
    [Tooltip("El objeto que mueve el bote (la Canoa con su script de waypoints)")]
    public MonoBehaviour boatMover;
    public NarrationManager narration;
    [Tooltip("El rig del jugador ([BuildingBlock] Camera Rig)")]
    public Transform playerRig;
    [Tooltip("La cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Camera playerCamera;

    [Header("Audios del recorrido")]
    [Tooltip("Los diálogos del inicio, en orden")]
    public AudioClip[] introClips;
    [Tooltip("Espera antes del primer diálogo (s)")]
    public float introStartDelay = 1.5f;
    public AudioClip caughtFishClip;      // salió de la zona 1 con pez
    public AudioClip noFishClip;          // salió de la zona 1 sin pez (hay otra oportunidad)
    public AudioClip finalClip;           // antes de pasar a la casa

    [Header("Reinicio")]
    [Tooltip("Número del waypoint donde reaparece el bote al reiniciar " +
             "(un poco antes de la zona del audio previo a la pesca)")]
    public int checkpointWaypointIndex = 0;
    [Tooltip("Segundos antes de reiniciar cuando no hay pez. 0 = de una.")]
    public float restartDelay = 0f;

    [Header("Llegada a la casa")]
    [Tooltip("Objeto vacío en el piso de la casa; la flecha azul apunta al mesón")]
    public Transform houseSpawnPoint;
    [Tooltip("Se apagan al inicio y se encienden al llegar (ej. el balde de la casa)")]
    public GameObject[] enableOnArrival;
    [Tooltip("Se apagan al llegar")]
    public GameObject[] disableOnArrival;

    [Header("Transición")]
    public ScreenFader fader;
    public float fadeDuration = 0.6f;
    public float blackHoldTime = 0.3f;

    [Header("Sonido ambiente")]
    public AudioSource riverAmbience;
    public AudioSource houseAmbience;
    public float ambienceCrossfade = 2f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onAct2Start;
    public UnityEvent onArrivedWithoutFish;

    // Sobrevive a la recarga de la escena: indica que hay que empezar en el checkpoint
    private static bool resumeFromCheckpoint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => resumeFromCheckpoint = false;  // cada Play empieza limpio

    private bool busy;
    private bool firstZoneResolved;
    private float houseTargetVolume = 1f;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (narration == null) narration = NarrationManager.Instance;

        foreach (var go in enableOnArrival)
            if (go != null) go.SetActive(false);

        if (riverAmbience != null)
        {
            riverAmbience.loop = true;
            if (!riverAmbience.isPlaying) riverAmbience.Play();
        }
        if (houseAmbience != null)
        {
            houseTargetVolume = houseAmbience.volume;
            houseAmbience.loop = true;
            houseAmbience.Stop();
            houseAmbience.volume = 0f;
        }

        if (resumeFromCheckpoint)
        {
            resumeFromCheckpoint = false;
            StartCoroutine(ResumeAtCheckpoint());
        }
        else
        {
            StartCoroutine(PlayIntro());
        }

        if (debugLogs)
        {
            if (bucket == null) Debug.LogWarning("[Experiencia] Falta asignar el balde del bote.", this);
            if (boatMover == null) Debug.LogWarning("[Experiencia] Falta asignar el bote (Boat Mover).", this);
            if (narration == null) Debug.LogWarning("[Experiencia] Falta el NarrationManager.", this);
            if (playerRig == null) Debug.LogWarning("[Experiencia] Falta asignar el rig del jugador.", this);
            if (houseSpawnPoint == null) Debug.LogWarning("[Experiencia] Falta el punto de aparición en la casa.", this);
        }
    }

    // ---------- Inicio ----------

    private IEnumerator PlayIntro()
    {
        if (introStartDelay > 0f) yield return new WaitForSeconds(introStartDelay);
        if (narration != null && introClips != null) narration.EnqueueMany(introClips);
    }

    private IEnumerator ResumeAtCheckpoint()
    {
        yield return null;  // esperamos un frame para que el bote ya haya hecho su Start
        if (boatMover != null)
            boatMover.SendMessage("JumpToWaypoint", checkpointWaypointIndex, SendMessageOptions.RequireReceiver);
        if (debugLogs) Debug.Log($"[Experiencia] Reinicio: el bote empieza en el waypoint {checkpointWaypointIndex}.", this);
    }

    // ---------- Primera zona de pesca ----------

    public void OnFirstFishingZoneExit()
    {
        if (firstZoneResolved) return;
        firstZoneResolved = true;

        bool caught = bucket != null && bucket.HasFish;
        if (debugLogs) Debug.Log($"[Experiencia] Salió de la zona 1. ¿Pescó? {caught}", this);
        if (narration != null) narration.Enqueue(caught ? caughtFishClip : noFishClip);
    }

    // ---------- Fin del recorrido ----------

    public void OnJourneyComplete()
    {
        if (busy) return;
        busy = true;

        if (bucket != null && bucket.HasFish) StartCoroutine(GoToHouse());
        else StartCoroutine(RestartRoutine());
    }

    private IEnumerator GoToHouse()
    {
        if (debugLogs) Debug.Log("[Experiencia] Llegó con el pez. Audio final y luego a la casa.", this);

        // El audio final entra a la cola y esperamos a que termine todo lo que suena
        if (narration != null)
        {
            narration.Enqueue(finalClip);
            yield return narration.WaitUntilFinished();
        }

        if (fader != null) yield return fader.FadeOut(fadeDuration);

        TeleportPlayer(houseSpawnPoint);

        foreach (var go in disableOnArrival)
            if (go != null) go.SetActive(false);
        foreach (var go in enableOnArrival)
            if (go != null) go.SetActive(true);

        StartCoroutine(CrossfadeAmbience());

        yield return new WaitForSeconds(blackHoldTime);
        if (fader != null) yield return fader.FadeIn(fadeDuration);

        if (debugLogs) Debug.Log("[Experiencia] Jugador en la casa. Empieza el acto 2.", this);
        onAct2Start?.Invoke();
    }

    private void TeleportPlayer(Transform target)
    {
        if (playerRig == null || target == null) return;

        playerRig.SetParent(null, true);

        if (playerCamera == null)
        {
            playerRig.SetPositionAndRotation(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));
            return;
        }

        Transform cam = playerCamera.transform;
        float yawDelta = Mathf.DeltaAngle(cam.eulerAngles.y, target.eulerAngles.y);
        playerRig.RotateAround(cam.position, Vector3.up, yawDelta);

        Vector3 headOffset = cam.position - playerRig.position;
        headOffset.y = 0f;
        playerRig.position = target.position - headOffset;
    }

    private IEnumerator CrossfadeAmbience()
    {
        float riverStart = riverAmbience != null ? riverAmbience.volume : 0f;
        if (houseAmbience != null)
        {
            houseAmbience.volume = 0f;
            houseAmbience.Play();
        }

        float t = 0f;
        while (t < ambienceCrossfade)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / ambienceCrossfade);
            if (riverAmbience != null) riverAmbience.volume = Mathf.Lerp(riverStart, 0f, k);
            if (houseAmbience != null) houseAmbience.volume = Mathf.Lerp(0f, houseTargetVolume, k);
            yield return null;
        }

        if (riverAmbience != null) { riverAmbience.Stop(); riverAmbience.volume = riverStart; }
        if (houseAmbience != null) houseAmbience.volume = houseTargetVolume;
    }

    // ---------- Reinicio ----------

    private IEnumerator RestartRoutine()
    {
        if (debugLogs) Debug.Log("[Experiencia] Llegó sin pez. Reiniciando desde el checkpoint...", this);
        onArrivedWithoutFish?.Invoke();

        if (restartDelay > 0f) yield return new WaitForSeconds(restartDelay);
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        RestartFromCheckpoint();
    }

    // Recarga la escena y el bote arranca en el checkpoint (sin diálogos del inicio)
    public void RestartFromCheckpoint()
    {
        resumeFromCheckpoint = true;
        ReloadScene();
    }

    // Recarga la escena desde el principio (por si el menú lo necesita después)
    public void RestartFromBeginning()
    {
        resumeFromCheckpoint = false;
        ReloadScene();
    }

    private void ReloadScene()
    {
        int index = SceneManager.GetActiveScene().buildIndex;
        if (index < 0)
        {
            Debug.LogError("[Experiencia] La escena no está en la lista de escenas del build. " +
                           "Agrégala en File > Build Profiles (Scene List).", this);
            return;
        }
        SceneManager.LoadScene(index);
    }
}