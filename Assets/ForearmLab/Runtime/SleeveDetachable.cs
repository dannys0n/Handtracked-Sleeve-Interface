using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ForearmLab
{
    // A first detachable image example. Gesture capture and hand grabbing are separate inputs.
    public class SleeveDetachable : MonoBehaviour
    {
        public SleeveImageContent source;
        public UnityEvent<GameObject> onDetached=new UnityEvent<GameObject>();
        GameObject floating;
        public void DetachOrStore()
        {
            if(floating){Destroy(floating);floating=null;return;}
            if(!source||!source.image)return;
            floating=new GameObject("Detached sleeve image",typeof(RectTransform),typeof(Canvas));
            var canvas=floating.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
            var rect=(RectTransform)floating.transform;rect.sizeDelta=new Vector2(160,100);rect.localScale=Vector3.one*.001f;
            var camera=Camera.main;
            rect.position=camera?camera.transform.position+camera.transform.forward*.45f:transform.position-transform.forward*.15f;
            rect.rotation=camera?camera.transform.rotation:transform.rotation;
            var picture=new GameObject("Image",typeof(RectTransform),typeof(RawImage));picture.transform.SetParent(rect,false);
            var p=(RectTransform)picture.transform;p.anchorMin=Vector2.zero;p.anchorMax=Vector2.one;p.sizeDelta=Vector2.zero;
            picture.GetComponent<RawImage>().texture=source.image.texture;
            onDetached.Invoke(floating);
        }
        void OnDestroy(){if(floating)Destroy(floating);}
    }
}
