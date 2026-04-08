using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
//  EL VIDEOJUEGOS  v1
//  Patrulla normal (más light que Stanis/Pelón).
//  Cada X segundos (random entre valores fijos) entra
//  en MODO DESESPERADO 20 seg: revisa pasillo + salones
//  con visión y velocidad máxima. Luego regresa a normal.
// ══════════════════════════════════════════════════════
public class ElVideojuegos : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  RUTINA NORMAL
    // ─────────────────────────────────────────
    [Header("Rutina normal")]
    public float tiempoMinEnSalon = 15f;
    public float tiempoMaxEnSalon = 25f;

    // ─────────────────────────────────────────
    //  VISIÓN NORMAL (más light)
    // ─────────────────────────────────────────
    [Header("Visión normal")]
    [Range(60f, 180f)]
    public float anguloNormal   = 100f;
    public float alcanceNormal  = 5f;
    public float alcanceSalon   = 8f;

    // ─────────────────────────────────────────
    //  VISIÓN DESESPERADA
    // ─────────────────────────────────────────
    [Header("Visión desesperada")]
    [Range(120f, 359f)]
    public float anguloDeses    = 270f;   // mucho mayor que Stanis
    public float alcanceDeses   = 14f;
    public float alcanceSalonD  = 16f;

    // ─────────────────────────────────────────
    //  VELOCIDADES
    // ─────────────────────────────────────────
    [Header("Movimiento")]
    public float velCaminata      = 2f;    // más light que Stanis
    public float velPersecucion   = 3.5f;
    public float velDesesperada   = 5.5f;  // más que Stanis
    public float distanciaLlegada = 0.5f;

    // ─────────────────────────────────────────
    //  MODO DESESPERADO
    // ─────────────────────────────────────────
    [Header("Modo desesperado")]
    public float duracionDeses = 20f;
    // Posibles tiempos de espera antes del siguiente modo desesperado
    private float[] tiemposEspera = { 7f, 10f, 15f, 20f, 30f };

    // ─────────────────────────────────────────
    //  CASTIGO
    // ─────────────────────────────────────────
    [Header("Castigo")]
    public int   castigo        = 20;
    public float tiempoEncierro = 10f;
    public string tagJugador    = "Player";

    // ─────────────────────────────────────────
    //  CONSTANTES EDIFICIO (igual que Pelón)
    // ─────────────────────────────────────────
    const float SAL_W         = 20f;
    const float xIzq          = -81f;
    const float Y_NORTE       =  18f;
    const float Y_SUR         = -18f;
    const float Y_DIV_N       =   9f;
    const float Y_DIV_S       =  -9f;
    const float Y_PASILLO     =   0f;
    const float OFFSET_PUERTA =  0.5f;

    // ─────────────────────────────────────────
    //  ESTADOS
    // ─────────────────────────────────────────
    enum Estado { Esperando, Ruta, Persiguiendo, Buscando, Desesperado, Atrapado }
    Estado estadoActual = Estado.Esperando;

    // Antes del modo desesperado guardamos el estado para volver
    Estado estadoAntesDeses = Estado.Esperando;

    struct PuntoSalon { public Vector2 centro; public Vector2 puerta; }
    PuntoSalon[] salonesNorte = new PuntoSalon[8];
    PuntoSalon[] salonesSur   = new PuntoSalon[8];

    int   salonIdx   = 0;
    bool  salonNorte = true;

    float timerSalon    = 0f;
    float duracionSalon = 0f;

    // Timer modo desesperado
    float timerProxDeses  = 0f;  // cuenta hasta disparar el modo
    float timerDuraDeses  = 0f;  // cuenta los 20 seg activos
    float proxDesesTiempo = 0f;  // valor elegido al azar

    // Ruta
    Queue<Vector2> rutaActual = new Queue<Vector2>();

    // Búsqueda normal / desesperada
    Vector2       ultimoPuntoConocido;
    List<Vector2> puntosBusqueda   = new List<Vector2>();
    int           indiceBusqueda   = 0;
    float         timerEspera      = 0f;
    bool          esperandoEnPunto = false;

    // Búsqueda desesperada por salones
    List<(int idx, bool norte)> salonesARevisar = new List<(int, bool)>();
    int indiceSalonDeses = 0;
    bool revisandoPasillo = false; // primero pasillo, luego salones

    Vector2 destinoActual   = Vector2.zero;
    float   speedActual     = 0f;
    bool    moviendose      = false;
    Vector2 direccionActual = Vector2.right;
    bool    enPasillo       = false;

    float timerEncierroActual = 0f;

    string animActual = "";

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Animator       animator;
    Transform      jugador;

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

        GameObject goJ = GameObject.FindGameObjectWithTag(tagJugador);
        if (goJ != null) jugador = goJ.transform;

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

        ElegirSalonInicial();
        ElegirProximoDeses(); // primer timer desesperado
    }

    // Elige al azar uno de los tiempos fijos para el próximo modo
    void ElegirProximoDeses()
    {
        proxDesesTiempo = tiemposEspera[Random.Range(0, tiemposEspera.Length)];
        timerProxDeses  = 0f;
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        enPasillo = Mathf.Abs(transform.position.y) < 8.5f;

        // ── Tick del timer desesperado (solo cuando NO está ya desesperado ni atrapado)
        if (estadoActual != Estado.Desesperado && estadoActual != Estado.Atrapado)
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
        estadoAntesDeses  = estadoActual;
        estadoActual      = Estado.Desesperado;
        timerDuraDeses    = 0f;
        rutaActual.Clear();

        MusicaManager.Instancia?.IniciarPersecucion();

        // Armar lista de salones a revisar (todos excepto salón 4)
        salonesARevisar.Clear();
        var todos = SalonesValidos();
        // Shuffle simple
        for (int i = todos.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = todos[i]; todos[i] = todos[j]; todos[j] = tmp;
        }
        salonesARevisar = todos;
        indiceSalonDeses = 0;
        revisandoPasillo = true;

        // Primero va al pasillo
        SetDestino(new Vector2(transform.position.x, Y_PASILLO), velDesesperada);
    }

    void LogicaDesesperado()
    {
        timerDuraDeses += Time.deltaTime;

        // Detección con visión desesperada
        if (JugadorDetectadoDeses())
        {
            IniciarPersecucionDeses();
            return;
        }

        // Tiempo agotado → volver a normal
        if (timerDuraDeses >= duracionDeses)
        {
            TerminarModoDesesperado();
            return;
        }

        // Navegación: pasillo → salones
        if (moviendose && Vector2.Distance(rb.position, destinoActual) > distanciaLlegada)
            return; // en camino

        // Llegó al punto actual
        if (revisandoPasillo)
        {
            // Recorre el pasillo de izq a der y der a izq rápido
            // Pasamos a revisar salones
            revisandoPasillo = false;
            IrSiguienteSalonDeses();
        }
        else
        {
            indiceSalonDeses++;
            if (indiceSalonDeses >= salonesARevisar.Count)
            {
                // Ya revisó todo, vuelve al pasillo y repite si queda tiempo
                revisandoPasillo = true;
                SetDestino(new Vector2(transform.position.x, Y_PASILLO), velDesesperada);
            }
            else
            {
                IrSiguienteSalonDeses();
            }
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
        // Persigue desde modo desesperado (con velocidad desesperada)
        estadoActual = Estado.Persiguiendo;
        rutaActual.Clear();
        MusicaManager.Instancia?.IniciarPersecucion();
    }

    void TerminarModoDesesperado()
    {
        MusicaManager.Instancia?.TerminarPersecucion();
        ElegirProximoDeses();

        // Volver al salón más cercano y retomar rutina
        TerminarEnSalonCercano();
    }

    // ─────────────────────────────────────────
    //  DETECCIÓN
    // ─────────────────────────────────────────
    bool JugadorDetectado()
    {
        if (jugador == null) return false;
        if (enPasillo) return JugadorEnCono(anguloNormal, alcanceNormal);
        return JugadorEnMiSalon(alcanceSalon);
    }

    bool JugadorDetectadoDeses()
    {
        if (jugador == null) return false;
        // En modo desesperado la visión es enorme en cualquier lugar
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcanceDeses) return false;
        float a = Vector2.Angle(direccionActual, diff.normalized);
        return a <= anguloDeses / 2f;
    }

    bool JugadorEnMiSalon(float alcance)
    {
        if (jugador == null || enPasillo) return false;
        if (Vector2.Distance(rb.position, jugador.position) > alcance) return false;
        bool jugadorEnPasillo = Mathf.Abs(jugador.position.y) < 8.5f;
        if (jugadorEnPasillo) return false;
        bool peloEnNorte = transform.position.y > 0f;
        bool jugEnNorte  = jugador.position.y  > 0f;
        if (peloEnNorte != jugEnNorte) return false;
        float miX  = salonNorte ? salonesNorte[salonIdx].centro.x : salonesSur[salonIdx].centro.x;
        return Mathf.Abs(jugador.position.x - miX) < SAL_W / 2f - 0.5f;
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

        Vector2 puertaOrigen   = salonNorte ? salonesNorte[salonIdx].puerta : salonesSur[salonIdx].puerta;
        Vector2 pasilloOrigen  = new Vector2(puertaOrigen.x, Y_PASILLO);
        Vector2 centroDestino  = dest.norte ? salonesNorte[dest.idx].centro : salonesSur[dest.idx].centro;
        Vector2 puertaDestino  = dest.norte ? salonesNorte[dest.idx].puerta : salonesSur[dest.idx].puerta;
        Vector2 pasilloDestino = new Vector2(puertaDestino.x, Y_PASILLO);
        Vector2 entradaDestino = dest.norte
            ? new Vector2(puertaDestino.x, Y_DIV_N - OFFSET_PUERTA)
            : new Vector2(puertaDestino.x, Y_DIV_S + OFFSET_PUERTA);

        rutaActual.Clear();
        rutaActual.Enqueue(puertaOrigen);
        rutaActual.Enqueue(pasilloOrigen);
        rutaActual.Enqueue(pasilloDestino);
        rutaActual.Enqueue(entradaDestino);
        rutaActual.Enqueue(centroDestino);

        salonIdx   = dest.idx;
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

        // Velocidad según si venía de modo desesperado
        float velUsar = (timerDuraDeses > 0f && timerDuraDeses < duracionDeses)
            ? velDesesperada
            : velPersecucion;

        bool loVe = JugadorDetectado() || JugadorDetectadoDeses();
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
    //  ATRAPAR
    // ─────────────────────────────────────────
    void Atrapar()
    {
        estadoActual          = Estado.Atrapado;
        moviendose            = false;
        timerEncierroActual   = 0f;
        timerDuraDeses        = 0f; // reset modo desesperado

        MusicaManager.Instancia?.TerminarPersecucion();

        if (jugador != null)
        {
            PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;

            Rigidbody2D rbJ = jugador.GetComponent<Rigidbody2D>();
            if (rbJ != null) rbJ.linearVelocity = Vector2.zero;

            Vector2 centroSalon = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
            jugador.position = new Vector3(centroSalon.x + 1.5f, centroSalon.y, 0f);
        }

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.AplicarCastigo(castigo);

        if (rb != null) rb.linearVelocity = Vector2.zero;
        ActualizarAnimacion(Vector2.zero);
    }

    // ─────────────────────────────────────────
    //  ENCIERRO
    // ─────────────────────────────────────────
    void LogicaEncierro()
    {
        moviendose = false;
        ActualizarAnimacion(Vector2.zero);
        timerEncierroActual += Time.deltaTime;

        if (jugador != null)
        {
            Vector2 centroSalon = salonNorte ? salonesNorte[salonIdx].centro : salonesSur[salonIdx].centro;
            if (Vector2.Distance(jugador.position, centroSalon + new Vector2(1.5f, 0f)) > 1.2f)
                jugador.position = new Vector3(centroSalon.x + 1.5f, centroSalon.y, 0f);
        }

        if (timerEncierroActual >= tiempoEncierro)
        {
            if (jugador != null)
            {
                PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
                if (pm != null) pm.enabled = true;
            }
            ElegirProximoDeses();
            IniciarEspera();
        }
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────
    List<(int idx, bool norte)> SalonesValidos()
    {
        var lista = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
        {
            if (i != 4) { lista.Add((i, true)); lista.Add((i, false)); }
        }
        return lista;
    }

    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual   = speed;
        moviendose    = true;
        Vector2 dir = (destino - rb.position).normalized;
        if (dir.magnitude > 0.01f) { direccionActual = dir; ActualizarAnimacion(dir); }
    }

    void FixedUpdate()
    {
        if (!moviendose || rb == null) return;
        rb.MovePosition(Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime));
    }

    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;
        string nueva;
        if (dir.magnitude < 0.01f) nueva = "Idle";
        else if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) nueva = dir.x > 0 ? "WalkRight" : "WalkLeft";
        else nueva = dir.y > 0 ? "WalkUp" : "WalkDown";

        if (nueva != animActual) { animActual = nueva; animator.Play(nueva); }
    }

    void ActualizarSorting()
    {
        if (sr != null) sr.sortingOrder = 300 + Mathf.RoundToInt(-transform.position.y * 10f);
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
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
