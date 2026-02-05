using UnityEngine;

public class ProfesorAI : MonoBehaviour
{
    public float velocidadPatrulla = 2f;
    public float velocidadPersecucion = 4f;
    public float distanciaPatrulla = 4f;
    public float distanciaAtrapar = 0.6f;
    public float cooldownAtrapar = 3f;

    private Rigidbody2D rb;
    private Vector2 puntoInicio;
    private bool moviendoDerecha = true;

    private Transform jugador;
    private bool persiguiendo = false;
    private bool puedeAtrapar = true;

    private GameManager manager;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        puntoInicio = rb.position;
        manager = FindObjectOfType<GameManager>();
    }

    void FixedUpdate()
    {
        if (persiguiendo && jugador != null)
        {
            Perseguir();
        }
        else
        {
            Patrullar();
        }
    }

    void Patrullar()
    {
        Vector2 direccion = moviendoDerecha ? Vector2.right : Vector2.left;
        rb.MovePosition(rb.position + direccion * velocidadPatrulla * Time.fixedDeltaTime);

        if (rb.position.x >= puntoInicio.x + distanciaPatrulla)
            moviendoDerecha = false;

        if (rb.position.x <= puntoInicio.x - distanciaPatrulla)
            moviendoDerecha = true;
    }

    void Perseguir()
    {
        Vector2 direccion = (jugador.position - (Vector3)rb.position).normalized;
        rb.MovePosition(rb.position + direccion * velocidadPersecucion * Time.fixedDeltaTime);

        float distancia = Vector2.Distance(rb.position, jugador.position);
        if (distancia <= distanciaAtrapar && puedeAtrapar)
        {
            AtraparJugador();
        }
    }

    void AtraparJugador()
    {
        if (manager != null)
        {
            manager.AplicarCastigo(20);
            puedeAtrapar = false;
            Invoke(nameof(ReactivarAtrapar), cooldownAtrapar);
        }
    }

    void ReactivarAtrapar()
    {
        puedeAtrapar = true;
    }

    // 👇 CAMPO DE VISIÓN REAL
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugador = collision.transform;
            persiguiendo = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            persiguiendo = false;
            jugador = null;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!persiguiendo && !collision.collider.CompareTag("Player"))
        {
            moviendoDerecha = !moviendoDerecha;
        }
    }

    void OnDisable()
    {
        CancelInvoke();
    }
}
