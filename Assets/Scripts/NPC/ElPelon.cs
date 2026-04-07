using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
//  EL PELÓN  v4
//  Novedad v4:
//  Al atrapar al jugador lo teletransporta al salón
//  actual y lo encierra 10 seg (PlayerMovement off).
//  Después lo libera y El Pelón retoma su rutina.
// ══════════════════════════════════════════════════════
public class ElPelon : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  TIEMPOS EN SALÓN
    // ─────────────────────────────────────────
    [Header("Rutina")]
    public float tiempoMinEnSalon = 15f;
    public float tiempoMaxEnSalon = 25f;

    // ─────────────────────────────────────────
    //  CONO DE VISIÓN
    // ─────────────────────────────────────────
    [Header("Cono de visión")]
    [Range(90f, 200f)]
    public float anguloPasillo = 180f;
    public float alcanceVision = 7f;
    public float alcanceEnSalon = 12f;
    public string tagJugador = "Player";

    // ─────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────
    [Header("Movimiento")]
    public float velocidadCaminata    = 2.5f;
    public float velocidadPersecucion = 4f;
    public float distanciaLlegada     = 0.5f;

    // ─────────────────────────────────────────
    //  BÚSQUEDA
    // ─────────────────────────────────────────
    [Header("Búsqueda")]
    public int   puntosARevisar       = 3;
    public float radioBusqueda        = 4f;
    public float tiempoEsperaBusqueda = 1f;

    // ─────────────────────────────────────────
    //  CASTIGO + ENCIERRO
    // ─────────────────────────────────────────
    [Header("Castigo")]
    public int   castigo        = 20;
    public float tiempoEncierro = 10f;   // segundos que el jugador queda encerrado

    // ─────────────────────────────────────────
    //  CONSTANTES DEL EDIFICIO
    // ─────────────────────────────────────────
    const float SAL_W        = 20f;
    const float xIzq         = -81f;
    const float Y_NORTE      =  18f;
    const float Y_SUR        = -18f;
    const float Y_DIV_N      =   9f;
    const float Y_DIV_S      =  -9f;
    const float Y_PASILLO    =   0f;
    const float OFFSET_PUERTA = 0.5f;

    // ─────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────
    enum Estado { Esperando, Ruta, Persiguiendo, Buscando, Atrapado }
    Estado estadoActual = Estado.Esperando;

    struct PuntoSalon { public Vector2 centro; public Vector2 puerta; }
    PuntoSalon[] salonesNorte = new PuntoSalon[8];
    PuntoSalon[] salonesSur   = new PuntoSalon[8];

    int   salonIdx   = 0;
    bool  salonNorte = true;
    float timerSalon    = 0f;
    float duracionSalon = 0f;

    Queue<Vector2> rutaActual = new Queue<Vector2>();

    Vector2       ultimoPuntoConocido;
    List<Vector2> puntosBusqueda   = new List<Vector2>();
    int           indiceBusqueda   = 0;
    float         timerEspera      = 0f;
    bool          esperandoEnPunto = false;

    Vector2 destinoActual   = Vector2.zero;
    float   speedActual     = 0f;
    bool    moviendose      = false;
    Vector2 direccionActual = Vector2.right;
    bool    enPasillo       = false;

    // Encierro
    float timerEncierro = 0f;

    // Animación cacheada
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
    }

    void ElegirSalonInicial()
    {
        var validos = SalonesValidos();
        var elegido = validos[Random.Range(0, validos.Count)];
        salonIdx   = elegido.idx;
        salonNorte = elegido.norte;
        Vector2 pos = salonNorte
            ? salonesNorte[salonIdx].centro
            : salonesSur[salonIdx].centro;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        IniciarEspera();
    }

    List<(int idx, bool norte)> SalonesValidos()
    {
        var lista = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
        {
            if (i != 4) lista.Add((i, true));
            if (i != 4) lista.Add((i, false));
        }
        return lista;
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        enPasillo = Mathf.Abs(transform.position.y) < 8.5f;

        switch (estadoActual)
        {
            case Estado.Esperando:
                LogicaEspera();
                if (JugadorEnMiSalon())
                    IniciarPersecucion();
                break;

            case Estado.Ruta:
                LogicaRuta();
                if (enPasillo && JugadorEnCono())
                    IniciarPersecucion();
                else if (!enPasillo && JugadorEnMiSalon())
                    IniciarPersecucion();
                break;

            case Estado.Persiguiendo:
                LogicaPersecucion();
                break;

            case Estado.Buscando:
                LogicaBusqueda();
                if (enPasillo && JugadorEnCono())
                    IniciarPersecucion();
                else if (!enPasillo && JugadorEnMiSalon())
                    IniciarPersecucion();
                break;

            case Estado.Atrapado:
                LogicaEncierro();
                break;
        }

        ActualizarSorting();
    }

    // ─────────────────────────────────────────
    //  ENCIERRO — cuenta 10 seg y libera
    // ─────────────────────────────────────────
    void LogicaEncierro()
    {
        moviendose = false;
        ActualizarAnimacion(Vector2.zero);

        timerEncierro += Time.deltaTime;

        // Mantener al jugador en la posición del salón por si algo lo mueve
        if (jugador != null)
        {
            Vector2 centroSalon = salonNorte
                ? salonesNorte[salonIdx].centro
                : salonesSur[salonIdx].centro;
            Vector2 posJugador = jugador.position;

            // Si el jugador se aleja más de 1u del punto de encierro, lo regresa
            if (Vector2.Distance(posJugador, centroSalon + new Vector2(1.5f, 0f)) > 1.2f)
            {
                jugador.position = new Vector3(
                    centroSalon.x + 1.5f,
                    centroSalon.y,
                    0f);
            }
        }

        if (timerEncierro >= tiempoEncierro)
        {
            LiberarJugador();
        }
    }

    void LiberarJugador()
    {
        // Reactiva movimiento del jugador
        if (jugador != null)
        {
            PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = true;
        }

        // El Pelón retoma su rutina
        IniciarEspera();
    }

    // ─────────────────────────────────────────
    //  DETECCIÓN: jugador en el mismo salón
    // ─────────────────────────────────────────
    bool JugadorEnMiSalon()
    {
        if (jugador == null) return false;
        if (enPasillo) return false;

        float dist = Vector2.Distance(rb.position, jugador.position);
        if (dist > alcanceEnSalon) return false;

        bool jugadorEnPasillo = Mathf.Abs(jugador.position.y) < 8.5f;
        if (jugadorEnPasillo) return false;

        bool peloEnNorte = transform.position.y > 0f;
        bool jugEnNorte  = jugador.position.y > 0f;
        if (peloEnNorte != jugEnNorte) return false;

        float miX  = salonNorte ? salonesNorte[salonIdx].centro.x : salonesSur[salonIdx].centro.x;
        float difX = Mathf.Abs(jugador.position.x - miX);
        return difX < SAL_W / 2f - 0.5f;
    }

    // ─────────────────────────────────────────
    //  ESPERA EN SALÓN
    // ─────────────────────────────────────────
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

        Vector2 puertaOrigen  = salonNorte
            ? salonesNorte[salonIdx].puerta
            : salonesSur[salonIdx].puerta;
        Vector2 pasilloOrigen = new Vector2(puertaOrigen.x, Y_PASILLO);

        Vector2 centroDestino  = dest.norte
            ? salonesNorte[dest.idx].centro
            : salonesSur[dest.idx].centro;
        Vector2 puertaDestino  = dest.norte
            ? salonesNorte[dest.idx].puerta
            : salonesSur[dest.idx].puerta;
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
        SetDestino(rutaActual.Dequeue(), velocidadCaminata);
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

        bool loVe = enPasillo ? JugadorEnCono() : JugadorEnMiSalon();
        if (loVe) ultimoPuntoConocido = jugador.position;

        float dist = Vector2.Distance(rb.position, jugador.position);
        if (dist < distanciaLlegada) { Atrapar(); return; }

        if (loVe)
            SetDestino(jugador.position, velocidadPersecucion);
        else
        {
            SetDestino(ultimoPuntoConocido, velocidadPersecucion);
            if (Vector2.Distance(rb.position, ultimoPuntoConocido) < distanciaLlegada)
                IniciarBusqueda();
        }
    }

    // ─────────────────────────────────────────
    //  BÚSQUEDA
    // ─────────────────────────────────────────
    void IniciarBusqueda()
    {
        estadoActual     = Estado.Buscando;
        indiceBusqueda   = 0;
        timerEspera      = 0f;
        esperandoEnPunto = false;
        MusicaManager.Instancia?.TerminarPersecucion();

        puntosBusqueda.Clear();
        for (int i = 0; i < puntosARevisar; i++)
        {
            float ang = (360f / puntosARevisar) * i * Mathf.Deg2Rad;
            puntosBusqueda.Add(ultimoPuntoConocido + new Vector2(
                Mathf.Cos(ang) * radioBusqueda,
                Mathf.Sin(ang) * radioBusqueda));
        }
    }

    void LogicaBusqueda()
    {
        if (puntosBusqueda.Count == 0) { TerminarBusqueda(); return; }

        Vector2 punto = puntosBusqueda[indiceBusqueda];
        if (!esperandoEnPunto)
        {
            SetDestino(punto, velocidadCaminata * 0.8f);
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
    //  ATRAPAR  →  teletransportar + encerrar
    // ─────────────────────────────────────────
    void Atrapar()
    {
        estadoActual  = Estado.Atrapado;
        moviendose    = false;
        timerEncierro = 0f;

        MusicaManager.Instancia?.TerminarPersecucion();

        // Desactiva movimiento del jugador
        if (jugador != null)
        {
            PlayerMovement pm = jugador.GetComponent<PlayerMovement>();
            if (pm != null) pm.enabled = false;

            // Parar física del jugador si tiene Rigidbody2D
            Rigidbody2D rbJugador = jugador.GetComponent<Rigidbody2D>();
            if (rbJugador != null) rbJugador.linearVelocity = Vector2.zero;

            // Teletransportar al jugador al salón donde está El Pelón
            Vector2 centroSalon = salonNorte
                ? salonesNorte[salonIdx].centro
                : salonesSur[salonIdx].centro;

            jugador.position = new Vector3(
                centroSalon.x + 1.5f,
                centroSalon.y,
                0f);
        }

        // Aplicar castigo de puntos
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.AplicarCastigo(castigo);

        // Parar al propio Pelón
        if (rb != null) rb.linearVelocity = Vector2.zero;

        ActualizarAnimacion(Vector2.zero);
    }

    // ─────────────────────────────────────────
    //  CONO (pasillo)
    // ─────────────────────────────────────────
    bool JugadorEnCono()
    {
        if (jugador == null) return false;
        Vector2 diff = (Vector2)jugador.position - rb.position;
        if (diff.magnitude > alcanceVision) return false;
        float a = Vector2.Angle(direccionActual, diff.normalized);
        return a <= anguloPasillo / 2f;
    }

    // ─────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────
    void SetDestino(Vector2 destino, float speed)
    {
        destinoActual = destino;
        speedActual   = speed;
        moviendose    = true;
        Vector2 dir = (destino - rb.position).normalized;
        if (dir.magnitude > 0.01f)
        {
            direccionActual = dir;
            ActualizarAnimacion(dir);
        }
    }

    void FixedUpdate()
    {
        if (!moviendose || rb == null) return;
        Vector2 nueva = Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime);
        rb.MovePosition(nueva);
    }

    // ─────────────────────────────────────────
    //  ANIMACIÓN — cacheada
    // ─────────────────────────────────────────
    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;

        string nueva;
        if (dir.magnitude < 0.01f)
            nueva = "Idle";
        else if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            nueva = dir.x > 0 ? "WalkRight" : "WalkLeft";
        else
            nueva = dir.y > 0 ? "WalkUp" : "WalkDown";

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
        if (sr == null) return;
        sr.sortingOrder = 300 + Mathf.RoundToInt(-transform.position.y * 10f);
    }

    // ─────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────
    void OnDrawGizmos()
    {
        bool mostrarCono = Application.isPlaying ? enPasillo : true;
        Gizmos.color = mostrarCono
            ? new Color(1f, 0.5f, 0f, 0.7f)
            : new Color(0.2f, 0.5f, 1f, 0.15f);

        Vector3 dir   = Application.isPlaying ? (Vector3)direccionActual : transform.right;
        float   mitad = anguloPasillo / 2f;
        float   alc   = alcanceVision;

        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, 0,  mitad) * dir * alc);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, 0, -mitad) * dir * alc);
        for (int i = 0; i < 16; i++)
        {
            float a1 = -mitad + anguloPasillo * (i / 16f);
            float a2 = -mitad + anguloPasillo * ((i + 1) / 16f);
            Gizmos.DrawLine(
                transform.position + Quaternion.Euler(0, 0, a1) * dir * alc,
                transform.position + Quaternion.Euler(0, 0, a2) * dir * alc);
        }

        if (!mostrarCono)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            DrawCircleGizmo(transform.position, alcanceEnSalon);
        }

        // Ruta pendiente
        if (Application.isPlaying && rutaActual != null)
        {
            Gizmos.color = Color.green;
            Vector3 prev = transform.position;
            foreach (var p in rutaActual)
            {
                Gizmos.DrawLine(prev, p);
                Gizmos.DrawWireSphere(p, 0.3f);
                prev = p;
            }
        }

        // Mostrar timer de encierro en Gizmos cuando está activo
        if (Application.isPlaying && estadoActual == Estado.Atrapado)
        {
            Gizmos.color = Color.red;
            DrawCircleGizmo(transform.position, 2f);
        }
    }

    void DrawCircleGizmo(Vector3 center, float radius)
    {
        int segs = 32;
        for (int i = 0; i < segs; i++)
        {
            float a1 = i * Mathf.PI * 2f / segs;
            float a2 = (i + 1) * Mathf.PI * 2f / segs;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * radius,
                center + new Vector3(Mathf.Cos(a2), Mathf.Sin(a2)) * radius);
        }
    }
}