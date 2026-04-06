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
    public float anguloConoBase    = 90f;
    public float anguloConoMaximo  = 200f;
    public float alcanceVisionBase    = 8f;
    public float alcanceVisionMaximo  = 20f;
    public string tagJugador = "Player";

    [Tooltip("Cuántos segundos dentro del cono para llegar al máximo")]
    public float tiempoParaMaximo = 10f;

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    [Header("Persecución")]
    public float distanciaAtrapar = 0.8f;

    // ─────────────────────────────────────────
    //  BÚSQUEDA ACTIVA (último punto conocido)
    // ─────────────────────────────────────────
    [Header("Búsqueda activa")]
    [Tooltip("Cuántos puntos alrededor del último punto conocido revisará antes de rendirse")]
    public int   puntosARevisar       = 4;
    [Tooltip("Radio en el que genera los puntos de búsqueda alrededor del último punto conocido")]
    public float radioBusqueda        = 5f;
    [Tooltip("Tiempo máximo parado en un punto de búsqueda antes de ir al siguiente")]
    public float tiempoEsperaBusqueda = 1.2f;

    // ─────────────────────────────────────────
    //  MEMORIA PERMANENTE
    // ─────────────────────────────────────────
    [Header("Memoria permanente")]
    [Tooltip("Cuánto crece el ángulo base del cono por cada escape del jugador")]
    public float bonusConeAnguloPorEscape      = 10f;
    [Tooltip("Cuánto crece el alcance base por cada escape")]
    public float bonusAlcancePorEscape         = 1.5f;
    [Tooltip("Cuánto más rápido patrulla por cada escape")]
    public float bonusVelocidadPorEscape       = 0.4f;
    [Tooltip("Cuántos segundos menos tarda en llegar al máximo de alerta por cada escape")]
    public float reduccionTiempoMaxPorEscape   = 1.5f;
    [Tooltip("Tiempo mínimo al que puede llegar tiempoParaMaximo")]
    public float tiempoParaMaximoMinimo        = 2f;

    // ─────────────────────────────────────────
    //  ESTADO INTERNO
    // ─────────────────────────────────────────
    private enum Estado { Patrullando, Persiguiendo, Buscando, Atrapado }
    private Estado estadoActual = Estado.Patrullando;

    private float nivelAlerta = 0f;

    private float anguloConoActual;
    private float alcanceVisionActual;
    private float velocidadActual;

    // Memoria acumulada entre escapes
    private int   vecesEscapo              = 0;
    private float bonusAnguloAcumulado     = 0f;
    private float bonusAlcanceAcumulado    = 0f;
    private float bonusVelocidadAcumulado  = 0f;
    private float tiempoParaMaximoActual;

    // Patrullaje
    private int  indiceWaypoint = 0;
    private bool yendo          = true;

    // Movimiento
    private Vector2 destinoActual = Vector2.zero;
    private float   speedActual   = 2f;
    private bool    moviendose    = false;

    // Último punto conocido
    private Vector2 ultimoPuntoConocido;

    // Búsqueda activa
    private List<Vector2> puntosBusqueda   = new List<Vector2>();
    private int           indiceBusqueda   = 0;
    private float         timerEspera      = 0f;
    private bool          esperandoEnPunto = false;

    // Puntos random del mapa
    private List<Vector2> puntosRandom = new List<Vector2>();
    private int           indicePunto  = 0;

    private Transform      jugador;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Animator       animator;
    private Vector2        direccionActual = Vector2.right;

    private const float SAL_W        = 20f;
    private const float Y_PAS        =  0f;
    private const float Y_N          =  9f;
    private const float Y_S          = -9f;
    private const float xIzq         = -81f;
    private const float MARGEN_SALON =  3f;

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
        if (goJugador != null) jugador = goJugador.transform;

        tiempoParaMaximoActual = tiempoParaMaximo;
        anguloConoActual       = anguloConoBase;
        alcanceVisionActual    = alcanceVisionBase;
        velocidadActual        = velocidadBase;

        if (!usarWaypointsManuales)
            GenerarPuntosRandom();
    }

    // ─────────────────────────────────────────
    //  UPDATE
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

            case Estado.Buscando:
                LogicaBusqueda();
                if (JugadorEnCono())
                    IniciarPersecucion();
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
            nivelAlerta += Time.deltaTime / tiempoParaMaximoActual;
            nivelAlerta  = Mathf.Clamp01(nivelAlerta);
        }
        // No baja nunca

        float anguloBaseEfectivo    = anguloConoBase    + bonusAnguloAcumulado;
        float alcanceBaseEfectivo   = alcanceVisionBase + bonusAlcanceAcumulado;
        float velocidadBaseEfectiva = velocidadBase     + bonusVelocidadAcumulado;

        anguloConoActual    = Mathf.Lerp(anguloBaseEfectivo,    anguloConoMaximo,    nivelAlerta);
        alcanceVisionActual = Mathf.Lerp(alcanceBaseEfectivo,   alcanceVisionMaximo, nivelAlerta);
        velocidadActual     = Mathf.Lerp(velocidadBaseEfectiva, velocidadMaxima,     nivelAlerta);
    }

    // ─────────────────────────────────────────
    //  PATRULLAJE
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

        SetDestino(destino, velocidadBase + bonusVelocidadAcumulado);

        if (Vector2.Distance(rb.position, destino) < distanciaWaypoint)
        {
            if (!usarWaypointsManuales)
            {
                indicePunto = (indicePunto + 1) % puntosRandom.Count;
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
            if (indiceWaypoint >= waypoints.Length) { indiceWaypoint = waypoints.Length - 2; yendo = false; }
        }
        else
        {
            indiceWaypoint--;
            if (indiceWaypoint < 0) { indiceWaypoint = 1; yendo = true; }
        }
    }

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    void IniciarPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        MusicaManager.Instancia?.IniciarPersecucion();
        Debug.Log(gameObject.name + " ¡PERSIGUIENDO! Alerta: " + (nivelAlerta * 100f).ToString("F0") + "% | Escapes previos: " + vecesEscapo);
    }

    void LogicaPersecucion()
    {
        if (jugador == null) return;

        // Actualiza el último punto conocido mientras tenga visual
        if (JugadorEnCono())
            ultimoPuntoConocido = jugador.position;

        if (Vector2.Distance(rb.position, jugador.position) < distanciaAtrapar)
        {
            Atrapar();
            return;
        }

        if (JugadorEnCono())
        {
            // Tiene visual — persigue directo
            SetDestino(jugador.position, velocidadActual);
        }
        else
        {
            // Perdió visual — va al último punto conocido
            SetDestino(ultimoPuntoConocido, velocidadActual);

            // Si llegó al último punto y el jugador no está, inicia búsqueda
            if (Vector2.Distance(rb.position, ultimoPuntoConocido) < distanciaWaypoint)
                IniciarBusqueda();
        }
    }

    // ─────────────────────────────────────────
    //  BÚSQUEDA ACTIVA
    // ─────────────────────────────────────────
    void IniciarBusqueda()
    {
        estadoActual     = Estado.Buscando;
        indiceBusqueda   = 0;
        timerEspera      = 0f;
        esperandoEnPunto = false;
        MusicaManager.Instancia?.TerminarPersecucion(); // vuelve ambiente en cuanto pierde visual

        puntosBusqueda.Clear();
        for (int i = 0; i < puntosARevisar; i++)
        {
            float angulo = (360f / puntosARevisar) * i;
            float rad    = angulo * Mathf.Deg2Rad;
            puntosBusqueda.Add(ultimoPuntoConocido + new Vector2(
                Mathf.Cos(rad) * radioBusqueda,
                Mathf.Sin(rad) * radioBusqueda));
        }

        Debug.Log(gameObject.name + " búsqueda activa en " + ultimoPuntoConocido);
    }

    void LogicaBusqueda()
    {
        if (puntosBusqueda.Count == 0) { TerminarBusqueda(); return; }

        Vector2 puntoBusq = puntosBusqueda[indiceBusqueda];

        if (!esperandoEnPunto)
        {
            // Se mueve un poco más lento buscando — más tenso, más cuidadoso
            SetDestino(puntoBusq, velocidadActual * 0.8f);

            if (Vector2.Distance(rb.position, puntoBusq) < distanciaWaypoint)
            {
                esperandoEnPunto = true;
                timerEspera      = 0f;
                moviendose       = false;
                ActualizarAnimacion(Vector2.zero);
            }
        }
        else
        {
            timerEspera += Time.deltaTime;
            if (timerEspera >= tiempoEsperaBusqueda)
            {
                esperandoEnPunto = false;
                indiceBusqueda++;
                if (indiceBusqueda >= puntosBusqueda.Count)
                    TerminarBusqueda();
            }
        }
    }

    void TerminarBusqueda()
    {
        // El jugador escapó — penalización de memoria permanente
        vecesEscapo++;
        bonusAnguloAcumulado    += bonusConeAnguloPorEscape;
        bonusAlcanceAcumulado   += bonusAlcancePorEscape;
        bonusVelocidadAcumulado += bonusVelocidadPorEscape;
        tiempoParaMaximoActual   = Mathf.Max(
            tiempoParaMaximoMinimo,
            tiempoParaMaximoActual - reduccionTiempoMaxPorEscape);

        Debug.Log(gameObject.name + " perdió al jugador. Escape #" + vecesEscapo +
                  " | +Ángulo: "   + bonusAnguloAcumulado.ToString("F0") + "°" +
                  " | +Alcance: "  + bonusAlcanceAcumulado.ToString("F1") +
                  " | TiempoMax: " + tiempoParaMaximoActual.ToString("F1") + "s");

        estadoActual = Estado.Patrullando;
    }

    // ─────────────────────────────────────────
    //  ATRAPAR
    // ─────────────────────────────────────────
    void Atrapar()
    {
        estadoActual = Estado.Atrapado;
        moviendose   = false;

        PlayerMovement pm = jugador?.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        Debug.Log("¡ATRAPADO por " + gameObject.name + "! Alerta: " + (nivelAlerta * 100f).ToString("F0") + "%");
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
    //  PUNTOS RANDOM DEL MAPA
    // ─────────────────────────────────────────
    void GenerarPuntosRandom()
    {
        puntosRandom.Clear();

        float dentroN = Y_N - MARGEN_SALON;
        float dentroS = Y_S + MARGEN_SALON;

        for (float x = -70f; x <= 70f; x += 15f)
            puntosRandom.Add(new Vector2(x, Y_PAS));

        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            puntosRandom.Add(new Vector2(sx, dentroN));
            puntosRandom.Add(new Vector2(sx, dentroN + 4f));
        }

        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            puntosRandom.Add(new Vector2(sx, dentroS));
            puntosRandom.Add(new Vector2(sx, dentroS - 4f));
        }

        MezclarLista();
        indicePunto   = 0;
        destinoActual = puntosRandom[0];
        moviendose    = true;
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
        Gizmos.color = Color.Lerp(Color.yellow, Color.red, nivelAlerta);
        Vector3 dir     = Application.isPlaying ? (Vector3)direccionActual : transform.right;
        float   mitad   = Application.isPlaying ? anguloConoActual / 2f    : anguloConoBase / 2f;
        float   alcance = Application.isPlaying ? alcanceVisionActual       : alcanceVisionBase;

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

        // Naranja: puntos de búsqueda activa en el editor
        if (Application.isPlaying && estadoActual == Estado.Buscando)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Gizmos.DrawWireSphere(ultimoPuntoConocido, 1f);
            for (int i = 0; i < puntosBusqueda.Count; i++)
            {
                Gizmos.DrawWireSphere(puntosBusqueda[i], 0.5f);
                if (i < puntosBusqueda.Count - 1)
                    Gizmos.DrawLine(puntosBusqueda[i], puntosBusqueda[i+1]);
            }
        }
    }
}