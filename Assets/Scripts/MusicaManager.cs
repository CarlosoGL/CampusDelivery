using UnityEngine;
using System.Collections;

public class MusicaManager : MonoBehaviour
{
    public static MusicaManager Instancia { get; private set; }

    // ─────────────────────────────────────────
    //  CLIPS — arrastra tus archivos de audio aquí en el Inspector
    // ─────────────────────────────────────────
    [Header("Clips de música")]
    public AudioClip musicaAmbiente;
    public AudioClip musicaPersecucion;

    [Header("Crossfade")]
    [Tooltip("Segundos que tarda en hacer el crossfade")]
    public float duracionFade = 1.5f;

    [Header("Volúmenes máximos")]
    [Range(0f, 1f)] public float volumenAmbiente    = 0.6f;
    [Range(0f, 1f)] public float volumenPersecucion = 0.9f;

    // ─────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────
    private AudioSource sourceAmbiente;
    private AudioSource sourcePersecucion;
    private bool        enPersecucion = false;
    private Coroutine   fadeActivo;

    // ─────────────────────────────────────────
    //  SINGLETON
    // ─────────────────────────────────────────
    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
        DontDestroyOnLoad(gameObject);

        // Crea dos AudioSources en el mismo GameObject
        sourceAmbiente     = gameObject.AddComponent<AudioSource>();
        sourcePersecucion  = gameObject.AddComponent<AudioSource>();

        sourceAmbiente.loop        = true;
        sourcePersecucion.loop     = true;
        sourceAmbiente.playOnAwake = false;
        sourcePersecucion.playOnAwake = false;

        sourceAmbiente.volume    = volumenAmbiente;
        sourcePersecucion.volume = 0f;
    }

    void Start()
    {
        // Arranca la música de ambiente desde el inicio
        if (musicaAmbiente != null)
        {
            sourceAmbiente.clip = musicaAmbiente;
            sourceAmbiente.Play();
        }

        if (musicaPersecucion != null)
        {
            sourcePersecucion.clip = musicaPersecucion;
            sourcePersecucion.volume = 0f;
            sourcePersecucion.Play(); // corre en silencio, lista para subir
        }
    }

    // ─────────────────────────────────────────
    //  API PÚBLICA — el NPCController llama esto
    // ─────────────────────────────────────────
    public void IniciarPersecucion()
    {
        if (enPersecucion) return;
        enPersecucion = true;

        if (fadeActivo != null) StopCoroutine(fadeActivo);
        fadeActivo = StartCoroutine(Crossfade(
            sourceAmbiente,    volumenAmbiente,    0f,
            sourcePersecucion, 0f,                 volumenPersecucion));
    }

    public void TerminarPersecucion()
    {
        if (!enPersecucion) return;
        enPersecucion = false;

        if (fadeActivo != null) StopCoroutine(fadeActivo);
        fadeActivo = StartCoroutine(Crossfade(
            sourcePersecucion, volumenPersecucion, 0f,
            sourceAmbiente,    0f,                 volumenAmbiente));
    }

    // ─────────────────────────────────────────
    //  CROSSFADE
    // ─────────────────────────────────────────
    IEnumerator Crossfade(
        AudioSource sourceBaja, float volInicioBaja, float volFinBaja,
        AudioSource sourceSube, float volInicioSube, float volFinSube)
    {
        float t = 0f;
        while (t < duracionFade)
        {
            t += Time.deltaTime;
            float progreso = t / duracionFade;

            sourceBaja.volume = Mathf.Lerp(volInicioBaja, volFinBaja, progreso);
            sourceSube.volume = Mathf.Lerp(volInicioSube, volFinSube, progreso);

            yield return null;
        }

        sourceBaja.volume = volFinBaja;
        sourceSube.volume = volFinSube;
    }
}
