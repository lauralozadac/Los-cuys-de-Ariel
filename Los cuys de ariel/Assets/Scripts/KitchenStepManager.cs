using UnityEngine;
using UnityEngine.Events;

// Activa los pasos de la cocina en orden: cuando uno se completa, empieza el siguiente.
//
// Conexión:
//   ExperienceManager > On Act 2 Start -> KitchenStepManager.StartSequence
public class KitchenStepManager : MonoBehaviour
{
    [Tooltip("Los pasos del storyboard, en orden")]
    public KitchenStep[] steps;

    [Header("Narrador")]
    [Tooltip("Punto en la cocina a donde se mueve el narrador (la voz sale de ahí). " +
             "Si lo dejas vacío, el narrador se queda donde está.")]
    public Transform narratorPoint;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onAllStepsComplete;

    public int CurrentIndex { get; private set; } = -1;
    public KitchenStep CurrentStep =>
        CurrentIndex >= 0 && CurrentIndex < steps.Length ? steps[CurrentIndex] : null;

    public void StartSequence()
    {
        if (CurrentIndex >= 0) return;   // ya empezó

        // El narrador estaba en la canoa: lo traemos a la cocina
        if (narratorPoint != null && NarrationManager.Instance != null)
        {
            Transform n = NarrationManager.Instance.transform;
            n.SetParent(narratorPoint, false);
            n.localPosition = Vector3.zero;
            n.localRotation = Quaternion.identity;
        }

        if (debugLogs) Debug.Log("[Cocina] Empieza la secuencia de pasos.", this);
        BeginStep(0);
    }

    public void OnStepCompleted(KitchenStep step)
    {
        if (step != CurrentStep) return;
        BeginStep(CurrentIndex + 1);
    }

    private void BeginStep(int index)
    {
        CurrentIndex = index;
        if (index >= steps.Length)
        {
            if (debugLogs) Debug.Log("[Cocina] ¡Todos los pasos completados!", this);
            onAllStepsComplete?.Invoke();
            return;
        }
        steps[index].Begin(this);
    }
}