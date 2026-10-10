using UnityEngine;

/// Texturas procedurais no estilo da referencia:
/// simbolos das 4 pistas + paineis neon com cantos arredondados.
public static class SymbolTex
{
    public static Color LaneColor(int lane)
    {
        switch (lane)
        {
            case 0: return new Color(0.22f, 1f, 0.42f);
            case 1: return new Color(0.16f, 0.81f, 1f);
            case 2: return new Color(1f, 0.85f, 0.23f);
            default: return new Color(1f, 0.23f, 0.36f);
        }
    }

    /// Simbolo da pista em 128px, cor neon sobre transparente.
    public static Texture2D MakeSymbol(int lane, int size = 128)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(t);
        Color c = LaneColor(lane);
        float R = size * 0.5f;
        float cx = size / 2f, cy = size / 2f;
        if (lane == 0)
        {
            // espiral
            float turns = 2.6f; int steps = 220;
            for (int i = 0; i <= steps; i++)
            {
                float a = (float)i / steps * turns * Mathf.PI * 2f;
                float r = R * 0.92f * i / steps;
                Dot(t, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, size * 0.022f, c);
            }
        }
        else if (lane == 1)
        {
            // olho: amendoa + pupila
            for (int i = 0; i <= 60; i++)
            {
                float x = Mathf.Lerp(-R * 0.9f, R * 0.9f, i / 60f);
                float y = Mathf.Sin(i / 60f * Mathf.PI) * R * 0.55f;
                Dot(t, cx + x, cy - y, size * 0.020f, c);
                Dot(t, cx + x, cy + y, size * 0.020f, c);
            }
            FilledCircle(t, cx, cy, R * 0.32f, c);
            FilledCircle(t, cx, cy, R * 0.13f, Color.white);
        }
        else if (lane == 2)
        {
            // sol: nucleo + 12 raios
            FilledCircle(t, cx, cy, R * 0.34f, c);
            Ring(t, cx, cy, R * 0.34f, size * 0.020f, c);
            for (int k = 0; k < 12; k++)
            {
                float a = k / 12f * Mathf.PI * 2f + 0.26f;
                Line(t, cx + Mathf.Cos(a) * R * 0.50f, cy + Mathf.Sin(a) * R * 0.50f,
                        cx + Mathf.Cos(a) * R * 0.92f, cy + Mathf.Sin(a) * R * 0.92f,
                        size * 0.022f, c);
            }
        }
        else
        {
            // triskel: 3 virgulas a 120 graus
            for (int k = 0; k < 3; k++)
            {
                float a0 = k / 3f * Mathf.PI * 2f;
                float ccx = cx + Mathf.Cos(a0) * R * 0.28f;
                float ccy = cy + Mathf.Sin(a0) * R * 0.28f;
                for (int i = 0; i <= 40; i++)
                {
                    float a = a0 + Mathf.PI * 0.5f + (i / 40f) * Mathf.PI * 1.2f;
                    Dot(t, ccx + Mathf.Cos(a) * R * 0.44f, ccy + Mathf.Sin(a) * R * 0.44f, size * 0.024f, c);
                }
                float tip = a0 + Mathf.PI * 1.7f;
                Dot(t, ccx + Mathf.Cos(tip) * R * 0.44f, ccy + Mathf.Sin(tip) * R * 0.44f, size * 0.035f, c);
            }
        }
        t.Apply();
        return t;
    }

    /// Painel escuro translucido com borda neon e cantos arredondados.
    public static Texture2D MakePanel(int w, int h, Color fill, Color border, int bw = 4, int radius = 20)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Clear(t);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // cantos arredondados: fora do raio = transparente
                float dx = Mathf.Min(x, w - 1 - x);
                float dy = Mathf.Min(y, h - 1 - y);
                if (dx < radius && dy < radius)
                {
                    float dd = Mathf.Sqrt((radius - dx) * (radius - dx) + (radius - dy) * (radius - dy));
                    if (dd > radius) continue;
                }
                if (dx < bw || dy < bw)
                    t.SetPixel(x, y, border);
                else
                    t.SetPixel(x, y, fill);
            }
        t.Apply();
        return t;
    }

    /// Setas de energia brancas (o material tinta por pista). Rola em direcao ao jogador.
    public static Texture2D MakeStripes(int w = 64, int h = 256)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Clear(t);
        Color c = Color.white;
        for (int row = 0; row < 4; row++)
        {
            float yc = h * (0.125f + row * 0.25f);
            // chevron apontando para baixo (v=0 = jogador)
            Line(t, w * 0.12f, yc + h * 0.07f, w * 0.5f, yc - h * 0.07f, w * 0.07f, c);
            Line(t, w * 0.88f, yc + h * 0.07f, w * 0.5f, yc - h * 0.07f, w * 0.07f, c);
            Dot(t, w * 0.5f, yc - h * 0.07f, w * 0.09f, c);
        }
        // linha central
        Line(t, w * 0.5f, 0, w * 0.5f, h, w * 0.03f, new Color(1, 1, 1, 0.6f));
        t.wrapMode = TextureWrapMode.Repeat;
        t.Apply();
        return t;
    }

    /// Silhueta da multidao com palitos neon. Deterministica (sem random).
    public static Texture2D MakeCrowd(int w = 1024, int h = 128)
    {
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Clear(t);
        Color sil = new Color(0.03f, 0.01f, 0.07f, 1f);
        for (int x = 0; x < w; x++)
        {
            float n = Mathf.Sin(x * 0.021f) * 0.5f + Mathf.Sin(x * 0.053f + 1.7f) * 0.3f + Mathf.Sin(x * 0.11f + 4.2f) * 0.2f;
            float head = h * (0.45f + n * 0.22f);
            for (int y = 0; y < (int)head; y++)
                t.SetPixel(x, y, sil);
            // cabeca redonda
            FilledCircle(t, x, head, h * 0.075f * (0.8f + 0.4f * (0.5f + 0.5f * n)), sil);
        }
        // palitos neon levantados
        Color[] cols = new Color[] {
            new Color(0.22f, 1f, 0.42f), new Color(0.16f, 0.81f, 1f),
            new Color(1f, 0.85f, 0.23f), new Color(1f, 0.23f, 0.36f),
            new Color(0.7f, 0.3f, 1f) };
        int k = 0;
        for (int x = 20; x < w - 10; x += 34)
        {
            float hh = h * (0.55f + 0.3f * (0.5f + 0.5f * Mathf.Sin(x * 0.13f + k)));
            Color c = cols[k % cols.Length];
            Line(t, x, hh - h * 0.25f, x, hh, 2.5f, c);
            Dot(t, x, hh + 4, 4.5f, c);
            Dot(t, x, hh + 4, 8f, new Color(c.r, c.g, c.b, 0.35f));
            k++;
        }
        t.Apply();
        return t;
    }

    /// Mandala psicodelica para o horizonte da pista.
    public static Texture2D MakeMandala(int size = 512)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Clear(t);
        float cx = size / 2f, cy = size / 2f, R = size * 0.5f;
        Color cyan = new Color(0.2f, 0.9f, 1f);
        Color mag = new Color(1f, 0.25f, 0.75f);
        Color grn = new Color(0.3f, 1f, 0.5f);
        // aneis
        Ring(t, cx, cy, R * 0.97f, size * 0.008f, cyan);
        Ring(t, cx, cy, R * 0.88f, size * 0.006f, mag);
        Ring(t, cx, cy, R * 0.62f, size * 0.008f, cyan);
        Ring(t, cx, cy, R * 0.38f, size * 0.010f, mag);
        Ring(t, cx, cy, R * 0.16f, size * 0.012f, grn);
        // petalas externas (24)
        for (int k = 0; k < 24; k++)
        {
            float a = k / 24f * Mathf.PI * 2f;
            Color c = (k % 2 == 0) ? cyan : mag;
            for (int i = 0; i <= 30; i++)
            {
                float r = Mathf.Lerp(R * 0.64f, R * 0.86f, i / 30f);
                float wob = Mathf.Sin(i / 30f * Mathf.PI) * R * 0.035f;
                float aa = a + R * 0.02f / Mathf.Max(r, 1f);
                Dot(t, cx + Mathf.Cos(a) * r - Mathf.Sin(a) * wob, cy + Mathf.Sin(a) * r + Mathf.Cos(a) * wob, size * 0.008f, c);
                Dot(t, cx + Mathf.Cos(a) * r + Mathf.Sin(a) * wob, cy + Mathf.Sin(a) * r - Mathf.Cos(a) * wob, size * 0.008f, c);
            }
            Dot(t, cx + Mathf.Cos(a) * R * 0.92f, cy + Mathf.Sin(a) * R * 0.92f, size * 0.012f, c);
        }
        // petalas internas (12)
        for (int k = 0; k < 12; k++)
        {
            float a = (k + 0.5f) / 12f * Mathf.PI * 2f;
            Line(t, cx + Mathf.Cos(a) * R * 0.20f, cy + Mathf.Sin(a) * R * 0.20f,
                    cx + Mathf.Cos(a) * R * 0.36f, cy + Mathf.Sin(a) * R * 0.36f, size * 0.010f, grn);
            Dot(t, cx + Mathf.Cos(a) * R * 0.38f, cy + Mathf.Sin(a) * R * 0.38f, size * 0.014f, mag);
        }
        // pontos orbitais
        for (int k = 0; k < 36; k++)
        {
            float a = k / 36f * Mathf.PI * 2f;
            Dot(t, cx + Mathf.Cos(a) * R * 0.74f, cy + Mathf.Sin(a) * R * 0.74f, size * 0.010f, (k % 3 == 0) ? mag : cyan);
        }
        // nucleo brilhante
        FilledCircle(t, cx, cy, R * 0.12f, Color.white);
        FilledCircle(t, cx, cy, R * 0.09f, new Color(0.7f, 1f, 1f));
        t.Apply();
        return t;
    }

    /// Recorta o preto do selo (chroma-key) e devolve textura limpa.
    public static Texture2D KeyOutBlack(Texture2D src, float threshold = 0.14f)
    {
        int w = src.width, h = src.height;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = src.GetPixels();
        for (int i = 0; i < px.Length; i++)
        {
            if (px[i].r + px[i].g + px[i].b < threshold) px[i].a = 0f;
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    static void Clear(Texture2D t)
    {
        Color[] c = new Color[t.width * t.height];
        for (int i = 0; i < c.Length; i++) c[i] = Color.clear;
        t.SetPixels(c);
    }

    static void Dot(Texture2D t, float x, float y, float r, Color c)
    {
        int x0 = Mathf.Max(0, (int)(x - r)), x1 = Mathf.Min(t.width - 1, (int)(x + r));
        int y0 = Mathf.Max(0, (int)(y - r)), y1 = Mathf.Min(t.height - 1, (int)(y + r));
        for (int yy = y0; yy <= y1; yy++)
            for (int xx = x0; xx <= x1; xx++)
            {
                float d = Mathf.Sqrt((xx - x) * (xx - x) + (yy - y) * (yy - y));
                if (d <= r) t.SetPixel(xx, yy, c);
            }
    }

    static void FilledCircle(Texture2D t, float cx, float cy, float r, Color c)
    {
        Dot(t, cx, cy, r, c);
        // preenche o interior: varre linhas dentro do raio
        int x0 = Mathf.Max(0, (int)(cx - r)), x1 = Mathf.Min(t.width - 1, (int)(cx + r));
        int y0 = Mathf.Max(0, (int)(cy - r)), y1 = Mathf.Min(t.height - 1, (int)(cy + r));
        for (int yy = y0; yy <= y1; yy++)
            for (int xx = x0; xx <= x1; xx++)
            {
                float d = Mathf.Sqrt((xx - cx) * (xx - cx) + (yy - cy) * (yy - cy));
                if (d <= r) t.SetPixel(xx, yy, c);
            }
    }

    static void Ring(Texture2D t, float cx, float cy, float r, float w, Color c)
    {
        for (int i = 0; i <= 90; i++)
        {
            float a = i / 90f * Mathf.PI * 2f;
            Dot(t, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, w, c);
        }
    }

    static void Line(Texture2D t, float x0, float y0, float x1, float y1, float w, Color c)
    {
        float len = Mathf.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        int steps = Mathf.Max(1, (int)(len / (w * 0.5f)));
        for (int i = 0; i <= steps; i++)
            Dot(t, Mathf.Lerp(x0, x1, i / (float)steps), Mathf.Lerp(y0, y1, i / (float)steps), w, c);
    }
}
