using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEngine;

namespace ForearmLab
{
    // Project-specific cylindrical ISurfacePatch; Meta still owns hover/press/drag lifecycle.
    public class SleevePokeSurface : MonoBehaviour, ISurfacePatch
    {
        public SleeveElement element;
        public PokeInteractable interactable;
        public Transform Transform => element&&element.Sleeve?element.Sleeve.transform:transform;
        CylinderBacking backing;
        public ISurface BackingSurface => backing??(backing=new CylinderBacking(this));
        sealed class CylinderBacking : ISurface
        {
            readonly SleevePokeSurface owner;
            public CylinderBacking(SleevePokeSurface owner){this.owner=owner;}
            public Transform Transform=>owner.Transform;
            public bool ClosestSurfacePoint(in Vector3 point,out SurfaceHit hit,float maxDistance=0)=>owner.Closest(point,out hit,maxDistance,false);
            public bool Raycast(in Ray ray,out SurfaceHit hit,float maxDistance=0)=>owner.Cast(ray,out hit,maxDistance,false);
        }
        bool Ready => element&&!element.isEmptyPlaceholder&&element.Sleeve&&element.panel&&element.panel.enabled&&element.Sleeve.closeAmount<.01f;
        void Update(){if(interactable)interactable.enabled=Ready;}
        public float UnwrappedU(Vector3 local)
        {
            float u=Mathf.Atan2(local.y,local.x)/(2*Mathf.PI);
            float middle=(element.Region.x+element.Region.z)*.5f;
            return u+Mathf.Round(middle-u);
        }
        public Vector2 CanvasPoint(Vector3 world)
        {
            var local=Transform.InverseTransformPoint(world);var rect=element.Region;
            float radius=element.Sleeve.radius+element.Lift;
            return new Vector2((UnwrappedU(local)-(rect.x+rect.z)*.5f)*2*Mathf.PI*radius*1000,
                -((local.z-element.AxialShift)/element.Sleeve.EffectiveLength-(rect.y+rect.w)*.5f)*element.Sleeve.EffectiveLength*1000);
        }
        public bool ClosestSurfacePoint(in Vector3 point,out SurfaceHit hit,float maxDistance=0)
            =>Closest(point,out hit,maxDistance,true);
        bool Closest(Vector3 point,out SurfaceHit hit,float maxDistance,bool bounded)
        {
            hit=default;if(!Ready)return false;
            var local=Transform.InverseTransformPoint(point);var rect=element.Region;
            float u=UnwrappedU(local),v=(local.z-element.AxialShift)/element.Sleeve.EffectiveLength;
            if(bounded){u=Mathf.Clamp(u,rect.x,rect.z);v=Mathf.Clamp(v,rect.y,rect.w);}
            float radius=element.Sleeve.radius+element.Lift;
            var p=new Vector3(Mathf.Cos(u*2*Mathf.PI)*radius,Mathf.Sin(u*2*Mathf.PI)*radius,v*element.Sleeve.EffectiveLength+element.AxialShift);
            return Hit(point,p,out hit,maxDistance);
        }
        bool Hit(Vector3 origin,Vector3 local,out SurfaceHit hit,float maxDistance)
        {
            var point=Transform.TransformPoint(local);float distance=Vector3.Distance(origin,point);
            hit=new SurfaceHit{Point=point,Normal=Transform.TransformDirection(new Vector3(local.x,local.y,0).normalized),Distance=distance};
            return maxDistance<=0||distance<=maxDistance;
        }
        public bool Raycast(in Ray ray,out SurfaceHit hit,float maxDistance=0)
            =>Cast(ray,out hit,maxDistance,true);
        bool Cast(Ray ray,out SurfaceHit hit,float maxDistance,bool bounded)
        {
            hit=default;if(!Ready)return false;
            var o=Transform.InverseTransformPoint(ray.origin);var d=Transform.InverseTransformVector(ray.direction);
            float radius=element.Sleeve.radius+element.Lift;
            float a=d.x*d.x+d.y*d.y,b=2*(o.x*d.x+o.y*d.y),c=o.x*o.x+o.y*o.y-radius*radius;
            float disc=b*b-4*a*c;if(a<.000001f||disc<0)return false;
            for(int i=0;i<2;i++)
            {
                float t=(-b+(i==0?-1:1)*Mathf.Sqrt(disc))/(2*a);if(t<0)continue;
                var p=o+d*t;var rect=element.Region;float u=UnwrappedU(p),v=(p.z-element.AxialShift)/element.Sleeve.EffectiveLength;
                if(bounded&&(u<rect.x||u>rect.z||v<rect.y||v>rect.w))continue;
                return Hit(ray.origin,p,out hit,maxDistance);
            }
            return false;
        }
    }
}
