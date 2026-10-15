using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Controla el flujo entre el acto 1 (río) y el acto 2 (cocina).
//
//   Fin del recorrido del bote:
//     - Con pez en el balde -> fundido a negro, el jugador aparece dentro de la casa
//                              frente al mesón, se activa el balde de la casa,
//                              cambia el sonido ambiente y empieza el acto 2.
//     - Sin pez en el balde -> se reinicia la experiencia.
//
// Conexión:
//   BoatController > On Journey Complete -> ExperienceManager.OnJourneyComplete
public class ExperienceManager : MonoBehaviour
{
    [Header("Referencias")]
    public BucketController bucket;
    [Tooltip("El rig del jugador ([BuildingBlock] Camera Rig)")]
    public Transform playerRig;
    [Tooltip("La cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Camera playerCamera;

    [Header("Llegada a la casa")]
    [Tooltip("Objeto vacío dentro de la casa: posición en el piso y hacia dónde mira el jugador " +
             "(la flecha azul apunta al mesón)")]
    public Transform houseSpawnPoint;
    [Tooltip("Objetos que se apagan al inicio y se encienden al llegar (ej. el balde de la casa con su pez)")]
    public GameObject[] enableOnArrival;
    [Tooltip("Objetos que se apagan al llegar (ej. cosas del río que ya no se necesitan)")]
    public GameObject[] disableOnArrival;

    [Header("Transición")]
    [Tooltip("Opcional. Si no hay fader, el cambio es instantáneo.")]
    public ScreenFader fader;
    public float fadeDuration = 0.6f;
    [Tooltip("Tiempo en negro antes de volver a aparecer (s)")]
    public float blackHoldTime = 0.3f;

    [Header("Sonido ambiente")]
    public AudioSource riverAmbience;
    public AudioSource houseAmbience;
    [Tooltip("Duración del cambio de sonido río -> casa (s)")]
    public float ambienceCrossfade = 2f;

    [Header("Reinicio")]
    [Tooltip("Segundos antes de reiniciar cuando no hay pez. 0 = de una (para pruebas).")]
    public float restartDelay = 0f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando el jugador ya está en la casa: empieza el acto 2")]
    public UnityEvent onAct2Start;
    [Tooltip("Para el sonido y la UI de reinicio más adelante")]
    public UnityEvent onArrivedWithoutFish;

    private bool busy;
    private float houseTargetVolume = 1f;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        foreach (var go in enableOnArrival)
            if (go != null) go.SetActive(false);

        // Sonido del río desde el inicio, el de la casa en silencio
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

        if (debugLogs)
        {
            if (bucket == null) Debug.LogWarning("[Experiencia] Falta asignar el balde del bote.", this);
            if (playerRig == null) Debug.LogWarning("[Experiencia] Falta asignar el rig del jugador.", this);
            if (houseSpawnPoint == null) Debug.LogWarning("[Experiencia] Falta asignar el punto de aparición en la casa.", this);
            if (playerCamera == null) Debug.LogWarning("[Experiencia] No encontré la cámara del jugador.", this);
        }
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
        if (debugLogs) Debug.Log("[Experiencia] Llegó con el pez. Pasando a la casa...", this);

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

    // Mueve el rig para que la CABEZA del jugador quede sobre el punto de aparición
    // mirando hacia donde apunta la flecha azul del punto.
    private void TeleportPlayer(Transform target)
    {
        if (playerRig == null || target == null) return;

        playerRig.SetParent(null, true);   // deja de moverse con el bote

        if (playerCamera == null)
        {
            playerRig.SetPositionAndRotation(target.position, Quaternion.Euler(0f, target.eulerAngles.y, 0f));
            return;
        }

        Transform cam = playerCamera.transform;

        // 1. Girar el rig para que la mirada coincida con la del punto
        float yawDelta = Mathf.DeltaAngle(cam.eulerAngles.y, target.eulerAngles.y);
        playerRig.RotateAround(cam.position, Vector3.up, yawDelta);

        // 2. Mover el rig para que la cabeza quede sobre el punto (el piso del rig a la altura del punto)
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
        if (debugLogs) Debug.Log("[Experiencia] Llegó sin pez. Reiniciando...", this);
        onArrivedWithoutFish?.Invoke();

        if (restartDelay > 0f) yield return new WaitForSeconds(restartDelay);
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        RestartExperience();
    }

    public void RestartExperience()
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