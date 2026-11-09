using UnityEngine;

// Marcador de destino: un aro brillante con una flecha encima que sube y baja.
// Se pone donde hay que DEJAR algo (sobre una snap zone, la olla, la tabla...).
// Se dibuja solo con líneas: no necesita modelo, solo un material.
//
// Los pasos de la cocina lo prenden y apagan solos (lista "Target Markers").
public class TargetMarker : MonoBehaviour
{
    [Tooltip("Material URP > Particles > Unlit, transparente y Additive")]
    public Material lineMaterial;
    public Color color = new Color(1f, 0.8f, 0.2f);

    [Header("Aro")]
    public float radius = 0.12f;
    public float lineWidth = 0.008f;
    public int segments = 40;

    [Header("Flecha")]
    public bool showArrow = true;
    [Tooltip("Altura de la flecha sobre el aro (m)")]
    public float arrowHeight = 0.15f;
    public float arrowSize = 0.05f;

    [Header("Animación")]
    public float bobHeight = 0.03f;
    public float bobSpeed = 2.5f;
    public float pulseSpeed = 3f;

    private LineRenderer ring;
    private LineRenderer arrow;
    private Transform arrowPivot;
    private Vector3 baseLocalPos;
    private bool built;

    void Awake()
    {
        Build();
    }

    void OnEnable()
    {
        baseLocalPos = transform.localPosition;
    }

    void OnDisable()
    {
        transform.localPosition = baseLocalPos;
    }

    void Update()
    {
        float t = Time.time;
        transform.localPosition = baseLocalPos + Vector3.up * Mathf.Sin(t * bobSpeed) * bobHeight;

        // Pulso de brillo
        float k = (Mathf.Sin(t * pulseSpeed) + 1f) * 0.5f;
        Color c = color;
        c.a = Mathf.Lerp(0.4f, 1f, k);
        if (ring != null) ring.startColor = ring.endColor = c;
        if (arrow != null) arrow.startColor = arrow.endColor = c;

        // La flecha siempre mira al jugador
        if (arrowPivot != null && Camera.main != null)
        {
            Vector3 look = arrowPivot.position - Camera.main.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f) arrowPivot.rotation = Quaternion.LookRotation(look, Vector3.up);
        }
    }

    private void Build()
    {
        if (built) return;
        built = true;

        if (lineMaterial == null)
            Debug.LogWarning($"[Marcador {name}] Falta el material de las líneas.", this);

        // Aro
        ring = CreateLine("Aro", transform);
        ring.loop = true;
        ring.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        // Flecha (una "V" apuntando hacia abajo)
        if (showArrow)
        {
            arrowPivot = new GameObject("Flecha").transform;
            arrowPivot.SetParent(transform, false);
            arrowPivot.localPosition = Vector3.up * arrowHeight;

            arrow = CreateLine("Linea", arrowPivot);
            arrow.positionCount = 3;
            arrow.SetPosition(0, new Vector3(-arrowSize, arrowSize, 0f));
            arrow.SetPosition(1, Vector3.zero);
            arrow.SetPosition(2, new Vector3(arrowSize, arrowSize, 0f));
        }
    }

    private LineRenderer CreateLine(string objName, Transform parent)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.widthMultiplier = lineWidth;
        lr.material = lineMaterial;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        return lr;
    }
}