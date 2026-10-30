using Oculus.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    // Meta Grabbable/GrabFreeTransformer own movement and two-hand scaling.
    public class CapturedImageCard : MonoBehaviour
    {
        public Grabbable grabbable;
        public RawImage image;
        public RectTransform canvas;
        public BoxCollider grabCollider;
        [Min(.005f)] public float storeDistance = .035f;
        public SleeveImageContent destination;
        Texture2D ownedTexture;
        bool wasHeld;
        static readonly System.Collections.Generic.HashSet<CapturedImageCard> activeCards = new System.Collections.Generic.HashSet<CapturedImageCard>();
        public static System.Collections.Generic.IEnumerable<CapturedImageCard> ActiveCards => activeCards;
        public bool HasImage => ownedTexture;
        public SleeveImageInsertion Insertion => destination ? destination.GetComponent<SleeveImageInsertion>() : null;
        void OnEnable()=>activeCards.Add(this);
        void OnDisable()=>activeCards.Remove(this);
        public bool IsHeld => grabbable && grabbable.GrabPoints.Count > 0;
        public Vector3 ClosestPoint(Vector3 world)
        {
            if(!grabCollider)return transform.position;
            var p=grabCollider.transform.InverseTransformPoint(world)-grabCollider.center;
            var half=grabCollider.size*.5f;
            p=new Vector3(Mathf.Clamp(p.x,-half.x,half.x),Mathf.Clamp(p.y,-half.y,half.y),Mathf.Clamp(p.z,-half.z,half.z));
            return grabCollider.transform.TransformPoint(p+grabCollider.center);
        }
        public void Initialize(Texture2D texture, SleeveImageContent target)
        {
            ownedTexture = texture; destination = target;
            if(image)image.texture = texture;
            float height = 200f / Mathf.Max(.1f, (float)texture.width / texture.height);
            if(canvas)canvas.sizeDelta = new Vector2(200, height);
            if(grabCollider)grabCollider.size = new Vector3(.2f, height * .001f, .012f);
        }
        void Update()
        {
            bool held = IsHeld;
            if(wasHeld && !held)
            {
                if(Insertion)Insertion.Released(this);
                else TryStore();
            }
            wasHeld = held;
        }
        public bool TryStore()
        {
            if(Insertion)return Insertion.TryStore(this);
            if(!destination || !ownedTexture || !grabCollider)return false;
            var element = destination.GetComponentInParent<SleeveElement>();
            var surface = element && element.panel ? element.panel.GetComponent<SleevePokeSurface>() : null;
            if(!surface || !element.panel.enabled || !element.Sleeve || element.Sleeve.closeAmount >= .01f)return false;
            // Collider-sized tolerance lets the card overlap its destination without requiring its centre to penetrate it.
            float reach = storeDistance + .5f * Mathf.Max(grabCollider.bounds.size.x, grabCollider.bounds.size.y);
            if(!surface.ClosestSurfacePoint(transform.position,out var hit,reach))return false;
            return CompleteStorage();
        }
        public bool CompleteStorage()
        {
            if(!destination||!ownedTexture)return false;
            destination.SetOwnedTexture(ownedTexture);ownedTexture=null;Destroy(gameObject);return true;
        }
        void OnDestroy(){if(ownedTexture)Destroy(ownedTexture);}
    }
}
