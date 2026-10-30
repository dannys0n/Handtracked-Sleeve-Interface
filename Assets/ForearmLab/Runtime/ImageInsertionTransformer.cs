using Oculus.Interaction;
using UnityEngine;

namespace ForearmLab
{
    // SDK free movement/scaling remains the baseline; insertion constrains its result.
    public class ImageInsertionTransformer : MonoBehaviour, ITransformer
    {
        public GrabFreeTransformer free;
        public CapturedImageCard card;
        IGrabbable grabbable;
        public void Initialize(IGrabbable target){grabbable=target;free.Initialize(target);}
        public void BeginTransform()
        {
            free.BeginTransform();
            if(card.Insertion)card.Insertion.RebaseGrab(card,grabbable);
        }
        public void UpdateTransform()
        {
            free.UpdateTransform();
            var slot=card.Insertion;
            if(slot&&slot.Constrain(card,grabbable))return;
            if(slot&&slot.ConsumeRebase(card)){free.EndTransform();free.BeginTransform();}
        }
        public void EndTransform()=>free.EndTransform();
    }
}
