using System;
using ForearmLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class SleevePrefabSetup
{
    public const string PrefabPath = "Assets/ForearmLab/Prefabs/WristSleeve.prefab";
    public const string MaterialPath = "Assets/ForearmLab/Materials/SleeveWhiteTransparent.mat";
    const string MeshPath = "Assets/ForearmLab/Meshes/SleeveTube.asset";

    [MenuItem("Forearm Lab/Create sleeve prefab")]
    public static void CreateAssets()
    {
        Folder("Assets/ForearmLab", "Prefabs"); Folder("Assets/ForearmLab", "Materials"); Folder("Assets/ForearmLab", "Meshes");
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (!mesh) { mesh = Tube(); AssetDatabase.CreateAsset(mesh, MeshPath); }
        else if(mesh.name != "Yoshimura reference facets v3")
        {
            var updated=Tube();mesh.Clear();mesh.name=updated.name;
            mesh.vertices=updated.vertices;mesh.normals=updated.normals;mesh.uv=updated.uv;
            var bary=new System.Collections.Generic.List<Vector3>();updated.GetUVs(1,bary);mesh.SetUVs(1,bary);
            var folded=new System.Collections.Generic.List<Vector3>();updated.GetUVs(2,folded);mesh.SetUVs(2,folded);
            mesh.triangles=updated.triangles;mesh.bounds=updated.bounds;
            UnityEngine.Object.DestroyImmediate(updated);EditorUtility.SetDirty(mesh);
        }
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Sleeve white transparent" };
            material.SetColor("_BaseColor", new Color(1,1,1,.18f));
            material.SetFloat("_Surface",1); material.SetFloat("_Blend",0);
            material.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha",(float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha",(float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite",0); material.SetFloat("_Cull",(float)CullMode.Off);
            material.SetOverrideTag("RenderType","Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        var hologramShader=Shader.Find("ForearmLab/Sleeve Hologram");
        if(hologramShader && material.shader!=hologramShader)
        {material.shader=hologramShader;EditorUtility.SetDirty(material);}
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
        {
            var root = new GameObject("Wrist Sleeve");
            try
            {
                var sleeve = root.AddComponent<WristSleeve>();
                var surface = new GameObject("Sleeve surface", typeof(MeshFilter), typeof(MeshRenderer));
                surface.transform.SetParent(root.transform,false);
                surface.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = surface.GetComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                var serialized = new SerializedObject(sleeve);
                serialized.FindProperty("surface").objectReferenceValue = surface.transform;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                sleeve.UpdateSleeve();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Forearm Lab/Install sleeve in ForearmXR")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/ForearmLab/Scenes/ForearmXR.unity") throw new InvalidOperationException("Open ForearmXR first");
        AddToScene(); EditorSceneManager.SaveScene(scene);
    }

    public static void AddToScene()
    {
        CreateAssets();
        if (UnityEngine.Object.FindAnyObjectByType<WristSleeve>()) return;
        var old = UnityEngine.Object.FindAnyObjectByType<ForearmAnchor>();
        var rig = UnityEngine.Object.FindAnyObjectByType<OVRCameraRig>();
        var skeleton = old ? old.leftSkeleton : rig ? rig.leftHandAnchor.GetComponentInChildren<OVRSkeleton>() : null;
        if (!skeleton) throw new InvalidOperationException("Left Meta hand skeleton missing");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var sleeve = instance.GetComponent<WristSleeve>();
        instance.transform.SetPositionAndRotation(new Vector3(-.2f,1.15f,.5f),Quaternion.identity);
        var input = new GameObject("Sleeve wrist input"); input.transform.SetParent(skeleton.transform,false);
        input.transform.SetPositionAndRotation(new Vector3(-.2f,1.15f,.5f),Quaternion.identity);
        var binding = input.AddComponent<MetaSleeveWristBinding>(); binding.skeleton = skeleton; binding.sleeve = sleeve;
        sleeve.wristAnchor = input.transform;
        sleeve.UpdateSleeve(); PrefabUtility.RecordPrefabInstancePropertyModifications(sleeve);
        if (old) UnityEngine.Object.DestroyImmediate(old.gameObject);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    static void Folder(string parent,string name)
    { if (!AssetDatabase.IsValidFolder(parent+"/"+name)) AssetDatabase.CreateFolder(parent,name); }

    static Mesh Tube()
    {
        // Broad alternating square rings match the supplied paper reference.
        // Subdivision keeps the open tube round; the shader morphs each coarse triangle to a flat face.
        const int around=4, along=8, subdivisions=8;
        var vertices=new System.Collections.Generic.List<Vector3>();
        var uv=new System.Collections.Generic.List<Vector2>();
        var barycentric=new System.Collections.Generic.List<Vector3>();
        var folded=new System.Collections.Generic.List<Vector3>();
        var triangles=new System.Collections.Generic.List<int>();
        Vector2 Node(int row,int column)=>new Vector2((column+(row%2)*.5f)/around,(float)row/along);
        Vector3 Position(Vector2 p)=>new Vector3(Mathf.Cos(p.x*Mathf.PI*2),Mathf.Sin(p.x*Mathf.PI*2),p.y);
        void Face(Vector2 a,Vector2 b,Vector2 c)
        {
            var pa=Position(a);var pb=Position(b);var pc=Position(c);
            if(Vector3.Dot(Vector3.Cross(pb-pa,pc-pa),new Vector3(pa.x,pa.y,0))<0)
            {var swap=b;b=c;c=swap;pb=Position(b);pc=Position(c);}
            void Vertex(Vector3 bary)
            {
                var coordinates=a*bary.x+b*bary.y+c*bary.z;
                vertices.Add(Position(coordinates));uv.Add(coordinates);barycentric.Add(bary);
                folded.Add(pa*bary.x+pb*bary.y+pc*bary.z);triangles.Add(vertices.Count-1);
            }
            Vector3 Bary(int x,int y)=>new Vector3(1-(float)(x+y)/subdivisions,(float)x/subdivisions,(float)y/subdivisions);
            for(int y=0;y<subdivisions;y++)for(int x=0;x<subdivisions-y;x++)
            {
                Vertex(Bary(x,y));Vertex(Bary(x+1,y));Vertex(Bary(x,y+1));
                if(x+y<subdivisions-1){Vertex(Bary(x+1,y));Vertex(Bary(x+1,y+1));Vertex(Bary(x,y+1));}
            }
        }
        // Half-cell stagger gives circumferential edges and alternating diagonal creases.
        for(int row=0;row<along;row++)for(int col=0;col<around;col++)
        {
            int offset=row%2;
            Face(Node(row,col),Node(row,col+1),Node(row+1,col+offset));
            Face(Node(row,col+1-offset),Node(row+1,col+1),Node(row+1,col));
        }
        var mesh=new Mesh{name="Yoshimura reference facets v3"};
        mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetUVs(1,barycentric);mesh.SetUVs(2,folded);mesh.SetTriangles(triangles,0);
        mesh.RecalculateNormals();
        mesh.bounds=new Bounds(new Vector3(0,0,.5f),new Vector3(2.1f,2.1f,1.05f));return mesh;
    }
}