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
        // FIX: si el componente está deshabilitado este Update no corre,
        // así que no hace falta ningún guard extra aquí.
        // El NPC hace pm.enabled = false para bloquearnos.
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
        // Si ya había un slowdown activo, resetear primero
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
        // FIX: cuando el NPC pone rbJugador.bodyType = Kinematic,
        // MovePosition aún funciona (es lo que usa el NPC para arrastrarte).
        // Cuando el componente está disabled, este FixedUpdate NO corre,
        // así que no hay conflicto con el arrastre del NPC.
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}