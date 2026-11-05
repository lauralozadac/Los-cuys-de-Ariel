using UnityEngine;
using UnityEngine.Events;

// La olla: detecta lo que cae adentro.
//
// Va en un objeto "Contenido_Olla" HIJO de la olla, con un Box Collider (Is Trigger)
// que cubra el interior de la olla. Los pedazos que entran se quedan quietos adentro
// y pasan a ser hijos de la olla (ya no se mueven con el plato).
//
// La olla necesita además colliders normales en el fondo y las paredes para que
// los pedazos no la atraviesen.
[RequireComponent(typeof(Collider))]
public class PotContents : MonoBehaviour
{
    [Tooltip("Centro de la boca de la olla. Si lo dejas vacío, usa este objeto.")]
    public Transform opening;
    [Tooltip("Qué tan cerca de la boca debe estar el plato (horizontalmente) para poder voltearlo (m)")]
    public float pourRadius = 0.35f;
    [Tooltip("Tiempo que se deja caer un pedazo antes de dejarlo quieto en la olla (s)")]
    public float settleTime = 0.4f;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    [Tooltip("Recibe el nombre del ingrediente que se completó en la olla")]
    public UnityEvent<string> onIngredientAdded;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Awake()
    {
        if (opening == null) opening = transform;
    }

    // ¿Este punto está encima de la boca de la olla?
    public bool IsAbove(Vector3 worldPoint)
    {
        Vector3 d = worldPoint - opening.position;
        float height = d.y;
        d.y = 0f;
        return d.magnitude <= pourRadius && height > -0.05f;
    }

    void OnTriggerEnter(Collider other)
    {
        PourableItem item = other.GetComponentInParent<PourableItem>();
        if (item != null) item.EnterPot(this);
    }

    public void ReportIngredient(string ingredientName)
    {
        if (debugLogs) Debug.Log($"[Olla] ¡{ingredientName} en la olla!", this);
        onIngredientAdded?.Invoke(ingredientName);
    }
}