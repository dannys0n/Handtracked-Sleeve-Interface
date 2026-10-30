using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    // Clip only the portion behind the insert mouth. No shared material changes.
    [RequireComponent(typeof(Graphic))]
    public class ImageInsertionClip : BaseMeshEffect
    {
        public CapturedImageCard card;
        bool wasClipped;
        readonly List<UIVertex> source=new List<UIVertex>();
        readonly List<UIVertex> polygon=new List<UIVertex>(4);
        void LateUpdate()
        {
            bool clipping=card&&card.Insertion&&card.Insertion.LockedCard==card;
            if(clipping||wasClipped)graphic.SetVerticesDirty();
            wasClipped=clipping;
        }
        public override void ModifyMesh(VertexHelper mesh)
        {
            if(!IsActive()||!card||!card.Insertion||card.Insertion.LockedCard!=card)return;
            var slot=card.Insertion.SlotPose;
            var normal=slot.rotation*Vector3.forward;
            float Distance(UIVertex v)=>Vector3.Dot(transform.TransformPoint(v.position)-slot.position,normal);
            UIVertex Lerp(UIVertex a,UIVertex b,float t)
            {
                var v=a;v.position=Vector3.LerpUnclamped(a.position,b.position,t);
                v.uv0=Vector4.LerpUnclamped(a.uv0,b.uv0,t);v.color=Color.LerpUnclamped(a.color,b.color,t);return v;
            }
            source.Clear();mesh.GetUIVertexStream(source);mesh.Clear();
            for(int i=0;i<source.Count;i+=3)
            {
                polygon.Clear();
                var previous=source[i+2];float previousDistance=Distance(previous);
                for(int j=0;j<3;j++)
                {
                    var current=source[i+j];float currentDistance=Distance(current);
                    bool wasInside=previousDistance>=0,isInside=currentDistance>=0;
                    if(wasInside!=isInside)polygon.Add(Lerp(previous,current,previousDistance/(previousDistance-currentDistance)));
                    if(isInside)polygon.Add(current);
                    previous=current;previousDistance=currentDistance;
                }
                if(polygon.Count<3)continue;
                int start=mesh.currentVertCount;
                foreach(var v in polygon)mesh.AddVert(v);
                for(int j=1;j<polygon.Count-1;j++)mesh.AddTriangle(start,start+j,start+j+1);
            }
        }
    }
}
