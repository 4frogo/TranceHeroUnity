using UnityEditor;
using UnityEngine;

/// Analisa o mp3 importado: acha o primeiro kick e estima o BPM.
/// Rode batchmode: -executeMethod AudioAnalysis.Run
public static class AudioAnalysis
{
    public static void Run()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/psybeyond.mp3");
        if (clip == null) { Debug.LogError("[ANALYSIS] clip nao encontrado"); return; }

        int ch = clip.channels;
        int sr = clip.frequency;
        int total = clip.samples;
        float[] data = new float[total * ch];
        clip.GetData(data, 0);

        Debug.LogFormat("[ANALYSIS] length={0:0.00}s sr={1} ch={2} samples={3}", clip.length, sr, ch, total);

        // envelope de pico em janelas de 10ms (mono)
        int win = sr / 100;
        int nw = total / win;
        float[] env = new float[nw];
        float max = 0f;
        for (int w = 0; w < nw; w++)
        {
            float peak = 0f;
            for (int i = w * win; i < (w + 1) * win; i += ch)
            {
                float v = 0f;
                for (int c = 0; c < ch; c++) v += Mathf.Abs(data[i * 1 + c]);
                v /= ch;
                if (v > peak) peak = v;
            }
            env[w] = peak;
            if (peak > max) max = peak;
        }

        // primeiro kick: primeiro bloco acima de 40% do pico global (nos primeiros 60s)
        int limit = Mathf.Min(nw, sr / 100 * 60);
        double firstKick = -1;
        for (int w = 0; w < limit; w++)
        {
            if (env[w] > max * 0.40f) { firstKick = (double)w / 100.0; break; }
        }
        Debug.LogFormat("[ANALYSIS] picoMax={0:0.000} firstKick={1:0.000}s", max, firstKick);

        // estima BPM por autocorrelacao do envelope (100ms..800ms de lag) nos primeiros 60s
        double bestLag = 0; double bestCorr = 0;
        for (int lag = 10; lag <= 80; lag++)
        {
            double corr = 0; int n = 0;
            for (int w = 0; w + lag < limit; w += 2)
            {
                corr += env[w] * env[w + lag];
                n++;
            }
            corr /= n;
            // prefere lags perto de 140bpm (42.9 janelas) em caso de multiplos
            if (corr > bestCorr) { bestCorr = corr; bestLag = lag; }
        }
        // refina: procura pico harmonico compativel com 130-150bpm (lag 40..46)
        double bestRef = 0; double bestRefCorr = 0;
        for (int lag = 40; lag <= 46; lag++)
        {
            double corr = 0; int n = 0;
            for (int w = 0; w + lag < limit; w += 2) { corr += env[w] * env[w + lag]; n++; }
            corr /= n;
            if (corr > bestRefCorr) { bestRefCorr = corr; bestRef = lag; }
        }
        Debug.LogFormat("[ANALYSIS] lagGlobal={0} (~{1:0.0}bpm) lagFaixa140={2} (~{3:0.00}bpm)",
            bestLag, 600.0 / bestLag, bestRef, 600.0 / bestRef);

        // detalhe dos primeiros 5s para calibrar o ouvido
        string detail = "[ANALYSIS] env 0-5s (picos por 100ms): ";
        for (int w = 0; w < 50; w++)
        {
            float pk = 0f;
            for (int k = w * 10; k < (w + 1) * 10 && k < nw; k++)
                if (env[k] > pk) pk = env[k];
            detail += string.Format("{0:0.00} ", pk / max);
        }
        Debug.Log(detail);
    }
}
