using UnityEngine;

public class PuntoCarga : MonoBehaviour
{
    public int comidaPorCarga = 5;
    private bool jugadorCerca = false;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            jugadorCerca = true;
            Debug.Log("Presiona E para cargar comida");
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
                if (manager.comidaEnInventario == 0)
                {
                    manager.CargarComida(comidaPorCarga);
                    Debug.Log("¡Comida cargada! Total: " + manager.comidaEnInventario);
                }
                else
                {
                    Debug.Log("Ya tienes comida cargada. Entrégala primero.");
                }
            }
        }
    }
}