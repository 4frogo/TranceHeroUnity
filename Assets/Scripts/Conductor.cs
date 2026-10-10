using UnityEngine;

/// Relogio-mestre do jogo: segue o DSP do audio (sem drift).
[RequireComponent(typeof(AudioSource))]
public class Conductor : MonoBehaviour
{
    public double bpm = 140.0;
    public double approach = 2.0;   // segundos que a nota leva ate o receptor
    public double startDelay = 3.0; // contagem regressiva antes da musica
    public double offset = -0.03;   // calibracao fina (s), + = musica adiantada

    [HideInInspector] public AudioSource music;
    [HideInInspector] public double SongTime = -3.0;
    [HideInInspector] public bool Finished = false;

    double startDsp;
    bool started = false;

    public double BeatLength => 60.0 / bpm;
    public double SongLength => (music != null && music.clip != null) ? music.clip.length + 1.0 : 30.0;

    void Awake()
    {
        music = GetComponent<AudioSource>();
        offset = PlayerPrefs.GetFloat("th_offset", (float)offset);
    }

    void Start()
    {
        Begin();
    }

    public void Begin()
    {
        SongTime = -startDelay;
        Finished = false;
        started = false;
        if (music != null) music.Stop();
        startDsp = AudioSettings.dspTime + startDelay;
        if (music != null && music.clip != null)
            music.PlayDelayed((float)startDelay);
        started = true;
    }

    void Update()
    {
        if (!started || Finished) return;
        if (music != null && music.isPlaying)
        {
            // relogio do hardware de audio: preciso e sem lerp
            SongTime = AudioSettings.dspTime - startDsp + offset;
        }
        else
        {
            // contagem regressiva ou fim do stream: avanca pelo relogio
            SongTime += Time.deltaTime;
        }
        if (SongTime > SongLength) Finished = true;
    }

    /// 1.0 logo apos o kick, 0.0 no fim do beat. Para pulsar o visual.
    public float BeatPhase()
    {
        if (SongTime <= 0) return 0f;
        double b = BeatLength;
        return (float)(1.0 - (SongTime % b) / b);
    }
}
