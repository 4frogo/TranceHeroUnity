using System.Collections.Generic;
using UnityEngine;

/// Pista 3D: cria/move notas, aneis dos receptores, pulsos e colunas de luz.
public class Highway : MonoBehaviour
{
    public Conductor conductor;
    public float[] laneX = new float[] { -3f, -1f, 1f, 3f };
    public float spawnZ = -20f;
    public float receptorZ = 6f;
    public float noteY = 0.35f;

    [HideInInspector] public Material[] noteMats = new Material[4];
    [HideInInspector] public Material[] receptorMats = new Material[4];
    [HideInInspector] public Material[] beamMats = new Material[4];
    [HideInInspector] public Material[] trailMats = new Material[4];
    [HideInInspector] public Material[] symbolMats = new Material[4];
    [HideInInspector] public Light[] laneLights = new Light[4];
    [HideInInspector] public Transform[] receptors = new Transform[4];

    readonly List<Note> active = new List<Note>();
    readonly float[] press = new float[4];
    readonly float[] beam = new float[4];
    float[] baseEmission = new float[4];
    Material[] stripMats;
    readonly float[] stripHue = new float[4] { 0.38f, 0.53f, 0.14f, 0.96f };

    void Start()
    {
        // faixas da pista: energia rolando ate o jogador
        stripMats = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            var go = GameObject.Find("Strip_" + i);
            if (go != null) stripMats[i] = go.GetComponent<MeshRenderer>().material;
        }
    }

    public float Speed => (receptorZ - spawnZ) / (float)conductor.approach;

    void Update()
    {
        if (conductor == null) return;
        float bp = conductor.BeatPhase();
        for (int i = 0; i < 4; i++)
        {
            press[i] = Mathf.Max(0f, press[i] - Time.deltaTime * 6f);
            beam[i] = Mathf.Max(0f, beam[i] - Time.deltaTime * 2.5f);
            // pulso do receptor no kick + press
            if (receptors[i] != null)
            {
                float s = 1f + 0.10f * press[i] + 0.05f * bp;
                receptors[i].localScale = new Vector3(s, 1f, s);
                float e = baseEmission[i] * (1f + 1.2f * press[i] + 0.5f * bp);
                receptorMats[i].SetColor("_EmissionColor", EmissionOf(i) * e);
            }
            // coluna de luz some suave
            Color bc = beamMats[i].color;
            bc.a = 0.35f * beam[i];
            beamMats[i].color = bc;
        }
        // move notas vivas
        double st = conductor.SongTime;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Note n = active[i];
            float z = receptorZ - (float)(n.time - st) * Speed;
            n.transform.position = new Vector3(laneX[n.lane], noteY, z);
        }
        // luzes das pistas apagam suave
        if (laneLights != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (laneLights[i] != null && laneLights[i].intensity > 0f)
                    laneLights[i].intensity = Mathf.Max(0f, laneLights[i].intensity - Time.deltaTime * 30f);
            }
        }
        // energia das faixas correndo para o jogador + matiz ciclando
        if (stripMats != null)
        {
            float hueShift = (Time.time * 0.03f) % 1f;
            for (int i = 0; i < 4; i++)
            {
                if (stripMats[i] != null && stripMats[i].mainTexture != null)
                {
                    Vector2 o = stripMats[i].mainTextureOffset;
                    o.y -= Time.deltaTime * 1.6f;
                    stripMats[i].mainTextureOffset = o;
                    Color cyc = Color.HSVToRGB((stripHue[i] + hueShift) % 1f, 0.9f, 1f);
                    stripMats[i].SetColor("_EmissionColor", cyc * 0.45f);
                }
            }
        }
    }

    Color EmissionOf(int lane)
    {
        // cor emissiva da pista (verde/azul/amarelo/vermelho)
        switch (lane)
        {
            case 0: return new Color(0.22f, 1f, 0.42f);
            case 1: return new Color(0.16f, 0.81f, 1f);
            case 2: return new Color(1f, 0.85f, 0.23f);
            default: return new Color(1f, 0.23f, 0.36f);
        }
    }

    public void RegisterMats(Material[] notes, Material[] receptors_, Material[] beams, Transform[] recs, Material[] trails, Light[] lights, Material[] symbols)
    {
        noteMats = notes; receptorMats = receptors_; beamMats = beams; receptors = recs; trailMats = trails; laneLights = lights; symbolMats = symbols;
        for (int i = 0; i < 4; i++) baseEmission[i] = 1.6f;
    }

    public Vector3 ReceptorPos(int lane)
    {
        return new Vector3(laneX[lane], noteY + 0.3f, receptorZ);
    }

    public void SpawnNote(double time, int lane)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Note_" + lane;
        go.transform.localScale = new Vector3(1.1f, 0.25f, 1.1f);
        var mr = go.GetComponent<MeshRenderer>();
        mr.material = noteMats[lane];
        // aro claro: segundo cilindro menor e branco por cima
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.transform.SetParent(go.transform, false);
        cap.transform.localScale = new Vector3(0.62f, 1.4f, 0.62f);
        var cmr = cap.GetComponent<MeshRenderer>();
        var capMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        capMat.color = Color.white * 0.9f;
        capMat.EnableKeyword("_EMISSION");
        capMat.SetColor("_EmissionColor", Color.white * 0.7f);
        cmr.material = capMat;
        Destroy(cap.GetComponent<Collider>());
        Destroy(go.GetComponent<Collider>());
        // rastro neon (perspectiva real ja escala com a distancia)
        if (trailMats != null && trailMats[lane] != null)
        {
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = 0.35f;
            tr.startWidth = 0.7f;
            tr.endWidth = 0.05f;
            tr.minVertexDistance = 0.1f;
            tr.material = trailMats[lane];
        }
        // simbolo da pista no topo do disco
        if (symbolMats != null && symbolMats[lane] != null)
        {
            var dq = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dq.name = "Glyph";
            dq.transform.SetParent(go.transform, false);
            dq.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            dq.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            dq.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            dq.GetComponent<MeshRenderer>().material = symbolMats[lane];
            Destroy(dq.GetComponent<Collider>());
        }
        Note n = go.AddComponent<Note>();
        n.time = time; n.lane = lane;
        active.Add(n);
    }

    public void ClearNotes()
    {
        for (int i = active.Count - 1; i >= 0; i--)
            if (active[i] != null) Destroy(active[i].gameObject);
        active.Clear();
    }

    /// Procura a nota viva mais proxima do tempo na pista. Retorna diff assinado.
    public bool TryHit(int lane, double songTime, double window, out Note note, out double signedDiff)
    {
        note = null; signedDiff = 0;
        double best = double.MaxValue;
        for (int i = 0; i < active.Count; i++)
        {
            Note n = active[i];
            if (n == null || n.lane != lane || n.hit || n.missed) continue;
            double d = System.Math.Abs(songTime - n.time);
            if (d < best) { best = d; note = n; }
        }
        if (note != null && best <= window)
        {
            signedDiff = songTime - note.time;
            return true;
        }
        note = null;
        return false;
    }

    /// Notas que passaram da janela sem acerto.
    public List<Note> CollectMissed(double songTime, double window)
    {
        var res = new List<Note>();
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Note n = active[i];
            if (n == null) { active.RemoveAt(i); continue; }
            if (!n.hit && !n.missed && songTime - n.time > window)
            {
                n.missed = true;
                res.Add(n);
            }
            // limpa bem atras
            if (songTime - n.time > 1.0)
            {
                Destroy(n.gameObject);
                active.RemoveAt(i);
            }
        }
        return res;
    }

    public void OnPress(int lane) { press[lane] = 1f; }

    public void OnHitFx(int lane)
    {
        press[lane] = 1f;
        beam[lane] = 1f;
        if (laneLights != null && laneLights[lane] != null)
            laneLights[lane].intensity = 9f;
    }
}
