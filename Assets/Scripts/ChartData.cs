using System;
using System.Collections.Generic;
using System.Globalization;

/// Uma nota do chart: tempo em segundos + pista 0-3.
[Serializable]
public struct NoteData
{
    public double time;
    public int lane;
}

/// Parser do formato {"bpm":140,...,"notes":[[t,l],...]} sem dependencias.
public static class ChartData
{
    public static List<NoteData> Parse(string json, out double bpm)
    {
        bpm = 140.0;
        var list = new List<NoteData>();
        if (string.IsNullOrEmpty(json)) return list;

        // bpm
        int bi = json.IndexOf("\"bpm\"");
        if (bi >= 0)
        {
            int c = json.IndexOf(':', bi);
            int e = json.IndexOfAny(new char[] { ',', '}', ' ', '\n', '\r', '\t' }, c + 1);
            double.TryParse(json.Substring(c + 1, e - c - 1).Trim(),
                NumberStyles.Float, CultureInfo.InvariantCulture, out bpm);
        }

        // notas: procura "notes":[ e le pares [t,l]
        int ni = json.IndexOf("\"notes\"");
        if (ni < 0) return list;
        int p = json.IndexOf('[', ni + 7);
        if (p < 0) return list;
        p++; // dentro do array externo
        while (p < json.Length)
        {
            // pula espacos, virgulas e quebras
            while (p < json.Length && (char.IsWhiteSpace(json[p]) || json[p] == ',')) p++;
            if (p >= json.Length || json[p] == ']') break;
            if (json[p] != '[') { p++; continue; }
            p++; // dentro do par
            double t = ReadNumber(json, ref p);
            while (p < json.Length && (char.IsWhiteSpace(json[p]) || json[p] == ',')) p++;
            int lane = (int)Math.Round(ReadNumber(json, ref p));
            while (p < json.Length && json[p] != ']') p++;
            if (p < json.Length) p++; // fecha par
            list.Add(new NoteData { time = t, lane = lane < 0 ? 0 : (lane > 3 ? 3 : lane) });
        }
        list.Sort((a, b) => a.time.CompareTo(b.time));
        return list;
    }

    static double ReadNumber(string s, ref int p)
    {
        while (p < s.Length && char.IsWhiteSpace(s[p])) p++;
        int start = p;
        while (p < s.Length && (char.IsDigit(s[p]) || s[p] == '-' || s[p] == '+' || s[p] == '.' || s[p] == 'e' || s[p] == 'E')) p++;
        double v = 0;
        double.TryParse(s.Substring(start, p - start),
            NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        return v;
    }
}
