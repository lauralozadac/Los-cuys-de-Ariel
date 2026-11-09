using UnityEngine;

// Guía del río: muestra la instrucción correcta y el marcador del balde según lo que pase.
//   - En la zona de pesca, sin pez en la mano    -> "Agarra un pez con la mano"
//   - Con un pez en la mano                       -> "Ponlo en el balde" + marcador sobre el balde
//   - Con el pez en el balde                      -> "¡Muy bien!..." y se apaga el marcador
//
// Va en un objeto vacío (ej. "Guia_Rio").
// Conexiones (en cada zona de pesca, RiverTriggerZone):
//   On Boat Enter -> RiverGuide.EnterFishingZone
//   On Boat Exit  -> RiverGuide.ExitFishingZone
public class RiverGuide : MonoBehaviour
{
    public BucketController bucket;
    [Tooltip("TargetMarker sobre la snap zone del balde. Empieza apagado.")]
    public GameObject bucketMarker;

    [Header("Textos")]
    [TextArea] public string grabText = "Agarra un pez con la mano";
    [TextArea] public string carryText = "Ponlo en el balde";
    [TextArea] public string caughtText = "¡Muy bien! Disfruta el recorrido";

    private enum Mode { None, Grab, Carry, Caught }
    private Mode mode = Mode.None;
    private bool inFishingZone;
    private FishCatchable[] fish;

    void Start()
    {
        fish = FindObjectsByType<FishCatchable>(FindObjectsSortMode.None);
        if (bucketMarker != null) bucketMarker.SetActive(false);
    }

    public void EnterFishingZone() => inFishingZone = true;
    public void ExitFishingZone() => inFishingZone = false;

    void Update()
    {
        bool holding = false;
        foreach (var f in fish)
            if (f != null && f.IsHeld) { holding = true; break; }

        Mode next;
        if (bucket != null && bucket.HasFish) next = Mode.Caught;
        else if (holding) next = Mode.Carry;
        else if (inFishingZone) next = Mode.Grab;
        else next = Mode.None;

        if (next == mode) return;
        mode = next;

        if (bucketMarker != null) bucketMarker.SetActive(mode == Mode.Carry);

        InstructionPanel panel = InstructionPanel.Instance;
        if (panel == null) return;
        switch (mode)
        {
            case Mode.Grab:   panel.Show(grabText); break;
            case Mode.Carry:  panel.Show(carryText); break;
            case Mode.Caught: panel.Show(caughtText); break;
            // Mode.None: no tocamos el texto (pueden estar sonando otras instrucciones)
        }
    }
}