using UnityEngine;
using TMPro;

/// <summary>
/// Muestra el nombre del salón cuando Shelby entra al área.
/// Coloca un TextMeshPro en la escena y asigna este script
/// al trigger (BoxCollider2D isTrigger=true) de cada salón.
/// </summary>
public class RotuloSalon : MonoBehaviour
{
    [Header("Identificación")]
    public string nombreSalon = "CB-01";

    [Header("UI (opcional)")]
    [Tooltip("Si asignas un TextMeshProUGUI, mostrará el nombre en pantalla.")]
    public TextMeshProUGUI textoUI;

    private static RotuloSalon salonActual;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        salonActual = this;

        if (textoUI != null)
        {
            textoUI.text = nombreSalon;
            textoUI.gameObject.SetActive(true);
        }

        Debug.Log("Entraste a: " + nombreSalon);
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        if (salonActual == this)
        {
            if (textoUI != null)
                textoUI.gameObject.SetActive(false);

            salonActual = null;
        }
    }
}
