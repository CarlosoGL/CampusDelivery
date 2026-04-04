using UnityEngine;

/// <summary>
/// Cámara que sigue al jugador de cerca en X e Y,
/// sin mostrar bordes negros fuera del escenario.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform objetivo;

    [Header("Seguimiento")]
    public float velocidadSeguimiento = 5f;

    [Header("Zoom — qué tan cerca sigue al personaje")]
    [Tooltip("Más pequeño = más zoom/cerca. Recomendado: 10 a 14")]
    public float orthographicSize = 12f;

    // Límites físicos del escenario — actualizados para SAL_W=20, SAL_H=18
    private const float MAP_MIN_X = -81f;
    private const float MAP_MAX_X =  81f;
    private const float MAP_MIN_Y = -28f;
    private const float MAP_MAX_Y =  28f;

    private Camera _cam;

    void Start()
    {
        _cam = GetComponent<Camera>();
        _cam.orthographic     = true;
        _cam.orthographicSize = orthographicSize;

        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindGameObjectWithTag("Player");
            if (jugador != null) objetivo = jugador.transform;
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        float camH = _cam.orthographicSize;
        float camW = _cam.orthographicSize * _cam.aspect;

        float desX = objetivo.position.x;
        float desY = objetivo.position.y;

        float minX = MAP_MIN_X + camW;
        float maxX = MAP_MAX_X - camW;
        float minY = MAP_MIN_Y + camH;
        float maxY = MAP_MAX_Y - camH;

        desX = (minX < maxX) ? Mathf.Clamp(desX, minX, maxX) : (MAP_MIN_X + MAP_MAX_X) / 2f;
        desY = (minY < maxY) ? Mathf.Clamp(desY, minY, maxY) : (MAP_MIN_Y + MAP_MAX_Y) / 2f;

        Vector3 posDeseada = new Vector3(desX, desY, -10f);

        transform.position = Vector3.Lerp(
            transform.position,
            posDeseada,
            velocidadSeguimiento * Time.deltaTime
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            new Vector3((MAP_MIN_X + MAP_MAX_X) / 2f, (MAP_MIN_Y + MAP_MAX_Y) / 2f, 0),
            new Vector3(MAP_MAX_X - MAP_MIN_X, MAP_MAX_Y - MAP_MIN_Y, 0)
        );
    }
}