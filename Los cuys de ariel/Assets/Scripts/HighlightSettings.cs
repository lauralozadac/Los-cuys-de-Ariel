using UnityEngine;

// Configuración general del resaltado (color, contorno, velocidad del pulso).
// Pon UNO en la escena (por ejemplo en el ExperienceManager).
// Todos los Highlighter usan estos valores, incluso los que se agregan solos desde los pasos.
public class HighlightSettings : MonoBehaviour
{
    public static HighlightSettings Instance { get; private set; }

    public Color color = new Color(1f, 0.8f, 0.2f);
    [Tooltip("Velocidad del pulso")]
    public float pulseSpeed = 3f;
    [Tooltip("Brillo máximo del pulso (emisión)")]
    public float maxEmission = 1.2f;

    [Header("Contorno")]
    public bool useOutline = true;
    [Tooltip("Material con el shader Custom/HighlightOutline")]
    public Material outlineMaterial;
    [Tooltip("Grosor del contorno (m)")]
    public float outlineWidth = 0.004f;

    void Awake()
    {
        Instance = this;
        if (useOutline && outlineMaterial == null)
            Debug.LogWarning("[Resaltado] Falta el material del contorno. Solo se verá el pulso de brillo.", this);
    }
}