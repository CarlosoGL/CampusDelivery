using UnityEngine;

/// <summary>
/// Mapa interior del edificio con efecto de profundidad estilo Eastward.
/// Las paredes que dan al pasillo tienen franja oscura inferior (cara frontal).
/// </summary>
public class GeneradorEdificio : MonoBehaviour
{
    // ── Paleta ────────────────────────────────────────────────────
    static readonly Color C_PARED    = new Color(0.28f, 0.28f, 0.35f, 1f);
    static readonly Color C_PARED_CARA = new Color(0.18f, 0.18f, 0.24f, 1f); // cara frontal oscura
    static readonly Color C_TECHO    = new Color(0.76f, 0.74f, 0.68f, 1f);   // techo del salón
    static readonly Color C_PISO_SAL = new Color(0.84f, 0.82f, 0.76f, 1f);   // piso interior salón
    static readonly Color C_PISO_PAS = new Color(0.68f, 0.66f, 0.60f, 1f);   // piso pasillo
    static readonly Color C_ESC_BASE = new Color(0.50f, 0.46f, 0.42f, 1f);
    static readonly Color C_ESC_STEP = new Color(0.70f, 0.66f, 0.60f, 1f);
    static readonly Color C_ESC_DARK = new Color(0.22f, 0.20f, 0.18f, 1f);
    static readonly Color C_SOMBRA   = new Color(0.10f, 0.10f, 0.14f, 0.55f); // sombra suelo

    const float SAL_W    = 10f;
    const float SAL_H    = 9f;
    const float PAS_H    = 6f;
    const float PARED_G  = 0.9f;
    const float PUERTA_W = 2.5f;
    const float CARA_H   = 1.4f; // altura de la "cara frontal" 3D

    const float Y_PARED_NORTE = 16f;
    const float Y_SAL_NORTE   = 11f;
    const float Y_DIV_NORTE   = 6f;
    const float Y_PASILLO     = 2.5f;
    const float Y_DIV_SUR     = -1f;
    const float Y_SAL_SUR     = -6f;
    const float Y_PARED_SUR   = -11f;

    const float xIzq = -41f;
    const float xDer =  41f;

    Sprite _px;

    void Awake()
    {
        _px = MakePixel();
        Build();
    }

    void Build()
    {
        float totalW = xDer - xIzq;
        float cx = 0f;

        string[] nombresNorte = { "CB-06","CB-08","CB-09","Papeleria",
                                   "Escaleras","Cubiculo1","Cubiculo2","Cubiculo3" };
        string[] nombresSur   = { "CB-04","CB-03","CB-02","CB-01",
                                   "Entrada","Laboratoristas","CB-05","CB-010" };

        // ── PISOS ──────────────────────────────────────────────────
        Vis("Piso_Pasillo", cx, Y_PASILLO, totalW, PAS_H, C_PISO_PAS, -3);

        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            if (nombresNorte[i] != "Escaleras")
            {
                // Techo (parte superior visible del salón)
                Vis("TechoN_" + i, sx, Y_SAL_NORTE + 1f, SAL_W, SAL_H - 2f, C_TECHO, -3);
                // Piso interior (franja inferior del salón, más clara)
                Vis("PisoN_" + i, sx, Y_SAL_NORTE - SAL_H/2f + 1f, SAL_W, 2f, C_PISO_SAL, -2);
            }
            if (nombresSur[i] != "Entrada")
            {
                Vis("TechoS_" + i, sx, Y_SAL_SUR + 1f, SAL_W, SAL_H - 2f, C_TECHO, -3);
                Vis("PisoS_" + i, sx, Y_SAL_SUR - SAL_H/2f + 1f, SAL_W, 2f, C_PISO_SAL, -2);
            }
        }

        // ── PAREDES EXTERIORES ─────────────────────────────────────
        Muro("Ext_Norte", cx, Y_PARED_NORTE, totalW, PARED_G);
        // Cara frontal de pared norte (da hacia el pasillo, abajo)
        Vis("CaraN_Norte", cx, Y_DIV_NORTE - PARED_G/2f - CARA_H/2f,
            totalW, CARA_H, C_PARED_CARA, 1);

        // Sur con hueco de entrada
        float entCX = SalonCX(4);
        float surIzqW = entCX - PUERTA_W/2f - xIzq;
        float surDerStart = entCX + PUERTA_W/2f;
        float surDerW = xDer - surDerStart;
        Muro("Ext_Sur_Izq", xIzq + surIzqW/2f, Y_PARED_SUR, surIzqW, PARED_G);
        Muro("Ext_Sur_Der", surDerStart + surDerW/2f, Y_PARED_SUR, surDerW, PARED_G);

        Muro("Ext_Oeste", xIzq + PARED_G/2f, (Y_PARED_NORTE+Y_PARED_SUR)/2f,
             PARED_G, Y_PARED_NORTE - Y_PARED_SUR);
        Muro("Ext_Este",  xDer - PARED_G/2f, (Y_PARED_NORTE+Y_PARED_SUR)/2f,
             PARED_G, Y_PARED_NORTE - Y_PARED_SUR);

        // ── PARED DIVISORA NORTE con cara frontal ──────────────────
        for (int i = 0; i < 8; i++)
        {
            float sx = xIzq + 1f + SAL_W * i;
            float scx = sx + SAL_W/2f;
            bool esEsc = nombresNorte[i] == "Escaleras";

            if (esEsc)
            {
                Muro("DivN_" + i, scx, Y_DIV_NORTE, SAL_W, PARED_G);
                Vis("CaraN_" + i, scx, Y_DIV_NORTE - PARED_G/2f - CARA_H/2f,
                    SAL_W, CARA_H, C_PARED_CARA, 1);
            }
            else
            {
                float segW = (SAL_W - PUERTA_W) / 2f;
                // Izquierda
                Muro("DivN_" + i + "_L", sx + segW/2f, Y_DIV_NORTE, segW, PARED_G);
                Vis("CaraN_" + i + "_L", sx + segW/2f,
                    Y_DIV_NORTE - PARED_G/2f - CARA_H/2f, segW, CARA_H, C_PARED_CARA, 1);
                // Derecha
                Muro("DivN_" + i + "_R", sx + SAL_W - segW/2f, Y_DIV_NORTE, segW, PARED_G);
                Vis("CaraN_" + i + "_R", sx + SAL_W - segW/2f,
                    Y_DIV_NORTE - PARED_G/2f - CARA_H/2f, segW, CARA_H, C_PARED_CARA, 1);
                // Sombra en el piso justo debajo de la puerta
                Vis("SombraPuertaN_" + i, scx,
                    Y_DIV_NORTE - PARED_G/2f - 0.2f, PUERTA_W, 0.4f, C_SOMBRA, 1);
            }
        }

        // ── PARED DIVISORA SUR con cara frontal ────────────────────
        for (int i = 0; i < 8; i++)
        {
            float sx = xIzq + 1f + SAL_W * i;
            float scx = sx + SAL_W/2f;
            bool esEnt = nombresSur[i] == "Entrada";

            if (esEnt)
            {
                Muro("DivS_" + i, scx, Y_DIV_SUR, SAL_W, PARED_G);
                Vis("CaraS_" + i, scx, Y_DIV_SUR - PARED_G/2f - CARA_H/2f,
                    SAL_W, CARA_H, C_PARED_CARA, 1);
            }
            else
            {
                float segW = (SAL_W - PUERTA_W) / 2f;
                Muro("DivS_" + i + "_L", sx + segW/2f, Y_DIV_SUR, segW, PARED_G);
                Vis("CaraS_" + i + "_L", sx + segW/2f,
                    Y_DIV_SUR - PARED_G/2f - CARA_H/2f, segW, CARA_H, C_PARED_CARA, 1);
                Muro("DivS_" + i + "_R", sx + SAL_W - segW/2f, Y_DIV_SUR, segW, PARED_G);
                Vis("CaraS_" + i + "_R", sx + SAL_W - segW/2f,
                    Y_DIV_SUR - PARED_G/2f - CARA_H/2f, segW, CARA_H, C_PARED_CARA, 1);
                Vis("SombraPuertaS_" + i, scx,
                    Y_DIV_SUR - PARED_G/2f - 0.2f, PUERTA_W, 0.4f, C_SOMBRA, 1);
            }
        }

        // ── DIVISIONES VERTICALES ──────────────────────────────────
        for (int i = 0; i <= 8; i++)
        {
            float xd = xIzq + 1f + SAL_W * i;
            Muro("DivVertN_" + i, xd, Y_SAL_NORTE, PARED_G, SAL_H);
            Muro("DivVertS_" + i, xd, Y_SAL_SUR,   PARED_G, SAL_H);
        }

        // ── ESCALERAS DECORATIVAS ─────────────────────────────────
        float escCX = SalonCX(4);
        BuildEscaleras(escCX, Y_SAL_NORTE);

        // ── SOMBRA EN BASE DE PAREDES (piso del pasillo) ──────────
        // Línea oscura justo encima de la pared div sur (simula pared que se alza)
        Vis("SombraSueloPas", cx, Y_DIV_SUR - PARED_G/2f - 0.15f,
            totalW, 0.3f, C_SOMBRA, 1);

        // ── TRIGGERS ──────────────────────────────────────────────
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            if (nombresNorte[i] != "Escaleras")
                Area("AreaN_" + i, sx, Y_SAL_NORTE, SAL_W - PARED_G, SAL_H - PARED_G, nombresNorte[i]);
            if (nombresSur[i] != "Entrada")
                Area("AreaS_" + i, sx, Y_SAL_SUR, SAL_W - PARED_G, SAL_H - PARED_G, nombresSur[i]);
        }
    }

    float SalonCX(int i) => xIzq + 1f + SAL_W * i + SAL_W / 2f;

    void BuildEscaleras(float cx, float cy)
    {
        float w = SAL_W - PARED_G * 2f;
        float h = SAL_H - PARED_G * 2f;
        Vis("Esc_Base", cx, cy, w, h, C_ESC_BASE, -2);

        float tramW = w * 0.35f;
        float tramH = h * 0.85f;
        int pasos = 5;
        for (int s = 0; s < pasos; s++)
        {
            float stepY = cy - tramH/2f + (tramH/pasos)*s + (tramH/pasos)*0.5f;
            Color c = Color.Lerp(C_ESC_DARK, C_ESC_STEP, (float)s/pasos);
            Vis("EscL_" + s, cx - w/4f, stepY, tramW, tramH/pasos - 0.05f, c, -1);
            Vis("EscR_" + s, cx + w/4f, stepY, tramW, tramH/pasos - 0.05f, c, -1);
        }
        Vis("Esc_Descanso", cx, cy, w * 0.18f, h * 0.6f, C_ESC_STEP, -1);

        // Colisión
        GameObject go = new GameObject("Esc_Col");
        go.transform.position  = new Vector3(cx, cy, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        go.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    void Muro(string n, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(n);
        go.transform.position  = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        go.AddComponent<SpriteRenderer>().sprite = _px;
        go.GetComponent<SpriteRenderer>().color = C_PARED;
        go.GetComponent<SpriteRenderer>().sortingOrder = 0;
        go.AddComponent<BoxCollider2D>().size = Vector2.one;
    }

    void Vis(string n, float x, float y, float w, float h, Color c, int order)
    {
        GameObject go = new GameObject(n);
        go.transform.position  = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _px; sr.color = c; sr.sortingOrder = order;
    }

    void Area(string n, float x, float y, float w, float h, string label)
    {
        GameObject go = new GameObject(n);
        go.transform.position  = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one; bc.isTrigger = true;
        go.AddComponent<RotuloSalon>().nombreSalon = label;
    }

    Sprite MakePixel()
    {
        Texture2D t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white); t.Apply();
        return Sprite.Create(t, new Rect(0,0,1,1), new Vector2(0.5f,0.5f), 1f);
    }
}
