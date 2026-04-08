using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
// EL VIDEOJUEGOS v8.5 - FIX PERSECUCIÓN NORMAL (modo rutinario)
// ══════════════════════════════════════════════════════
public class ElVideojuegos : MonoBehaviour
{
    [Header("Rutina normal")]
    public float tiempoMinEnSalon = 15f;
    public float tiempoMaxEnSalon = 25f;

    [Header("Visión normal")]
    [Range(60f, 180f)] public float anguloNormal = 110f;
    public float alcanceNormal = 6f;
    public float alcanceSalon = 9f;

    [Header("Visión desesperada")]
    [Range(120f, 359f)] public float anguloDeses = 270f;
    public float alcanceDeses = 14f;
    public float alcanceSalonD = 16f;

    [Header("Movimiento")]
    public float velCaminata = 2f;
    public float velPersecucion = 3.5f;
    public float velDesesperada = 5.5f;
    public float distanciaLlegada = 0.6f;

    [Header("Modo desesperado")]
    public float duracionDeses = 20f;
    private float[] tiemposEspera = { 7f, 10f, 15f, 20f, 30f };

    [Header("Castigo")]
    public int castigo = 20;
    public float tiempoEncierro = 15f;
    public float tiempoSlowdown = 60f;
    public float factorSlowdown = 0.35f;
    public string tagJugador = "Player";

    // Constantes edificio
    const float SAL_W = 20f;
    const float xIzq = -81f;
    const float Y_NORTE = 18f;
    const float Y_SUR = -18f;
    const float Y_DIV_N = 9f;
    const float Y_DIV_S = -9f;
    const float Y_PASILLO = 0f;
    const float OFFSET_PUERTA = 0.5f;

    enum Estado { Esperando, Ruta, Persiguiendo, Buscando, Desesperado, Atrapado }
    Estado estadoActual = Estado.Esperando;
    Estado estadoAntesDeses = Estado.Esperando;

    struct PuntoSalon { public Vector2 centro; public Vector2 puerta; }
    PuntoSalon[] salonesNorte = new PuntoSalon[8];
    PuntoSalon[] salonesSur = new PuntoSalon[8];

    int salonIdx = 0;
    bool salonNorte = true;

    float timerSalon = 0f;
    float duracionSalon = 0f;

    float timerProxDeses = 0f;
    float timerDuraDeses = 0f;
    float proxDesesTiempo = 0f;

    Queue<Vector2> rutaActual = new Queue<Vector2>();
    Vector2 ultimoPuntoConocido;
    List<Vector2> puntosBusqueda = new List<Vector2>();
    int indiceBusqueda = 0;
    float timerEspera = 0f;
    bool esperandoEnPunto = false;

    List<(int idx, bool norte)> salonesARevisar = new List<(int, bool)>();
    int indiceSalonDeses = 0;
    bool faseInicialDeses = true;

    Vector2 destinoActual = Vector2.zero;
    float speedActual = 0f;
    bool moviendose = false;
    Vector2 direccionActual = Vector2.right;
    bool enPasillo = false;

    float timerEncierroActual = 0f;
    bool jugadorFijoEnSalon = false;

    string animActual = "";

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator animator;
    Transform jugador;
    Rigidbody2D rbJugador;
    PlayerMovement pmJugador;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        GameObject goJ = GameObject.FindGameObjectWithTag(tagJugador);
        if (goJ != null)
        {
            jugador = goJ.transform;
            rbJugador = goJ.GetComponent<Rigidbody2D>();
            pmJugador = goJ.GetComponent<PlayerMovement>();
        }
        else
        {
            Debug.LogError("No se encontró al jugador con tag: " + tagJugador);
        }

        for (int i = 0; i < 8; i++)
        {
            float sx = xIzq + 1f + SAL_W * i + SAL_W / 2f;
            salonesNorte[i] = new PuntoSalon { centro = new Vector2(sx, Y_NORTE), puerta = new Vector2(sx, Y_DIV_N + OFFSET_PUERTA) };
            salonesSur[i]   = new PuntoSalon { centro = new Vector2(sx, Y_SUR),   puerta = new Vector2(sx, Y_DIV_S - OFFSET_PUERTA) };
        }

        if (jugador != null) ultimoPuntoConocido = jugador.position;

        ElegirSalonInicial();
        ElegirProximoDeses();
    }

    void ElegirProximoDeses()
    {
        proxDesesTiempo = tiemposEspera[Random.Range(0, tiemposEspera.Length)];
        timerProxDeses = 0f;
    }

    void Update()
    {
        enPasillo = Mathf.Abs(transform.position.y) < 8.5f;

        if (estadoActual != Estado.Desesperado && estadoActual != Estado.Atrapado)
        {
            timerProxDeses += Time.deltaTime;
            if (timerProxDeses >= proxDesesTiempo)
                IniciarModoDesesperado();
        }

        switch (estadoActual)
        {
            case Estado.Esperando: LogicaEspera(); if (JugadorDetectado()) IniciarPersecucion(); break;
            case Estado.Ruta: LogicaRuta(); if (JugadorDetectado()) IniciarPersecucion(); break;
            case Estado.Persiguiendo: LogicaPersecucion(); break;
            case Estado.Buscando: LogicaBusqueda(); if (JugadorDetectado()) IniciarPersecucion(); break;
            case Estado.Desesperado: LogicaDesesperado(); break;
            case Estado.Atrapado: LogicaEncierro(); break;
        }

        ActualizarSorting();
    }

    // ==================== MODO DESESPERADO ====================
    void IniciarModoDesesperado()
    {
        estadoAntesDeses = estadoActual;
        estadoActual = Estado.Desesperado;
        timerDuraDeses = 0f;
        rutaActual.Clear();

        MusicaManager.Instancia?.IniciarPersecucion();

        var todos = SalonesValidos();
        for (int i = todos.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = todos[i]; todos[i] = todos[j]; todos[j] = tmp;
        }

        salonesARevisar = todos;
        indiceSalonDeses = 0;
        faseInicialDeses = true;
        ultimoPuntoConocido = jugador != null ? (Vector2)jugador.position : rb.position;

        SetDestino(ultimoPuntoConocido, velDesesperada);
    }

    void LogicaDesesperado()
    {
        timerDuraDeses += Time.deltaTime;
        if (timerDuraDeses >= duracionDeses) { TerminarModoDesesperado(); return; }

        if (JugadorDetectadoDeses())
        {
            ultimoPuntoConocido = jugador.position;
            IniciarPersecucionDeses();
            return;
        }

        if (faseInicialDeses)
        {
            if (Vector2.Distance(rb.position, ultimoPuntoConocido) <= distanciaLlegada * 2.5f)
            {
                faseInicialDeses = false;
                IrSiguienteSalonDeses();
            }
            else
            {
                SetDestino(ultimoPuntoConocido, velDesesperada);
            }
            return;
        }

        if (moviendose && Vector2.Distance(rb.position, destinoActual) > distanciaLlegada) return;

        indiceSalonDeses++;
        if (indiceSalonDeses >= salonesARevisar.Count)
        {
            faseInicialDeses = true;
            indiceSalonDeses = 0;
            SetDestino(ultimoPuntoConocido, velDesesperada);
        }
        else
        {
            IrSiguienteSalonDeses();
        }
    }

    void IrSiguienteSalonDeses()
    {
        if (indiceSalonDeses >= salonesARevisar.Count) return;
        var sal = salonesARevisar[indiceSalonDeses];
        Vector2 centro = sal.norte ? salonesNorte[sal.idx].centro : salonesSur[sal.idx].centro;
        SetDestino(centro, velDesesperada);
    }

    void IniciarPersecucionDeses()
    {
        estadoActual = Estado.Persiguiendo;
        rutaActual.Clear();
    }

    void TerminarModoDesesperado()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        ElegirProximoDeses();
        TerminarEnSalonCercano();
    }

    // ==================== DETECCIÓN ====================
    bool JugadorDetectado()
    {
        if (jugador == null) return false;
        if (enPasillo) return JugadorEnCono(anguloNormal, alcanceNormal);
        return JugadorEnSalon(alcanceSalon);
    }

    bool JugadorDetectadoDeses()
    {
        if (jugador == null) return false;
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcanceDeses) return false;
        return Vector2.Angle(direccionActual, diff.normalized) <= anguloDeses / 2f;
    }

    bool JugadorEnSalon(float alcance)
    {
        if (jugador == null) return false;
        if (Vector2.Distance(rb.position, jugador.position) > alcance) return false;
        if (Mathf.Abs(jugador.position.y) < 8.5f) return false;

        bool npcNorte = transform.position.y > 0f;
        bool jugNorte = jugador.position.y > 0f;
        return npcNorte == jugNorte;
    }

    bool JugadorEnCono(float angulo, float alcance)
    {
        if (jugador == null) return false;
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcance) return false;
        return Vector2.Angle(direccionActual, diff.normalized) <= angulo / 2f;
    }

    // ==================== RUTINA Y RUTA ====================
    void ElegirSalonInicial()
    {
        var validos = SalonesValidos();
        var elegido = validos[Random.Range(0, validos.Count)];
        salonIdx = elegido.idx;
        salonNorte = elegido.norte;
        transform.position = new Vector3(
            salonNorte ? salonesNorte[salonIdx].centro.x : salonesSur[salonIdx].centro.x,
            salonNorte ? Y_NORTE : Y_SUR, 0f);
        IniciarEspera();
    }

    void IniciarEspera()
    {
        estadoActual = Estado.Esperando;
        moviendose = false;
        timerSalon = 0f;
        duracionSalon = Random.Range(tiempoMinEnSalon, tiempoMaxEnSalon);
        ActualizarAnimacion(Vector2.zero);
    }

    void LogicaEspera()
    {
        timerSalon += Time.deltaTime;
        if (timerSalon >= duracionSalon)
            ElegirSiguienteSalonYRuta();
    }

    void ElegirSiguienteSalonYRuta()
    {
        var validos = SalonesValidos();
        validos.RemoveAll(v => v.idx == salonIdx && v.norte == salonNorte);
        var dest = validos[Random.Range(0, validos.Count)];

        Vector2 puertaOrigen = salonNorte ? salonesNorte[salonIdx].puerta : salonesSur[salonIdx].puerta;
        Vector2 pasilloOrigen = new Vector2(puertaOrigen.x, Y_PASILLO);
        Vector2 centroDestino = dest.norte ? salonesNorte[dest.idx].centro : salonesSur[dest.idx].centro;
        Vector2 puertaDestino = dest.norte ? salonesNorte[dest.idx].puerta : salonesSur[dest.idx].puerta;
        Vector2 pasilloDestino = new Vector2(puertaDestino.x, Y_PASILLO);
        Vector2 entradaDestino = dest.norte ?
            new Vector2(puertaDestino.x, Y_DIV_N - OFFSET_PUERTA) :
            new Vector2(puertaDestino.x, Y_DIV_S + OFFSET_PUERTA);

        rutaActual.Clear();
        rutaActual.Enqueue(puertaOrigen);
        rutaActual.Enqueue(pasilloOrigen);
        rutaActual.Enqueue(pasilloDestino);
        rutaActual.Enqueue(entradaDestino);
        rutaActual.Enqueue(centroDestino);

        salonIdx = dest.idx;
        salonNorte = dest.norte;

        estadoActual = Estado.Ruta;
        AvanzarRuta();
    }

    void AvanzarRuta()
    {
        if (rutaActual.Count == 0) { IniciarEspera(); return; }
        SetDestino(rutaActual.Dequeue(), velCaminata);
    }

    void LogicaRuta()
    {
        if (Vector2.Distance(rb.position, destinoActual) < distanciaLlegada)
        {
            if (rutaActual.Count > 0)
                AvanzarRuta();
            else
                IniciarEspera();
        }
    }

    // ==================== PERSECUCIÓN ====================
    void IniciarPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        rutaActual.Clear();
        MusicaManager.Instancia?.IniciarPersecucion();
    }

    void LogicaPersecucion()
    {
        if (jugador == null) return;

        float velUsar = (estadoAntesDeses == Estado.Desesperado && timerDuraDeses > 0) ? velDesesperada : velPersecucion;
        bool loVe = JugadorDetectado() || JugadorDetectadoDeses();

        if (loVe) ultimoPuntoConocido = jugador.position;

        // No hacemos chequeo de distancia aquí (lo movimos a FixedUpdate para mayor precisión)
        if (loVe)
            SetDestino(jugador.position, velUsar);
        else
        {
            SetDestino(ultimoPuntoConocido, velUsar);
            if (Vector2.Distance(rb.position, ultimoPuntoConocido) < distanciaLlegada * 1.5f)
                IniciarBusqueda();
        }
    }

    void IniciarBusqueda()
    {
        estadoActual = Estado.Buscando;
        indiceBusqueda = 0;
        timerEspera = 0f;
        esperandoEnPunto = false;
        MusicaManager.Instancia?.TerminarPersecucion();

        puntosBusqueda.Clear();
        for (int i = 0; i < 3; i++)
        {
            float ang = (360f / 3) * i * Mathf.Deg2Rad;
            puntosBusqueda.Add(ultimoPuntoConocido + new Vector2(Mathf.Cos(ang) * 4f, Mathf.Sin(ang) * 4f));
        }
    }

    void LogicaBusqueda()
    {
        if (puntosBusqueda.Count == 0) { TerminarEnSalonCercano(); return; }

        Vector2 punto = puntosBusqueda[indiceBusqueda];
        if (!esperandoEnPunto)
        {
            SetDestino(punto, velCaminata * 0.8f);
            if (Vector2.Distance(rb.position, punto) < distanciaLlegada)
            {
                esperandoEnPunto = true;
                timerEspera = 0f;
                moviendose = false;
                ActualizarAnimacion(Vector2.zero);
            }
        }
        else
        {
            timerEspera += Time.deltaTime;
            if (timerEspera >= 1f)
            {
                esperandoEnPunto = false;
                indiceBusqueda++;
                if (indiceBusqueda >= puntosBusqueda.Count)
                    TerminarEnSalonCercano();
            }
        }
    }

    void TerminarEnSalonCercano()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        float menorD = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            if (i == 4) continue;
            float dN = Vector2.Distance(rb.position, salonesNorte[i].centro);
            float dS = Vector2.Distance(rb.position, salonesSur[i].centro);
            if (dN < menorD) { menorD = dN; salonIdx = i; salonNorte = true; }
            if (dS < menorD) { menorD = dS; salonIdx = i; salonNorte = false; }
        }
        ElegirSiguienteSalonYRuta();
    }

    // ==================== ATRAPAR ====================
    void Atrapar()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        timerDuraDeses = 0f;
        estadoAntesDeses = Estado.Esperando;

        estadoActual = Estado.Atrapado;
        moviendose = false;
        destinoActual = rb.position;   // evitar movimiento residual

        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (rbJugador != null)
        {
            rbJugador.linearVelocity = Vector2.zero;
            rbJugador.angularVelocity = 0f;
            rbJugador.bodyType = RigidbodyType2D.Kinematic;
        }

        if (pmJugador != null) pmJugador.enabled = false;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.AplicarCastigo(castigo);

        // Teletransportar jugador al salón más cercano
        float menorD = float.MaxValue;
        for (int i = 0; i < 8; i++)
        {
            if (i == 4) continue;
            float dN = Vector2.Distance(rb.position, salonesNorte[i].centro);
            float dS = Vector2.Distance(rb.position, salonesSur[i].centro);
            if (dN < menorD) { menorD = dN; salonIdx = i; salonNorte = true; }
            if (dS < menorD) { menorD = dS; salonIdx = i; salonNorte = false; }
        }

        Vector2 centroSalon = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
        Vector2 posJugador = centroSalon + new Vector2(1.5f, 0f);

        if (jugador != null)
            jugador.position = new Vector3(posJugador.x, posJugador.y, 0f);

        IniciarEncierro();
    }

    void IniciarEncierro()
    {
        estadoActual = Estado.Atrapado;
        moviendose = false;
        timerEncierroActual = 0f;
        jugadorFijoEnSalon = true;
        ActualizarAnimacion(Vector2.zero);
    }

    void LogicaEncierro()
    {
        timerEncierroActual += Time.deltaTime;
        moviendose = false;
        ActualizarAnimacion(Vector2.zero);

        if (timerEncierroActual >= tiempoEncierro)
        {
            jugadorFijoEnSalon = false;
            if (rbJugador != null) rbJugador.bodyType = RigidbodyType2D.Dynamic;
            if (pmJugador != null)
            {
                pmJugador.enabled = true;
                pmJugador.AplicarSlowdown(factorSlowdown, tiempoSlowdown);
            }
            ElegirProximoDeses();
            ElegirSiguienteSalonYRuta();
        }
    }

    // ==================== MOVIMIENTO ====================
    void FixedUpdate()
    {
        // CHEQUEO DE ATRAPAR EN FIXEDUPDATE (para modo normal y desesperado)
        if (estadoActual == Estado.Persiguiendo && jugador != null)
        {
            float dist = Vector2.Distance(rb.position, jugador.position);
            if (dist < distanciaLlegada + 0.9f)   // tolerancia generosa
            {
                Atrapar();
                return;
            }
        }

        if (!moviendose || rb == null) return;

        Vector2 nuevaPos = Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime);
        rb.MovePosition(nuevaPos);

        if (estadoActual == Estado.Atrapado && jugadorFijoEnSalon && rbJugador != null)
        {
            Vector2 centro = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
            rbJugador.MovePosition(centro + new Vector2(1.5f, 0f));
        }
    }

    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual = speed;
        moviendose = true;

        Vector2 dir = (destino - rb.position).normalized;
        if (dir.magnitude > 0.01f)
        {
            direccionActual = dir;
            ActualizarAnimacion(dir);
        }
    }

    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;

        string nueva = dir.magnitude < 0.01f ? "Idleq" :
                       Mathf.Abs(dir.x) >= Mathf.Abs(dir.y) ?
                       (dir.x > 0 ? "WalkRightr" : "WalkLeftt") :
                       (dir.y > 0 ? "WalkUpw" : "WalkDowne");

        if (nueva != animActual)
        {
            animActual = nueva;
            animator.Play(nueva);
        }
    }

    void ActualizarSorting()
    {
        if (sr != null)
            sr.sortingOrder = 300 + Mathf.RoundToInt(-transform.position.y * 10f);
    }

    List<(int idx, bool norte)> SalonesValidos()
    {
        var lista = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
            if (i != 4)
            {
                lista.Add((i, true));
                lista.Add((i, false));
            }
        return lista;
    }

    void OnDrawGizmos() { }
}