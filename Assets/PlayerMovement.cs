using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Animator animator;
    private Vector2 movement;
    private string currentAnim = "";

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");
        movement = movement.normalized;

        ActualizarAnimacion();
    }

    void ActualizarAnimacion()
    {
        string nuevaAnim = "Idle";

        if (movement.magnitude > 0.1f)
        {
            // Prioridad: horizontal sobre vertical
            if (Mathf.Abs(movement.x) >= Mathf.Abs(movement.y))
            {
                nuevaAnim = movement.x > 0 ? "WalkRight" : "WalkLeft";
            }
            else
            {
                nuevaAnim = movement.y > 0 ? "WalkUp" : "WalkDown";
            }
        }

        // Solo cambia si es diferente para evitar interrupciones
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