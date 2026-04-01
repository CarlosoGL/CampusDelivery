using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sr;
    private Vector2 movement;
    private string currentAnim = "";

    // Y-sorting: jugador en pasillo (Y≈2.5) → order=75
    // Cara frontal norte tiene order=48 → jugador (75) queda ENCIMA ✓
    // Jugador cerca de pared norte (Y≈6) → order=40 → pared (48) queda ENCIMA ✓
    const float SORT_SCALE = 10f;
    const int   SORT_BASE  = 300;

    void Start()
    {
        rb       = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sr       = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        ActualizarAnimacion();
        ActualizarSorting();
    }

    // Más abajo en pantalla (Y menor) = más al FRENTE = sorting mayor
    void ActualizarSorting()
    {
        if (sr == null) return;
        sr.sortingOrder = SORT_BASE + Mathf.RoundToInt(-transform.position.y * SORT_SCALE);
    }

    void ActualizarAnimacion()
    {
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
}