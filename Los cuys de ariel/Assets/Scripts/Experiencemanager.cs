using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// Controla el flujo de la primera parte de la experiencia:
//   Fin del recorrido del bote:
//     - Con pez en el balde  -> el jugador sale del bote, puede agarrar el balde
//                               y se activan los teletransportes.
//     - Sin pez en el balde  -> se reinicia la experiencia.
//   Entrar a la cocina      -> termina la primera parte.
//
// Conexiones:
//   BoatController  > On Journey Complete -> ExperienceManager.OnJourneyComplete
//   PlayerEnterZone > On Player Enter      -> ExperienceManager.OnPlayerEnteredKitchen
public class ExperienceManager : MonoBehaviour
{
    [Header("Referencias")]
    public BucketController bucket;
    [Tooltip("El rig del jugador ([BuildingBlock] Camera Rig). Se separa del bote al llegar.")]
    public Transform playerRig;

    [Header("Llegada a la bahía")]
    [Tooltip("Objetos que se apagan durante el viaje y se encienden al llegar " +
             "(Teleport Hotspots, Teleport NavMesh...)")]
    public GameObject[] enableOnArrival;

    [Header("Reinicio")]
    [Tooltip("Segundos antes de reiniciar cuando no hay pez. 0 = de una (para pruebas).")]
    public float restartDelay = 0f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onArrivedWithFish;
    [Tooltip("Para el sonido y la UI de reinicio más adelante")]
    public UnityEvent onArrivedWithoutFish;
    public UnityEvent onPart1Complete;

    private bool part1Done;

    void Start()
    {
        foreach (var go in enableOnArrival)
            if (go != null) go.SetActive(false);

        if (debugLogs)
        {
            if (bucket == null) Debug.LogWarning("[Experiencia] Falta asignar el balde.", this);
            if (playerRig == null) Debug.LogWarning("[Experiencia] Falta asignar el rig del jugador.", this);
        }
    }

    // ---------- Fin del recorrido ----------

    public void OnJourneyComplete()
    {
        if (bucket != null && bucket.HasFish) ArriveWithFish();
        else ArriveWithoutFish();
    }

    private void ArriveWithFish()
    {
        if (debugLogs) Debug.Log("[Experiencia] Llegó con el pez. El jugador puede bajar del bote.", this);

        if (playerRig != null) playerRig.SetParent(null, true);  // deja de moverse con el bote
        if (bucket != null) bucket.EnableGrab();
        foreach (var go in enableOnArrival)
            if (go != null) go.SetActive(true);

        onArrivedWithFish?.Invoke();
    }

    private void ArriveWithoutFish()
    {
        if (debugLogs) Debug.Log("[Experiencia] Llegó sin pez. Reiniciando...", this);
        onArrivedWithoutFish?.Invoke();
        StartCoroutine(RestartAfter(restartDelay));
    }

    // ---------- Cocina ----------

    public void OnPlayerEnteredKitchen()
    {
        if (part1Done) return;
        part1Done = true;
        if (debugLogs) Debug.Log("[Experiencia] El jugador entró a la cocina. Fin de la primera parte.", this);
        onPart1Complete?.Invoke();
    }

    // ---------- Reinicio ----------

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

    private IEnumerator RestartAfter(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        RestartExperience();
    }
}