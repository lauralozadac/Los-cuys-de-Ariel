using System.Collections.Generic;
using UnityEngine;

// Resalta un objeto: su material pulsa con un brillo suave y aparece un contorno.
// No hace falta ponerlo a mano: los pasos lo agregan solos a los objetos de sus listas.
// También se puede poner a mano y usar StartHighlight / StopHighlight desde eventos.
// Los valores (color, contorno) salen de HighlightSettings.
public class Highlighter : MonoBehaviour
{
    [Tooltip("Resaltar apenas empieza la escena")]
    public bool highlightOnStart = false;

    public bool IsHighlighted { get; private set; }

    private Renderer[] renderers;
    private readonly List<Material> mats = new List<Material>();
    private readonly List<Color> originalEmission = new List<Color>();
    private readonly List<bool> hadEmissionKeyword = new List<bool>();
    private Material outlineInstance;

    // Agrega (si no lo tiene) y devuelve el Highlighter de un objeto
    public static Highlighter Get(GameObject go)
    {
        if (go == null) return null;
        Highlighter h = go.GetComponent<Highlighter>();
        return h != null ? h : go.AddComponent<Highlighter>();
    }

    void Awake()
    {
        var list = new List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer || r is SkinnedMeshRenderer) list.Add(r);
        renderers = list.ToArray();
    }

    void Start()
    {
        if (highlightOnStart) StartHighlight();
    }

    void OnDisable()
    {
        if (IsHighlighted) StopHighlight();
    }

    void Update()
    {
        if (!IsHighlighted) return;

        HighlightSettings s = HighlightSettings.Instance;
        Color color = s != null ? s.color : new Color(1f, 0.8f, 0.2f);
        float speed = s != null ? s.pulseSpeed : 3f;
        float maxE = s != null ? s.maxEmission : 1.2f;

        float k = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;   // 0..1
        Color emission = color * Mathf.Lerp(0.05f, maxE, k);

        foreach (var m in mats)
            if (m != null && m.HasProperty("_EmissionColor"))
                m.SetColor("_EmissionColor", emission);

        if (outlineInstance != null)
        {
            Color c = color;
            c.a = Mathf.Lerp(0.5f, 1f, k);
            outlineInstance.SetColor("_Color", c);
        }
    }

    public void StartHighlight()
    {
        if (IsHighlighted) return;
        IsHighlighted = true;

        mats.Clear();
        originalEmission.Clear();
        hadEmissionKeyword.Clear();

        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var m in r.materials)   // copias propias de este objeto
            {
                mats.Add(m);
                bool hasEm = m.HasProperty("_EmissionColor");
                originalEmission.Add(hasEm ? m.GetColor("_EmissionColor") : Color.black);
                hadEmissionKeyword.Add(m.IsKeywordEnabled("_EMISSION"));
                if (hasEm) m.EnableKeyword("_EMISSION");
            }
        }

        AddOutline();
    }

    public void StopHighlight()
    {
        if (!IsHighlighted) return;
        IsHighlighted = false;

        for (int i = 0; i < mats.Count; i++)
        {
            Material m = mats[i];
            if (m == null) continue;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", originalEmission[i]);
            if (!hadEmissionKeyword[i]) m.DisableKeyword("_EMISSION");
        }

        RemoveOutline();
    }

    // ---------- Contorno ----------

    private void AddOutline()
    {
        HighlightSettings s = HighlightSettings.Instance;
        if (s == null || !s.useOutline || s.outlineMaterial == null) return;

        outlineInstance = new Material(s.outlineMaterial);
        outlineInstance.SetFloat("_Width", s.outlineWidth);

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var list = new List<Material>(r.sharedMaterials) { outlineInstance };
            r.sharedMaterials = list.ToArray();
        }
    }

    private void RemoveOutline()
    {
        if (outlineInstance == null) return;

        foreach (var r in renderers)
        {
            if (r == null) continue;
            var list = new List<Material>(r.sharedMaterials);
            list.RemoveAll(m => m == outlineInstance);
            r.sharedMaterials = list.ToArray();
        }

        Destroy(outlineInstance);
        outlineInstance = null;
    }
}