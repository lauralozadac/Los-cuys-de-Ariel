using TMPro;
using UnityEngine;

// Panel de texto que flota frente al jugador y muestra la instrucción del paso actual.
// Sigue la cabeza con suavidad: solo se reacomoda cuando el jugador gira bastante.
//
// Estructura en la Hierarchy (en la raíz, NO hijo de la cámara):
//   Panel_Instrucciones   <- este script
//     Canvas (World Space) <- "Visual Root"
//       Image (fondo)
//       Text (TMP)          <- "Text"
//
// Desde cualquier evento del Inspector: InstructionPanel.Show (string)
public class InstructionPanel : MonoBehaviour
{
    public static InstructionPanel Instance { get; private set; }

    [Tooltip("El texto TextMeshPro del panel")]
    public TMP_Text text;
    [Tooltip("El Canvas (hijo de este objeto). Se oculta cuando no hay texto.")]
    public GameObject visualRoot;
    [Tooltip("La cámara del jugador. Si lo dejas vacío, usa Camera.main.")]
    public Transform head;

    [Header("Posición")]
    [Tooltip("Distancia al frente del jugador (m)")]
    public float distance = 1f;
    [Tooltip("Altura respecto a los ojos (m). Negativo = más abajo.")]
    public float heightOffset = -0.3f;
    [Tooltip("Qué tan rápido se reacomoda")]
    public float followSpeed = 4f;
    [Tooltip("Cuántos grados debe girar la cabeza para que el panel la siga")]
    public float recenterAngle = 30f;

    [Header("Comportamiento")]
    public bool hideWhenEmpty = true;

    private bool needsRecenter = true;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (head == null && Camera.main != null) head = Camera.main.transform;
        Show(text != null ? text.text : "");
        SnapToTarget();
    }

    void LateUpdate()
    {
        if (head == null)
        {
            if (Camera.main == null) return;
            head = Camera.main.transform;
        }

        Vector3 forward = FlatForward();
        Vector3 desired = head.position + forward * distance + Vector3.up * heightOffset;

        // Si el jugador se teletransportó (salto grande), aparece de una
        if ((transform.position - desired).sqrMagnitude > 9f)
        {
            SnapToTarget();
            return;
        }

        Vector3 toPanel = transform.position - head.position;
        toPanel.y = 0f;
        if (Vector3.Angle(forward, toPanel) > recenterAngle || Mathf.Abs(toPanel.magnitude - distance) > 0.3f)
            needsRecenter = true;

        if (needsRecenter)
        {
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            if ((transform.position - desired).sqrMagnitude < 0.0004f) needsRecenter = false;
        }
        else
        {
            // La altura siempre sigue a la cabeza (por si se agacha o se para)
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, desired.y, 1f - Mathf.Exp(-followSpeed * Time.deltaTime));
            transform.position = p;
        }

        FaceHead();
    }

    // ---------- API ----------

    public void Show(string message)
    {
        if (text != null) text.text = message;
        bool empty = string.IsNullOrWhiteSpace(message);
        if (visualRoot != null) visualRoot.SetActive(!(hideWhenEmpty && empty));
        needsRecenter = true;   // al cambiar el texto, se pone frente al jugador
    }

    public void Clear() => Show("");

    // ---------- Interno ----------

    private Vector3 FlatForward()
    {
        Vector3 f = head.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 0.001f) f = head.up;
        f.y = 0f;
        return f.normalized;
    }

    private void SnapToTarget()
    {
        if (head == null) return;
        transform.position = head.position + FlatForward() * distance + Vector3.up * heightOffset;
        FaceHead();
        needsRecenter = false;
    }

    private void FaceHead()
    {
        Vector3 look = transform.position - head.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(look, Vector3.up);   // el Canvas se lee de frente
    }
}