using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeliveryManager : MonoBehaviour
{
    public static DeliveryManager Instance { get; private set; }

    [Header("=== Configuración de Entregas ===")]
    public int dineroActual = 0;
    public int metaDineroParaGanar = 1000;

    [Header("=== UI (arrastra desde la escena) ===")]
    public GameObject panelNotificacion;           // Panel que aparece cuando Lorena pide
    public TextMeshProUGUI textoNotificacion;      // Texto dentro del panel
    public Button botonAceptar;                    // Botón "Aceptar entrega"
    public TextMeshProUGUI textoDinero;            // Texto que muestra el dinero actual
    public TextMeshProUGUI textoTimer;             // Texto que muestra el tiempo restante

    [Header("=== Tiempos ===")]
    public float tiempoBaseEntrega = 75f;          // Segundos que tienes para entregar

    private bool tieneEntregaActiva = false;
    private float tiempoRestante = 0f;
    private string pedidoActual = "";

    private Lorena lorenaActual;                   // Referencia a Lorena

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        ActualizarUI();
        // Lorena empezará a pedir después de unos segundos
        InvokeRepeating("PedirNuevaEntrega", 12f, 35f);
    }

    // Lorena llama a este método para pedir comida
    public void PedirNuevaEntrega()
    {
        if (tieneEntregaActiva) return;

        pedidoActual = "Comida caliente";
        tiempoRestante = tiempoBaseEntrega;

        if (panelNotificacion != null)
        {
            textoNotificacion.text = $"¡Nueva entrega!\n{pedidoActual}\n¿Aceptas?";
            panelNotificacion.SetActive(true);
        }

        if (botonAceptar != null)
        {
            botonAceptar.onClick.RemoveAllListeners();
            botonAceptar.onClick.AddListener(AceptarEntrega);
        }
    }

    private void AceptarEntrega()
    {
        if (panelNotificacion != null)
            panelNotificacion.SetActive(false);

        tieneEntregaActiva = true;
        tiempoRestante = tiempoBaseEntrega;

        Debug.Log("<color=green>Entrega aceptada: " + pedidoActual + "</color>");

        // Buscar a Lorena en la escena y avisarle
        Lorena lorena = FindObjectOfType<Lorena>();
        if (lorena != null)
        {
            lorenaActual = lorena;
            lorena.RecibirPedido(pedidoActual, tiempoBaseEntrega);
        }

        ActualizarUI();
        StartCoroutine(ContarTiempoEntrega());
    }

    private IEnumerator ContarTiempoEntrega()
    {
        while (tieneEntregaActiva && tiempoRestante > 0)
        {
            tiempoRestante -= Time.deltaTime;
            if (textoTimer != null)
                textoTimer.text = $"Tiempo: {Mathf.Ceil(tiempoRestante)}s";
            yield return null;
        }

        if (tieneEntregaActiva)
        {
            FallarEntrega();
        }
    }

    // Este método lo llama Lorena cuando Shelby colisiona con ella
    public void CompletarEntregaConLorena()
    {
        if (!tieneEntregaActiva) return;

        // Éxito
        int ganancia = 90 + (int)(tiempoRestante * 0.8f); // más rápido = más dinero
        dineroActual += ganancia;
        tieneEntregaActiva = false;

        if (textoTimer != null) textoTimer.text = "¡Entregado!";
        Debug.Log($"<color=cyan>¡Entrega completada! +${ganancia}</color>");

        ActualizarUI();

        if (dineroActual >= metaDineroParaGanar)
        {
            GanarJuego();
        }
    }

    private void FallarEntrega()
    {
        tieneEntregaActiva = false;
        if (textoTimer != null) textoTimer.text = "¡Tiempo agotado!";
        Debug.Log("<color=red>Entrega fallida por tiempo</color>");
        ActualizarUI();
    }

    private void GanarJuego()
    {
        Debug.Log("<color=magenta>¡FELICIDADES! Shelby llegó a $" + dineroActual + " y ganó el juego</color>");

        // Activa tu panel de victoria (créalo en la escena y desactívalo al inicio)
        GameObject winPanel = GameObject.Find("WinPanel");
        if (winPanel != null)
            winPanel.SetActive(true);

        Time.timeScale = 0f; // pausa el juego
    }

    private void ActualizarUI()
    {
        if (textoDinero != null)
            textoDinero.text = $"Dinero: ${dineroActual}";
    }

    // Para que Lorena pueda actualizar el timer desde su script
    void Update()
    {
        if (tieneEntregaActiva && lorenaActual != null)
        {
            lorenaActual.ActualizarTimerPedido(Time.deltaTime);
        }
    }
}