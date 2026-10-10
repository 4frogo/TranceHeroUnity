using UnityEngine;

/// Suco visual: punch da camera no kick, shake no erro, clar pescoço,
/// pano de fundo pulsando, holofotes varrendo e explosao de particulas.
public class Juice : MonoBehaviour
{
    public Conductor conductor;
    public Camera cam;
    public Material bgMat;
    public Light[] sweeps;
    public ParticleSystem burst;
    public Transform[] lasers;
    public Material horizonMat;
    public Transform mandala;

    float baseFov;
    Vector3 basePos;
    float trauma = 0f;

    void Start()
    {
        if (cam != null)
        {
            baseFov = cam.fieldOfView;
            basePos = cam.transform.position;
        }
    }

    public void OnHit(int lane, Vector3 pos)
    {
        trauma = Mathf.Min(1f, trauma + 0.10f);
        if (burst != null)
        {
            var m = burst.main;
            m.startColor = LaneColor(lane);
            burst.transform.position = pos;
            burst.Emit(26);
        }
    }

    public void OnMiss()
    {
        trauma = Mathf.Min(1f, trauma + 0.35f);
    }

    void Update()
    {
        float bp = (conductor != null) ? conductor.BeatPhase() : 0f;

        // soco sutil da camera no kick + tremor pelo trauma
        if (cam != null)
        {
            cam.fieldOfView = baseFov - 0.9f * bp;
            trauma = Mathf.Max(0f, trauma - Time.deltaTime * 2.2f);
            float sh = trauma * trauma * 0.4f;
            cam.transform.position = basePos + new Vector3(
                (Mathf.PerlinNoise(Time.time * 42f, 0f) - 0.5f) * sh,
                (Mathf.PerlinNoise(0f, Time.time * 42f) - 0.5f) * sh, 0f);
        }

        // palco respira no beat
        if (bgMat != null)
            bgMat.color = Color.white * (1f + 0.18f * bp);

        // holofotes varrendo a pista
        if (sweeps != null)
        {
            for (int i = 0; i < sweeps.Length; i++)
            {
                sweeps[i].transform.rotation = Quaternion.Euler(
                    42f, Mathf.Sin(Time.time * (0.7f + i * 0.4f) + i * 2.1f) * 55f, 0f);
                sweeps[i].intensity = 2.4f + 1.6f * bp;
            }
        }

        // lasers do ceu balancando + brilho do horizonte respirando
        if (lasers != null)
        {
            for (int i = 0; i < lasers.Length; i++)
                if (lasers[i] != null)
                    lasers[i].rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 0.5f + i * 2.1f) * 0.28f);
        }
        if (horizonMat != null)
        {
            Color hc = horizonMat.color;
            hc.a = 0.22f + 0.30f * bp;
            horizonMat.color = hc;
        }
        // mandala do horizonte girando e respirando no beat + batida global do shader
        if (mandala != null)
        {
            mandala.rotation = Quaternion.Euler(0f, 0f, Time.time * 8f);
            float ms = 1f + 0.10f * bp;
            mandala.localScale = new Vector3(9f * ms, 9f * ms, 1f);
        }
        Shader.SetGlobalFloat("_Beat", bp);
    }

    static Color LaneColor(int lane)
    {
        switch (lane)
        {
            case 0: return new Color(0.22f, 1f, 0.42f);
            case 1: return new Color(0.16f, 0.81f, 1f);
            case 2: return new Color(1f, 0.85f, 0.23f);
            default: return new Color(1f, 0.23f, 0.36f);
        }
    }
}
