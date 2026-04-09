using UnityEngine;
using UnityEngine.UI;
using TMPro;                    // ← Necesario para TextMeshPro

// ══════════════════════════════════════════════════════
// LORENA v5.6 - Versión Final Estable
// Colisiones respetadas + Siempre por encima de la mesa
// ══════════════════════════════════════════════════════
public class Lorena : MonoBehaviour
{
    // ─────────────────────────────────────────
    // ZONA DEL SALÓN
    // ─────────────────────────────────────────
    [Header("Zona del Salón")]
    [Tooltip("Centro del salón (ajustado para que toque la mesa desde arriba)")]
    public Vector2 salonCentro = new Vector2(25f, -13.0f);

    [Tooltip("Radio del área donde Lorena puede caminar")]
    public float radioSalon = 4.2f;

    [Tooltip("Límite inferior Y (toca la mesa)")]
    public float minYPatrulla = -15.8f;

    [Tooltip("Límite superior Y (pared de arriba)")]
    public float maxYPatrulla = -9.5f;

    // ─────────────────────────────────────────
    // MOVIMIENTO
    // ─────────────────────────────────────────
    [Header("Movimiento")]
    public float velocidad = 1.8f;

    // ─────────────────────────────────────────
    // PEDIDOS AUTOMÁTICOS
    // ─────────────────────────────────────────
    [Header("Pedidos de comida")]
    public int precioPorPedido = 20;
    [Tooltip("Cada cuántos segundos Lorena pide comida automáticamente")]
    public float tiempoEntrePedidos = 7f;

    // ─────────────────────────────────────────
    // UI - DINERO (TextMeshPro)
    // ─────────────────────────────────────────
    [Header("UI Dinero")]
    [Tooltip("Arrastra aquí tu TextMeshPro del dinero")]
    public TextMeshProUGUI dineroText;

    // ─────────────────────────────────────────
    // ANIMACIONES
    // ─────────────────────────────────────────
    [Header("Animaciones - NOMBRES EXACTOS del Animator")]
    public string Lorena_Idle = "Lorena_Idle";
    public string Lorena_WalkUp = "Lorena_WalkUp";
    public string Lorena_WalkDown = "Lorena_WalkDown";
    public string Lorena_WalkLeft = "Lorena_WalkLeft";
    public string Lorena_WalkRight = "Lorena_WalkRight";
    public string Lorena_Pedir = "Lorena_Pedir";

    // ─────────────────────────────────────────
    // ESTADO
    // ─────────────────────────────────────────
    enum Estado { CaminandoEnSalon, PidiendoComida }
    Estado estadoActual = Estado.CaminandoEnSalon;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 destinoActual;
    private bool moviendose = false;
    private float timerSiguientePedido = 0f;
    private int dineroTotal = 0;
    private string animActual = "";

    // =============================================
    // SISTEMA DE ENTREGAS
    // =============================================
    private bool pedidoActivo = false;
    private string pedidoActual = "";
    private float tiempoEntregaRestante = 0f;

    public DeliveryManager deliveryManager;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Configuración Rigidbody
        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        transform.position = new Vector3(salonCentro.x, salonCentro.y, 0f);
        timerSiguientePedido = 3f;

        IniciarCaminataEnSalon();
    }

    void Update()
    {
        timerSiguientePedido -= Time.deltaTime;

        if (estadoActual == Estado.CaminandoEnSalon)
        {
            if (timerSiguientePedido <= 0f)
            {
                IniciarPidiendoComida();
                return;
            }
            LogicaCaminataEnSalon();
        }

        // Sorting Order (siempre por encima)
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }

    void FixedUpdate()
    {
        if (estadoActual == Estado.PidiendoComida)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (moviendose)
        {
            Vector2 direction = (destinoActual - rb.position).normalized;
            rb.linearVelocity = direction * velocidad;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Clamp posición Y
        Vector2 pos = rb.position;
        pos.y = Mathf.Clamp(pos.y, minYPatrulla, maxYPatrulla);
        rb.position = pos;
    }

    void IniciarCaminataEnSalon()
    {
        estadoActual = Estado.CaminandoEnSalon;
        moviendose = true;
        ElegirNuevoDestinoDentroDelSalon();
    }

    void LogicaCaminataEnSalon()
    {
        if (Vector2.Distance(rb.position, destinoActual) < 0.6f)
        {
            ElegirNuevoDestinoDentroDelSalon();
        }

        Vector2 dir = (destinoActual - rb.position).normalized;
        ActualizarAnimacion(dir);
    }

    void ElegirNuevoDestinoDentroDelSalon()
    {
        Vector2 offset = new Vector2(
            Random.Range(-radioSalon, radioSalon),
            Random.Range(0f, radioSalon)
        );

        destinoActual = salonCentro + offset;
        destinoActual.y = Mathf.Clamp(destinoActual.y, minYPatrulla, maxYPatrulla);
    }

    void IniciarPidiendoComida()
    {
        estadoActual = Estado.PidiendoComida;
        moviendose = false;
        rb.linearVelocity = Vector2.zero;

        Debug.Log("<color=cyan>Lorena: ¡Hola Shelby! ¿Me das comida por favor?</color>");

        dineroTotal += precioPorPedido;
        if (dineroText != null)
            dineroText.text = "$" + dineroTotal.ToString();

        if (!string.IsNullOrEmpty(Lorena_Pedir))
            animator.CrossFade(Lorena_Pedir, 0.1f);
        else
            ActualizarAnimacion(Vector2.zero);

        Invoke("TerminarPedido", 2.5f);
        timerSiguientePedido = tiempoEntrePedidos;
    }

    void TerminarPedido()
    {
        IniciarCaminataEnSalon();
    }

    void ActualizarAnimacion(Vector2 dir)
    {
        if (animator == null) return;

        string nueva = Lorena_Idle;

        if (dir.magnitude > 0.01f)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                nueva = dir.x > 0 ? Lorena_WalkRight : Lorena_WalkLeft;
            else
                nueva = dir.y > 0 ? Lorena_WalkUp : Lorena_WalkDown;
        }

        if (nueva != animActual)
        {
            animActual = nueva;
            animator.CrossFade(nueva, 0.1f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(salonCentro, radioSalon);
    }

    // =============================================
    // SISTEMA DE ENTREGAS
    // =============================================

    public void RecibirPedido(string nombrePedido, float tiempoLimite)
    {
        if (pedidoActivo) return;

        pedidoActivo = true;
        pedidoActual = nombrePedido;
        tiempoEntregaRestante = tiempoLimite;

        estadoActual = Estado.PidiendoComida;
        moviendose = false;
        rb.linearVelocity = Vector2.zero;

        Debug.Log($"<color=yellow>Lorena: ¡Nuevo pedido! {nombrePedido} - {tiempoLimite} segundos</color>");

        if (!string.IsNullOrEmpty(Lorena_Pedir))
            animator.CrossFade(Lorena_Pedir, 0.1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && pedidoActivo)
        {
            if (deliveryManager != null)
            {
                deliveryManager.CompletarEntregaConLorena();
                CompletarEntrega();           // ← Importante
            }
            else
            {
                Debug.LogWarning("Lorena: No tengo referencia al DeliveryManager");
            }
        }
    }

    public void CompletarEntrega()
    {
        pedidoActivo = false;
        Debug.Log("<color=green>Lorena: ¡Pedido entregado! Gracias ❤️</color>");
        IniciarCaminataEnSalon();
    }

    public void ActualizarTimerPedido(float deltaTime)
    {
        if (!pedidoActivo) return;

        tiempoEntregaRestante -= deltaTime;

        if (tiempoEntregaRestante <= 0f)
        {
            FallarPedido();
        }
    }

    private void FallarPedido()
    {
        pedidoActivo = false;
        Debug.Log("<color=red>Lorena: Se te acabó el tiempo del pedido...</color>");
        IniciarCaminataEnSalon();
    }

    public bool TienePedidoActivo()
    {
        return pedidoActivo;
    }
}