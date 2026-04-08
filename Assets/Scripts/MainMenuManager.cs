using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Referencias")]
    public Image imagenPortada;
    public TextMeshProUGUI textoTocar;
    public Image panelFade;

    [Header("Audio")]
    public AudioClip musicaMenu;
    [Range(0f, 1f)] public float volumenMenu = 0.25f;

    [Header("Escena")]
    public string nombreEscena = "SampleScene";

    private AudioSource audioSource;
    private bool puedeInteractuar = false;
    private float tiempoParpadeo = 0.8f;

    void Start()
    {
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = musicaMenu;
        audioSource.loop = true;
        audioSource.volume = 0f;
        audioSource.Play();

        // Ocultar texto al inicio
        if (textoTocar != null)
            textoTocar.alpha = 0f;

        // Iniciar fade de entrada
        StartCoroutine(FadeEntrada());
    }

    void Update()
    {
        if (!puedeInteractuar) return;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            puedeInteractuar = false;
            StopAllCoroutines();
            StartCoroutine(FadeSalida());
        }
    }

    IEnumerator FadeEntrada()
    {
        float duracion = 1.5f;
        float tiempo = 0f;

        // Fade: negro -> portada visible, subir volumen
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;

            // Bajar alpha del panel negro
            if (panelFade != null)
            {
                Color c = panelFade.color;
                c.a = 1f - t;
                panelFade.color = c;
            }

            // Subir volumen
            audioSource.volume = Mathf.Lerp(0f, volumenMenu, t);

            yield return null;
        }

        // Panel completamente transparente
        if (panelFade != null)
        {
            Color c = panelFade.color;
            c.a = 0f;
            panelFade.color = c;
        }

        audioSource.volume = volumenMenu;

        // Mostrar texto parpadeando
        puedeInteractuar = true;
        StartCoroutine(ParpadeaTexto());
    }

    IEnumerator ParpadeaTexto()
    {
        while (true)
        {
            // Aparecer
            float t = 0f;
            while (t < tiempoParpadeo)
            {
                t += Time.deltaTime;
                if (textoTocar != null)
                    textoTocar.alpha = Mathf.Lerp(0f, 1f, t / tiempoParpadeo);
                yield return null;
            }

            // Desaparecer
            t = 0f;
            while (t < tiempoParpadeo)
            {
                t += Time.deltaTime;
                if (textoTocar != null)
                    textoTocar.alpha = Mathf.Lerp(1f, 0f, t / tiempoParpadeo);
                yield return null;
            }
        }
    }

    IEnumerator FadeSalida()
    {
        float duracion = 1f;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;

            // Subir alpha del panel negro (fade out)
            if (panelFade != null)
            {
                Color c = panelFade.color;
                c.a = t;
                panelFade.color = c;
            }

            // Bajar volumen
            audioSource.volume = Mathf.Lerp(volumenMenu, 0f, t);

            yield return null;
        }

        SceneManager.LoadScene(nombreEscena);
    }
}
