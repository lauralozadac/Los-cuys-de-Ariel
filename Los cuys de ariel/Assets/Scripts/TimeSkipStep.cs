using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Paso 6c: esperar a que se cocine.
// Cuando empieza su paso: fundido a negro, aparece el texto "Unos minutos más tarde...",
// mientras está en negro cambia lo que hay dentro de la olla (crudo -> cocinado),
// espera unos segundos, vuelve a aparecer la escena y se completa el paso.
//
// Va en un objeto vacío (ej. "Salto_Tiempo"). No necesita conexiones de eventos.
public class TimeSkipStep : MonoBehaviour
{
    [Tooltip("El paso de esperar (ej. Paso_6c_Esperar)")]
    public KitchenStep step;
    [Tooltip("El ScreenFader de la escena")]
    public ScreenFader fader;
    public float fadeDuration = 0.6f;
    [Tooltip("Segundos en negro con el texto")]
    public float blackDuration = 5f;

    [Header("Texto")]
    [Tooltip("Texto 3D (TextMeshPro) hijo de la cámara, unos 0.5 m al frente. Empieza apagado.")]
    public GameObject skipText;

    [Header("Cambios mientras está en negro")]
    [Tooltip("Se apagan en el negro (ej. los ingredientes crudos de la olla, el agua clara)")]
    public GameObject[] disableAtBlack;
    [Tooltip("Se encienden en el negro (ej. el pez cocinado en la olla, el caldo de color)")]
    public GameObject[] enableAtBlack;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onBlack;
    public UnityEvent onFinished;

    private bool started;

    void Start()
    {
        if (skipText != null) skipText.SetActive(false);
        foreach (var go in enableAtBlack)
            if (go != null) go.SetActive(false);

        if (debugLogs && fader == null)
            Debug.LogWarning("[Salto de tiempo] Falta asignar el ScreenFader.", this);
    }

    void Update()
    {
        if (started || step == null || !step.IsActive) return;
        Play();
    }

    // También se puede llamar desde un evento si no se usa con un paso
    public void Play()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (debugLogs) Debug.Log("[Salto de tiempo] Fundido a negro.", this);
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        foreach (var go in disableAtBlack)
            if (go != null) go.SetActive(false);
        foreach (var go in enableAtBlack)
            if (go != null) go.SetActive(true);

        if (skipText != null)
        {
            skipText.SetActive(true);
            DrawOnTop(skipText);
        }
        onBlack?.Invoke();

        yield return new WaitForSeconds(blackDuration);

        if (skipText != null) skipText.SetActive(false);
        if (fader != null) yield return fader.FadeIn(fadeDuration);

        if (debugLogs) Debug.Log("[Salto de tiempo] Listo, la comida se cocinó.", this);
        onFinished?.Invoke();
        if (step != null) step.Complete();
    }

    // El texto se dibuja después del fundido negro para que se vea encima
    private static void DrawOnTop(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.materials)
                m.renderQueue = 5000;   // 5000 es el máximo que dibuja URP; el fundido está en 4990
    }
}