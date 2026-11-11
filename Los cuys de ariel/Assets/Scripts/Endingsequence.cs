using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Final de la experiencia:
//   pescado servido -> espera 3 s -> fundido a negro -> aparece el logo
//   -> 10 s -> vuelve al menú de inicio.
//
// Conexión:
//   Cocina_Pasos (KitchenStepManager) > On All Steps Complete -> EndingSequence.Play
public class EndingSequence : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos entre servir el pescado y el fundido a negro")]
    public float delayBeforeFade = 3f;
    [Tooltip("Espera además a que termine de sonar el narrador (ej. el último audio de la mamá)")]
    public bool waitForNarration = true;
    public float fadeDuration = 1f;
    [Tooltip("Segundos que se muestra el logo antes de volver al menú")]
    public float logoDuration = 10f;
    [Tooltip("Tiempo en que aparece el logo suavemente (si tiene CanvasGroup)")]
    public float logoFadeIn = 1f;

    [Header("Referencias")]
    public ScreenFader fader;
    [Tooltip("El logo: un Quad con material transparente (o un Canvas) hijo del CenterEyeAnchor, " +
             "unos 1.5 m al frente. Empieza apagado.")]
    public GameObject logo;

    [Header("Volver al menú")]
    public string menuSceneName = "Menu";
    [Tooltip("Sonidos que se apagan con el fundido (ambientes, música...). Opcional.")]
    public AudioSource[] fadeOutAudio;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    private bool started;

    void Start()
    {
        if (logo != null) logo.SetActive(false);
        if (debugLogs && fader == null)
            Debug.LogWarning("[Final] Falta asignar el ScreenFader.", this);
    }

    public void Play()
    {
        if (started) return;
        started = true;
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (debugLogs) Debug.Log("[Final] Pescado servido. Empieza el final.", this);

        yield return new WaitForSeconds(delayBeforeFade);

        if (waitForNarration && NarrationManager.Instance != null)
            yield return NarrationManager.Instance.WaitUntilFinished();

        StartCoroutine(FadeAudio());
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        if (logo != null)
        {
            logo.SetActive(true);
            DrawOnTop(logo);
            yield return FadeInLogo();
        }
        if (debugLogs) Debug.Log("[Final] Logo en pantalla.", this);

        yield return new WaitForSeconds(logoDuration);

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"[Final] No encuentro la escena '{menuSceneName}'. Revisa el nombre y que esté " +
                           "en File > Build Profiles (Scene List).", this);
            yield break;
        }
        SceneManager.LoadScene(menuSceneName);
    }

    private IEnumerator FadeAudio()
    {
        if (fadeOutAudio == null || fadeOutAudio.Length == 0) yield break;

        float[] start = new float[fadeOutAudio.Length];
        for (int i = 0; i < fadeOutAudio.Length; i++)
            start[i] = fadeOutAudio[i] != null ? fadeOutAudio[i].volume : 0f;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            for (int i = 0; i < fadeOutAudio.Length; i++)
                if (fadeOutAudio[i] != null) fadeOutAudio[i].volume = Mathf.Lerp(start[i], 0f, k);
            yield return null;
        }
    }

    private IEnumerator FadeInLogo()
    {
        CanvasGroup group = logo.GetComponentInChildren<CanvasGroup>();
        if (group == null || logoFadeIn <= 0f) yield break;

        float t = 0f;
        group.alpha = 0f;
        while (t < logoFadeIn)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / logoFadeIn);
            yield return null;
        }
        group.alpha = 1f;
    }

    // El logo se dibuja después del fundido negro para que se vea encima
    private static void DrawOnTop(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.materials)
                m.renderQueue = 5000;   // 5000 es el máximo que dibuja URP; el fundido está en 4990

        foreach (var g in go.GetComponentsInChildren<Graphic>(true))
        {
            var m = new Material(g.materialForRendering) { renderQueue = 5000 };
            g.material = m;
        }
    }
}