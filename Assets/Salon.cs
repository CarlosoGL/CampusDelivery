using UnityEngine;

public class Salon : MonoBehaviour
{
    private bool jugadorCerca = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = true;
            Debug.Log("Presiona E para entregar comida");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = false;
        }
    }

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            GameManager manager = FindObjectOfType<GameManager>();
            if (manager != null)
            {
                if (manager.comidaEnInventario > 0)
                {
                    manager.EntregarComida();
                }
                else
                {
                    Debug.Log("No tienes comida para entregar. Ve a cargar primero.");
                }
            }
        }
    }
}