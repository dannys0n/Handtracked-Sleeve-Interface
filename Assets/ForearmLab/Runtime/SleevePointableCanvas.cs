using Oculus.Interaction;
using UnityEngine;

namespace ForearmLab
{
    // Convert actual cylinder hits to the flat canvas coordinates expected by Meta's UI module.
    public class SleevePointableCanvas : PointableCanvas
    {
        public SleevePokeSurface surface;
        public override void ProcessPointerEvent(PointerEvent evt)
        {
            if(!surface||!Canvas)return;
            var p=surface.CanvasPoint(evt.Pose.position);
            var pose=new Pose(Canvas.transform.TransformPoint(new Vector3(p.x,p.y,0)),Canvas.transform.rotation);
            base.ProcessPointerEvent(new PointerEvent(evt.Identifier,evt.Type,pose,evt.Data));
        }
    }
}
