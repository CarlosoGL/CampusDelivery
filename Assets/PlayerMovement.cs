using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Vector2 movement;
    private string currentAnim = "";

    // Slowdown post-encierro
    private float moveSpeedBase  = 0f;
    private float timerSlowdown  = 0f;
    private bool  enSlowdown     = false;

    const float SORT_SCALE = 10f;
    const int   SORT_BASE  = 300;

    // =============================================
    // === SISTEMA DE ENTREGAS (AGREGADO) ===
    // =============================================
    private bool tieneEntregaActiva = false;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr       = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();

        moveSpeedBase = moveSpeed;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        ActualizarSlowdown();
        ActualizarAnimacion();
        ActualizarSorting();
    }

    void ActualizarSlowdown()
    {
        if (!enSlowdown) return;

        timerSlowdown -= Time.deltaTime;
        if (timerSlowdown <= 0f)
        {
            enSlowdown = false;
            moveSpeed  = moveSpeedBase;
        }
    }

    public void AplicarSlowdown(float factor, float duracion)
    {
        if (enSlowdown)
            moveSpeed = moveSpeedBase;

        enSlowdown    = true;
        timerSlowdown = duracion;
        moveSpeed     = moveSpeedBase * factor;
    }

    void ActualizarSorting()
    {
        if (sr == null) return;
        sr.sortingOrder = SORT_BASE + Mathf.RoundToInt(-transform.position.y * SORT_SCALE);
    }

    void ActualizarAnimacion()
    {
        if (animator == null) return;

        string nuevaAnim = "Idle";

        if (movement.magnitude > 0.1f)
        {
            if (Mathf.Abs(movement.x) >= Mathf.Abs(movement.y))
                nuevaAnim = movement.x > 0 ? "WalkRight" : "WalkLeft";
            else
                nuevaAnim = movement.y > 0 ? "WalkUp" : "WalkDown";
        }

        if (nuevaAnim != currentAnim)
        {
            currentAnim = nuevaAnim;
            animator.Play(currentAnim);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }

    // =============================================
    // === SISTEMA DE ENTREGAS (AGREGADO) ===
    // =============================================

    // Método que llama DeliveryManager cuando Shelby toca a Lorena
    public void CompletarEntrega()
    {
        if (!tieneEntregaActiva) return;

        if (DeliveryManager.Instance != null)
        {
            DeliveryManager.Instance.CompletarEntregaConLorena();
        }

        tieneEntregaActiva = false;
    }

    // Método que usa DeliveryManager para activar el pedido
    public void ActivarEntrega()
    {
        tieneEntregaActiva = true;
    }

    // Método para que DeliveryManager pueda saber la posición del jugador
    public Vector2 ObtenerPosicion()
    {
        return rb != null ? rb.position : (Vector2)transform.position;
    }
}