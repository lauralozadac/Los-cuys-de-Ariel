using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Reproduce los diálogos en cola: nunca se pisan dos audios.
// Si llega un audio mientras otro suena, espera a que termine (más una pausa corta).
//
// Va en un objeto "Narrador" con un AudioSource ESPACIAL (3D), hijo de la canoa,
// ubicado donde debe "salir" la voz (ej. frente al jugador, a la altura de la cabeza).
//
// Desde cualquier evento del Inspector:  NarrationManager.Enqueue (AudioClip)
[RequireComponent(typeof(AudioSource))]
public class NarrationManager : MonoBehaviour
{
    public static NarrationManager Instance { get; private set; }

    [Tooltip("Pausa entre un audio y el siguiente cuando están en cola (s)")]
    public float gapBetweenClips = 1f;
    [Tooltip("Fuerza el AudioSource a 3D (Spatial Blend = 1) para que dependa de la cabeza")]
    public bool forceSpatial = true;
    public bool debugLogs = true;

    private AudioSource source;
    private readonly Queue<AudioClip> queue = new Queue<AudioClip>();
    private Coroutine runner;

    // Hay algo sonando o esperando en la cola
    public bool IsBusy => runner != null;

    void Awake()
    {
        Instance = this;
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        if (forceSpatial) source.spatialBlend = 1f;
    }

    // Agrega un audio a la cola. Se puede llamar desde eventos del Inspector.
    public void Enqueue(AudioClip clip)
    {
        if (clip == null) return;
        queue.Enqueue(clip);
        if (runner == null) runner = StartCoroutine(Run());
    }

    public void EnqueueMany(IEnumerable<AudioClip> clips)
    {
        foreach (var c in clips) Enqueue(c);
    }

    // Corta todo lo que suena y vacía la cola
    public void StopAll()
    {
        queue.Clear();
        if (runner != null) StopCoroutine(runner);
        runner = null;
        source.Stop();
    }

    // Para esperar desde otro script: yield return narration.WaitUntilFinished();
    public IEnumerator WaitUntilFinished()
    {
        while (IsBusy) yield return null;
    }

    private IEnumerator Run()
    {
        bool first = true;
        while (queue.Count > 0)
        {
            if (!first && gapBetweenClips > 0f)
                yield return new WaitForSeconds(gapBetweenClips);
            first = false;

            AudioClip clip = queue.Dequeue();
            source.clip = clip;
            source.Play();
            if (debugLogs) Debug.Log($"[Narrador] Sonando: {clip.name}", this);

            yield return new WaitWhile(() => source.isPlaying);
        }
        runner = null;
    }
}
