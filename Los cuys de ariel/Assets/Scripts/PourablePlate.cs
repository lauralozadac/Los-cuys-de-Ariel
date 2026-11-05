using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Paso 4b (y después los demás ingredientes): voltear un plato sobre la olla.
// Va en el plato.
//
//   - Mientras el jugador carga el plato, lo que hay encima va pegado y quieto.
//   - Cuando el plato está encima de la olla y se inclina más de "Pour Angle",
//     los pedazos se SUELTAN del plato (dejan de ser sus hijos) y caen con gravedad.
//   - Si un pedazo cae fuera de la olla, vuelve solo al plato después de unos segundos.
//   - Cuando todos los pedazos están en la olla, se completa el paso.
//
// No necesita conexiones de eventos.
public class PourablePlate : MonoBehaviour
{
    public string ingredientName = "Pescado";
    [Tooltip("Los pedazos que hay en el plato (ej. cabeza, mitad y COLA de PEZ_EN_PLATO)")]
    public Transform[] contents;
    public PotContents pot;
    [Tooltip("El paso de este ingrediente (ej. Paso_4b_PezOlla)")]
    public KitchenStep step;

    [Header("Inclinación")]
    [Tooltip("Hacia dónde mira la cara del plato en sus ejes locales (normalmente arriba = Y)")]
    public Vector3 localFaceAxis = Vector3.up;
    [Tooltip("Ángulo a partir del cual el plato está volteado (grados). 90 = de lado.")]
    public float pourAngle = 90f;

    [Header("Si se cae fuera de la olla")]
    [Tooltip("Segundos hasta que un pedazo que cayó fuera vuelve al plato")]
    public float returnDelay = 4f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onPoured;
    public UnityEvent onAllInPot;

    public bool IsDone { get; private set; }

    private readonly List<PourableItem> items = new List<PourableItem>();
    private bool poured;

    void Start()
    {
        foreach (var t in contents)
        {
            if (t == null) continue;
            PourableItem item = t.GetComponent<PourableItem>();
            if (item == null) item = t.gameObject.AddComponent<PourableItem>();
            item.Init(this, returnDelay);
            items.Add(item);
        }

        if (debugLogs)
        {
            if (pot == null) Debug.LogWarning($"[Plato {ingredientName}] Falta asignar la olla.", this);
            if (items.Count == 0) Debug.LogWarning($"[Plato {ingredientName}] No tiene pedazos en Contents.", this);
        }
    }

    void Update()
    {
        if (IsDone || poured || pot == null) return;
        if (step != null && !step.IsActive) return;

        Vector3 face = transform.TransformDirection(localFaceAxis.normalized);
        bool tilted = Vector3.Angle(face, Vector3.up) > pourAngle;
        if (!tilted || !pot.IsAbove(transform.position)) return;

        Pour();
    }

    private void Pour()
    {
        poured = true;
        foreach (var item in items)
            if (!item.IsInPot) item.Release();

        if (debugLogs) Debug.Log($"[Plato] Volteando {ingredientName} en la olla.", this);
        onPoured?.Invoke();
    }

    // ---------- Avisos de los pedazos ----------

    public void OnItemInPot(PourableItem item)
    {
        if (IsDone) return;
        foreach (var i in items)
            if (!i.IsInPot) return;   // todavía falta alguno

        IsDone = true;
        if (pot != null) pot.ReportIngredient(ingredientName);
        onAllInPot?.Invoke();
        if (step != null) step.Complete();
    }

    public void OnItemReturned(PourableItem item)
    {
        poured = false;   // se puede volver a voltear
        if (debugLogs) Debug.Log($"[Plato] Un pedazo de {ingredientName} cayó fuera y volvió al plato.", this);
    }
}