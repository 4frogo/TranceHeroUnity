using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

/// Julgamento, placar, especial e HUD. Teclado D-F-J-K, toque e mouse.
public class Judge : MonoBehaviour
{
    public Conductor conductor;
    public Highway highway;
    public Juice juice;

    public Text txtSong, txtScore, txtMult, txtCombo, txtJudge, txtSpecial, txtMsg, txtHelp;
    public UnityEngine.UI.Image songBar, specialBar;
    public RectTransform comboBadge;
    public Text comboNum;

    const double WIN_PERFECT = 0.045;
    const double WIN_GREAT = 0.09;
    const double WIN_HIT = 0.15;

    readonly KeyCode[] keys = new KeyCode[] { KeyCode.M, KeyCode.N, KeyCode.B, KeyCode.V };

    List<NoteData> chart = new List<NoteData>();
    int chartIdx = 0;
    bool needTap = false;

    int score, combo, maxCombo, mult = 1;
    int perfects, greats, goods, misses, special;
    bool specialReady, fever;
    float feverTime, feverFlash;
    string judgeText = "";
    Color judgeColor = Color.white;
    float judgeTimer;
    float comboPop;

    void Start()
    {
        needTap = Application.platform == RuntimePlatform.WebGLPlayer;
        double bpm = 140.0;
        var ta = Resources.Load<TextAsset>("chart"); // funciona em todas as plataformas, incl. WebGL
        if (ta != null)
            chart = ChartData.Parse(ta.text, out bpm);
        else
        {
            string path = Path.Combine(Application.streamingAssetsPath, "chart.json");
            if (File.Exists(path))
                chart = ChartData.Parse(File.ReadAllText(path), out bpm);
        }
        if (chart.Count == 0)
        {
            // chart reserva caso o json falhe: 64 beats a 140bpm
            for (int i = 0; i < 64; i++)
                chart.Add(new NoteData { time = 2.0 + i * (60.0 / 140.0), lane = i % 4 });
            bpm = 140.0;
        }
        conductor.bpm = bpm;
        txtHelp.text = "M N B V = tocar  |  L = especial  |  - / = = offset  |  ENTER = recomecar";
    }

    void Update()
    {
        if (needTap)
        {
            txtMsg.text = "TOQUE PARA COMECAR";
            return;
        }
        double st = conductor.SongTime;

        // spawna notas que entram na janela de aproximacao
        while (chartIdx < chart.Count && chart[chartIdx].time - st <= conductor.approach)
        {
            highway.SpawnNote(chart[chartIdx].time, chart[chartIdx].lane);
            chartIdx++;
        }
        // misses automaticos
        foreach (Note n in highway.CollectMissed(st, WIN_HIT))
            RegisterMiss();

        if (conductor.Finished)
        {
            txtMsg.text = "FIM! " + score + " pts  max " + maxCombo + "  ENTER p/ de novo";
        }
        else if (st < 0)
        {
            txtMsg.text = "Prepare-se... " + System.Math.Abs(st).ToString("0.0");
        }
        else txtMsg.text = "";

        judgeTimer -= Time.deltaTime;
        if (fever)
        {
            feverTime -= Time.deltaTime;
            feverFlash = Mathf.Max(0f, feverFlash - Time.deltaTime * 2f);
            if (feverTime <= 0) fever = false;
        }
        comboPop = Mathf.Max(0f, comboPop - Time.deltaTime * 2.8f);
        HandleInput();
        UpdateHud();
    }

    // ---------------- entrada ----------------
    void HandleInput()
    {
        for (int i = 0; i < 4; i++)
            if (Input.GetKeyDown(keys[i])) HitLane(i);

        // toque: metade inferior da tela dividida em 4 faixas
        if (Input.touchCount > 0)
        {
            foreach (Touch t in Input.touches)
                if (t.phase == TouchPhase.Began) TapAt(t.position);
        }
        if (Input.GetMouseButtonDown(0)) TapAt(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus)) NudgeOffset(-0.005);
        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus)) NudgeOffset(0.005);
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
        {
            conductor.offset = -0.03;
            PlayerPrefs.SetFloat("th_offset", (float)conductor.offset);
            ShowJudge("OFFSET " + Ms(conductor.offset), new Color(0.49f, 0.98f, 1f));
        }
        if (Input.GetKeyDown(KeyCode.L)) TryFever();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.KeypadEnter))
            if (conductor.Finished || conductor.SongTime < 0.5) ResetRun();
    }

    void TapAt(Vector2 pos)
    {
        if (conductor.Finished) { ResetRun(); return; }
        if (pos.y > Screen.height * 0.35f)
        {
            int lane = Mathf.Clamp((int)(pos.x / Screen.width * 4f), 0, 3);
            HitLane(lane);
        }
        else if (specialReady && !fever && pos.x > Screen.width * 0.74f)
        {
            TryFever();
        }
    }

    void TryFever()
    {
        if (!specialReady || fever) return;
        fever = true; feverTime = 10f; feverFlash = 1f;
        special = 0; specialReady = false;
        ShowJudge("ESPECIAL LIBERADO!", new Color(1f, 0.85f, 0.23f));
    }

    void NudgeOffset(double d)
    {
        conductor.offset += d;
        PlayerPrefs.SetFloat("th_offset", (float)conductor.offset);
        ShowJudge("OFFSET " + Ms(conductor.offset), new Color(0.49f, 0.98f, 1f));
    }

    static string Ms(double s) { return ((int)System.Math.Round(s * 1000.0)).ToString() + "ms"; }

    // ---------------- julgamento ----------------
    void HitLane(int lane)
    {
        if (conductor.Finished) return;
        highway.OnPress(lane);
        Note note; double signed;
        if (!highway.TryHit(lane, conductor.SongTime, WIN_HIT, out note, out signed)) return;
        note.hit = true;
        double adiff = System.Math.Abs(signed);
        string tag = adiff < 0.02 ? "" : (signed > 0 ? " -" : " +") + ((int)System.Math.Round(adiff * 1000.0)) + "ms";
        if (adiff <= WIN_PERFECT) { RegisterHit("PERFEITA!" + tag, new Color(0.22f, 1f, 0.42f), 300); perfects++; AddSpecial(); }
        else if (adiff <= WIN_GREAT) { RegisterHit("OTIMO!" + tag, new Color(0.16f, 0.81f, 1f), 150); greats++; AddSpecial(); }
        else { RegisterHit("BOM" + tag, new Color(1f, 0.85f, 0.23f), 50); goods++; }
        highway.OnHitFx(lane);
        if (juice != null) juice.OnHit(lane, highway.ReceptorPos(lane));
        Object.Destroy(note.gameObject);
    }

    void RegisterHit(string txt, Color col, int bas)
    {
        combo++; maxCombo = System.Math.Max(maxCombo, combo);
        mult = combo >= 32 ? 8 : (combo >= 16 ? 4 : (combo >= 8 ? 2 : 1));
        int gain = bas * mult;
        if (fever) gain *= 2;
        score += gain;
        ShowJudge(txt, col);
        comboPop = 1f;
    }

    void ShowJudge(string txt, Color col)
    {
        judgeText = txt; judgeColor = col; judgeTimer = 0.8f;
    }

    void AddSpecial()
    {
        if (fever || specialReady) return;
        special = System.Math.Min(20, special + 1);
        if (special >= 20)
        {
            specialReady = true;
            ShowJudge("ESPECIAL LIBERADO!", new Color(1f, 0.85f, 0.23f));
        }
    }

    void RegisterMiss()
    {
        combo = 0; mult = 1; misses++;
        ShowJudge("MISS", new Color(1f, 0.23f, 0.36f));
        if (juice != null) juice.OnMiss();
    }

    void ResetRun()
    {
        highway.ClearNotes();
        chartIdx = 0;
        score = 0; combo = 0; maxCombo = 0; mult = 1;
        perfects = greats = goods = misses = special = 0;
        specialReady = false; fever = false;
        judgeText = ""; judgeTimer = 0;
        conductor.Begin();
    }

    // ---------------- HUD ----------------
    void UpdateHud()
    {
        txtScore.text = score.ToString();
        txtMult.text = "x" + mult + (fever ? " FEVER!" : "");
        txtMult.color = fever ? new Color(1f, 0.85f, 0.23f) : new Color(0.22f, 1f, 0.42f);
        if (txtCombo != null) txtCombo.text = "";
        float sc = 1f + 0.25f * Mathf.Clamp01(judgeTimer);
        txtJudge.transform.localScale = new Vector3(sc, sc, 1f);
        txtJudge.text = judgeText;
        txtJudge.color = judgeColor;
        var jc = txtJudge.color; jc.a = Mathf.Clamp01(judgeTimer * 2f); txtJudge.color = jc;
        if (specialReady && !fever) { txtSpecial.text = "ESPECIAL\n20/20 - aperte [L]!"; txtSpecial.color = new Color(1f, 0.85f, 0.23f); }
        else if (fever) { txtSpecial.text = "SINTA O FLOW!\n" + feverTime.ToString("0.0") + "s"; }
        else { txtSpecial.text = "ESPECIAL\n" + special + "/20"; txtSpecial.color = new Color(0.71f, 1f, 0.18f); }
        double total = conductor.SongLength;
        double cur = System.Math.Max(0, System.Math.Min(total, conductor.SongTime));
        txtSong.text = "PSY BEYOND\nCosmic Flow  " + Fmt(cur) + " / " + Fmt(total);
        // barras + selo mandala
        if (songBar != null) songBar.fillAmount = total > 0 ? (float)(cur / total) : 0f;
        if (specialBar != null)
            specialBar.fillAmount = fever ? Mathf.Clamp01(feverTime / 10f) : special / 20f;
        if (comboNum != null) comboNum.text = combo >= 2 ? combo.ToString() : "";
        if (comboBadge != null)
        {
            float s = 1f + 0.22f * comboPop;
            comboBadge.localScale = new Vector3(s, s, 1f);
            comboBadge.gameObject.SetActive(combo >= 2 || comboPop > 0f);
        }
    }

    static string Fmt(double s)
    {
        int m = (int)(s / 60.0);
        int sec = (int)s % 60;
        return m + ":" + sec.ToString("00");
    }
}
