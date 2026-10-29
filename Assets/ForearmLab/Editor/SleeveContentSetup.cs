using System;
using ForearmLab;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SleeveContentSetup
{
    public const string ContentPath="Assets/ForearmLab/Prefabs/SleevePlaceholderContent.prefab";
    const string TetherPath="Assets/ForearmLab/Materials/HologramLine.mat";
    const string VolumePath="Assets/ForearmLab/Materials/HologramVolume.mat";
    static TMP_FontAsset Font=>AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

    [MenuItem("Forearm Lab/Add placeholder sleeve content")]
    public static void Setup()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play Mode first");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.path!="Assets/ForearmLab/Scenes/ForearmXR.unity")throw new InvalidOperationException("Open ForearmXR first");
        AddToScene();EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
    }

    public static void AddToScene()
    {
        CreateContent();
        var sleeve=UnityEngine.Object.FindAnyObjectByType<WristSleeve>();
        if(!sleeve)throw new InvalidOperationException("Install the sleeve first");
        var grid=sleeve.GetComponentInChildren<SleeveGridLayout>(true);
        if(!grid)throw new InvalidOperationException("Sleeve prefab content missing");
        grid.sleeve=sleeve;grid.Refresh();grid.Layout();
        if(!UnityEngine.Object.FindAnyObjectByType<SleeveDemoControls>())
        {var controls=new GameObject("Sleeve content preview controls").AddComponent<SleeveDemoControls>();controls.grid=grid;}
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    public static void CreateContent()
    {
        SleevePrefabSetup.CreateAssets();
        var line=Material(TetherPath,"Universal Render Pipeline/Unlit",new Color(.18f,.9f,1,1));
        var volume=Material(VolumePath,"Universal Render Pipeline/Lit",new Color(.1f,.65f,.9f,1));
        volume.shader=Shader.Find("ForearmLab/Projected Volume");
        line.shader=Shader.Find("ForearmLab/Projected Volume");line.SetFloat("_Unlit",1);
        EditorUtility.SetDirty(volume);EditorUtility.SetDirty(line);
        if(!AssetDatabase.LoadAssetAtPath<GameObject>(ContentPath))
        {
            var root=new GameObject("Sleeve content");
            try
            {
                root.AddComponent<SleeveGridLayout>();
                Tile(root.transform,"Focus",0,0,4,2,"FOCUS\nWorkspace\nPrototype 01",new Color(.05f,.32f,.4f,.3f),line);
                Tile(root.transform,"Time",4,0,2,1,"TIME\n10:24",new Color(.06f,.12f,.2f,.3f),line);
                Tile(root.transform,"Status",4,1,2,1,"STATUS\nREADY",new Color(.04f,.24f,.19f,.3f),line);
                Tile(root.transform,"Tools",0,2,2,2,"TOOLS\nDraw\nCapture",new Color(.09f,.13f,.28f,.3f),line);
                var model=Tile(root.transform,"3D preview",2,2,2,2,"3D",new Color(.08f,.14f,.25f,.3f),line);
                var label=model.panel.GetComponentInChildren<TMP_Text>();label.alignment=TextAlignmentOptions.Top;label.fontSizeMax=9;
                var modelRoot=new GameObject("Replaceable 3D content");modelRoot.transform.SetParent(model.transform,false);
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name="Sample volume";cube.transform.SetParent(modelRoot.transform,false);
                cube.transform.localRotation=Quaternion.Euler(25,35,15);cube.transform.localScale=Vector3.one*.8f;
                UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
                cube.GetComponent<Renderer>().sharedMaterial=volume;cube.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                var outline=new GameObject("Volume edges").AddComponent<LineRenderer>();outline.transform.SetParent(cube.transform,false);
                outline.useWorldSpace=false;outline.sharedMaterial=line;outline.widthMultiplier=.0004f;outline.positionCount=16;
                outline.SetPositions(new[]{new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f)});
                model.volumeContent=modelRoot.transform;
                Tile(root.transform,"Notes",4,2,2,3,"NOTES\n\nFlexible\ncontent\nslots",new Color(.17f,.09f,.27f,.3f),line);
                Tile(root.transform,"Audio",0,4,4,1,"AUDIO  /  02:48  /  PLAY",new Color(.04f,.2f,.25f,.3f),line);
                Tile(root.transform,"Action",0,5,2,1,"ACTION\nCAPTURE",new Color(.03f,.25f,.23f,.3f),line);
                Tile(root.transform,"More",2,5,4,1,"MORE  /  CUSTOM ELEMENTS",new Color(.08f,.14f,.2f,.3f),line);
                AddReverseTiles(root.transform,line);
                PrefabUtility.SaveAsPrefabAsset(root,ContentPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        UpgradeContent(line);
        var prefab=PrefabUtility.LoadPrefabContents(SleevePrefabSetup.PrefabPath);
        try
        {
            if(!prefab.GetComponentInChildren<SleeveGridLayout>(true))
            {
                var content=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ContentPath),prefab.scene);
                content.transform.SetParent(prefab.transform,false);
                var grid=content.GetComponent<SleeveGridLayout>();grid.sleeve=prefab.GetComponent<WristSleeve>();grid.Refresh();grid.Layout();
                PrefabUtility.RecordPrefabInstancePropertyModifications(grid);
                PrefabUtility.SaveAsPrefabAsset(prefab,SleevePrefabSetup.PrefabPath);
            }
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        AssetDatabase.SaveAssets();
    }

    static void UpgradeContent(Material line)
    {
        var tileMaterial=Material("Assets/ForearmLab/Materials/HologramTile.mat","ForearmLab/Outward Hologram Tile",Color.white);
        string textPath="Assets/ForearmLab/Materials/HologramText.mat";
        var textMaterial=AssetDatabase.LoadAssetAtPath<Material>(textPath);
        if(!textMaterial){textMaterial=new Material(Font.material);textMaterial.SetFloat("_CullMode",2);AssetDatabase.CreateAsset(textMaterial,textPath);}
        textMaterial.shader=Shader.Find("ForearmLab/Projected SDF Text");EditorUtility.SetDirty(textMaterial);
        var root=PrefabUtility.LoadPrefabContents(ContentPath);
        try
        {
            var grid=root.GetComponent<SleeveGridLayout>();grid.columns=12;grid.arcDegrees=360;grid.centerAngle=90;grid.surfaceOffset=.004f;
            grid.explosion=1;grid.explodeRowGap=0;
            grid.showDebugLines=false;
            if(!root.transform.Find("Activity"))AddReverseTiles(root.transform,line);
            foreach(var element in root.GetComponentsInChildren<SleeveElement>(true))
            {
                if(!element.label)element.label=element.panel.GetComponentInChildren<TMP_Text>();
                if(element.label&&!element.label.GetComponent<SleeveCurvedText>())element.label.gameObject.AddComponent<SleeveCurvedText>();
                if(element.label){element.label.fontSharedMaterial=textMaterial;element.label.alpha=1;}
                if(element.plate)element.plate.material=tileMaterial;
                if(element.plate)element.plate.canvasRenderer.SetAlpha(1);
                if(element.volumeContent)element.volumeSizeMultiplier=2;
                if(element.volumeContent&&!element.volumeSpinRoot)
                {
                    var mount=element.volumeContent;
                    // Move existing geometry beneath a spin pivot without replacing meshes/materials.
                    var children=new System.Collections.Generic.List<Transform>();foreach(Transform child in mount)children.Add(child);
                    var spin=new GameObject("Volume spin pivot").transform;spin.SetParent(mount,false);
                    foreach(var child in children)child.SetParent(spin,false);
                    mount.localRotation=Quaternion.identity;
                    element.volumeSpinRoot=spin;element.RefreshVolumeBounds();
                }
                if(element.tether)element.tether.useWorldSpace=false;
            }
            PrefabUtility.SaveAsPrefabAsset(root,ContentPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    static void AddReverseTiles(Transform parent,Material line)
    {
        Tile(parent,"Activity",6,0,3,2,"ACTIVITY\nDaily overview",new Color(.04f,.2f,.25f,.3f),line);
        Tile(parent,"Messages",9,0,3,2,"MESSAGES\n3 new",new Color(.14f,.08f,.25f,.3f),line);
        Tile(parent,"Navigation",6,2,4,2,"NAVIGATION\nNext destination",new Color(.04f,.18f,.24f,.3f),line);
        Tile(parent,"Battery",10,2,2,2,"POWER\n84%",new Color(.04f,.22f,.16f,.3f),line);
        Tile(parent,"Quick tools",6,4,2,2,"TOOLS\nQuick access",new Color(.12f,.1f,.25f,.3f),line);
        Tile(parent,"Timeline",8,4,4,2,"TIMELINE\nRecent activity",new Color(.04f,.16f,.22f,.3f),line);
    }

    static Material Material(string path,string shader,Color tint)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find(shader));material.SetColor("_BaseColor",tint);AssetDatabase.CreateAsset(material,path);}
        return material;
    }

    static SleeveElement Tile(Transform parent,string name,int col,int row,int width,int height,string value,Color tint,Material line)
    {
        var root=new GameObject(name);root.transform.SetParent(parent,false);
        var element=root.AddComponent<SleeveElement>();element.column=col;element.row=row;element.columnSpan=width;element.rowSpan=height;
        var panel=new GameObject("Curved hologram",typeof(RectTransform),typeof(Canvas));panel.transform.SetParent(root.transform,false);
        panel.transform.localScale=Vector3.one*.001f;
        var canvas=panel.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        var graphic=panel.AddComponent<SleeveTileGraphic>();graphic.color=tint;graphic.raycastTarget=false;
        var text=new GameObject("Placeholder content",typeof(RectTransform)).AddComponent<TextMeshProUGUI>();text.transform.SetParent(panel.transform,false);
        text.rectTransform.anchorMin=Vector2.zero;text.rectTransform.anchorMax=Vector2.one;
        text.rectTransform.offsetMin=new Vector2(2,2);text.rectTransform.offsetMax=new Vector2(-2,-2);
        text.rectTransform.localPosition+=Vector3.back*.8f;
        text.font=Font;text.color=new Color(.72f,1,1,1);text.fontSize=12;text.enableAutoSizing=true;text.fontSizeMin=2;text.fontSizeMax=12;
        text.alignment=TextAlignmentOptions.Center;text.richText=false;text.isOrthographic=true;text.raycastTarget=false;
        text.textWrappingMode=TextWrappingModes.Normal;text.overflowMode=TextOverflowModes.Ellipsis;text.text=value;
        text.gameObject.AddComponent<SleeveCurvedText>();
        element.panel=canvas;element.plate=graphic;element.label=text;element.fullLabel=value;
        element.compactLabel=name=="3D preview"?"3D":name.ToUpperInvariant();
        var tether=new GameObject("Exploded tether").AddComponent<LineRenderer>();tether.transform.SetParent(root.transform,false);
        tether.sharedMaterial=line;tether.positionCount=2;tether.useWorldSpace=false;tether.widthMultiplier=.0005f;tether.enabled=false;
        element.tether=tether;return element;
    }
}
