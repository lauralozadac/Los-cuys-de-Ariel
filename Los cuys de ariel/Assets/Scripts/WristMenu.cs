using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Menú en la muñeca izquierda: aparece solo cuando el jugador mira su muñeca.
// Tiene un botón "Volver al menú" con confirmación (Sí / No).
//
// Estructura (hijo de LeftHandAnchor dentro del Camera Rig):
//   Menu_Muneca                <- este script
//     Canvas (World Space)     <- "Visual Root"
//       Panel_Principal        <- "Main Panel"   (botón "Volver al menú")
//       Panel_Confirmar        <- "Confirm Panel" (texto "¿Seguro?" + botones Sí / No)
//
// Conexiones (On Click de cada botón):
//   Volver al menú -> WristMenu.AskConfirm
//   Sí             -> WristMenu.ConfirmGoToMenu
//   No             -> WristMenu.Cancel
public class WristMenu : MonoBehaviour
{
    [Tooltip("La cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Transform head;
    [Tooltip("El Canvas del menú (hijo de este objeto)")]
    public GameObject visualRoot;
    public GameObject mainPanel;
    public GameObject confirmPanel;

    [Header("Cuándo aparece")]
    [Tooltip("Qué tan de frente debe mirar el jugador el menú para que aparezca (grados)")]
    public float showAngle = 40f;
    [Tooltip("Distancia máxima entre la cabeza y la muñeca (m)")]
    public float maxDistance = 0.8f;

    [Header("Volver al menú")]
    [Tooltip("Nombre EXACTO de la escena del menú")]
    public string menuSceneName = "Menu";
    public ScreenFader fader;
    public float fadeDuration = 0.6f;

    private bool visible;
    private bool loading;

    void Start()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        SetVisible(false);
    }

    void Update()
    {
        if (loading) return;
        if (head == null)
        {
            if (Camera.main == null) return;
            head = Camera.main.transform;
        }

        Vector3 toMenu = visualRoot != null ? visualRoot.transform.position - head.position
                                            : transform.position - head.position;
        Transform face = visualRoot != null ? visualRoot.transform : transform;

        // El Canvas se lee de frente cuando su eje azul (forward) apunta en la misma dirección que la mirada
        float angle = Vector3.Angle(face.forward, toMenu);
        bool close = toMenu.magnitude <= maxDistance;

        // Un poco de margen para que no parpadee en el borde
        bool shouldShow = visible ? (angle < showAngle + 10f && close) : (angle < showAngle && close);
        if (shouldShow != visible) SetVisible(shouldShow);
    }

    // ---------- Botones ----------

    public void AskConfirm()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    public void Cancel()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    public void ConfirmGoToMenu()
    {
        if (loading) return;
        loading = true;
        StartCoroutine(GoToMenu());
    }

    // ---------- Interno ----------

    private void SetVisible(bool v)
    {
        visible = v;
        if (visualRoot != null) visualRoot.SetActive(v);
        if (!v) Cancel();   // al ocultarse, vuelve a la pantalla principal
    }

    private IEnumerator GoToMenu()
    {
        if (NarrationManager.Instance != null) NarrationManager.Instance.StopAll();
        if (fader != null) yield return fader.FadeOut(fadeDuration);

        if (!Application.CanStreamedLevelBeLoaded(menuSceneName))
        {
            Debug.LogError($"[Muñeca] No encuentro la escena '{menuSceneName}'. Revisa el nombre y que esté " +
                           "en File > Build Profiles (Scene List).", this);
            loading = false;
            if (fader != null) yield return fader.FadeIn(fadeDuration);
            yield break;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}