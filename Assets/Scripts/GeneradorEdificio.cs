using UnityEngine;

public class GeneradorEdificio : MonoBehaviour
{
    static readonly Color C_PISO_A       = new Color(0.882f, 0.867f, 0.831f, 1f);
    static readonly Color C_PISO_B       = new Color(0.859f, 0.843f, 0.804f, 1f);
    static readonly Color C_PISO_JUNTA   = new Color(0.620f, 0.608f, 0.580f, 1f);
    static readonly Color C_PISO_SOMBRA  = new Color(0.560f, 0.548f, 0.520f, 1f);
    static readonly Color C_SAL_PISO_A   = new Color(0.910f, 0.900f, 0.878f, 1f);
    static readonly Color C_SAL_PISO_B   = new Color(0.890f, 0.878f, 0.855f, 1f);
    static readonly Color C_PARED_MURO   = new Color(0.780f, 0.770f, 0.748f, 1f);
    static readonly Color C_PARED_ZOCALO = new Color(0.580f, 0.565f, 0.538f, 1f);
    static readonly Color C_PARED_CARA   = new Color(0.320f, 0.312f, 0.298f, 1f);
    static readonly Color C_PARED_EXT    = new Color(0.260f, 0.255f, 0.245f, 1f);
    static readonly Color C_COLUMNA_CLR  = new Color(0.720f, 0.710f, 0.688f, 1f);
    static readonly Color C_COLUMNA_OSC  = new Color(0.480f, 0.472f, 0.455f, 1f);
    static readonly Color C_TECHO_BASE   = new Color(0.940f, 0.935f, 0.920f, 1f);
    static readonly Color C_TECHO_PANEL  = new Color(0.920f, 0.914f, 0.898f, 1f);
    static readonly Color C_TECHO_JUNTA  = new Color(0.780f, 0.774f, 0.758f, 1f);
    static readonly Color C_PUERTA_ALU   = new Color(0.620f, 0.622f, 0.618f, 1f);
    static readonly Color C_PUERTA_ALU2  = new Color(0.500f, 0.502f, 0.498f, 1f);
    static readonly Color C_PUERTA_VID   = new Color(0.680f, 0.760f, 0.800f, 0.85f);
    static readonly Color C_PUERTA_VID2  = new Color(0.740f, 0.820f, 0.855f, 0.60f);
    static readonly Color C_PUERTA_SOM   = new Color(0.080f, 0.082f, 0.090f, 0.55f);
    static readonly Color C_SILLA_ASIEN  = new Color(0.180f, 0.360f, 0.180f, 1f);
    static readonly Color C_SILLA_PALA   = new Color(0.820f, 0.780f, 0.660f, 1f);
    static readonly Color C_SILLA_METAL  = new Color(0.480f, 0.480f, 0.480f, 1f);
    static readonly Color C_ESCRITORIO   = new Color(0.560f, 0.420f, 0.220f, 1f);
    static readonly Color C_ESCRIT_BORDE = new Color(0.380f, 0.280f, 0.140f, 1f);
    static readonly Color C_PIZARRON     = new Color(0.100f, 0.260f, 0.140f, 1f);
    static readonly Color C_PIZ_BORDE    = new Color(0.500f, 0.360f, 0.160f, 1f);
    static readonly Color C_PIZ_TIZA     = new Color(0.860f, 0.858f, 0.840f, 0.5f);
    static readonly Color C_LAMP_CUERPO  = new Color(0.860f, 0.858f, 0.840f, 1f);
    static readonly Color C_LAMP_LUZ     = new Color(1.000f, 0.980f, 0.920f, 0.40f);
    static readonly Color C_ESC_PARED    = new Color(0.700f, 0.690f, 0.668f, 1f);
    static readonly Color C_ESC_STEP_CLR = new Color(0.800f, 0.792f, 0.768f, 1f);
    static readonly Color C_ESC_STEP_OSC = new Color(0.400f, 0.392f, 0.378f, 1f);
    static readonly Color C_ESC_BARAND   = new Color(0.540f, 0.540f, 0.535f, 1f);
    static readonly Color C_ESC_BARAND2  = new Color(0.700f, 0.698f, 0.688f, 1f);
    static readonly Color C_LETRERO_BG   = new Color(0.080f, 0.200f, 0.520f, 1f);
    static readonly Color C_LETRERO_BRD  = new Color(0.780f, 0.640f, 0.180f, 1f);
    static readonly Color C_SOMBRA_FUERTE = new Color(0.040f, 0.040f, 0.048f, 0.70f);
    static readonly Color C_SOMBRA_SUAVE  = new Color(0.060f, 0.060f, 0.072f, 0.38f);

    const float SAL_W    = 20f;
    const float SAL_H    = 18f;
    const float PAS_H    = 9f;
    const float PARED_G  = 1.2f;
    const float PUERTA_W = 3.1f;
    const float CARA_H   = 1.7f;

    const float Y_PARED_NORTE =  28f;
    const float Y_SAL_NORTE   =  18f;
    const float Y_DIV_NORTE   =   9f;
    const float Y_PASILLO     =   0f;
    const float Y_DIV_SUR     =  -9f;
    const float Y_SAL_SUR     = -18f;
    const float Y_PARED_SUR   = -28f;

    const float xIzq = -81f;
    const float xDer =  81f;

    Sprite _px;

    void Awake() { _px = MakePixel(); Build(); }

    void Build()
    {
        float totalW = xDer - xIzq;
        float cx     = (xIzq + xDer) / 2f;

        string[] nombresN = { "CB-06","CB-08","CB-09","Papeleria",
                               "Escaleras","Cubiculo 1","Cubiculo 2","Cubiculo 3" };
        string[] nombresS = { "CB-04","CB-03","CB-02","CB-01",
                               "Entrada","Laboratoristas","CB-05","CB-010" };

        BuildRellenoHuecos(cx, totalW);
        BuildSalonesNorte(nombresN);
        BuildSalonesSur(nombresS);
        BuildPasillo(cx, totalW);
        BuildParedDivisoraNorte(nombresN);
        BuildParedDivisoraSur(nombresS);
        BuildDivisionesVerticales();
        BuildParedesExteriores(cx, totalW);
        BuildEscaleras(SalonCX(4), Y_SAL_NORTE);
        BuildColumnasPasillo();
        BuildLamparasPasillo();
        BuildSalida();
        BuildTriggers(nombresN, nombresS);
    }

    void BuildRellenoHuecos(float cx, float totalW)
    {
        float tileW  = 3.0f, tileH = 3.0f;
        float yMin   = Y_PARED_SUR - PARED_G / 2f;
        float yMax   = Y_PARED_NORTE + PARED_G / 2f;
        float h      = yMax - yMin;
        float tileCY = (yMin + yMax) / 2f;

        Vis("PisoTotal_Base", cx, tileCY, totalW, h, C_PISO_A, -10);

        int cols = Mathf.CeilToInt(totalW / tileW) + 1;
        int rows = Mathf.CeilToInt(h      / tileH) + 1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float tx = xIzq + tileW * c + tileW / 2f;
                float ty = yMin  + tileH * r + tileH / 2f;
                Color tc = ((c + r) % 2 == 0) ? C_PISO_A : C_PISO_B;
                Vis("PT_L_" + r + "_" + c, tx, ty, tileW - 0.06f, tileH - 0.06f, tc, -9);
            }
        for (int c = 0; c <= cols; c++)
            Vis("PT_JV_" + c, xIzq + tileW * c, tileCY, 0.06f, h, C_PISO_JUNTA, -8);
        for (int r = 0; r <= rows; r++)
            Vis("PT_JH_" + r, cx, yMin + tileH * r, totalW, 0.06f, C_PISO_JUNTA, -8);
    }

    void BuildSalonesNorte(string[] nombres)
    {
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            if (nombres[i] == "Escaleras") continue;
            BuildInterior(sx, Y_SAL_NORTE, nombres[i], true);
        }
    }

    void BuildSalonesSur(string[] nombres)
    {
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            if (nombres[i] == "Entrada") continue;
            BuildInterior(sx, Y_SAL_SUR, nombres[i], false);
        }
    }

    void BuildInterior(float sx, float cy, string nombre, bool esNorte)
    {
        float w = SAL_W - PARED_G;
        float h = SAL_H - PARED_G;

        float lampY1 = cy + h * 0.15f, lampY2 = cy - h * 0.15f;
        Vis("Lamp_"     + nombre + "_1", sx, lampY1, w * 0.6f,  0.22f, C_LAMP_LUZ,    -1);
        Vis("LampTubo_" + nombre + "_1", sx, lampY1, w * 0.55f, 0.12f, C_LAMP_CUERPO,  0);
        Vis("Lamp_"     + nombre + "_2", sx, lampY2, w * 0.6f,  0.22f, C_LAMP_LUZ,    -1);
        Vis("LampTubo_" + nombre + "_2", sx, lampY2, w * 0.55f, 0.12f, C_LAMP_CUERPO,  0);

        bool esCub = nombre.StartsWith("Cubiculo");
        bool esPap = nombre == "Papeleria";
        bool esLab = nombre == "Laboratoristas";

        if (esCub)       BuildCubiculo(sx, cy, nombre, esNorte, w, h);
        else if (esPap)  BuildPapeleria(sx, cy, nombre, w, h);
        else if (esLab)  BuildLaboratorio(sx, cy, nombre, w, h);
        else             BuildSalonClases(sx, cy, nombre, esNorte, w, h);
    }

    void BuildSalonClases(float sx, float cy, string nombre, bool esNorte, float w, float h)
    {
        float sign = esNorte ? 1f : -1f;

        // Pizarron pegado al fondo del salon
        float fondoY = cy + (h / 2f - 1.2f) * sign;
        Vis("PizMarco_" + nombre, sx, fondoY, w * 0.75f + 0.5f, 2.6f, C_PIZ_BORDE, 0);
        Vis("Pizarron_" + nombre, sx, fondoY, w * 0.75f,         2.0f, C_PIZARRON,  1);
        Vis("Tiza1_"    + nombre, sx - w*0.15f, fondoY + 0.10f, w*0.18f, 0.14f, C_PIZ_TIZA, 2);
        Vis("Tiza2_"    + nombre, sx + w*0.10f, fondoY - 0.10f, w*0.10f, 0.10f, C_PIZ_TIZA, 2);

        // Escritorio del maestro debajo del pizarron
        float escY = cy + (h / 2f - 4.0f) * sign;
        Vis("EscritSom_"  + nombre, sx + 0.20f, escY - 0.20f*sign, 6.0f, 2.0f, C_SOMBRA_SUAVE, 0);
        Vis("EscritBase_" + nombre, sx, escY, 5.8f, 1.8f, C_ESCRITORIO,   1);
        Vis("EscritBord_" + nombre, sx, escY, 6.0f, 2.0f, C_ESCRIT_BORDE, 0);
        Vis("EscritTop_"  + nombre, sx, escY + 0.30f*sign, 5.5f, 0.65f,
            new Color(0.64f,0.50f,0.28f,1f), 2);

        // Sillas: 3 filas x 4 columnas correctamente dentro del salon
        // Norte: pizarron arriba (y+), sillas van hacia y-
        // Sur:   pizarron abajo (y-), sillas van hacia y+
        float sillaStartY = cy + (h / 2f - 7.0f) * sign;  // primera fila cerca del escritorio
        float sillaStepY  = -3.5f * sign;                  // cada fila hacia la puerta

        float colSpacing = w * 0.22f;
        float colStart   = sx - colSpacing * 1.5f;

        for (int fila = 0; fila < 3; fila++)
        {
            float rowY = sillaStartY + sillaStepY * fila;
            for (int col = 0; col < 4; col++)
            {
                float colX = colStart + colSpacing * col;
                BuildSillaPaleta(nombre + "_" + fila + "_" + col, colX, rowY);
            }
        }
    }

    void BuildSillaPaleta(string id, float x, float y)
    {
        Vis("SilSom_"   + id, x+0.08f, y-0.08f, 1.05f, 0.80f, C_SOMBRA_SUAVE,                  0);
        Vis("SilBord_"  + id, x, y, 1.00f, 0.75f, new Color(0.12f,0.28f,0.12f,1f),              0);
        Vis("SilAsien_" + id, x, y, 0.88f, 0.65f, C_SILLA_ASIEN,                                1);
        Vis("SilPala_"  + id, x+0.40f, y+0.10f, 0.52f, 0.35f, C_SILLA_PALA,                     2);
        Vis("SilPataL_" + id, x-0.32f, y-0.27f, 0.10f, 0.12f, C_SILLA_METAL,                    2);
        Vis("SilPataR_" + id, x+0.32f, y-0.27f, 0.10f, 0.12f, C_SILLA_METAL,                    2);
    }

    void BuildCubiculo(float sx, float cy, string nombre, bool esNorte, float w, float h)
    {
        float sign = esNorte ? 1f : -1f;
        Vis("CubDesk1_"    + nombre, sx-0.6f,    cy+1.5f*sign, w*0.6f, 1.2f, C_ESCRITORIO, 1);
        Vis("CubDesk2_"    + nombre, sx-w*0.2f,  cy+0.3f*sign, 1.2f,   2.5f, C_ESCRITORIO, 1);
        Vis("Monitor_"     + nombre, sx, cy+2.0f*sign, 1.3f, 0.85f, new Color(0.10f,0.12f,0.18f,1f), 2);
        Vis("MonitorPant_" + nombre, sx, cy+2.0f*sign, 1.1f, 0.68f, new Color(0.14f,0.32f,0.52f,1f), 3);
        Vis("SillaGir_"    + nombre, sx+1.5f, cy-0.5f*sign, 1.1f, 1.1f, C_SILLA_ASIEN, 1);
    }

    void BuildPapeleria(float sx, float cy, string nombre, float w, float h)
    {
        Vis("MostrFrent_" + nombre, sx, cy+0.8f, w*0.65f, 0.4f,  new Color(0.38f,0.28f,0.14f,1f), 2);
        Vis("Mostrador_"  + nombre, sx, cy+1.0f, w*0.65f, 1.4f,  C_ESCRITORIO, 1);
        Vis("MostrTop_"   + nombre, sx, cy+1.8f, w*0.63f, 0.28f, new Color(0.64f,0.50f,0.28f,1f), 2);
        float shelfW = (w - 1f) / 3f;
        for (int s = 0; s < 3; s++)
        {
            float shX = sx - (w/2f - 0.5f) + shelfW*s + shelfW/2f;
            Vis("Estante_" + nombre + "_" + s, shX, cy+3.5f, shelfW-0.12f, 3.8f,
                new Color(0.50f,0.38f,0.20f,1f), 1);
            for (int t = 0; t < 4; t++)
                Vis("Tablilla_" + nombre + "_" + s + "_" + t, shX, cy+1.8f+t*0.9f,
                    shelfW-0.16f, 0.10f, new Color(0.35f,0.26f,0.12f,1f), 2);
        }
    }

    void BuildLaboratorio(float sx, float cy, string nombre, float w, float h)
    {
        Vis("LabMesa1_" + nombre, sx,           cy+2.8f, w*0.75f, 1.3f, C_ESCRITORIO, 1);
        Vis("LabMesa2_" + nombre, sx-w*0.28f,   cy-0.5f, 1.2f,    4.5f, C_ESCRITORIO, 1);
        Vis("LabMesa3_" + nombre, sx+w*0.28f,   cy-0.5f, 1.2f,    4.5f, C_ESCRITORIO, 1);
        for (int e = 0; e < 3; e++)
            Vis("Equipo_" + nombre + "_" + e, sx-w*0.25f+e*(w*0.25f), cy+2.8f,
                0.85f, 0.70f, new Color(0.12f,0.12f,0.16f,1f), 2);
    }

    void BuildPasillo(float cx, float totalW)
    {
        Vis("PasBase", cx, Y_PASILLO, totalW, PAS_H, C_PISO_A, -6);
        float tileW = 3.0f, tileH = 3.0f;
        int cols = Mathf.CeilToInt(totalW / tileW) + 1;
        int rows = Mathf.CeilToInt(PAS_H  / tileH) + 1;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float tx = xIzq + tileW*c + tileW/2f;
                float ty = Y_PASILLO - PAS_H/2f + tileH*r + tileH/2f;
                Color tc = ((c+r)%2==0) ? C_PISO_A : C_PISO_B;
                Vis("Loseta_"+r+"_"+c, tx, ty, tileW-0.06f, tileH-0.06f, tc, -5);
            }
        for (int c = 0; c <= cols; c++)
            Vis("JuntaV_"+c, xIzq+tileW*c, Y_PASILLO, 0.06f, PAS_H, C_PISO_JUNTA, -4);
        for (int r = 0; r <= rows; r++)
            Vis("JuntaH_"+r, cx, Y_PASILLO-PAS_H/2f+tileH*r, totalW, 0.06f, C_PISO_JUNTA, -4);
        Vis("Desgaste", cx, Y_PASILLO, totalW, 0.40f, C_PISO_SOMBRA, -3);
    }

    void BuildParedDivisoraNorte(string[] nombres)
    {
        for (int i = 0; i < 8; i++)
        {
            float sx  = xIzq + 1f + SAL_W * i;
            float scx = sx + SAL_W / 2f;
            bool esEsc = nombres[i] == "Escaleras";

            if (esEsc)
            {
                float segW = (SAL_W - PUERTA_W * 1.6f) / 2f;
                Vis("MuroN_"+i+"_L", sx+segW/2f,        Y_DIV_NORTE, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColN_"+i+"_L", sx+segW/2f,      Y_DIV_NORTE, segW, PARED_G);
                Vis("MuroN_"+i+"_R", sx+SAL_W-segW/2f,  Y_DIV_NORTE, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColN_"+i+"_R", sx+SAL_W-segW/2f,Y_DIV_NORTE, segW, PARED_G);
                Vis("CaraN_"+i+"_L", sx+segW/2f,        Y_DIV_NORTE-PARED_G/2f-CARA_H/2f, segW, CARA_H, C_PARED_CARA, 10);
                Vis("CaraN_"+i+"_R", sx+SAL_W-segW/2f,  Y_DIV_NORTE-PARED_G/2f-CARA_H/2f, segW, CARA_H, C_PARED_CARA, 10);
            }
            else
            {
                float segW = (SAL_W - PUERTA_W) / 2f;
                Vis("MuroN_"+i+"_L", sx+segW/2f,        Y_DIV_NORTE, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColN_"+i+"_L", sx+segW/2f,      Y_DIV_NORTE, segW, PARED_G);
                Vis("MuroN_"+i+"_R", sx+SAL_W-segW/2f,  Y_DIV_NORTE, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColN_"+i+"_R", sx+SAL_W-segW/2f,Y_DIV_NORTE, segW, PARED_G);
                BuildPuerta("PN_"+i, scx, Y_DIV_NORTE, true);
                Vis("CaraN_"+i,   scx, Y_DIV_NORTE-PARED_G/2f-CARA_H/2f,    SAL_W, CARA_H, C_PARED_CARA,    10);
                Vis("ZocaloN_"+i, scx, Y_DIV_NORTE-PARED_G/2f-CARA_H-0.22f, SAL_W, 0.45f,  C_PARED_ZOCALO,  11);
                Vis("SomN_"+i,    scx, Y_DIV_NORTE-PARED_G/2f-CARA_H-0.70f, SAL_W, 0.65f,  C_SOMBRA_FUERTE,  1);
                float letrY = Y_DIV_NORTE-PARED_G/2f-CARA_H/2f;
                Vis("LetrBrd_N_"+i, scx, letrY, PUERTA_W+0.70f, 1.05f, C_LETRERO_BRD, 12);
                Vis("LetrBg_N_"+i,  scx, letrY, PUERTA_W+0.45f, 0.80f, C_LETRERO_BG,  13);
            }
        }
    }

    void BuildParedDivisoraSur(string[] nombres)
    {
        for (int i = 0; i < 8; i++)
        {
            float sx  = xIzq + 1f + SAL_W * i;
            float scx = sx + SAL_W / 2f;
            bool esEnt = nombres[i] == "Entrada";

            if (esEnt)
            {
                float segW = (SAL_W - PUERTA_W * 1.6f) / 2f;
                Vis("MuroS_"+i+"_L", sx+segW/2f,        Y_DIV_SUR, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColS_"+i+"_L", sx+segW/2f,      Y_DIV_SUR, segW, PARED_G);
                Vis("MuroS_"+i+"_R", sx+SAL_W-segW/2f,  Y_DIV_SUR, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColS_"+i+"_R", sx+SAL_W-segW/2f,Y_DIV_SUR, segW, PARED_G);
                Vis("CaraS_Top_"+i+"_L", sx+segW/2f,       Y_DIV_SUR+PARED_G/2f+CARA_H/2f, segW, CARA_H, C_PARED_CARA, 250);
                Vis("CaraS_Top_"+i+"_R", sx+SAL_W-segW/2f, Y_DIV_SUR+PARED_G/2f+CARA_H/2f, segW, CARA_H, C_PARED_CARA, 250);
            }
            else
            {
                float segW = (SAL_W - PUERTA_W) / 2f;
                Vis("MuroS_"+i+"_L", sx+segW/2f,        Y_DIV_SUR, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColS_"+i+"_L", sx+segW/2f,      Y_DIV_SUR, segW, PARED_G);
                Vis("MuroS_"+i+"_R", sx+SAL_W-segW/2f,  Y_DIV_SUR, segW, PARED_G, C_PARED_MURO, 0);
                MuroCol("ColS_"+i+"_R", sx+SAL_W-segW/2f,Y_DIV_SUR, segW, PARED_G);
                BuildPuerta("PS_"+i, scx, Y_DIV_SUR, false);
                Vis("CaraS_Top_"+i,   scx, Y_DIV_SUR+PARED_G/2f+CARA_H/2f,    SAL_W, CARA_H, C_PARED_CARA,   250);
                Vis("ZocaloS_Top_"+i, scx, Y_DIV_SUR+PARED_G/2f+CARA_H+0.22f, SAL_W, 0.45f,  C_PARED_ZOCALO, 251);
                float letrYTop = Y_DIV_SUR+PARED_G/2f+CARA_H/2f;
                Vis("LetrBrd_S_Top_"+i, scx, letrYTop, PUERTA_W+0.70f, 1.05f, C_LETRERO_BRD, 252);
                Vis("LetrBg_S_Top_"+i,  scx, letrYTop, PUERTA_W+0.45f, 0.80f, C_LETRERO_BG,  253);
            }
        }
    }

    void BuildPuerta(string id, float cx, float wallY, bool esNorte)
    {
        float frameH = PARED_G + 0.06f;
        Vis("PMarcoExt_"+id, cx, wallY, PUERTA_W+0.36f, frameH+0.05f, C_PUERTA_ALU2, 1);
        Vis("PMarcoInt_"+id, cx, wallY, PUERTA_W+0.16f, frameH,        C_PUERTA_ALU,  2);
        Vis("PVid_"+id,      cx, wallY, PUERTA_W-0.26f, frameH-0.13f,  C_PUERTA_VID,  3);
        Vis("PVidRef_"+id, cx-PUERTA_W*0.18f, wallY+0.06f, PUERTA_W*0.22f, frameH*0.55f, C_PUERTA_VID2, 4);
        Vis("Manija_"+id,   cx, wallY, PUERTA_W-0.45f, 0.13f, C_PUERTA_ALU2, 5);
        Vis("Bisagra1_"+id, cx-PUERTA_W/2f+0.22f, wallY+0.22f, 0.13f, 0.22f, C_PUERTA_ALU2, 5);
        Vis("Bisagra2_"+id, cx-PUERTA_W/2f+0.22f, wallY-0.22f, 0.13f, 0.22f, C_PUERTA_ALU2, 5);
        float sombraDir = esNorte ? -1f : 1f;
        Vis("PSom_"+id, cx, wallY+sombraDir*(PARED_G+0.45f), PUERTA_W+0.26f, 0.70f, C_PUERTA_SOM, 1);
    }

    void BuildDivisionesVerticales()
    {
        for (int i = 0; i <= 8; i++)
        {
            float xd = xIzq + 1f + SAL_W * i;
            Vis("DivVN_"+i, xd, Y_SAL_NORTE, PARED_G, SAL_H+PARED_G, C_PARED_MURO, 0);
            Vis("DivVS_"+i, xd, Y_SAL_SUR,   PARED_G, SAL_H+PARED_G, C_PARED_MURO, 0);
            MuroCol("DivVertN_"+i, xd, Y_SAL_NORTE, PARED_G, SAL_H+PARED_G);
            MuroCol("DivVertS_"+i, xd, Y_SAL_SUR,   PARED_G, SAL_H+PARED_G);
        }
    }

    void BuildParedesExteriores(float cx, float totalW)
    {
        Vis("ExtNVis",  cx, Y_PARED_NORTE, totalW, PARED_G, C_PARED_MURO, 0);
        MuroCol("ExtN", cx, Y_PARED_NORTE, totalW, PARED_G);

        float entCX = SalonCX(4);
        float izqW  = entCX - PUERTA_W/2f - xIzq;
        float derSt = entCX + PUERTA_W/2f;
        float derW  = xDer - derSt;
        Vis("ExtSIzqVis",    xIzq+izqW/2f, Y_PARED_SUR, izqW, PARED_G, C_PARED_MURO, 0);
        MuroCol("ExtSIzq",   xIzq+izqW/2f, Y_PARED_SUR, izqW, PARED_G);
        Vis("ExtSDerVis",    derSt+derW/2f, Y_PARED_SUR, derW, PARED_G, C_PARED_MURO, 0);
        MuroCol("ExtSDer",   derSt+derW/2f, Y_PARED_SUR, derW, PARED_G);
        Vis("ExtSFranjaIzq", xIzq+izqW/2f, Y_PARED_SUR-PARED_G, izqW, PARED_G*0.6f, C_PARED_EXT, 0);
        Vis("ExtSFranjaDer", derSt+derW/2f, Y_PARED_SUR-PARED_G, derW, PARED_G*0.6f, C_PARED_EXT, 0);

        float midY   = (Y_PARED_NORTE + Y_PARED_SUR) / 2f;
        float altTot = Y_PARED_NORTE - Y_PARED_SUR;
        Vis("ExtOVis",  xIzq+PARED_G/2f, midY, PARED_G, altTot, C_PARED_MURO, 0);
        MuroCol("ExtO", xIzq+PARED_G/2f, midY, PARED_G, altTot);
        Vis("ExtEVis",  xDer-PARED_G/2f, midY, PARED_G, altTot, C_PARED_MURO, 0);
        MuroCol("ExtE", xDer-PARED_G/2f, midY, PARED_G, altTot);
    }

    void BuildEscaleras(float cx, float cy)
    {
        float w = SAL_W - PARED_G * 2f;
        float h = SAL_H - PARED_G * 2f;
        Vis("EscFondo", cx, cy, w, h, C_ESC_PARED, -2);

        float tramoW  = w * 0.38f;
        float tramoH  = h * 0.82f;
        float tramoY  = cy + h * 0.04f;
        float tramoXL = cx - w * 0.26f;
        float tramoXR = cx + w * 0.26f;
        float stepH   = tramoH / 8;

        for (int s = 0; s < 8; s++)
        {
            float t     = (float)s / 7f;
            Color stepC = Color.Lerp(C_ESC_STEP_OSC, C_ESC_STEP_CLR, t);
            Color shadC = Color.Lerp(C_SOMBRA_FUERTE, C_ESC_STEP_OSC, t*0.5f);
            float stepY = tramoY - tramoH/2f + stepH*s + stepH/2f;
            Vis("EscL_Step_"+s, tramoXL, stepY, tramoW, stepH-0.09f, stepC,         -3);
            Vis("EscL_Shad_"+s, tramoXL, stepY-stepH*0.44f, tramoW, 0.15f, shadC,   -2);
            Vis("EscL_Frnt_"+s, tramoXL, stepY-stepH*0.5f+0.06f, tramoW, 0.10f, C_ESC_STEP_OSC, -1);
            Vis("EscR_Step_"+s, tramoXR, stepY, tramoW, stepH-0.09f, stepC,         -3);
            Vis("EscR_Shad_"+s, tramoXR, stepY-stepH*0.44f, tramoW, 0.15f, shadC,   -2);
            Vis("EscR_Frnt_"+s, tramoXR, stepY-stepH*0.5f+0.06f, tramoW, 0.10f, C_ESC_STEP_OSC, -1);
        }

        float descansoW = w * 0.22f;
        Vis("EscDescanso", cx, tramoY, descansoW, tramoH, C_ESC_STEP_CLR, -3);
        Vis("EscDescBord", cx, tramoY, descansoW, tramoH,
            new Color(C_ESC_STEP_OSC.r, C_ESC_STEP_OSC.g, C_ESC_STEP_OSC.b, 0.4f), -2);

        float[] brdXs = {
            tramoXL-tramoW*0.5f+0.10f, tramoXL+tramoW*0.5f-0.10f,
            tramoXR-tramoW*0.5f+0.10f, tramoXR+tramoW*0.5f-0.10f,
        };
        foreach (float bx in brdXs)
        {
            Vis("Brd_"+bx,  bx, tramoY, 0.16f, tramoH+0.25f, C_ESC_BARAND,  2);
            Vis("BrdT_"+bx, bx, tramoY, 0.25f, tramoH+0.25f, C_ESC_BARAND2, 1);
        }
        for (int p = 0; p < 5; p++)
        {
            float py = tramoY - tramoH/2f + (tramoH/4f)*p;
            foreach (float bx in brdXs)
                Vis("Post_"+p+"_"+bx, bx, py, 0.20f, 0.26f, C_ESC_STEP_OSC, 3);
        }
        MuroCol("EscBrdColL", tramoXL-tramoW*0.5f, tramoY, 0.18f, tramoH);
        MuroCol("EscBrdColR", tramoXR+tramoW*0.5f, tramoY, 0.18f, tramoH);
    }

    void BuildSalida()
    {
        float entCX  = SalonCX(4);
        float entY   = Y_PARED_SUR;
        float huecoW = SAL_W - PARED_G;
        float pisoH  = Mathf.Abs(Y_DIV_SUR - Y_PARED_SUR) + PARED_G;
        float pisoCY = (Y_DIV_SUR + Y_PARED_SUR) / 2f;

        Vis("EntradaPisoBase", entCX, pisoCY, huecoW, pisoH, C_PISO_A, -6);
        float tileW = 3.0f, tileH = 3.0f;
        int cols = Mathf.CeilToInt(huecoW/tileW) + 2;
        int rows = Mathf.CeilToInt(pisoH /tileH) + 2;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float tx = entCX - huecoW/2f + tileW*c + tileW/2f;
                float ty = Y_PARED_SUR - PARED_G/2f + tileH*r + tileH/2f;
                Color tc = ((c+r)%2==0) ? C_PISO_A : C_PISO_B;
                Vis("EntLoseta_"+r+"_"+c, tx, ty, tileW-0.06f, tileH-0.06f, tc, -5);
            }
        Vis("SalidaFranja", entCX, Y_DIV_SUR+1.3f, huecoW, 2.3f,
            new Color(0.75f,0.65f,0.15f,0.25f), 2);

        GameObject go = new GameObject("TriggerSalida");
        go.transform.position   = new Vector3(entCX, entY+0.8f, 0);
        go.transform.localScale = new Vector3(PUERTA_W+0.6f, 2.0f, 1f);
        go.transform.SetParent(transform);
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one; bc.isTrigger = true;
        var p = go.AddComponent<Puerta>();
        p.escenaDestino = "SampleScene";
    }

    void BuildColumnasPasillo()
    {
        for (int i = 1; i < 8; i++)
        {
            float colX = xIzq + 1f + SAL_W * i;
            BuildColumna(colX, Y_DIV_NORTE, "N");
            BuildColumna(colX, Y_DIV_SUR,   "S");
        }
    }

    void BuildColumna(float x, float y, string suffix)
    {
        string id = "Col_"+x+"_"+suffix;
        float cw = 1.10f, ch = PARED_G + 0.65f;
        Vis(id+"_Som",  x+0.12f, y-0.12f, cw+0.12f, ch+0.12f, C_SOMBRA_SUAVE, 0);
        Vis(id+"_Cpo",  x, y, cw, ch, C_COLUMNA_CLR, 2);
        Vis(id+"_BrdL", x-cw/2f+0.08f, y, 0.15f, ch, C_COLUMNA_OSC, 3);
        Vis(id+"_BrdR", x+cw/2f-0.08f, y, 0.15f, ch, C_COLUMNA_OSC, 3);
        Vis(id+"_Cap",  x, y+ch/2f, cw+0.22f, 0.22f, C_COLUMNA_OSC, 4);
        Vis(id+"_Bas",  x, y-ch/2f, cw+0.22f, 0.22f, C_COLUMNA_OSC, 4);
    }

    void BuildLamparasPasillo()
    {
        for (int i = 0; i < 8; i++)
        {
            float lx = SalonCX(i);
            float ly = Y_DIV_NORTE - PARED_G/2f - CARA_H - 1.0f;
            Vis("Halo_"+i,     lx, Y_PASILLO, 6.5f, 6.5f, C_LAMP_LUZ,                    -2);
            Vis("LampCarc_"+i, lx, ly, 2.30f, 0.22f, C_LAMP_CUERPO,                        5);
            Vis("LampTubo_"+i, lx, ly, 2.00f, 0.11f, new Color(1f,0.97f,0.88f,1f),         6);
        }
    }

    void BuildTriggers(string[] nombresN, string[] nombresS)
    {
        for (int i = 0; i < 8; i++)
        {
            float sx = SalonCX(i);
            if (nombresN[i] != "Escaleras")
                Area("AreaN_"+i, sx, Y_SAL_NORTE, SAL_W-PARED_G, SAL_H-PARED_G, nombresN[i]);
            if (nombresS[i] != "Entrada")
                Area("AreaS_"+i, sx, Y_SAL_SUR,   SAL_W-PARED_G, SAL_H-PARED_G, nombresS[i]);
        }
    }

    float SalonCX(int i) => xIzq + 1f + SAL_W * i + SAL_W / 2f;

    void MuroCol(string n, float x, float y, float w, float h)
    {
        GameObject go = new GameObject(n);
        go.transform.position   = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _px; sr.color = C_PARED_MURO; sr.sortingOrder = 0;
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one; bc.isTrigger = false;
    }

    void Vis(string n, float x, float y, float w, float h, Color c, int order)
    {
        GameObject go = new GameObject(n);
        go.transform.position   = new Vector3(x, y, 0);
        go.transform.localScale = new Vector3(w, h, 1f);
        go.transform.SetParent(transform);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _px; sr.color = c; sr.sortingOrder = order;
    }

    void Area(string n, float x, float y, float w, float h, string label)
    {
        GameObject go = new GameObject(n);
        go.transform.position   = new Vector3(x, y, 0);
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