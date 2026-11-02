using UnityEngine;

// Va en el objeto raíz del cuchillo (el que tiene el Grabbable).
// Sabe si el cuchillo está en la mano y a qué velocidad se mueve la hoja.
//
// Eventos del agarre (Pointable Unity Event Wrapper apuntando al Grabbable del cuchillo):
//   When Select   -> Knife.OnGrab
//   When Unselect -> Knife.OnRelease
public class Knife : MonoBehaviour
{
    [Tooltip("Punto de la hoja para medir la velocidad del corte. Si lo dejas vacío, usa este objeto.")]
    public Transform blade;

    public bool IsHeld { get; private set; }
    public Vector3 Velocity { get; private set; }

    private Vector3 lastPos;

    void Start()
    {
        if (blade == null) blade = transform;
        lastPos = blade.position;
    }

    void Update()
    {
        Vector3 p = blade.position;
        if (Time.deltaTime > 0f)
        {
            Vector3 raw = (p - lastPos) / Time.deltaTime;
            Velocity = Vector3.Lerp(Velocity, raw, 0.5f);   // suaviza un poco
        }
        lastPos = p;
    }

    public void OnGrab() => IsHeld = true;
    public void OnRelease() => IsHeld = false;
}