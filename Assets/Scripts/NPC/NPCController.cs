using UnityEngine;
using System.Collections.Generic;

public class NPCController : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  PATRULLAJE
    // ─────────────────────────────────────────
    [Header("Patrullaje")]
    public float velocidadBase        = 2f;
    public float velocidadMaxima      = 8f;
    public float distanciaWaypoint    = 0.5f;

    [Tooltip("Si true, usa waypoints manuales. Si false, movimiento random automático.")]
    public bool usarWaypointsManuales = false;
    public Transform[] waypoints;

    // ─────────────────────────────────────────
    //  CONO DE VISIÓN ESCALABLE
    // ─────────────────────────────────────────
    [Header("Cono de visión")]
    [Range(10f, 360f)]
    public float anguloConoBase   = 90f;
    public float anguloConoMaximo = 200f;
    public float alcanceVisionBase   = 8f;
    public float alcanceVisionMaximo = 20f;
    public string tagJugador = "Player";

    [Tooltip("Cuántos segundos dentro del cono para llegar al máximo")]
    public float tiempoParaMaximo = 10f;

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    [Header("Persecución")]
    public float tiempoPersecucionBase = 3f;
    public float incrementoPersecucion = 1f;
    public float distanciaAtrapar      = 0.8f;

    // ─────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────
    private enum Estado { Patrullando, Persiguiendo, Atrapado }
    private Estado estadoActual = Estado.Patrullando;

    // Escalado (0 a 1) — sube mientras el jugador esté en el cono
    private float nivelAlerta         = 0f;

    // Valores actuales calculados desde nivelAlerta
    private float anguloConoActual;
    private float alcanceVisionActual;
    private float velocidadActual;

    private int   indiceWaypoint          = 0;
    private bool  yendo                   = true;
    private float timerPersecucion        = 0f;
    private float tiempoPersecucionActual;
    private int   vecesAtrapado           = 0;

    private Vector2 destinoActual = Vector2.zero;
    private float   speedActual   = 2f;
    private bool    moviendose    = false;

    // Puntos random del mapa
    private List<Vector2> puntosRandom = new List<Vector2>();
    private int           indicePunto  = 0;

    private Transform      jugador;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Animator       animator;
    private Vector2        direccionActual = Vector2.right;

    // Coordenadas del mapa
    private const float SAL_W         = 20f;
    private const float Y_PAS         =  0f;
    private const float Y_N           =  9f;
    private const float Y_S           = -9f;
    private const float xIzq          = -81f;
    private const float MARGEN_SALON  =  3f;

    // ─────────────────────────────────────────
    //  INICIO
    // ─────────────────────────────────────────
    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sr == null)       sr       = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        GameObject goJugador = GameObject.FindGameObjectWithTag(tagJugador);
        if (goJugador != null)
            jugador = goJugador.transform;

        tiempoPersecucionActual = tiempoPersecucionBase;

        // Valores iniciales
        anguloConoActual    = anguloConoBase;
        alcanceVisionActual = alcanceVisionBase;
        velocidadActual     = velocidadBase;

        if (!usarWaypointsManuales)
            GenerarPuntosRandom();
    }

    // ─────────────────────────────────────────
    //  PUNTOS RANDOM DEL MAPA
    // ─────────────────────────────────────────
    void GenerarPuntosRandom()
    {
        puntosRandom.Clear();

        float dentroN = Y_N - MARGEN_SALON;
        float dentroS = Y_S + MARGEN_SALON;

        // Pasillo completo — muchos puntos para cubrir todo
        for (float x = -70f; x <= 70f; x += 15f)
            puntosRandom.Add(new Vector2(x, Y_PAS));

        // Entradas de salones norte
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            puntosRandom.Add(new Vector2(sx, dentroN));
            puntosRandom.Add(new Vector2(sx, dentroN + 4f));
        }

        // Entradas de salones sur
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            puntosRandom.Add(new Vector2(sx, dentroS));
            puntosRandom.Add(new Vector2(sx, dentroS - 4f));
        }

        // Mezcla la lista para que el orden sea random
        MezclarLista();

        indicePunto = 0;
        destinoActual = puntosRandom[0];
        moviendose = true;
    }

    void MezclarLista()
    {
        for (int i = puntosRandom.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector2 tmp = puntosRandom[i];
            puntosRandom[i] = puntosRandom[j];
            puntosRandom[j] = tmp;
        }
    }

    float SalonCX(int i) => xIzq + 1f + SAL_W * i + SAL_W / 2f;

    // ─────────────────────────────────────────
    //  UPDATE — lógica
    // ─────────────────────────────────────────
    void Update()
    {
        ActualizarAlerta();

        switch (estadoActual)
        {
            case Estado.Patrullando:
                LogicaPatrullaje();
                if (JugadorEnCono())
                    IniciarPersecucion();
                break;

            case Estado.Persiguiendo:
                LogicaPersecucion();
                break;

            case Estado.Atrapado:
                moviendose = false;
                ActualizarAnimacion(Vector2.zero);
                break;
        }

        ActualizarSorting();
    }

    // ─────────────────────────────────────────
    //  ALERTA ESCALABLE
    // ─────────────────────────────────────────
    void ActualizarAlerta()
    {
        if (estadoActual == Estado.Atrapado) return;

        if (JugadorEnCono())
        {
            // Sube el nivel de alerta mientras el jugador esté en el cono
            nivelAlerta += Time.deltaTime / tiempoParaMaximo;
            nivelAlerta  = Mathf.Clamp01(nivelAlerta);
        }
        // Si sale del cono, el nivel se mantiene (no baja)

        // Actualiza valores según el nivel de alerta
        anguloConoActual    = Mathf.Lerp(anguloConoBase,    anguloConoMaximo,    nivelAlerta);
        alcanceVisionActual = Mathf.Lerp(alcanceVisionBase, alcanceVisionMaximo, nivelAlerta);
        velocidadActual     = Mathf.Lerp(velocidadBase,     velocidadMaxima,     nivelAlerta);
    }

    // ─────────────────────────────────────────
    //  FIXED UPDATE — movimiento físico
    // ─────────────────────────────────────────
    void FixedUpdate()
    {
        if (!moviendose || rb == null) return;

        Vector2 nuevaPos = Vector2.MoveTowards(
            rb.position, destinoActual, speedActual * Time.fixedDeltaTime);
        rb.MovePosition(nuevaPos);
    }

    // ─────────────────────────────────────────
    //  LÓGICA PATRULLAJE
    // ─────────────────────────────────────────
    void LogicaPatrullaje()
    {
        Vector2 destino;

        if (usarWaypointsManuales)
        {
            if (waypoints == null || waypoints.Length < 2) return;
            destino = waypoints[indiceWaypoint].position;
        }
        else
        {
            if (puntosRandom.Count == 0) return;
            destino = puntosRandom[indicePunto];
        }

        SetDestino(destino, velocidadBase); // Patrulla siempre a velocidad base

        if (Vector2.Distance(rb.position, destino) < distanciaWaypoint)
        {
            // Elige el siguiente punto random
            if (!usarWaypointsManuales)
            {
                indicePunto = (indicePunto + 1) % puntosRandom.Count;
                // Cuando termina la lista, la vuelve a mezclar
                if (indicePunto == 0) MezclarLista();
            }
            else
            {
                AvanzarWaypointManual();
            }
        }
    }

    void AvanzarWaypointManual()
    {
        if (yendo)
        {
            indiceWaypoint++;
            if (indiceWaypoint >= waypoints.Length)
            {
                indiceWaypoint = waypoints.Length - 2;
                yendo = false;
            }
        }
        else
        {
            indiceWaypoint--;
            if (indiceWaypoint < 0) { indiceWaypoint = 1; yendo = true; }
        }
    }

    // ─────────────────────────────────────────
    //  LÓGICA PERSECUCIÓN
    // ─────────────────────────────────────────
    void IniciarPersecucion()
    {
        estadoActual     = Estado.Persiguiendo;
        timerPersecucion = 0f;
        Debug.Log(gameObject.name + " persiguiendo! Nivel alerta: " + (nivelAlerta * 100f).ToString("F0") + "%");
    }

    void LogicaPersecucion()
    {
        if (jugador == null) return;

        timerPersecucion += Time.deltaTime;

        if (Vector2.Distance(rb.position, jugador.position) < distanciaAtrapar)
        {
            Atrapar();
            return;
        }

        if (timerPersecucion >= tiempoPersecucionActual)
        {
            Debug.Log(gameObject.name + " perdió al jugador");
            estadoActual = Estado.Patrullando;
            return;
        }

        // Persigue a velocidad escalada por nivelAlerta
        SetDestino(jugador.position, velocidadActual);
    }

    void Atrapar()
    {
        vecesAtrapado++;
        tiempoPersecucionActual = tiempoPersecucionBase + (incrementoPersecucion * vecesAtrapado);
        estadoActual = Estado.Atrapado;
        moviendose   = false;

        PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.Log("¡Atrapado! Nivel alerta: " + (nivelAlerta * 100f).ToString("F0") + "%");
    }

    // ─────────────────────────────────────────
    //  SET DESTINO
    // ─────────────────────────────────────────
    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual   = speed;
        moviendose    = true;

        Vector2 dir = (destino - rb.position).normalized;
        if (dir.magnitude > 0.01f) direccionActual = dir;

        ActualizarAnimacion(dir);
    }

    // ─────────────────────────────────────────
    //  ANIMACIÓN
    // ─────────────────────────────────────────
    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;
        if (dir.magnitude < 0.01f) { animator.Play("Idle"); return; }

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            animator.Play(dir.x > 0 ? "WalkRight" : "WalkLeft");
        else
            animator.Play(dir.y > 0 ? "WalkUp" : "WalkDown");
    }

    // ─────────────────────────────────────────
    //  CONO DE VISIÓN
    // ─────────────────────────────────────────
    bool JugadorEnCono()
    {
        if (jugador == null) return false;
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcanceVisionActual) return false;
        float angulo = Vector2.Angle(direccionActual, diff.normalized);
        return angulo <= anguloConoActual / 2f;
    }

    // ─────────────────────────────────────────
    //  Y-SORTING
    // ─────────────────────────────────────────
    void ActualizarSorting()
    {
        if (sr == null) return;
        sr.sortingOrder = 300 + Mathf.RoundToInt(-transform.position.y * 10f);
    }

    // ─────────────────────────────────────────
    //  GIZMOS EDITOR
    // ─────────────────────────────────────────
    void OnDrawGizmos()
    {
        // Color cambia según el nivel de alerta
        Gizmos.color = Color.Lerp(Color.yellow, Color.red, nivelAlerta);
        Vector3 dir = Application.isPlaying ? (Vector3)direccionActual : transform.right;
        float mitad = Application.isPlaying ? anguloConoActual / 2f : anguloConoBase / 2f;
        float alcance = Application.isPlaying ? alcanceVisionActual : alcanceVisionBase;

        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0, mitad) * dir * alcance);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0,-mitad) * dir * alcance);

        for (int i = 0; i < 20; i++)
        {
            float a1 = -mitad + anguloConoActual * ((float)i / 20);
            float a2 = -mitad + anguloConoActual * ((float)(i+1) / 20);
            Gizmos.DrawLine(
                transform.position + Quaternion.Euler(0,0,a1) * dir * alcance,
                transform.position + Quaternion.Euler(0,0,a2) * dir * alcance);
        }
    }
}