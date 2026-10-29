using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace ForearmLab
{
    public class SleeveTouchPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public TMP_Text readout;
        public UnityEvent<Vector2> onPositionChanged=new UnityEvent<Vector2>();
        public UnityEvent onReleased=new UnityEvent();
        public Vector2 Position {get;private set;}
        void Track(PointerEventData e)
        {
            var rect=(RectTransform)transform;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,e.position,e.pressEventCamera,out var p))return;
            Position=new Vector2(Mathf.Clamp01((p.x-rect.rect.xMin)/rect.rect.width),Mathf.Clamp01((p.y-rect.rect.yMin)/rect.rect.height));
            if(readout)readout.text=$"TOUCH PAD\n{Position.x:F2} / {Position.y:F2}";
            onPositionChanged.Invoke(Position);
        }
        public void OnPointerDown(PointerEventData e)=>Track(e);
        public void OnDrag(PointerEventData e)=>Track(e);
        public void OnPointerUp(PointerEventData e)=>onReleased.Invoke();
    }
}
