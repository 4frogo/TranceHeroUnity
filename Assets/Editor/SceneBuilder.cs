using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// Monta a cena Main do TranceHero 3D. Rode via menu TranceHero/Build Scene
/// ou batchmode: -executeMethod SceneBuilder.Build
public static class SceneBuilder
{
    static readonly Color[] LaneColors = new Color[]
    {
        new Color(0.22f, 1f, 0.42f),   // verde
        new Color(0.16f, 0.81f, 1f),   // azul
        new Color(1f, 0.85f, 0.23f),   // amarelo
        new Color(1f, 0.23f, 0.36f),   // vermelho
    };
    static readonly float[] LaneX = new float[] { -3f, -1f, 1f, 3f };

    [MenuItem("TranceHero/Build Scene")]
    public static void Build()
    {
        SetupURP();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------- camera ----------
        var camGo = new GameObject("MainCamera");
        var cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>(); // <-- sem isso nao sai som!
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 8.5f, 13f);
        camGo.transform.LookAt(new Vector3(0f, 0.5f, -8f));
        cam.fieldOfView = 55f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.024f, 0.008f, 0.07f);

        // ---------- luz e nevoa ----------
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 0.8f;
        sun.shadows = LightShadows.None;
        sun.transform.rotation = Quaternion.Euler(50f, -20f, 0f);
        RenderSettings.ambientLight = new Color(0.25f, 0.25f, 0.45f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.02f, 0f, 0.06f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 65f;

        // ---------- fundo: sua foto do palco ----------
        var bgTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/bg.png");
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "Backdrop";
        bg.transform.position = new Vector3(0f, 10f, -30f);
        bg.transform.localScale = new Vector3(48f, 27f, 1f);
        var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        if (bgTex != null) bgMat.mainTexture = bgTex;
        bg.GetComponent<MeshRenderer>().material = bgMat;
        Object.DestroyImmediate(bg.GetComponent<Collider>());

        // ---------- palco vivo: multidao, estrelas, lasers, horizonte ----------
        var crowdMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        crowdMat.mainTexture = SymbolTex.MakeCrowd();
        var crowd = GameObject.CreatePrimitive(PrimitiveType.Quad);
        crowd.name = "Crowd";
        crowd.transform.position = new Vector3(0f, 1.6f, -24f);
        crowd.transform.localScale = new Vector3(42f, 5.2f, 1f);
        crowd.GetComponent<MeshRenderer>().material = crowdMat;
        Object.DestroyImmediate(crowd.GetComponent<Collider>());

        var starGo = new GameObject("Stars");
        var stars = starGo.AddComponent<ParticleSystem>();
        var smain = stars.main;
        smain.loop = true;
        smain.playOnAwake = true;
        smain.duration = 4f;
        smain.startLifetime = 4f;
        smain.startSpeed = 0f;
        smain.startSize = 0.14f;
        smain.maxParticles = 250;
        smain.simulationSpace = ParticleSystemSimulationSpace.World;
        var sem = stars.emission;
        sem.enabled = true;
        sem.rateOverTime = 60f;
        var sshape = stars.shape;
        sshape.enabled = true;
        sshape.shapeType = ParticleSystemShapeType.Box;
        sshape.scale = new Vector3(60f, 22f, 1f);
        starGo.transform.position = new Vector3(0f, 15f, -28f);
        var scol = stars.colorOverLifetime;
        scol.enabled = true;
        var alphaKeys = new Gradient();
        alphaKeys.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        scol.color = alphaKeys;

        Color[] laserCols = new Color[] { LaneColors[0], LaneColors[1], new Color(1f, 0.3f, 0.9f) };
        float[] laserX = new float[] { -8f, 0f, 8f };
        var laserT = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            var lm = UnlitTransparent(new Color(laserCols[i].r, laserCols[i].g, laserCols[i].b, 0.30f));
            var lq = GameObject.CreatePrimitive(PrimitiveType.Quad);
            lq.name = "SkyLaser_" + i;
            lq.transform.position = new Vector3(laserX[i], 19f, -27f);
            lq.transform.localScale = new Vector3(1.1f, 30f, 1f);
            lq.GetComponent<MeshRenderer>().material = lm;
            Object.DestroyImmediate(lq.GetComponent<Collider>());
            laserT[i] = lq.transform;
        }

        var horizonMat = UnlitTransparent(new Color(0.5f, 0.2f, 1f, 0.30f));
        var horizon = GameObject.CreatePrimitive(PrimitiveType.Quad);
        horizon.name = "HorizonGlow";
        horizon.transform.position = new Vector3(0f, 1.2f, -21f);
        horizon.transform.localScale = new Vector3(26f, 5f, 1f);
        horizon.GetComponent<MeshRenderer>().material = horizonMat;
        Object.DestroyImmediate(horizon.GetComponent<Collider>());

        // mandala psicodelica no ponto de fuga
        var mandMat = UnlitTransparent(Color.white);
        mandMat.mainTexture = SymbolTex.MakeMandala();
        var mand = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mand.name = "Mandala";
        mand.transform.position = new Vector3(0f, 4.2f, -22.5f);
        mand.transform.localScale = new Vector3(9f, 9f, 1f);
        mand.GetComponent<MeshRenderer>().material = mandMat;
        Object.DestroyImmediate(mand.GetComponent<Collider>());

        // ---------- pista ----------
        var deckMat = new Material(Shader.Find("Trance/PsyDeck"));
        deckMat.SetColor("_Tint", Color.white);
        var deck = GameObject.CreatePrimitive(PrimitiveType.Quad);
        deck.name = "Deck";
        deck.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
        deck.transform.position = new Vector3(0f, 0f, -7f);
        deck.transform.localScale = new Vector3(12f, 30f, 1f);
        deck.GetComponent<MeshRenderer>().material = deckMat;
        Object.DestroyImmediate(deck.GetComponent<Collider>());

        Material[] noteMats = new Material[4];
        Material[] trailMats = new Material[4];
        var stripeTex = SymbolTex.MakeStripes();
        for (int i = 0; i < 4; i++)
        {
            // faixa da pista emissiva com energia rolando
            var stripMat = Lit(new Color(0.02f, 0.02f, 0.06f), LaneColors[i] * 0.35f);
            stripMat.mainTexture = stripeTex;
            stripMat.SetTexture("_EmissionMap", stripeTex);
            var strip = GameObject.CreatePrimitive(PrimitiveType.Quad);
            strip.name = "Strip_" + i;
            strip.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            strip.transform.position = new Vector3(LaneX[i], 0.02f, -7f);
            strip.transform.localScale = new Vector3(1.3f, 30f, 1f);
            strip.GetComponent<MeshRenderer>().material = stripMat;
            Object.DestroyImmediate(strip.GetComponent<Collider>());

            // material das notas
            var nm = Lit(LaneColors[i] * 0.5f, LaneColors[i] * 1.4f);
            noteMats[i] = nm;

            // rastro das notas
            var tm = UnlitTransparent(new Color(LaneColors[i].r, LaneColors[i].g, LaneColors[i].b, 0.5f));
            trailMats[i] = tm;
        }

        // trilhos laterais neon
        MakeRail(-5.2f, new Color(0.15f, 0.9f, 1f));
        MakeRail(5.2f, new Color(1f, 0.25f, 0.9f));

        // ---------- receptores + colunas de luz ----------
        Material[] recMats = new Material[4];
        Material[] beamMats = new Material[4];
        Material[] symbolMats = new Material[4];
        Transform[] recs = new Transform[4];
        Light[] laneLights = new Light[4];
        for (int i = 0; i < 4; i++)
        {
            var sm = UnlitTransparent(Color.white);
            sm.mainTexture = SymbolTex.MakeSymbol(i);
            symbolMats[i] = sm;
        }
        for (int i = 0; i < 4; i++)
        {
            var dark = Lit(new Color(0.02f, 0.02f, 0.03f), Color.black);
            var bas = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bas.name = "ReceptorBase_" + i;
            bas.transform.position = new Vector3(LaneX[i], 0.3f, 6f);
            bas.transform.localScale = new Vector3(1.9f, 0.3f, 1.9f);
            bas.GetComponent<MeshRenderer>().material = dark;
            Object.DestroyImmediate(bas.GetComponent<Collider>());

            var rm = Lit(LaneColors[i] * 0.4f, LaneColors[i] * 1.6f);
            recMats[i] = rm;
            var top = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            top.name = "Receptor_" + i;
            top.transform.position = new Vector3(LaneX[i], 0.55f, 6f);
            top.transform.localScale = new Vector3(1.6f, 0.15f, 1.6f);
            top.GetComponent<MeshRenderer>().material = rm;
            Object.DestroyImmediate(top.GetComponent<Collider>());
            recs[i] = top.transform;

            // simbolo da pista gravado no botao
            var decal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            decal.name = "Symbol_" + i;
            decal.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            decal.transform.position = new Vector3(LaneX[i], 0.68f, 6f);
            decal.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            decal.GetComponent<MeshRenderer>().material = symbolMats[i];
            Object.DestroyImmediate(decal.GetComponent<Collider>());

            // luz da pista: explode no acerto
            var pl = new GameObject("LaneLight_" + i).AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = LaneColors[i];
            pl.intensity = 0f;
            pl.range = 13f;
            pl.transform.position = new Vector3(LaneX[i], 1.7f, 6f);
            laneLights[i] = pl;

            var bm = UnlitTransparent(new Color(LaneColors[i].r, LaneColors[i].g, LaneColors[i].b, 0f));
            beamMats[i] = bm;
            var beam = GameObject.CreatePrimitive(PrimitiveType.Quad);
            beam.name = "Beam_" + i;
            beam.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            beam.transform.position = new Vector3(LaneX[i], 0.04f, -7f);
            beam.transform.localScale = new Vector3(0.9f, 26f, 1f);
            beam.GetComponent<MeshRenderer>().material = bm;
            Object.DestroyImmediate(beam.GetComponent<Collider>());
        }

        // ---------- game manager ----------
        var gm = new GameObject("GameManager");
        var audio = gm.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/psybeyond.mp3");
        var conductor = gm.AddComponent<Conductor>();
        var highway = gm.AddComponent<Highway>();
        var judge = gm.AddComponent<Judge>();
        var juice = gm.AddComponent<Juice>();
        highway.conductor = conductor;
        highway.RegisterMats(noteMats, recMats, beamMats, recs, trailMats, laneLights, symbolMats);
        judge.conductor = conductor;
        judge.highway = highway;

        // ---------- suco: holofotes, explosao, camera ----------
        var sweeps = new Light[2];
        Color[] sweepCols = new Color[] { new Color(0.2f, 0.9f, 1f), new Color(1f, 0.3f, 0.9f) };
        for (int i = 0; i < 2; i++)
        {
            var sg = new GameObject("Sweep_" + i);
            sg.transform.position = new Vector3(i == 0 ? -6f : 6f, 9f, 2f);
            var sp = sg.AddComponent<Light>();
            sp.type = LightType.Spot;
            sp.color = sweepCols[i];
            sp.intensity = 2.5f;
            sp.spotAngle = 28f;
            sp.range = 40f;
            sweeps[i] = sp;
        }
        var burstGo = new GameObject("HitBurst");
        var burst = burstGo.AddComponent<ParticleSystem>();
        var bmain = burst.main;
        bmain.loop = false;
        bmain.playOnAwake = false;
        bmain.duration = 0.6f;
        bmain.startLifetime = 0.55f;
        bmain.startSpeed = 7f;
        bmain.startSize = 0.35f;
        bmain.maxParticles = 400;
        bmain.simulationSpace = ParticleSystemSimulationSpace.World;
        var bem = burst.emission;
        bem.enabled = false;
        var bshape = burst.shape;
        bshape.enabled = true;
        bshape.shapeType = ParticleSystemShapeType.Sphere;
        bshape.radius = 0.4f;
        // faíscas aditivas com textura de brilho gerada (redondas e suaves)
        var pmat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        pmat.SetFloat("_Surface", 1f);
        pmat.SetFloat("_Blend", 2f); // aditivo
        pmat.SetTexture("_BaseMap", MakeGlowTex());
        burstGo.GetComponent<ParticleSystemRenderer>().material = pmat;
        // mantem o material padrao de particula (redondo e suave); o Juice tinta por pista

        juice.conductor = conductor;
        juice.cam = cam;
        juice.bgMat = bgMat;
        juice.sweeps = sweeps;
        juice.burst = burst;
        juice.lasers = laserT;
        juice.horizonMat = horizonMat;
        juice.mandala = mand.transform;
        judge.juice = juice;

        // ---------- pos-processamento: bloom + vinheta ----------
        var volGo = new GameObject("PostFX");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var bloom = profile.Add<Bloom>();
        bloom.threshold.value = 1.0f;
        bloom.intensity.value = 0.9f;
        var vig = profile.Add<Vignette>();
        vig.color.value = Color.black;
        vig.intensity.value = 0.35f;
        var ca = profile.Add<ChromaticAberration>();
        ca.intensity.value = 0.22f;
        var cc = profile.Add<ColorAdjustments>();
        cc.saturation.value = 12f;
        cc.contrast.value = 8f;
        AssetDatabase.CreateAsset(profile, "Assets/Settings/TranceVolume.asset");
        vol.sharedProfile = profile;

        // ---------- HUD ----------
        var canvasGo = new GameObject("HUD");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280f, 720f);
        canvasGo.AddComponent<GraphicRaycaster>();
        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        // ---------- HUD: paineis neon + textos ----------
        var panelCyan = SymbolTex.MakePanel(256, 128, new Color(0.02f, 0.03f, 0.10f, 0.62f), new Color(0.15f, 0.9f, 1f, 0.95f));
        var panelGreen = SymbolTex.MakePanel(256, 128, new Color(0.02f, 0.08f, 0.03f, 0.62f), new Color(0.35f, 1f, 0.3f, 0.95f));
        MkPanel(canvasGo.transform, "SongPanel", new Vector2(0, 1), new Vector2(245, -60), new Vector2(470, 110), panelCyan);
        MkPanel(canvasGo.transform, "ScorePanel", new Vector2(1, 1), new Vector2(-160, -70), new Vector2(310, 125), panelCyan);
        MkPanel(canvasGo.transform, "SpecialPanel", new Vector2(1, 0.5f), new Vector2(-160, -20), new Vector2(300, 180), panelGreen);
        MkPanel(canvasGo.transform, "LegendPanel", new Vector2(0, 0), new Vector2(140, 95), new Vector2(260, 170), panelCyan);

        judge.txtSong = MkText(canvasGo.transform, "Song", new Vector2(0, 1), new Vector2(30, -45), new Vector2(500, 80), 26, TextAnchor.UpperLeft, new Color(0.49f, 0.98f, 1f));
        judge.txtScore = MkText(canvasGo.transform, "Score", new Vector2(1, 1), new Vector2(-150, -40), new Vector2(280, 60), 44, TextAnchor.UpperRight, Color.white);
        judge.txtMult = MkText(canvasGo.transform, "Mult", new Vector2(1, 1), new Vector2(-150, -95), new Vector2(280, 50), 32, TextAnchor.UpperRight, new Color(0.22f, 1f, 0.42f));
        judge.txtJudge = MkText(canvasGo.transform, "Judge", new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(560, 60), 38, TextAnchor.MiddleCenter, Color.white);
        judge.txtSpecial = MkText(canvasGo.transform, "Special", new Vector2(1, 0.5f), new Vector2(-160, -40), new Vector2(300, 130), 28, TextAnchor.MiddleCenter, new Color(0.71f, 1f, 0.18f));
        judge.txtMsg = MkText(canvasGo.transform, "Msg", new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(700, 60), 36, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.23f));
        judge.txtHelp = MkText(canvasGo.transform, "Help", new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(700, 30), 17, TextAnchor.LowerCenter, new Color(1, 1, 1, 0.75f));

        // barras de progresso (musica) e especial
        judge.songBar = MkBar(canvasGo.transform, "SongBar", new Vector2(0, 1), new Vector2(245, -100), new Vector2(400, 10), new Color(0.15f, 0.9f, 1f));
        judge.specialBar = MkBar(canvasGo.transform, "SpecBar", new Vector2(1, 0.5f), new Vector2(-160, 55), new Vector2(240, 18), new Color(0.45f, 1f, 0.25f));

        // legenda das pistas com os simbolos
        string[] keyNames = new string[] { "M", "N", "B", "V" };
        float[] rowY = new float[] { 55f, 18f, -19f, -56f };
        for (int i = 0; i < 4; i++)
        {
            var icon = new GameObject("LegendIcon_" + i);
            icon.transform.SetParent(canvasGo.transform, false);
            var ii = icon.AddComponent<Image>();
            ii.sprite = FlatSprite(SymbolTex.MakeSymbol(i, 64));
            var irt = icon.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(0, 0);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = new Vector2(55, 95 + rowY[i]);
            irt.sizeDelta = new Vector2(32, 32);
            MkText(canvasGo.transform, "LegendTxt_" + i, new Vector2(0, 0), new Vector2(150, 95 + rowY[i]), new Vector2(170, 30), 19, TextAnchor.MiddleLeft, Color.white).text =
                keyNames[i] + "  ·  PISTA " + (i + 1);
        }

        // selo mandala do combo (sua arte recortada + numero vivo)
        var tImp = AssetImporter.GetAtPath("Assets/Art/seguencia.png") as TextureImporter;
        if (tImp != null && !tImp.isReadable)
        {
            tImp.isReadable = true;
            tImp.SaveAndReimport();
        }
        var btex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/seguencia.png");
        if (btex != null)
        {
            var clean = SymbolTex.KeyOutBlack(btex);
            var bgo = new GameObject("ComboBadge");
            bgo.transform.SetParent(canvasGo.transform, false);
            var bi = bgo.AddComponent<Image>();
            bi.sprite = FlatSprite(clean);
            var brt = bgo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.5f); brt.anchorMax = new Vector2(0, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(155, 20);
            brt.sizeDelta = new Vector2(240, 232);
            var numT = MkText(bgo.transform, "ComboNum", new Vector2(0.5f, 0.5f), new Vector2(0, 52), new Vector2(240, 100), 64, TextAnchor.MiddleCenter, Color.white);
            var sh = numT.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.35f, 0.9f, 1f, 0.9f);
            sh.effectDistance = new Vector2(3, -3);
            judge.comboBadge = brt;
            judge.comboNum = numT;
        }

        // ---------- projeto ----------
        PlayerSettings.productName = "TranceHero";
        PlayerSettings.companyName = "TranceHero";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
        Debug.Log("[TranceHero] Cena Main construida com sucesso.");
    }

    static Material Lit(Color albedo, Color emission)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.color = albedo;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        return m;
    }

    static Material UnlitTransparent(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.color = c;
        return m;
    }

    static Texture2D MakeGlowTex()
    {
        int s = 64;
        var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - s / 2f) * (x - s / 2f) + (y - s / 2f) * (y - s / 2f)) / (s / 2f);
                float a = Mathf.Clamp01(1f - d);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        t.Apply();
        return t;
    }

    static void SetupURP()
    {
        System.IO.Directory.CreateDirectory("Assets/Settings");
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(renderer, "Assets/Settings/TranceRenderer.asset");
        var ppd = AssetDatabase.LoadAssetAtPath<PostProcessData>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        if (ppd != null)
            renderer.postProcessData = ppd;
        else
            Debug.LogWarning("[TranceHero] PostProcessData padrao nao achado; pos-efeitos desligados.");
        var urp = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        var so = new SerializedObject(urp);
        var rlist = so.FindProperty("m_RendererDataList");
        rlist.arraySize = 1;
        rlist.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
        so.ApplyModifiedProperties();
        urp.supportsHDR = true;
        urp.msaaSampleCount = 4;
        urp.renderScale = 1f;
        AssetDatabase.CreateAsset(urp, "Assets/Settings/TranceURP.asset");
        GraphicsSettings.defaultRenderPipeline = urp;
        QualitySettings.renderPipeline = urp;
        EditorUtility.SetDirty(urp);
        EditorUtility.SetDirty(renderer);
        AssetDatabase.SaveAssets();
        Debug.Log("[TranceHero] URP ativo com HDR + MSAA.");
    }

    static void MakeRail(float x, Color c)
    {
        var m = Lit(Color.black, c * 1.6f);
        var rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rail.name = "Rail_" + x;
        rail.transform.position = new Vector3(x, 0.12f, -7f);
        rail.transform.localScale = new Vector3(0.15f, 0.15f, 30f);
        rail.GetComponent<MeshRenderer>().material = m;
        Object.DestroyImmediate(rail.GetComponent<Collider>());
    }

    static Sprite FlatSprite(Texture2D t)
    {
        return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
    }

    static Image MkPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Texture2D tex)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(18, 18, 18, 18));
        img.type = Image.Type.Sliced;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return img;
    }

    static Image MkBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color fill)
    {
        var bg = new GameObject(name + "_Bg");
        bg.transform.SetParent(parent, false);
        var bgi = bg.AddComponent<Image>();
        bgi.color = new Color(0f, 0f, 0f, 0.65f);
        var brt = bg.GetComponent<RectTransform>();
        brt.anchorMin = anchor; brt.anchorMax = anchor;
        brt.pivot = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = pos;
        brt.sizeDelta = size;
        var fg = new GameObject(name);
        fg.transform.SetParent(parent, false);
        var fgi = fg.AddComponent<Image>();
        fgi.color = fill;
        fgi.type = Image.Type.Filled;
        fgi.fillMethod = Image.FillMethod.Horizontal;
        fgi.fillOrigin = 0;
        fgi.fillAmount = 0f;
        var frt = fg.GetComponent<RectTransform>();
        frt.anchorMin = anchor; frt.anchorMax = anchor;
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.anchoredPosition = pos;
        frt.sizeDelta = size;
        return fgi;
    }

    static Text MkText(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fs, TextAnchor align, Color col)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fs;
        t.alignment = align;
        t.color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }
}
