using UnityEngine;
using System.Collections.Generic;

// ══════════════════════════════════════════════════════
//  EL PELÓN
//  - Espera en salón (no detecta nada)
//  - Sale por la puerta, camina por el pasillo, entra a otro salón
//  - Cono 180° SOLO en pasillo
//  - Si te ve: persigue, busca, regresa a rutina
//  - Sin memoria permanente (más light que Stanis)
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
    public float anguloPasillo = 180f;   // activo solo en pasillo
    public float alcanceVision = 7f;
    public string tagJugador   = "Player";

    // ─────────────────────────────────────────
    //  MOVIMIENTO
    // ─────────────────────────────────────────
    [Header("Movimiento")]
    public float velocidadCaminata    = 2.5f;
    public float velocidadPersecucion = 4.5f;
    public float distanciaLlegada     = 0.6f;

    // ─────────────────────────────────────────
    //  BÚSQUEDA
    // ─────────────────────────────────────────
    [Header("Búsqueda")]
    public int   puntosARevisar       = 3;
    public float radioBusqueda        = 4f;
    public float tiempoEsperaBusqueda = 1f;

    // ─────────────────────────────────────────
    //  CASTIGO
    // ─────────────────────────────────────────
    [Header("Castigo")]
    public int castigo = 20;

    // ─────────────────────────────────────────
    //  CONSTANTES DEL EDIFICIO (igual que GeneradorEdificio)
    // ─────────────────────────────────────────
    const float SAL_W    = 20f;
    const float xIzq     = -81f;
    const float Y_NORTE  =  18f;   // centro de salones norte
    const float Y_SUR    = -18f;   // centro de salones sur
    const float Y_PARED_N =  9f;   // pared divisora norte (donde está la puerta)
    const float Y_PARED_S = -9f;   // pared divisora sur
    const float Y_PASILLO =  0f;   // centro del pasillo

    // ─────────────────────────────────────────
    //  ESTADO
    // ─────────────────────────────────────────
    enum Estado { Esperando, Ruta, Persiguiendo, Buscando, Atrapado }
    Estado estadoActual = Estado.Esperando;

    // Salones generados: índice = número de salón (0-7), bool = esNorte
    struct PuntoSalon { public Vector2 centro; public Vector2 puerta; }
    PuntoSalon[] salonesNorte = new PuntoSalon[8];
    PuntoSalon[] salonesSur   = new PuntoSalon[8];

    // Salón actual
    int  salonIdx    = 0;
    bool salonNorte  = true;
    float timerSalon = 0f;
    float duracionSalon = 0f;

    // Ruta: lista de waypoints por los que tiene que pasar
    Queue<Vector2> rutaActual = new Queue<Vector2>();

    // Persecución / búsqueda
    Vector2       ultimoPuntoConocido;
    List<Vector2> puntosBusqueda   = new List<Vector2>();
    int           indiceBusqueda   = 0;
    float         timerEspera      = 0f;
    bool          esperandoEnPunto = false;

    // Movimiento
    Vector2 destinoActual   = Vector2.zero;
    float   speedActual     = 0f;
    bool    moviendose      = false;
    Vector2 direccionActual = Vector2.right;
    bool    enPasillo       = false;   // true cuando Y está cerca de 0

    // Componentes
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

        // Precalcula centros y puertas de todos los salones
        for (int i = 0; i < 8; i++)
        {
            float sx = xIzq + 1f + SAL_W * i + SAL_W / 2f;
            salonesNorte[i] = new PuntoSalon
            {
                centro = new Vector2(sx, Y_NORTE),
                puerta = new Vector2(sx, Y_PARED_N)
            };
            salonesSur[i] = new PuntoSalon
            {
                centro = new Vector2(sx, Y_SUR),
                puerta = new Vector2(sx, Y_PARED_S)
            };
        }

        // Empieza en salón random (evita escaleras=4 norte y entrada=4 sur)
        ElegirSalonInicial();
    }

    void ElegirSalonInicial()
    {
        // Salones válidos: norte 0-3,5-7 / sur 0-3,5-7
        List<(int idx, bool norte)> validos = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
        {
            if (i != 4) validos.Add((i, true));
            if (i != 4) validos.Add((i, false));
        }
        var elegido = validos[Random.Range(0, validos.Count)];
        salonIdx   = elegido.idx;
        salonNorte = elegido.norte;

        Vector2 pos = salonNorte
            ? salonesNorte[salonIdx].centro
            : salonesSur[salonIdx].centro;
        transform.position = new Vector3(pos.x, pos.y, 0f);
        IniciarEspera();
    }

    // ─────────────────────────────────────────
    //  UPDATE
    // ─────────────────────────────────────────
    void Update()
    {
        // Saber si estamos en pasillo (entre las dos paredes)
        enPasillo = Mathf.Abs(transform.position.y) < 8.5f;

        switch (estadoActual)
        {
            case Estado.Esperando:
                LogicaEspera();
                // No detecta nada dentro del salón
                break;

            case Estado.Ruta:
                LogicaRuta();
                // Solo detecta si está en el pasillo
                if (enPasillo && JugadorEnCono())
                    IniciarPersecucion();
                break;

            case Estado.Persiguiendo:
                LogicaPersecucion();
                break;

            case Estado.Buscando:
                LogicaBusqueda();
                if (enPasillo && JugadorEnCono())
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
    //  ELEGIR SIGUIENTE SALÓN Y ARMAR RUTA
    //  Ruta: centro actual → puerta actual → pasillo X destino → puerta destino → centro destino
    // ─────────────────────────────────────────
    void ElegirSiguienteSalonYRuta()
    {
        // Elegir salón destino distinto al actual
        List<(int idx, bool norte)> validos = new List<(int, bool)>();
        for (int i = 0; i < 8; i++)
        {
            if (i != 4) validos.Add((i, true));
            if (i != 4) validos.Add((i, false));
        }
        // Remover el actual
        validos.RemoveAll(v => v.idx == salonIdx && v.norte == salonNorte);

        var dest = validos[Random.Range(0, validos.Count)];

        // Obtener puntos
        Vector2 puerraOrigen = salonNorte
            ? salonesNorte[salonIdx].puerta
            : salonesSur[salonIdx].puerta;

        Vector2 centroDestino = dest.norte
            ? salonesNorte[dest.idx].centro
            : salonesSur[dest.idx].centro;

        Vector2 puertaDestino = dest.norte
            ? salonesNorte[dest.idx].puerta
            : salonesSur[dest.idx].puerta;

        // Punto en el pasillo a la X del destino (para que camine por el pasillo)
        Vector2 pasilloDestino = new Vector2(puertaDestino.x, Y_PASILLO);
        // También punto en pasillo a la X de origen para salir
        Vector2 pasilloOrigen  = new Vector2(puerraOrigen.x, Y_PASILLO);

        // Armar cola de waypoints
        rutaActual.Clear();
        rutaActual.Enqueue(puerraOrigen);    // 1. salir por la puerta
        rutaActual.Enqueue(pasilloOrigen);   // 2. bajar/subir al pasillo
        rutaActual.Enqueue(pasilloDestino);  // 3. caminar por el pasillo hasta la X destino
        rutaActual.Enqueue(puertaDestino);   // 4. entrar por la puerta destino
        rutaActual.Enqueue(centroDestino);   // 5. llegar al centro del salón

        // Guardar destino
        salonIdx   = dest.idx;
        salonNorte = dest.norte;

        estadoActual = Estado.Ruta;
        AvanzarRuta();
    }

    // ─────────────────────────────────────────
    //  RUTA POR WAYPOINTS
    // ─────────────────────────────────────────
    void AvanzarRuta()
    {
        if (rutaActual.Count == 0)
        {
            IniciarEspera();
            return;
        }
        SetDestino(rutaActual.Dequeue(), velocidadCaminata);
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

        if (JugadorEnCono())
            ultimoPuntoConocido = jugador.position;

        float dist = Vector2.Distance(rb.position, jugador.position);
        if (dist < distanciaLlegada)
        {
            Atrapar();
            return;
        }

        if (JugadorEnCono())
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
                timerEspera = 0f;
                moviendose  = false;
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
        // Vuelve a una rutina nueva desde el pasillo
        // Elige el salón más cercano como "actual" y reanuda
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
        estadoActual = Estado.Atrapado;
        moviendose   = false;
        MusicaManager.Instancia?.TerminarPersecucion();

        PlayerMovement pm = jugador?.GetComponent<PlayerMovement>();
        if (pm != null) pm.enabled = false;

        GameManager gm = FindObjectOfType<GameManager>();
        if (gm != null) gm.AplicarCastigo(castigo);

        if (rb != null) rb.linearVelocity = Vector2.zero;
    }

    // ─────────────────────────────────────────
    //  CONO (solo activo en pasillo)
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
        if (dir.magnitude > 0.01f) direccionActual = dir;
        ActualizarAnimacion(dir);
    }

    void FixedUpdate()
    {
        if (!moviendose || rb == null) return;
        Vector2 nueva = Vector2.MoveTowards(rb.position, destinoActual, speedActual * Time.fixedDeltaTime);
        rb.MovePosition(nueva);
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
        // Cono: naranja si en pasillo, azul opaco si en salón
        bool mostrarCono = Application.isPlaying ? enPasillo : true;
        Gizmos.color = mostrarCono
            ? new Color(1f, 0.5f, 0f, 0.7f)
            : new Color(0.2f, 0.5f, 1f, 0.2f);

        Vector3 dir    = Application.isPlaying ? (Vector3)direccionActual : transform.right;
        float   mitad  = anguloPasillo / 2f;
        float   alc    = alcanceVision;

        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0, mitad) * dir * alc);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,0,-mitad) * dir * alc);
        for (int i = 0; i < 16; i++)
        {
            float a1 = -mitad + anguloPasillo * ((float)i / 16);
            float a2 = -mitad + anguloPasillo * ((float)(i+1) / 16);
            Gizmos.DrawLine(
                transform.position + Quaternion.Euler(0,0,a1) * dir * alc,
                transform.position + Quaternion.Euler(0,0,a2) * dir * alc);
        }

        // Ruta pendiente en verde
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
    }
}