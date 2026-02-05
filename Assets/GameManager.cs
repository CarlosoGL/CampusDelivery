using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public int comidaEnInventario = 0;
    public int dinero = 0;
    public int precioEntrega = 10;

    public TextMeshProUGUI textoDinero;
    public TextMeshProUGUI textoComida;

    void Start()
    {
        ActualizarUI();
    }

    public void CargarComida(int cantidad)
    {
        comidaEnInventario += cantidad;
        ActualizarUI();
    }

    public bool EntregarComida()
    {
        if (comidaEnInventario > 0)
        {
            comidaEnInventario--;
            dinero += precioEntrega;
            Debug.Log("¡Entrega exitosa! Dinero: $" + dinero + " | Comida restante: " + comidaEnInventario);
            ActualizarUI();
            return true;
        }
        return false;
    }

    public void AplicarCastigo(int cantidad)
    {
        dinero -= cantidad;
        if (dinero < 0) dinero = 0;
        Debug.Log("¡Te atraparon! Perdiste $" + cantidad + ". Dinero restante: $" + dinero);
        ActualizarUI();
    }

    void ActualizarUI()
    {
        if (textoDinero != null)
            textoDinero.text = "Dinero: $" + dinero;
        
        if (textoComida != null)
            textoComida.text = "Comida: " + comidaEnInventario;
    }
}