using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class SleeveTileGraphic : MaskableGraphic
    {
        public Color edgeColor = new Color(.25f,1,1,.9f);
        public WristSleeve sleeve;
        public Vector4 region;
        public float lift, axialShift;
        Vector4 previousRegion;
        Vector4 previousShape;
        public override Material materialForRendering => sleeve?sleeve.ProjectionMaterial(base.materialForRendering):base.materialForRendering;

        public void Configure(WristSleeve source,Vector4 bounds,float offset,float shift)
        {
            var shape=new Vector4(source.radius,source.EffectiveLength,0,offset);
            if(sleeve!=source||previousRegion!=bounds||previousShape!=shape||axialShift!=shift)
            {
                sleeve=source;region=bounds;lift=offset;axialShift=shift;
                previousRegion=bounds;previousShape=shape;SetVerticesDirty();SetMaterialDirty();
            }
        }

        Vector3 Point(float u,float v)
        {
            var p=sleeve.SampleSurface(u,v,lift).position+Vector3.forward*axialShift;
            return rectTransform.InverseTransformPoint(sleeve.transform.TransformPoint(p));
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(!sleeve)return;
            const int horizontal=16,vertical=8;
            for(int y=0;y<vertical;y++)for(int x=0;x<horizontal;x++)
            {
                float u0=Mathf.Lerp(region.x,region.z,(float)x/horizontal),u1=Mathf.Lerp(region.x,region.z,(float)(x+1)/horizontal);
                float v0=Mathf.Lerp(region.y,region.w,(float)y/vertical),v1=Mathf.Lerp(region.y,region.w,(float)(y+1)/vertical);
                Quad(mesh,Point(u0,v0),Point(u1,v0),Point(u1,v1),Point(u0,v1),color);
            }
            float du=.0007f/(Mathf.Max(.005f,sleeve.radius+lift)*Mathf.PI*2),dv=.0007f/Mathf.Max(.001f,sleeve.VisibleLength);
            du=Mathf.Min(du,(region.z-region.x)*.1f);dv=Mathf.Min(dv,(region.w-region.y)*.1f);
            for(int x=0;x<horizontal;x++)
            {
                float a=Mathf.Lerp(region.x,region.z,(float)x/horizontal),b=Mathf.Lerp(region.x,region.z,(float)(x+1)/horizontal);
                Quad(mesh,Point(a,region.y),Point(b,region.y),Point(b,region.y+dv),Point(a,region.y+dv),edgeColor);
                Quad(mesh,Point(a,region.w-dv),Point(b,region.w-dv),Point(b,region.w),Point(a,region.w),edgeColor);
            }
            for(int y=0;y<vertical;y++)
            {
                float a=Mathf.Lerp(region.y,region.w,(float)y/vertical),b=Mathf.Lerp(region.y,region.w,(float)(y+1)/vertical);
                Quad(mesh,Point(region.x,a),Point(region.x+du,a),Point(region.x+du,b),Point(region.x,b),edgeColor);
                Quad(mesh,Point(region.z-du,a),Point(region.z,a),Point(region.z,b),Point(region.z-du,b),edgeColor);
            }
        }
        static void Quad(VertexHelper mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint)
        {
            int i=mesh.currentVertCount;mesh.AddVert(a,tint,Vector2.zero);mesh.AddVert(b,tint,Vector2.right);
            mesh.AddVert(c,tint,Vector2.one);mesh.AddVert(d,tint,Vector2.up);
            mesh.AddTriangle(i,i+1,i+2);mesh.AddTriangle(i,i+2,i+3);
        }
    }
}
