using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Cinemachine;
using TownLife;

public static class TownLifeBuilder
{
    const string Folder = "Assets/CityLife/Generated";
    static Transform root;
    static Material warm, teal, coral, dark;
    static GameObject New(string name, Transform parent)
    {
        var g = new GameObject(name); Undo.RegisterCreatedObjectUndo(g, "Enrich town");
        g.transform.SetParent(parent, false); return g;
    }
    static Material Mat(string name, Color color, bool emission = false)
    {
        var path = Folder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color;
        if (emission) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 2); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; }
        return m;
    }
    static GameObject Shape(string name, PrimitiveType type, Vector3 p, Vector3 scale, Material m, Transform parent)
    {
        var g = GameObject.CreatePrimitive(type); Undo.RegisterCreatedObjectUndo(g, "Town decoration");
        g.name = name; g.transform.SetParent(parent, false); g.transform.position = p; g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        GameObjectUtility.SetStaticEditorFlags(g, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
        return g;
    }
    static GameObject Prefab(string path, string name, Vector3 p, float yaw, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) throw new Exception("Missing prefab: " + path);
        var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(g, "Town asset"); g.name = name;
        g.transform.SetParent(parent, true); g.transform.position = p; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
        return g;
    }
    static void Strand(Vector3 a, Vector3 b, Transform parent, int count)
    {
        var g = New("Festoon lights", parent);
        var line = g.AddComponent<LineRenderer>(); line.sharedMaterial = dark;
        line.startWidth = line.endWidth = .035f; line.positionCount = 33;
        for (int i = 0; i <= 32; i++) { float t = i / 32f; line.SetPosition(i, Vector3.Lerp(a, b, t) - Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.1f); }
        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count;
            var p = Vector3.Lerp(a, b, t) - Vector3.up * (Mathf.Sin(t * Mathf.PI) * 1.1f + .15f);
            Shape("Warm lantern", PrimitiveType.Sphere, p, Vector3.one * .3f, warm, g.transform);
        }
    }
    static TownCitizen Citizen(string role, Vector3 p, int variant, bool stationary, string idle, Transform parent)
    {
        string[] types = { "Tourist_White", "ShopKeeper_Brown", "Runner_Black", "Grandma_White", "Cheerleader_Brown", "Biker_Black", "Nerd_White" };
        var g = Prefab("Assets/SimpleWorldVol1/SimpleCitizens/Prefabs/SimpleCitizens_" + types[variant % types.Length] + ".prefab", role, p, variant * 53, parent);
        var animator = g.GetComponent<Animator>() ?? g.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SimpleWorldVol1/SimpleCitizens/Models/SimpleCitizens.controller");
        animator.applyRootMotion = false;
        if (!stationary) { var agent = g.AddComponent<NavMeshAgent>(); agent.speed = 1.4f + variant % 3 * .2f; agent.radius = .4f; agent.height = 2.4f; agent.angularSpeed = 150; agent.stoppingDistance = .6f; agent.avoidancePriority = 30 + variant; }
        var c = g.AddComponent<TownCitizen>(); c.stationary = stationary; c.idleState = idle; c.radius = 10; c.phase = variant * .137f % 1;
        return c;
    }
    [MenuItem("Town Life/Build town enhancement")]
    public static void BuildFromMenu() { Debug.Log(Build()); }
    public static string Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit Play Mode first");
        if (GameObject.Find("Town Life • Senja") != null) throw new Exception("Town enhancement already exists");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory("Assets/CityLife/Backups");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), "Assets/CityLife/Backups/Activity1Start_BeforeTownLife.unity", true);
        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Town life enhancement");
        root = New("Town Life • Senja", null).transform;
        warm = Mat("Lantern gold", new Color(1,.72f,.32f), true);
        teal = Mat("Cafe teal", new Color(.08f,.52f,.5f)); coral = Mat("Cafe terracotta", new Color(.86f,.33f,.21f));
        dark = Mat("Cable charcoal", new Color(.12f,.16f,.2f));
        Lighting(); Decor(); Crowd(); Cinematic(); PrepareLightmapUVs();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        return "Town enrichment created and saved.";
    }
    public static string PrepareLightmapUVs()
    {
        Directory.CreateDirectory(Folder + "/LightmapMeshes");
        var copies = new System.Collections.Generic.Dictionary<Mesh, Mesh>();
        int count = 0;
        foreach (var r in UnityEngine.Object.FindObjectsOfType<MeshRenderer>())
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            if (r.GetComponentInParent<Animator>() != null || r.GetComponentInParent<NavMeshAgent>() != null)
            {
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags & ~(StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic));
                continue;
            }
            if (r.GetComponentInParent<ParticleSystem>() != null) continue;
            Undo.RecordObject(r.gameObject, "Lightmap static geometry");
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            var original = mf.sharedMesh;
            if (AssetDatabase.GetAssetPath(original).StartsWith(Folder + "/LightmapMeshes")) continue;
            Mesh copy;
            if (!copies.TryGetValue(original, out copy))
            {
                copy = UnityEngine.Object.Instantiate(original);
                copy.name = original.name + " Lightmap UV";
                Unwrapping.GenerateSecondaryUVSet(copy);
                var path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/LightmapMeshes/" + original.name.Replace('/', '_').Replace(':','_') + ".asset");
                AssetDatabase.CreateAsset(copy, path); copies.Add(original, copy);
            }
            Undo.RecordObject(mf, "Assign lightmap UV mesh"); mf.sharedMesh = copy; count++;
        }
        AssetDatabase.SaveAssets();
        return "Prepared lightmap UVs for " + count + " renderers, " + copies.Count + " shared meshes; animated geometry excluded.";
    }
    static void Lighting()
    {
        var sun = GameObject.Find("Directional Light").GetComponent<Light>(); Undo.RecordObject(sun, "Warm afternoon"); Undo.RecordObject(sun.transform, "Sun angle");
        sun.color = new Color(1,.83f,.64f); sun.intensity = .85f; sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(32,-35,0);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.48f,.58f,.72f);
        RenderSettings.ambientEquatorColor = new Color(.55f,.48f,.39f);
        RenderSettings.ambientGroundColor = new Color(.27f,.3f,.33f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.62f,.66f,.7f);
        RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 160; RenderSettings.fogEndDistance = 480;
        var lights = New("Baked street lighting", root).transform;
        var lamps = UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(r => r.name.StartsWith("lamp_mesh")).ToArray();
        foreach (var lamp in lamps)
        {
            var p = lamp.bounds.center; p.y = lamp.bounds.max.y - .45f;
            var g = New("Street spot • " + lamp.name, lights); g.transform.position = p; g.transform.rotation = Quaternion.Euler(90,0,0);
            var l = g.AddComponent<Light>(); l.type = LightType.Spot; l.range = 15; l.spotAngle = 80; l.innerSpotAngle = 10;
            l.intensity = 100; l.color = new Color(1,.88f,.68f); l.shadows = LightShadows.Hard; l.lightmapBakeType = LightmapBakeType.Baked; l.bounceIntensity = .15f;
            Shape("Lamp glow", PrimitiveType.Sphere, p, new Vector3(.5f,.12f,.5f), warm, lights);
        }
        foreach (var r in GameObject.Find("Environment").GetComponentsInChildren<MeshRenderer>())
        {
            if (r.GetComponentInParent<Animator>() != null || r.GetComponentInParent<NavMeshAgent>() != null) continue;
            Undo.RecordObject(r.gameObject, "Static lighting");
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, GameObjectUtility.GetStaticEditorFlags(r.gameObject) | StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            var so = new SerializedObject(r); var scale = so.FindProperty("m_ScaleInLightmap"); if (scale != null) { scale.floatValue = .25f; so.ApplyModifiedProperties(); }
        }
        LightmapEditorSettings.lightmapper = LightmapEditorSettings.Lightmapper.ProgressiveCPU;
        LightmapEditorSettings.bakeResolution = 8; LightmapEditorSettings.maxAtlasSize = 1024;
        LightmapEditorSettings.realtimeResolution = 1;
        Lightmapping.realtimeGI = false; Lightmapping.bakedGI = true; Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        var probeGO = New("Plaza and market light probes", lights); var probes = probeGO.AddComponent<LightProbeGroup>();
        var positions = new System.Collections.Generic.List<Vector3>();
        for (int x = 10; x <= 100; x += 15) for (int z = 80; z <= 150; z += 14) { positions.Add(new Vector3(x,2,z)); positions.Add(new Vector3(x,5,z)); }
        probes.probePositions = positions.ToArray();
    }
    static void Decor()
    {
        var decor = New("Pasar Senja • cafe and gardens", root).transform;
        // Fill the open cafe frontage while keeping the existing vendor aisle clear.
        for (int row = 0; row < 3; row++) for (int col = 0; col < 2; col++)
        {
            var p = new Vector3(80 + col * 10, 1, 92 + row * 11);
            Prefab("Assets/SimpleWorldVol1/SimpleBuildings/Prefabs/table_chairs_" + (row % 2 == 0 ? "blue" : "red") + ".prefab", "Cafe seating", p, col * 90, decor);
            Prefab("Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/Prop_Umbrella_0" + (row % 3 + 1) + ".prefab", "Cafe parasol", p + new Vector3(0,0,0), 0, decor);
        }
        for (int i = 0; i < 4; i++)
        {
            float z = 87 + i * 12;
            foreach (float x in new float[] { 76,96 })
            {
                Shape("Lantern post", PrimitiveType.Cylinder, new Vector3(x,4,z), new Vector3(.16f,3,.16f), dark, decor);
            }
            Strand(new Vector3(76,7,z), new Vector3(96,7,z), decor, 12);
        }
        // Tree-lined edges soften empty streets without occupying the carriageway.
        Vector3[] trees = { new Vector3(8,1,84), new Vector3(8,1,108), new Vector3(8,1,145), new Vector3(62,1,83), new Vector3(62,1,145), new Vector3(86,1,73), new Vector3(95,1,145), new Vector3(127,1,70), new Vector3(127,1,113), new Vector3(-21,1,43), new Vector3(4,1,37), new Vector3(72,1,44) };
        for (int i=0;i<trees.Length;i++)
        {
            var g=Prefab("Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/tree_small_mesh.prefab", "Street tree", trees[i], i*47, decor); g.transform.localScale *= .8f;
            Prefab("Assets/SimpleWorldVol1/SimpleProps/_Prefabs/ParkPlanter_Round.prefab", "Tree planter", trees[i],0,decor);
        }
        for(int i=0;i<4;i++)
        {
            var p=new Vector3(21+i*9,1,83);
            Prefab("Assets/SimpleWorldVol1/SimpleTown/Prefabs/Props/flower_mesh.prefab","Flower border",p,0,decor);
        }
        Strand(new Vector3(27.7f,8.2f,92.7f),new Vector3(42.8f,8.2f,92.7f),decor,10);
        Strand(new Vector3(27.7f,8.2f,153.7f),new Vector3(42.8f,8.2f,153.7f),decor,10);
        var sign = New("Pasar Senja sign", decor);
        sign.transform.position = new Vector3(86,7.8f,86.9f);
        var text=sign.AddComponent<TextMesh>(); text.text="PASAR SENJA"; text.fontSize=64; text.characterSize=.12f; text.anchor=TextAnchor.MiddleCenter; text.color=new Color(1,.82f,.43f);
        // A gentle fountain jet adds continuous motion at the existing monument.
        var water=New("Fountain water",decor); water.transform.position=new Vector3(35.4f,17.1f,123.2f);
        var ps=water.AddComponent<ParticleSystem>(); var main=ps.main; main.startLifetime=1.8f; main.startSpeed=4; main.startSize=.13f; main.startColor=new Color(.4f,.85f,1,.7f); main.gravityModifier=.65f; main.maxParticles=160;
        var emission=ps.emission; emission.rateOverTime=45; var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Cone; shape.angle=14; shape.radius=.25f;
        water.transform.rotation=Quaternion.Euler(-90,0,0);
        var wm=Mat("Fountain aqua",new Color(.3f,.75f,.9f)); ps.GetComponent<ParticleSystemRenderer>().sharedMaterial=wm;
    }
    static void Crowd()
    {
        var crowd=New("Residents • walkers and neighbours",root).transform;
        Vector3[] points={new Vector3(22,1,100),new Vector3(48,1,100),new Vector3(21,1,143),new Vector3(50,1,142),new Vector3(63,1,130),new Vector3(80,1,119),new Vector3(92,1,127),new Vector3(88,1,80),new Vector3(16,1,72),new Vector3(-10,1,53),new Vector3(65,1,49),new Vector3(124,1,93)};
        for(int i=0;i<points.Length;i++) Citizen("Resident walking "+(i+1),points[i],i,false,"Idle",crowd);
        for(int i=0;i<3;i++)
        {
            Citizen("Market vendor "+(i+1),new Vector3(69,1,97+i*9),i+1,true,"Idle",crowd);
            Citizen("Cafe neighbour "+(i+1),new Vector3(84,1,94+i*11),i+3,true,i==1?"Idle_CheckTime":"Idle",crowd);
        }
        Citizen("Friends chatting A",new Vector3(46,1,135),4,true,"Idle_Wave",crowd);
        Citizen("Friends chatting B",new Vector3(48,1,135),5,true,"Idle",crowd);
    }
    static void Cinematic()
    {
        var go=New("Raka • introduction Timeline",root);
        var guide=Citizen("Raka • pemandu kota",new Vector3(48,1,85),1,true,"Idle_Wave",root);
        guide.transform.rotation=Quaternion.Euler(0,135,0);
        var d=go.AddComponent<PlayableDirector>(); d.playOnAwake=false; d.extrapolationMode=DirectorWrapMode.None;
        var timeline=ScriptableObject.CreateInstance<TimelineAsset>(); AssetDatabase.CreateAsset(timeline,Folder+"/RakaIntroduction.playable"); d.playableAsset=timeline;
        var main=Camera.main; var brain=main.GetComponent<CinemachineBrain>(); if(brain==null)brain=main.gameObject.AddComponent<CinemachineBrain>();
        var shots=timeline.CreateTrack<CinemachineTrack>(null,"01 • Establish / introduce / welcome"); d.SetGenericBinding(shots,brain);
        Vector3[] positions={new Vector3(125,65,49),new Vector3(57,6,77),new Vector3(54,4.5f,80)};
        Vector3[] targets={new Vector3(62,2,115),new Vector3(48,3,85),new Vector3(48,2.8f,85)};
        double[] starts={0,3.5,7.5}; double[] durations={4.5,5,4.5};
        for(int i=0;i<3;i++)
        {
            var cam=New("CM • Raka shot "+(i+1),go.transform).AddComponent<CinemachineVirtualCamera>();
            cam.Priority=-50; cam.m_Lens.FieldOfView=i==0?48:43; cam.transform.position=positions[i];cam.transform.LookAt(targets[i]);
            if(i>0)
            {
                cam.LookAt=guide.transform;
                var composer=cam.AddCinemachineComponent<CinemachineComposer>();composer.m_TrackedObjectOffset=new Vector3(0,1.6f,0);composer.m_HorizontalDamping=0;composer.m_VerticalDamping=0;
            }
            var clip=shots.CreateClip<CinemachineShot>(); clip.start=starts[i];clip.duration=durations[i];clip.displayName=new[]{"Pasar sore","Kenalkan Raka","Ayo jelajahi kota"}[i];
            var asset=(CinemachineShot)clip.asset;asset.VirtualCamera.exposedName=GUID.Generate().ToString();d.SetReferenceValue(asset.VirtualCamera.exposedName,cam);
            if(i>0)clip.blendInDuration=1;
            if(i<2)clip.blendOutDuration=1;
            // Animate the camera itself, using an absolute world-space transform track.
            var animator=cam.gameObject.AddComponent<Animator>();
            var track=timeline.CreateTrack<AnimationTrack>(null,"Dolly shot "+(i+1));track.trackOffset=TrackOffset.ApplyTransformOffsets;d.SetGenericBinding(track,animator);
            var ac=new AnimationClip();ac.name="Raka dolly "+(i+1);
            Vector3 end=positions[i]+(i==0?new Vector3(-6,-3,8):new Vector3(-1,0,1));
            for(int axis=0;axis<3;axis++)ac.SetCurve("",typeof(Transform),"localPosition."+"xyz"[axis],AnimationCurve.EaseInOut(0,positions[i][axis],(float)durations[i],end[axis]));
            for(int axis=0;axis<3;axis++)ac.SetCurve("",typeof(Transform),"localEulerAnglesRaw."+"xyz"[axis],AnimationCurve.Constant(0,(float)durations[i],cam.transform.eulerAngles[axis]));
            AssetDatabase.CreateAsset(ac,Folder+"/RakaDolly"+i+".anim");
            var tc=track.CreateClip<AnimationPlayableAsset>();tc.start=starts[i];tc.duration=durations[i];((AnimationPlayableAsset)tc.asset).clip=ac;((AnimationPlayableAsset)tc.asset).removeStartOffset=false;
        }
        var caption=New("Raka character card",go.transform);var canvas=caption.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
        var scaler=caption.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
        var label=New("Caption",caption.transform);var txt=label.AddComponent<Text>();txt.font=Resources.GetBuiltinResource<Font>("Arial.ttf");txt.fontSize=26;txt.alignment=TextAnchor.MiddleCenter;txt.color=new Color(1,.85f,.55f);txt.text="RAKA  /  PEMANDU KOTA\nSelamat datang! Mari nikmati taman dan Pasar Senja.";
        var rect=label.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.1f,.01f);rect.anchorMax=new Vector2(.9f,.15f);rect.offsetMin=rect.offsetMax=Vector2.zero;
        var at=timeline.CreateTrack<ActivationTrack>(null,"02 • Kartu karakter");d.SetGenericBinding(at,caption);var activation=at.CreateDefaultClip();activation.start=3.5;activation.duration=8.5;at.postPlaybackState=ActivationTrack.PostPlaybackState.Inactive;caption.SetActive(false);
        var audio=go.AddComponent<AudioSource>();audio.playOnAwake=false;audio.spatialBlend=0;audio.volume=.35f;
        var atr=timeline.CreateTrack<AudioTrack>(null,"03 • Original welcome chime");d.SetGenericBinding(atr,audio);
        MakeChime();var sound=AssetDatabase.LoadAssetAtPath<AudioClip>(Folder+"/WelcomeChime.wav");var aud=atr.CreateClip<AudioPlayableAsset>();aud.start=0;aud.duration=8;((AudioPlayableAsset)aud.asset).clip=sound;
        var intro=go.AddComponent<TownIntroduction>();intro.director=d;intro.guide=guide.transform;intro.player=GameObject.Find("Player").transform;intro.gameplayCamera=main;intro.input=GameObject.Find("InputManagerObj").GetComponent<InputManager>();intro.chase=main.GetComponent<ChaseCamera>();intro.caption=caption;
        intro.virtualCameras=go.GetComponentsInChildren<CinemachineVirtualCamera>(true).Select(v=>v.gameObject).ToArray();
        foreach(var vcam in intro.virtualCameras) vcam.SetActive(false);
        EditorUtility.SetDirty(timeline);
    }
    static void MakeChime()
    {
        const int rate=22050, seconds=8;int samples=rate*seconds;
        using(var w=new BinaryWriter(File.Create(Folder+"/WelcomeChime.wav")))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+samples*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));w.Write(16);w.Write((short)1);w.Write((short)1);w.Write(rate);w.Write(rate*2);w.Write((short)2);w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(samples*2);
            double[] notes={523.25,659.25,783.99,1046.5,783.99,659.25};
            for(int i=0;i<samples;i++){double t=i/(double)rate,v=0;for(int n=0;n<notes.Length;n++){double dt=t-n*.65;if(dt>=0)v+=Math.Sin(2*Math.PI*notes[n]*dt)*Math.Exp(-dt*2)*Math.Min(1,dt*50)*.15;}w.Write((short)(Math.Max(-1,Math.Min(1,v))*18000));}
        }
        AssetDatabase.ImportAsset(Folder+"/WelcomeChime.wav");
    }
}
