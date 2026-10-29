using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    // Adapts ordinary uGUI geometry; control state/layout remain standard Unity components.
    [ExecuteAlways, RequireComponent(typeof(Graphic))]
    public class SleeveContentProjection : BaseMeshEffect, IMaterialModifier
    {
        public SleeveElement element;
        TMP_Text text;
        Vector4 lastShape;
        Vector4 lastRegion;
        protected override void OnEnable()
        {
            base.OnEnable();text=GetComponent<TMP_Text>();
            if(text)text.OnPreRenderText+=CurveText;
        }
        protected override void OnDisable()
        {
            if(text)text.OnPreRenderText-=CurveText;
            base.OnDisable();
        }
        void LateUpdate()
        {
            if(!element||!element.Sleeve)return;
            var shape=new Vector4(element.Sleeve.radius,element.Sleeve.EffectiveLength,element.Lift,element.AxialShift);
            if(shape==lastShape&&element.Region==lastRegion)return;
            lastShape=shape;lastRegion=element.Region;graphic.SetVerticesDirty();graphic.SetMaterialDirty();
        }
        public Material GetModifiedMaterial(Material source) =>
            IsActive()&&element&&element.Sleeve?element.Sleeve.ProjectionMaterial(source):source;
        Vector3 Curve(Vector3 point)
        {
            if(!element||!element.Sleeve||!element.panel)return point;
            var canvas=(RectTransform)element.panel.transform;
            var local=canvas.InverseTransformPoint(transform.TransformPoint(point));
            var bounds=element.Region;var sleeve=element.Sleeve;
            float radius=Mathf.Max(.005f,sleeve.radius)+element.Lift;
            float u=(bounds.x+bounds.z)*.5f+local.x*.001f/(2*Mathf.PI*radius);
            float v=(bounds.y+bounds.w)*.5f-local.y*.001f/Mathf.Max(.001f,sleeve.EffectiveLength);
            var p=sleeve.SampleSurface(u,v,element.Lift+.0008f-local.z*.001f).position+Vector3.forward*element.AxialShift;
            return transform.InverseTransformPoint(sleeve.transform.TransformPoint(p));
        }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive()||text)return;
            var source=new System.Collections.Generic.List<UIVertex>();mesh.GetUIVertexStream(source);mesh.Clear();
            // Ordinary Images contain only two triangles. Subdivide before curving, otherwise
            // a wide control's chord cuts through the sleeve despite its corners being correct.
            for(int t=0;t<source.Count;t+=3)
            {
                var a=source[t];var b=source[t+1];var c=source[t+2];
                UIVertex Vertex(int x,int y)
                {
                    float u=x/4f,v=y/4f,w=1-u-v;var result=a;
                    result.position=Curve(a.position*w+b.position*u+c.position*v);
                    result.uv0=a.uv0*w+b.uv0*u+c.uv0*v;
                    result.color=(Color)a.color*w+(Color)b.color*u+(Color)c.color*v;return result;
                }
                for(int y=0;y<4;y++)for(int x=0;x<4-y;x++)
                {
                    int i=mesh.currentVertCount;mesh.AddVert(Vertex(x,y));mesh.AddVert(Vertex(x+1,y));mesh.AddVert(Vertex(x,y+1));mesh.AddTriangle(i,i+1,i+2);
                    if(x+y<3){i=mesh.currentVertCount;mesh.AddVert(Vertex(x+1,y));mesh.AddVert(Vertex(x+1,y+1));mesh.AddVert(Vertex(x,y+1));mesh.AddTriangle(i,i+1,i+2);}
                }
            }
        }
        void CurveText(TMP_TextInfo info)
        {
            if(!IsActive())return;
            for(int i=0;i<info.characterCount;i++)
            {
                var c=info.characterInfo[i];if(!c.isVisible)continue;
                var vertices=info.meshInfo[c.materialReferenceIndex].vertices;
                for(int j=0;j<4;j++)vertices[c.vertexIndex+j]=Curve(vertices[c.vertexIndex+j]);
            }
        }
    }
}
