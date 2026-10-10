using UnityEditor;
using UnityEngine;

/// Busca o grid (BPM + fase) que maximiza a energia nos tempos.
/// Rode batchmode: -executeMethod GridSearch.Run
public static class GridSearch
{
    public static void Run()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/psybeyond.mp3");
        if (clip == null) { Debug.LogError("[GRID] clip nao encontrado"); return; }

        int ch = clip.channels;
        int sr = clip.frequency;
        int total = clip.samples;
        float[] data = new float[total * ch];
        clip.GetData(data, 0);

        // envelope de pico a cada 10ms
        int win = sr / 100;
        int nw = total / win;
        float[] env = new float[nw];
        for (int w = 0; w < nw; w++)
        {
            float peak = 0f;
            for (int i = w * win; i < (w + 1) * win; i++)
            {
                float v = 0f;
                for (int c = 0; c < ch; c++) v += Mathf.Abs(data[i * ch + c]);
                if (v > peak) peak = v;
            }
            env[w] = peak / ch;
        }

        // grade: bpm 130-150 passo 0.1, fase 0..periodo passo 10ms, primeiros 120s
        int useW = Mathf.Min(nw, 120 * 100);
        double bestScore = 0, bestBpm = 0, bestPhase = 0;
        for (double bpm = 130.0; bpm <= 150.01; bpm += 0.1)
        {
            double period = 60.0 / bpm;
            for (double phase = 0.0; phase < period; phase += 0.01)
            {
                double s = 0;
                for (double t = phase; t < useW / 100.0; t += period)
                    s += env[(int)(t * 100)];
                if (s > bestScore) { bestScore = s; bestBpm = bpm; bestPhase = phase; }
            }
        }
        Debug.LogFormat("[GRID] best bpm={0:0.00} phase={1:0.000}s score={2:0.0}", bestBpm, bestPhase, bestScore);

        // top-3 vizinhos para ver se o pico e isolado
        for (double bpm = bestBpm - 0.5; bpm <= bestBpm + 0.51; bpm += 0.1)
        {
            double period = 60.0 / bpm;
            double bs = 0, bph = 0;
            for (double phase = 0.0; phase < period; phase += 0.01)
            {
                double s = 0;
                for (double t = phase; t < useW / 100.0; t += period)
                    s += env[(int)(t * 100)];
                if (s > bs) { bs = s; bph = phase; }
            }
            Debug.LogFormat("[GRID] bpm={0:0.0} phase={1:0.000}s score={2:0.0}", bpm, bph, bs);
        }
    }
}
