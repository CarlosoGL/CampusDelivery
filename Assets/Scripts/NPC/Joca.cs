using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
//  JOCA  v2 (Actualizado para animaciones EXACTAS)
//  Cambios realizados según tu solicitud:
//  - animIdle      = "Joca_Idle_animaton"
//  - animWalkUp    = "Joca_WalkUp"
//  - animWalkDown  = "Joca_WalkDown"
//  - animWalkLeft  = "Joca_WalkLeft"
//  - animWalkRight = "Joca_WalkRight"
//  Todo lo demás igual que El Pelón (funciona perfecto)
// ══════════════════════════════════════════════════════

public class Joca : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  PUESTO FIJO Y ZONA
    // ─────────────────────────────────────────
    [Header("Puesto fijo")]
    public Transform puestoFijo;
    public float radioZona = 12f;

    // ─────────────────────────────────────────
    //  DETECCIÓN
    // ─────────────────────────────────────────
    [Header("Detección")]
    public float radioEscuchaBase = 5f;
    public float umbralVelocidadCorriendo = 0.25f;
    public float bonusEscuchaPorEscape = 1.3f;
    public float radioEscuchaMaximo = 16f;

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    [Header("Persecución")]
    public float velocidadPersecucion = 4.5f;
    public float distanciaAtrapar = 2.2f;
    public float distanciaAtraparForzada = 3.5f;

    // ─────────────────────────────────────────
    //  CASTIGO Y ENCIERRO
    // ─────────────────────────────────────────
    [Header("Castigo y encierro")]
    public int castigo = 30;
    public float tiempoEncierro = 30f;

    // ─────────────────────────────────────────
    //  RONDA Y REGRESO
    // ─────────────────────────────────────────
    [Header("Ronda y regreso")]
    public float tiempoMinEnPuesto = 20f;
    public float tiempoMaxEnPuesto = 40f;
    public float tiempoEsperaRondaMin = 6f;
    public float tiempoEsperaRondaMax = 12f;
    public float radioRonda = 6f;
    public float velocidadRonda = 1.8f;
    public float velocidadRegreso = 2.5f;

    // ─────────────────────────────────────────
    //  ANIMACIONES (NOMBRES EXACTOS que pediste)
    // ─────────────────────────────────────────
    [Header("Animaciones")]
    public string Joca_Idle_animaton      = "Joca_Idle_animaton";
    public string Joca_WalkUp    = "Joca_WalkUp";
    public string Joca_WalkDown  = "Joca_WalkDown";
    public string Joca_WalkLeft  = "Joca_WalkLeft";
    public string Joca_WalkRight = "Joca_WalkRight";

    // ─────────────────────────────────────────
    //  DEBUG
    // ─────────────────────────────────────────
    [Header("DEBUG")]
    public bool modoDebugMovimiento = false;

    // ─────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────
    enum Estado { EnPuesto, AlertaPasiva, Ronda, EsperandoRonda, Persiguiendo, RegresandoAlPuesto, Encerrado }
    Estado estadoActual = Estado.EnPuesto;

    // Componentes
    Rigidbody2D    rb;
    SpriteRenderer sr;
    Animator       animator;
    Transform      jugador;

    // Posiciones y timers
    private Vector2 posicionPuesto;
    private Vector2 destinoActual;
    private float   speedActual;
    private bool    moviendose = false;

    private float radioEscuchaActual;
    private int   vecesEscapo = 0;

    private float timerPuesto = 0f;
    private float duracionPuesto = 0f;
    private float timerConfirmacion = 0f;
    private float timerEncierro = 0f;

    private Vector2 posJugadorAnterior;
    private float   velocidadJugadorSuave = 0f;

    // Salones de encierro
    private Vector2[] salonesNorte = new Vector2[8];
    private Vector2[] salonesSur   = new Vector2[8];
    private int   salonEncierroIdx;
    private bool  salonEncierroNorte;

    // Animación cacheada
    private string animActual = "";

    // ─────────────────────────────────────────
    //  START
    // ─────────────────────────────────────────
    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        if (sr == null)       sr       = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }

        posicionPuesto = puestoFijo ? (Vector2)puestoFijo.position : (Vector2)transform.position;

        GameObject pj = GameObject.FindGameObjectWithTag("Player");
        if (pj)
        {
            jugador = pj.transform;
            posJugadorAnterior = jugador.position;
        }

        // Inicializar salones
        for (int i = 0; i < 8; i++)
        {
            float x = -81f + 1f + 20f * i + 10f;
            salonesNorte[i] = new Vector2(x, 18f);
            salonesSur[i]   = new Vector2(x, -18f);
        }

        radioEscuchaActual = radioEscuchaBase;
        IniciarEspera();
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        if (jugador)
        {
            float vel = Vector2.Distance(jugador.position, posJugadorAnterior) / Mathf.Max(Time.deltaTime, 0.01f);
            posJugadorAnterior = jugador.position;
            velocidadJugadorSuave = Mathf.Lerp(velocidadJugadorSuave, vel, 0.2f);
        }

        switch (estadoActual)
        {
            case Estado.EnPuesto:        LogicaEnPuesto(); break;
            case Estado.AlertaPasiva:    LogicaAlertaPasiva(); break;
            case Estado.Ronda:           LogicaRonda(); break;
            case Estado.EsperandoRonda:  LogicaEsperandoRonda(); break;
            case Estado.Persiguiendo:    LogicaPersecucion(); break;
            case Estado.RegresandoAlPuesto: LogicaRegreso(); break;
            case Estado.Encerrado:       LogicaEncierro(); break;
        }

        ActualizarSorting();
    }

    void FixedUpdate()
    {
        if (!moviendose || rb == null) return;

        Vector2 nuevaPos = Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime);
        rb.MovePosition(nuevaPos);
    }

    // ─────────────────────────────────────────
    //  ESTADOS - EN PUESTO
    // ─────────────────────────────────────────
    void IniciarEspera()
    {
        estadoActual = Estado.EnPuesto;
        moviendose = false;
        timerPuesto = 0f;
        duracionPuesto = Random.Range(tiempoMinEnPuesto, tiempoMaxEnPuesto);
        ActualizarAnimacion(Vector2.zero);
    }

    void LogicaEnPuesto()
    {
        if (JugadorDetectado()) 
        { 
            IniciarAlertaPasiva(); 
            return; 
        }

        timerPuesto += Time.deltaTime;
        if (timerPuesto >= duracionPuesto)
            IniciarRonda();
    }

    // ─────────────────────────────────────────
    //  ALERTA PASIVA
    // ─────────────────────────────────────────
    void IniciarAlertaPasiva()
    {
        estadoActual = Estado.AlertaPasiva;
        timerConfirmacion = 0f;
        Vector2 dir = (Vector2)(jugador.position - transform.position);
        ActualizarAnimacion(dir);
    }

    void LogicaAlertaPasiva()
    {
        if (JugadorDetectado())
        {
            Vector2 dir = (Vector2)(jugador.position - transform.position);
            ActualizarAnimacion(dir);
            timerConfirmacion += Time.deltaTime;
            if (timerConfirmacion >= 1.2f)
                IniciarPersecucion();
        }
        else
        {
            IniciarRegreso();
        }
    }

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    void IniciarPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        MusicaManager.Instancia?.IniciarPersecucion();
        Debug.Log("<color=red>¡JOCA ESTÁ PERSIGUIENDO!</color>");
    }

    void LogicaPersecucion()
    {
        if (!jugador) return;

        float dist = Vector2.Distance(transform.position, jugador.position);

        if (dist < distanciaAtrapar || (dist < distanciaAtraparForzada && velocidadJugadorSuave < 1f))
        {
            Atrapar();
            return;
        }

        if (Vector2.Distance(jugador.position, posicionPuesto) > radioZona + 3f)
        {
            JugadorEscapo();
            return;
        }

        SetDestino(jugador.position, velocidadPersecucion);
    }

    // ─────────────────────────────────────────
    //  RONDA
    // ─────────────────────────────────────────
    void IniciarRonda()
    {
        estadoActual = Estado.Ronda;

        Vector2 offset = Random.insideUnitCircle * radioRonda;
        destinoActual = posicionPuesto + offset;

        SetDestino(destinoActual, velocidadRonda);
    }

    void LogicaRonda()
    {
        if (JugadorDetectado())
        {
            IniciarAlertaPasiva();
            return;
        }

        if (Vector2.Distance(transform.position, destinoActual) < 0.5f)
        {
            IniciarEsperandoRonda();
        }
    }

    void IniciarEsperandoRonda()
    {
        estadoActual = Estado.EsperandoRonda;
        moviendose = false;
        timerPuesto = 0f;
        duracionPuesto = Random.Range(tiempoEsperaRondaMin, tiempoEsperaRondaMax);
        ActualizarAnimacion(Vector2.zero);
    }

    void LogicaEsperandoRonda()
    {
        if (JugadorDetectado())
        {
            IniciarAlertaPasiva();
            return;
        }

        timerPuesto += Time.deltaTime;
        if (timerPuesto >= duracionPuesto)
            IniciarRegreso();
    }

    // ─────────────────────────────────────────
    //  REGRESO Y ENCIERRO
    // ─────────────────────────────────────────
    void IniciarRegreso()
    {
        estadoActual = Estado.RegresandoAlPuesto;
        SetDestino(posicionPuesto, velocidadRegreso);
    }

    void LogicaRegreso()
    {
        if (Vector2.Distance(transform.position, posicionPuesto) < 0.5f)
            IniciarEspera();
    }

    void Atrapar()
    {
        estadoActual = Estado.Encerrado;
        moviendose = false;
        timerEncierro = 0f;

        MusicaManager.Instancia?.TerminarPersecucion();

        ElegirSalonEncierro();

        Vector2 centro = salonEncierroNorte ? salonesNorte[salonEncierroIdx] : salonesSur[salonEncierroIdx];

        if (jugador)
        {
            PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
            if (pm) pm.enabled = false;

            Rigidbody2D rbj = jugador.GetComponent<Rigidbody2D>();
            if (rbj) rbj.linearVelocity = Vector2.zero;

            jugador.position = (Vector3)centro + new Vector3(1.8f, 0, 0);
        }

        FindObjectOfType<GameManager>()?.AplicarCastigo(castigo);

        ActualizarAnimacion(Vector2.zero);
        Debug.Log($"<color=green>¡ATRAPADO! Salón {(salonEncierroNorte ? "Norte" : "Sur")} {salonEncierroIdx}</color>");
    }

    void JugadorEscapo()
    {
        vecesEscapo++;
        radioEscuchaActual = Mathf.Min(radioEscuchaMaximo, radioEscuchaActual + bonusEscuchaPorEscape);
        MusicaManager.Instancia?.TerminarPersecucion();
        IniciarRegreso();
    }

    void LogicaEncierro()
    {
        timerEncierro += Time.deltaTime;
        if (timerEncierro >= tiempoEncierro)
            LiberarJugador();
    }

    void LiberarJugador()
    {
        if (jugador)
        {
            PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
            if (pm) pm.enabled = true;
        }
        IniciarRegreso();
    }

    // ─────────────────────────────────────────
    //  DETECCIÓN
    // ─────────────────────────────────────────
    bool JugadorDetectado()
    {
        if (modoDebugMovimiento) return true;
        return velocidadJugadorSuave >= umbralVelocidadCorriendo && JugadorEnRadio();
    }

    bool JugadorEnRadio() => Vector2.Distance(transform.position, jugador.position) <= radioEscuchaActual;

    // ─────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────
    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual   = speed;
        moviendose    = true;

        Vector2 dir = (destino - (Vector2)transform.position).normalized;
        if (dir.magnitude > 0.01f)
            ActualizarAnimacion(dir);
    }

    // ─────────────────────────────────────────
    //  ANIMACIÓN (usa exactamente los nombres que diste)
    // ─────────────────────────────────────────
    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;

        string nueva;

        if (dir.magnitude < 0.01f)
            nueva = Joca_Idle_animaton;                   // ← "Joca_Idle_animaton"
        else if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            nueva = dir.x > 0 ? Joca_WalkRight : Joca_WalkLeft;
        else
            nueva = dir.y > 0 ? Joca_WalkUp : Joca_WalkDown;

        if (nueva != animActual)
        {
            animActual = nueva;
            animator.Play(nueva);
        }
    }

    // ─────────────────────────────────────────
    //  Y-SORTING
    // ─────────────────────────────────────────
    void ActualizarSorting()
    {
        if (sr) sr.sortingOrder = 300 - Mathf.RoundToInt(transform.position.y * 10f);
    }

    // ─────────────────────────────────────────
    //  SALÓN DE ENCIERRO
    // ─────────────────────────────────────────
    void ElegirSalonEncierro()
    {
        List<(int idx, bool norte)> candidatos = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
        {
            if (i == 4) continue;
            if (Vector2.Distance(salonesNorte[i], posicionPuesto) <= radioZona)
                candidatos.Add((i, true));
            if (Vector2.Distance(salonesSur[i], posicionPuesto) <= radioZona)
                candidatos.Add((i, false));
        }

        if (candidatos.Count == 0)
        {
            salonEncierroIdx = 0; salonEncierroNorte = true;
            return;
        }

        var elegido = candidatos[Random.Range(0, candidatos.Count)];
        salonEncierroIdx = elegido.idx;
        salonEncierroNorte = elegido.norte;
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtrapar);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioEscuchaActual);
    }
}