using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
//  EL VIDEOJUEGOS  v5
//  FIXES v5:
//  - BUG #1: ultimoPuntoConocido ya NO se actualiza cada frame en Update().
//            Solo se actualiza cuando realmente lo detecta (en LogicaPersecucion y Desesperado).
//            Esto evitaba que el NPC "copiara" movimientos del jugador.
//  - BUG #2: SetDestino ya NO recalcula la animacion cuando el NPC esta muy cerca
//            del destino (< 0.3f). Esto evita el flickering/animacion caminando de lado.
//  - BUG #3: velUsar en LogicaPersecucion ya NO usa timerDuraDeses para determinar
//            velocidad fuera del modo desesperado. Ahora solo usa velDesesperada si
//            el estado anterior FUE desesperado y el timer aun esta activo.
// ══════════════════════════════════════════════════════
public class ElVideojuegos : MonoBehaviour
{
    [Header("Rutina normal")]
    public float tiempoMinEnSalon = 15f;
    public float tiempoMaxEnSalon = 25f;

    [Header("Visión normal")]
    [Range(60f, 180f)]
    public float anguloNormal   = 110f;
    public float alcanceNormal  = 6f;
    public float alcanceSalon   = 9f;

    [Header("Visión desesperada")]
    [Range(120f, 359f)]
    public float anguloDeses    = 270f;
    public float alcanceDeses   = 14f;
    public float alcanceSalonD  = 16f;

    [Header("Movimiento")]
    public float velCaminata      = 2f;
    public float velPersecucion   = 3.5f;
    public float velDesesperada   = 5.5f;
    public float distanciaLlegada = 0.5f;

    [Header("Modo desesperado")]
    public float duracionDeses = 20f;
    private float[] tiemposEspera = { 7f, 10f, 15f, 20f, 30f };

    [Header("Castigo")]
    public int   castigo           = 20;
    public float tiempoEncierro    = 15f;
    public float tiempoSlowdown    = 60f;
    public float factorSlowdown    = 0.35f;
    public string tagJugador       = "Player";

    // Constantes edificio
    const float SAL_W         = 20f;
    const float xIzq          = -81f;
    const float Y_NORTE       =  18f;
    const float Y_SUR         = -18f;
    const float Y_DIV_N       =   9f;
    const float Y_DIV_S       =  -9f;
    const float Y_PASILLO     =   0f;
    const float OFFSET_PUERTA =  0.5f;

    enum Estado { Esperando, Ruta, Persiguiendo, Buscando, Desesperado, YendoAEncerrar, Atrapado }
    Estado estadoActual = Estado.Esperando;
    Estado estadoAntesDeses = Estado.Esperando;

    struct PuntoSalon { public Vector2 centro; public Vector2 puerta; }
    PuntoSalon[] salonesNorte = new PuntoSalon[8];
    PuntoSalon[] salonesSur   = new PuntoSalon[8];

    int   salonIdx   = 0;
    bool  salonNorte = true;

    float timerSalon    = 0f;
    float duracionSalon = 0f;

    float timerProxDeses  = 0f;
    float timerDuraDeses  = 0f;
    float proxDesesTiempo = 0f;

    Queue<Vector2> rutaActual = new Queue<Vector2>();

    Vector2       ultimoPuntoConocido;
    bool          tienePuntoConocido = false;
    List<Vector2> puntosBusqueda   = new List<Vector2>();
    int           indiceBusqueda   = 0;
    float         timerEspera      = 0f;
    bool          esperandoEnPunto = false;

    List<(int idx, bool norte)> salonesARevisar = new List<(int, bool)>();
    int indiceSalonDeses = 0;
    bool faseInicialDeses = true;

    Vector2 destinoActual   = Vector2.zero;
    float   speedActual     = 0f;
    bool    moviendose      = false;
    Vector2 direccionActual = Vector2.right;
    bool    enPasillo       = false;

    float timerEncierroActual = 0f;
    bool  jugadorFijoEnSalon  = false;

    // Offset de arrastre (posición del jugador relativa al NPC)
    Vector2 offsetArrastre = new Vector2(1.2f, 0f);

    string animActual = "";

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Animator       animator;
    Transform      jugador;
    Rigidbody2D    rbJugador;
    PlayerMovement pmJugador;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        if (sr == null)       sr       = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation          = RigidbodyInterpolation2D.Interpolate;
        }

        GameObject goJ = GameObject.FindGameObjectWithTag(tagJugador);
        if (goJ != null)
        {
            jugador    = goJ.transform;
            rbJugador  = goJ.GetComponent<Rigidbody2D>();
            pmJugador  = goJ.GetComponent<PlayerMovement>();
            Debug.Log("NPC encontró jugador: " + goJ.name);

            if (rbJugador != null)
            {
                rbJugador.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rbJugador.interpolation          = RigidbodyInterpolation2D.Interpolate;
            }
        }
        else
        {
            Debug.LogError("NPC NO encontró ningún objeto con tag: " + tagJugador);
        }

        for (int i = 0; i < 8; i++)
        {
            float sx = xIzq + 1f + SAL_W * i + SAL_W / 2f;
            salonesNorte[i] = new PuntoSalon
            {
                centro = new Vector2(sx, Y_NORTE),
                puerta = new Vector2(sx, Y_DIV_N + OFFSET_PUERTA)
            };
            salonesSur[i] = new PuntoSalon
            {
                centro = new Vector2(sx, Y_SUR),
                puerta = new Vector2(sx, Y_DIV_S - OFFSET_PUERTA)
            };
        }

        // Inicializar ultimoPuntoConocido con posicion del jugador al arrancar
        if (jugador != null)
            ultimoPuntoConocido = jugador.position;

        ElegirSalonInicial();
        ElegirProximoDeses();
    }

    void ElegirProximoDeses()
    {
        proxDesesTiempo = tiemposEspera[Random.Range(0, tiemposEspera.Length)];
        timerProxDeses  = 0f;
    }

    void Update()
    {
        enPasillo = Mathf.Abs(transform.position.y) < 8.5f;

        // ✅ FIX BUG #1: Se ELIMINÓ la línea que actualizaba ultimoPuntoConocido SIEMPRE.
        // Antes: if (jugador != null) ultimoPuntoConocido = jugador.position;
        // Eso hacía que el NPC SIEMPRE supiera dónde estás, incluso sin verte,
        // lo que causaba que "copiara" tus movimientos y nunca perdiera el rastro.
        // Ahora solo se actualiza cuando realmente lo detecta (dentro de LogicaPersecucion
        // y LogicaDesesperado).

        if (estadoActual != Estado.Desesperado &&
            estadoActual != Estado.YendoAEncerrar &&
            estadoActual != Estado.Atrapado)
        {
            timerProxDeses += Time.deltaTime;
            if (timerProxDeses >= proxDesesTiempo)
                IniciarModoDesesperado();
        }

        switch (estadoActual)
        {
            case Estado.Esperando:
                LogicaEspera();
                if (JugadorDetectado()) IniciarPersecucion();
                break;
            case Estado.Ruta:
                LogicaRuta();
                if (JugadorDetectado()) IniciarPersecucion();
                break;
            case Estado.Persiguiendo:
                LogicaPersecucion();
                break;
            case Estado.Buscando:
                LogicaBusqueda();
                if (JugadorDetectado()) IniciarPersecucion();
                break;
            case Estado.Desesperado:
                LogicaDesesperado();
                break;
            case Estado.YendoAEncerrar:
                LogicaYendoAEncerrar();
                break;
            case Estado.Atrapado:
                LogicaEncierro();
                break;
        }

        ActualizarSorting();
    }

    // ─────────────────────────────────────────
    //  MODO DESESPERADO
    // ─────────────────────────────────────────
    void IniciarModoDesesperado()
    {
        estadoAntesDeses = estadoActual;
        estadoActual     = Estado.Desesperado;
        timerDuraDeses   = 0f;
        rutaActual.Clear();

        MusicaManager.Instancia?.IniciarPersecucion();

        var todos = SalonesValidos();
        for (int i = todos.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = todos[i]; todos[i] = todos[j]; todos[j] = tmp;
        }
        salonesARevisar  = todos;
        indiceSalonDeses = 0;

        faseInicialDeses = true;
        SetDestino(ultimoPuntoConocido, velDesesperada);
    }

    void LogicaDesesperado()
    {
        timerDuraDeses += Time.deltaTime;

        if (JugadorDetectadoDeses())
        {
            // Actualizar posición conocida antes de cambiar estado
            if (jugador != null) ultimoPuntoConocido = jugador.position;
            IniciarPersecucionDeses();
            return;
        }

        if (timerDuraDeses >= duracionDeses)
        {
            TerminarModoDesesperado();
            return;
        }

        if (faseInicialDeses)
        {
            // En modo desesperado SÍ actualizamos ultimoPuntoConocido mientras busca
            if (jugador != null)
            {
                ultimoPuntoConocido = jugador.position;
                SetDestino(jugador.position, velDesesperada);
            }

            if (Vector2.Distance(rb.position, ultimoPuntoConocido) <= distanciaLlegada * 2f)
            {
                faseInicialDeses = false;
                indiceSalonDeses = 0;
                IrSiguienteSalonDeses();
            }
            return;
        }

        if (moviendose && Vector2.Distance(rb.position, destinoActual) > distanciaLlegada)
            return;

        indiceSalonDeses++;
        if (indiceSalonDeses >= salonesARevisar.Count)
        {
            faseInicialDeses = true;
            SetDestino(ultimoPuntoConocido, velDesesperada);
            indiceSalonDeses = 0;
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
        Vector2 centro = sal.norte
            ? salonesNorte[sal.idx].centro
            : salonesSur[sal.idx].centro;
        SetDestino(centro, velDesesperada);
    }

    void IniciarPersecucionDeses()
    {
        estadoActual = Estado.Persiguiendo;
        rutaActual.Clear();
        MusicaManager.Instancia?.IniciarPersecucion();
    }

    void TerminarModoDesesperado()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        ElegirProximoDeses();
        TerminarEnSalonCercano();
    }

    // ─────────────────────────────────────────
    //  DETECCIÓN
    // ─────────────────────────────────────────
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
        float a = Vector2.Angle(direccionActual, diff.normalized);
        return a <= anguloDeses / 2f;
    }

    bool JugadorEnSalon(float alcance)
    {
        if (jugador == null) return false;
        if (Vector2.Distance(rb.position, jugador.position) > alcance) return false;
        bool jugadorEnPasillo = Mathf.Abs(jugador.position.y) < 8.5f;
        if (jugadorEnPasillo) return false;
        bool npcEnNorte = transform.position.y > 0f;
        bool jugEnNorte = jugador.position.y  > 0f;
        return npcEnNorte == jugEnNorte;
    }

    bool JugadorEnCono(float angulo, float alcance)
    {
        if (jugador == null) return false;
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcance) return false;
        return Vector2.Angle(direccionActual, diff.normalized) <= angulo / 2f;
    }

    // ─────────────────────────────────────────
    //  ESPERA EN SALÓN
    // ─────────────────────────────────────────
    void ElegirSalonInicial()
    {
        var validos = SalonesValidos();
        var elegido = validos[Random.Range(0, validos.Count)];
        salonIdx   = elegido.idx;
        salonNorte = elegido.norte;
        Vector2 pos = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        IniciarEspera();
    }

    void IniciarEspera()
    {
        estadoActual  = Estado.Esperando;
        moviendose    = false;
        timerSalon    = 0f;
        duracionSalon = Random.Range(tiempoMinEnSalon, tiempoMaxEnSalon);
        ActualizarAnimacion(Vector2.zero);
    }

    void LogicaEspera()
    {
        timerSalon += Time.deltaTime;
        if (timerSalon >= duracionSalon)
            ElegirSiguienteSalonYRuta();
    }

    // ─────────────────────────────────────────
    //  RUTA
    // ─────────────────────────────────────────
    void ElegirSiguienteSalonYRuta()
    {
        var validos = SalonesValidos();
        validos.RemoveAll(v => v.idx == salonIdx && v.norte == salonNorte);
        var dest = validos[Random.Range(0, validos.Count)];
        IrASalon(dest.idx, dest.norte, velCaminata, false);
    }

    void IrASalon(int destIdx, bool destNorte, float velocidad, bool esCaptura)
    {
        Vector2 puertaOrigen   = salonNorte ? salonesNorte[salonIdx].puerta : salonesSur[salonIdx].puerta;
        Vector2 pasilloOrigen  = new Vector2(puertaOrigen.x, Y_PASILLO);
        Vector2 centroDestino  = destNorte ? salonesNorte[destIdx].centro : salonesSur[destIdx].centro;
        Vector2 puertaDestino  = destNorte ? salonesNorte[destIdx].puerta : salonesSur[destIdx].puerta;
        Vector2 pasilloDestino = new Vector2(puertaDestino.x, Y_PASILLO);
        Vector2 entradaDestino = destNorte
            ? new Vector2(puertaDestino.x, Y_DIV_N - OFFSET_PUERTA)
            : new Vector2(puertaDestino.x, Y_DIV_S + OFFSET_PUERTA);

        rutaActual.Clear();
        rutaActual.Enqueue(puertaOrigen);
        rutaActual.Enqueue(pasilloOrigen);
        rutaActual.Enqueue(pasilloDestino);
        rutaActual.Enqueue(entradaDestino);
        rutaActual.Enqueue(centroDestino);

        salonIdx   = destIdx;
        salonNorte = destNorte;

        speedActual  = velocidad;
        estadoActual = esCaptura ? Estado.YendoAEncerrar : Estado.Ruta;
        AvanzarRuta();
    }

    void AvanzarRuta()
    {
        if (rutaActual.Count == 0)
        {
            if (estadoActual == Estado.YendoAEncerrar)
                IniciarEncierro();
            else
                IniciarEspera();
            return;
        }
        SetDestino(rutaActual.Dequeue(), speedActual);
    }

    void LogicaRuta()
    {
        if (Vector2.Distance(rb.position, destinoActual) < distanciaLlegada)
        {
            if (rutaActual.Count > 0) AvanzarRuta();
            else IniciarEspera();
        }
    }

    // ─────────────────────────────────────────
    //  PERSECUCIÓN
    // ─────────────────────────────────────────
    void IniciarPersecucion()
    {
        estadoActual = Estado.Persiguiendo;
        rutaActual.Clear();
        MusicaManager.Instancia?.IniciarPersecucion();
    }

    void LogicaPersecucion()
    {
        if (jugador == null) return;

        // ✅ FIX BUG #3: Solo usa velDesesperada si el estado ANTERIOR fue Desesperado
        // y el timer aun esta dentro del rango. En persecucion normal usa velPersecucion.
        float velUsar = (estadoAntesDeses == Estado.Desesperado &&
                         timerDuraDeses > 0f &&
                         timerDuraDeses < duracionDeses)
            ? velDesesperada
            : velPersecucion;

        bool loVe = JugadorDetectado() || JugadorDetectadoDeses();

        // ✅ FIX BUG #1 (parte 2): Solo actualiza ultimoPuntoConocido cuando lo ve
        if (loVe) ultimoPuntoConocido = jugador.position;

        float dist = Vector2.Distance(rb.position, jugador.position);
        if (dist < distanciaLlegada) { Atrapar(); return; }

        if (loVe)
            SetDestino(jugador.position, velUsar);
        else
        {
            SetDestino(ultimoPuntoConocido, velUsar);
            if (Vector2.Distance(rb.position, ultimoPuntoConocido) < distanciaLlegada)
                IniciarBusqueda();
        }
    }

    // ─────────────────────────────────────────
    //  BÚSQUEDA NORMAL
    // ─────────────────────────────────────────
    void IniciarBusqueda()
    {
        estadoActual     = Estado.Buscando;
        indiceBusqueda   = 0;
        timerEspera      = 0f;
        esperandoEnPunto = false;
        MusicaManager.Instancia?.TerminarPersecucion();

        puntosBusqueda.Clear();
        for (int i = 0; i < 3; i++)
        {
            float ang = (360f / 3) * i * Mathf.Deg2Rad;
            puntosBusqueda.Add(ultimoPuntoConocido + new Vector2(
                Mathf.Cos(ang) * 4f, Mathf.Sin(ang) * 4f));
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
                timerEspera      = 0f;
                moviendose       = false;
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

    // ─────────────────────────────────────────
    //  ATRAPAR → iniciar viaje al salón
    // ─────────────────────────────────────────
    void Atrapar()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        timerDuraDeses   = 0f;
        estadoAntesDeses = Estado.Esperando; // resetear para que no arrastre velDesesperada

        if (pmJugador != null) pmJugador.enabled = false;
        if (rbJugador != null)
        {
            rbJugador.linearVelocity = Vector2.zero;
            rbJugador.bodyType       = RigidbodyType2D.Kinematic;
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.AplicarCastigo(castigo);

        if (rb != null) rb.linearVelocity = Vector2.zero;

        offsetArrastre = new Vector2(direccionActual.x >= 0 ? 1.2f : -1.2f, 0f);

        float menorD = float.MaxValue;
        int   salidx = salonIdx;
        bool  salNrt = salonNorte;
        for (int i = 0; i < 8; i++)
        {
            if (i == 4) continue;
            float dN = Vector2.Distance(rb.position, salonesNorte[i].centro);
            float dS = Vector2.Distance(rb.position, salonesSur[i].centro);
            if (dN < menorD) { menorD = dN; salidx = i; salNrt = true; }
            if (dS < menorD) { menorD = dS; salidx = i; salNrt = false; }
        }

        IrASalon(salidx, salNrt, velPersecucion, true);
    }

    // ─────────────────────────────────────────
    //  YENDO AL SALÓN A ENCERRAR
    // ─────────────────────────────────────────
    void LogicaYendoAEncerrar()
    {
        if (Vector2.Distance(rb.position, destinoActual) < distanciaLlegada)
        {
            if (rutaActual.Count > 0)
                AvanzarRuta();
            else
                IniciarEncierro();
        }
    }

    // ─────────────────────────────────────────
    //  ENCIERRO
    // ─────────────────────────────────────────
    void IniciarEncierro()
    {
        estadoActual        = Estado.Atrapado;
        moviendose          = false;
        timerEncierroActual = 0f;
        jugadorFijoEnSalon  = true;

        if (rb != null) rb.linearVelocity = Vector2.zero;
        ActualizarAnimacion(Vector2.zero);

        Vector2 centroSalon = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
        Vector2 posJugador  = centroSalon + new Vector2(1.5f, 0f);
        if (rbJugador != null)
        {
            rbJugador.MovePosition(posJugador);
            rbJugador.linearVelocity = Vector2.zero;
        }
        else if (jugador != null)
        {
            jugador.position = new Vector3(posJugador.x, posJugador.y, 0f);
        }
    }

    void LogicaEncierro()
    {
        moviendose = false;
        ActualizarAnimacion(Vector2.zero);
        timerEncierroActual += Time.deltaTime;

        if (timerEncierroActual >= tiempoEncierro)
        {
            jugadorFijoEnSalon = false;

            if (rbJugador != null)
                rbJugador.bodyType = RigidbodyType2D.Dynamic;

            if (pmJugador != null)
            {
                pmJugador.enabled = true;
                pmJugador.AplicarSlowdown(factorSlowdown, tiempoSlowdown);
            }

            ElegirProximoDeses();
            ElegirSiguienteSalonYRuta();
        }
    }

    // ─────────────────────────────────────────
    //  FIXED UPDATE
    // ─────────────────────────────────────────
    void FixedUpdate()
    {
        if (moviendose && rb != null)
            rb.MovePosition(Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime));

        if (estadoActual == Estado.YendoAEncerrar && rbJugador != null)
        {
            Vector2 destJugador = rb.position + offsetArrastre;
            rbJugador.MovePosition(Vector2.MoveTowards(rbJugador.position, destJugador, speedActual * Time.fixedDeltaTime));
            rbJugador.linearVelocity = Vector2.zero;
        }

        if (estadoActual == Estado.Atrapado && jugadorFijoEnSalon && rbJugador != null)
        {
            Vector2 centroSalon = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
            Vector2 posJugador  = centroSalon + new Vector2(1.5f, 0f);
            rbJugador.MovePosition(posJugador);
            rbJugador.linearVelocity = Vector2.zero;
        }
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────
    List<(int idx, bool norte)> SalonesValidos()
    {
        var lista = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
            if (i != 4) { lista.Add((i, true)); lista.Add((i, false)); }
        return lista;
    }

    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual   = speed;
        moviendose    = true;
        Vector2 dir  = (destino - rb.position).normalized;
        float   dist = Vector2.Distance(rb.position, destino);

        // ✅ FIX BUG #2: Solo actualiza la direccion/animacion si esta suficientemente lejos.
        // Antes recalculaba cada frame aunque estuviera a 0.01f del destino,
        // causando que la animacion flippeara entre WalkLeft/WalkDown (caminaba de lado).
        if (dir.magnitude > 0.01f && dist > 0.3f)
        {
            direccionActual = dir;
            ActualizarAnimacion(dir);
        }
    }

    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;
        string nueva;
        if (dir.magnitude < 0.01f) nueva = "Idleq";
        else if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) nueva = dir.x > 0 ? "WalkRightr" : "WalkLeftt";
        else nueva = dir.y > 0 ? "WalkUpw" : "WalkDowne";

        if (nueva != animActual) { animActual = nueva; animator.Play(nueva); }
    }

    void ActualizarSorting()
    {
        if (sr != null) sr.sortingOrder = 300 + Mathf.RoundToInt(-transform.position.y * 10f);
    }

    void OnDrawGizmos()
    {
        bool deses = Application.isPlaying && estadoActual == Estado.Desesperado;
        float angulo = deses ? anguloDeses  : anguloNormal;
        float alc    = deses ? alcanceDeses : alcanceNormal;

        Gizmos.color = deses
            ? new Color(1f, 0f, 0f, 0.8f)
            : new Color(0.2f, 0.8f, 1f, 0.5f);

        Vector3 dir   = Application.isPlaying ? (Vector3)direccionActual : transform.right;
        float   mitad = angulo / 2f;
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0, mitad) * dir * alc);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0,-mitad) * dir * alc);
        for (int i = 0; i < 16; i++)
        {
            float a1 = -mitad + angulo * (i / 16f);
            float a2 = -mitad + angulo * ((i+1) / 16f);
            Gizmos.DrawLine(
                transform.position + Quaternion.Euler(0,0,a1)*dir*alc,
                transform.position + Quaternion.Euler(0,0,a2)*dir*alc);
        }
    }
}