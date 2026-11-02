using UnityEngine;
using UnityEngine.Events;

// Paso 2c: llevar el pez cortado al plato.
//
// El jugador lleva el pez cortado completo (cabeza, mitad y cola juntos) y lo pone
// en la snap zone del plato. En ese momento el pez cortado desaparece y aparece una
// copia con los pedazos ya acomodados dentro del plato.
//
// Va en el plato (PLATO KOE KOE).
// Conexión (Interactable Unity Event Wrapper de la snap zone del plato):
//   When Select -> PlateArranger.OnFishPlaced
public class PlateArranger : MonoBehaviour
{
    [Tooltip("El pez cortado que el jugador lleva en la mano")]
    public GameObject cutFish;
    [Tooltip("Copia de los pedazos acomodados dentro del plato. Empieza apagada.")]
    public GameObject fishOnPlate;
    public KitchenStep plateStep;   // 2c

    [Header("Sonido (opcional)")]
    public AudioSource audioSource;
    public AudioClip placeSound;

    [Header("Diagnóstico")]
    public bool debugLogs = true;

    [Header("Eventos")]
    public UnityEvent onFishOnPlate;

    public bool IsDone { get; private set; }

    void Start()
    {
        if (fishOnPlate != null) fishOnPlate.SetActive(false);
    }

    public void OnFishPlaced()
    {
        if (IsDone) return;
        IsDone = true;

        if (cutFish != null) cutFish.SetActive(false);
        if (fishOnPlate != null) fishOnPlate.SetActive(true);

        if (audioSource != null && placeSound != null) audioSource.PlayOneShot(placeSound);
        if (debugLogs) Debug.Log("[Plato] ¡Pez en el plato!", this);

        onFishOnPlate?.Invoke();
        if (plateStep != null) plateStep.Complete();
    }
}