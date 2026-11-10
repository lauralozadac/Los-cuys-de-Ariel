using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menú de inicio (escena "Menu").
// El botón "Iniciar" llama a StartExperience: fundido a negro y carga la escena de la experiencia.
//
// Conexión: Button (Iniciar) > On Click -> MainMenu.StartExperience
public class MainMenu : MonoBehaviour
{
    [Tooltip("Nombre EXACTO de la escena de la experiencia (ej. Terreno)")]
    public string experienceSceneName = "Terreno";

    [Header("Transición (opcional)")]
    public ScreenFader fader;
    public float fadeDuration = 0.6f;

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip clickSound;

    private bool loading;

    public void StartExperience()
    {
        if (loading) return;
        loading = true;
        if (audioSource != null && clickSound != null) audioSource.PlayOneShot(clickSound);
        StartCoroutine(LoadExperience());
    }

    // Por si después quieren un botón de "Salir"
    public void QuitApp()
    {
        Application.Quit();
    }

    private IEnumerator LoadExperience()
    {
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        if (!Application.CanStreamedLevelBeLoaded(experienceSceneName))
        {
            Debug.LogError($"[Menú] No encuentro la escena '{experienceSceneName}'. Revisa el nombre y que esté " +
                           "en File > Build Profiles (Scene List).", this);
            loading = false;
            if (fader != null) yield return fader.FadeIn(fadeDuration);
            yield break;
        }

        SceneManager.LoadScene(experienceSceneName);
    }
}