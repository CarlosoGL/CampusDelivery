using UnityEngine;

/// <summary>
/// Hace que la cámara siga a Shelby suavemente dentro del edificio.
/// Arrastra este script a la Main Camera de la escena Edificio.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform objetivo;          // Arrastra aquí a Shelby

    [Header("Seguimiento")]
    public float velocidadSeguimiento = 5f;
    public Vector2 limiteMin = new Vector2(-35f, -10f);  // límite inferior-izquierdo
    public Vector2 limiteMax = new Vector2(48f, 16f);    // límite superior-derecho

    private Vector3 offset;

    void Start()
    {
        // Si no se asignó manualmente, busca al jugador
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null)
                objetivo = jugador.transform;
        }

        offset = new Vector3(0, 0, -10f); // mantiene Z en -10
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Posición deseada
        Vector3 posDeseada = objetivo.position + offset;

        // Clamp dentro de los límites del mapa
        posDeseada.x = Mathf.Clamp(posDeseada.x, limiteMin.x, limiteMax.x);
        posDeseada.y = Mathf.Clamp(posDeseada.y, limiteMin.y, limiteMax.y);

        // Interpolación suave
        transform.position = Vector3.Lerp(transform.position, posDeseada, velocidadSeguimiento * Time.deltaTime);
    }
}
