using System.Collections;
using UnityEngine;

// Fundido a negro para VR: pone un plano delante de la cámara del jugador y cambia
// su transparencia. Lo usa el ExperienceManager al teletransportar y al reiniciar.
//
// Necesita un material transparente (ver "Fade Material" abajo).
public class ScreenFader : MonoBehaviour
{
    [Tooltip("Cámara del jugador (CenterEyeAnchor). Si lo dejas vacío, usa Camera.main.")]
    public Camera targetCamera;
    [Tooltip("Material URP > Unlit, Surface Type: Transparent, Render Face: Both")]
    public Material fadeMaterial;
    public Color fadeColor = Color.black;

    [Header("Al iniciar")]
    [Tooltip("La escena empieza en negro y aparece suavemente")]
    public bool fadeInOnStart = true;
    public float startFadeDuration = 1f;

    private Renderer quadRenderer;
    private Material mat;
    private float currentAlpha;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null || fadeMaterial == null)
        {
            Debug.LogWarning("[Fader] Falta la cámara o el material. El fundido no funcionará.", this);
            return;
        }

        BuildQuad();
        SetAlpha(fadeInOnStart ? 1f : 0f);
        if (fadeInOnStart) StartCoroutine(FadeIn(startFadeDuration));
    }

    public IEnumerator FadeOut(float duration) => FadeTo(1f, duration);
    public IEnumerator FadeIn(float duration) => FadeTo(0f, duration);

    private IEnumerator FadeTo(float target, float duration)
    {
        if (mat == null) yield break;

        float start = currentAlpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(start, target, t / duration));
            yield return null;
        }
        SetAlpha(target);
    }

    private void BuildQuad()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "FadeQuad";
        Destroy(quad.GetComponent<Collider>());

        quad.transform.SetParent(targetCamera.transform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, targetCamera.nearClipPlane + 0.01f);
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = Vector3.one * 2f;

        quadRenderer = quad.GetComponent<Renderer>();
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;

        mat = new Material(fadeMaterial) { renderQueue = 5000 };  // se dibuja encima de todo
        quadRenderer.sharedMaterial = mat;
    }

    private void SetAlpha(float a)
    {
        currentAlpha = a;
        if (mat == null) return;

        Color c = fadeColor;
        c.a = a;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
        quadRenderer.enabled = a > 0.001f;
    }
}