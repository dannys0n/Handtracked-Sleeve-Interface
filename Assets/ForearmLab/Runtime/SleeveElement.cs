using UnityEngine;
using TMPro;

namespace ForearmLab
{
    [ExecuteAlways]
    public class SleeveElement : MonoBehaviour
    {
        [Min(0)] public int column,row;
        [Min(1)] public int columnSpan=1,rowSpan=1;
        public float radialLift;
        [Tooltip("Fallback surface shown only when the grid has no active content elements.")]
        public bool isEmptyPlaceholder;
        [Tooltip("Normal uGUI children belong here. Use anchors/layout groups inside this rectangle.")]
        public RectTransform contentRoot;
        public WristSleeve Sleeve { get; private set; }
        public Vector4 Region { get; private set; }
        public float Lift { get; private set; }
        public float AxialShift { get; private set; }
        [Min(0)] public float explosionWeight=1;
        public Canvas panel;
        public SleeveTileGraphic plate;
        public TMP_Text label;
        [TextArea] public string fullLabel;
        public string compactLabel;
        [Tooltip("Optional arbitrary 3D content. Scaled to fit this cell's smaller dimension.")]
        public Transform volumeContent;
        [Tooltip("Rotating geometry beneath the fixed volume mount. Its centre stays above the host tile.")]
        public Transform volumeSpinRoot;
        [Min(0)] public float volumeClearance=.002f;
        [Min(.1f), Tooltip("Relative to the original sample volume size. Default doubles its linear dimensions.")]
        public float volumeSizeMultiplier=2;
        [Tooltip("Additional volume offset in the element's local metres.")]
        public Vector3 volumeOffset;
        public bool animateVolume = true;
        public LineRenderer tether;
        bool visible=true,dirty=true;
        Renderer[] renderers;
        bool[] rendererDefaults;
        SleeveCurvedText curvedText;
        Vector3 volumeBoundsCenter;
        float volumeBoundsRadius;
        bool volumeBoundsReady;

        void OnEnable() {dirty=true;volumeBoundsReady=false;}
        void OnTransformChildrenChanged() {dirty=true;volumeBoundsReady=false;}
        void Update()
        {if(Application.isPlaying&&visible&&animateVolume&&volumeSpinRoot)volumeSpinRoot.Rotate(0,20*Time.deltaTime,0,Space.Self);}

        [ContextMenu("Refresh volume bounds")]
        public void RefreshVolumeBounds()
        {
            volumeBoundsReady=true;volumeBoundsCenter=Vector3.zero;volumeBoundsRadius=0;
            if(!volumeSpinRoot)return;
            var corners=new System.Collections.Generic.List<Vector3>();
            foreach(var renderer in volumeSpinRoot.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer is LineRenderer)continue;
                var bounds=renderer.localBounds;
                for(int i=0;i<8;i++)
                {
                    var sign=new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1);
                    corners.Add(volumeSpinRoot.InverseTransformPoint(renderer.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,sign))));
                }
            }
            if(corners.Count==0)return;
            var combined=new Bounds(corners[0],Vector3.zero);
            foreach(var corner in corners)combined.Encapsulate(corner);
            volumeBoundsCenter=combined.center;
            foreach(var corner in corners)volumeBoundsRadius=Mathf.Max(volumeBoundsRadius,Vector3.Distance(corner,volumeBoundsCenter));
        }

        public void Apply(WristSleeve sleeve,Vector4 region,float lift,float shift,Vector2 size,float exploded,bool debugLines=false)
        {
            Sleeve=sleeve;Region=region;Lift=lift;AxialShift=shift;
            if(dirty||renderers==null)
            {
                renderers=GetComponentsInChildren<Renderer>(true);rendererDefaults=new bool[renderers.Length];
                for(int i=0;i<renderers.Length;i++)rendererDefaults[i]=renderers[i].enabled;
                dirty=false;
            }
            foreach(var renderer in renderers)sleeve.ConfigureProjectionRenderer(renderer);
            if(panel)
            {
                var rect=(RectTransform)panel.transform;
                var dimensions=size*1000;
                if(rect.sizeDelta!=dimensions)rect.sizeDelta=dimensions;
            }
            if(plate)plate.Configure(sleeve,region,lift,shift);
            if(label)
            {
                var rect=label.rectTransform;
                var middle=new Vector2(.5f,.5f);
                if(rect.anchorMin!=middle)rect.anchorMin=middle;
                if(rect.anchorMax!=middle)rect.anchorMax=middle;
                if(rect.anchoredPosition!=Vector2.zero)rect.anchoredPosition=Vector2.zero;
                var labelSize=new Vector2(Mathf.Max(.002f,size.x*.82f),Mathf.Max(.002f,size.y*.8f))*1000;
                if(rect.sizeDelta!=labelSize)rect.sizeDelta=labelSize;
                if(!string.IsNullOrEmpty(fullLabel))
                {
                    string text=size.y<.018f||size.x<.025f?compactLabel:fullLabel;
                    if(label.text!=text)label.text=text;
                }
                if(!curvedText)curvedText=label.GetComponent<SleeveCurvedText>();
                if(curvedText)curvedText.Configure(sleeve,region,lift,shift);
            }
            if(volumeContent)
            {
                float scale=Mathf.Min(size.x,size.y)*.45f*volumeSizeMultiplier;
                volumeContent.localScale=Vector3.one*scale;
                // The mount pivot stays on the host element. Only its geometry child spins.
                volumeContent.localPosition=volumeOffset;
                volumeContent.localRotation=Quaternion.identity;
                if(volumeSpinRoot)
                {
                    if(!volumeBoundsReady)RefreshVolumeBounds();
                    // A bounding sphere remains safe for every orientation, including tilted corners.
                    float clearance=volumeBoundsRadius+volumeClearance/Mathf.Max(.0001f,scale);
                    volumeSpinRoot.localPosition=Vector3.back*clearance-volumeSpinRoot.localRotation*volumeBoundsCenter;
                }
            }
            if(tether)
            {
                tether.enabled=visible&&debugLines&&lift>.0001f;
                // One coordinate convention: local points, transformed once by the common shader.
                tether.useWorldSpace=false;
                var start=sleeve.transform.TransformPoint(sleeve.SampleSurface((region.x+region.z)*.5f,(region.y+region.w)*.5f).position);
                tether.SetPosition(0,tether.transform.InverseTransformPoint(start));
                tether.SetPosition(1,tether.transform.InverseTransformPoint(transform.position));
            }
        }

        public void SetVisible(bool value)
        {
            if(dirty||renderers==null)
            {
                renderers=GetComponentsInChildren<Renderer>(true);rendererDefaults=new bool[renderers.Length];
                for(int i=0;i<renderers.Length;i++)rendererDefaults[i]=renderers[i].enabled;
                dirty=false;
            }
            visible=value;if(panel)panel.enabled=value;
            for(int i=0;i<renderers.Length;i++)if(renderers[i]&&renderers[i]!=tether)renderers[i].enabled=value&&rendererDefaults[i];
            if(!value&&tether)tether.enabled=false;
        }

    }
}
