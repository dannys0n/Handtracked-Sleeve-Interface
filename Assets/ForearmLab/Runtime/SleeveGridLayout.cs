using UnityEngine;

namespace ForearmLab
{
    [ExecuteAlways, DefaultExecutionOrder(1100)]
    public class SleeveGridLayout : MonoBehaviour
    {
        public WristSleeve sleeve;
        [Min(1)] public int columns = 12, rows = 6;
        [Range(30,360)] public float arcDegrees = 360;
        public float centerAngle = 90;
        [Tooltip("Circumferential content scroll in degrees, independent of the sleeve anchor.")]
        public float scrollAngle;
        [Min(0), Tooltip("Gain for hand scrolling and the desktop scroll dial.")]
        public float scrollSensitivity = 3;
        [Range(0,.3f)] public float endMargin = .06f;
        [Min(0)] public float surfaceOffset = .004f;
        [Min(0)] public float gap = .003f;
        [Range(0,1)] public float explosion = 1;
        [Min(0)] public float explodeDistance;
        [Min(0)] public float explodeRowGap;
        public bool showDebugLines;
        [Min(.1f)] public float transitionSpeed = 3;
        public float CurrentExplosion { get; private set; }
        SleeveElement[] elements;
        SleeveElement focusedElement;
        Vector2Int focusedSpan;
        float? focusedAngle;
        public SleeveElement FocusedElement => focusedElement;
        public event System.Action<SleeveElement> FocusChanged;

        public void Focus(SleeveElement element)
        {
            if(!element||element.isEmptyPlaceholder||!element.isActiveAndEnabled||element.GetComponentInParent<SleeveGridLayout>()!=this)return;
            focusedElement=element;focusedAngle=null;focusedSpan=new Vector2Int(Mathf.Max(1,columns),Mathf.Max(1,rows));Layout();FocusChanged?.Invoke(focusedElement);
        }
        public void ClearFocus(){focusedElement=null;focusedAngle=null;Layout();FocusChanged?.Invoke(null);}
        public void AimFocusedElement(float angle){if(!focusedElement)return;focusedAngle=angle;Layout();}
        public void ToggleFocus(SleeveElement element){if(focusedElement==element)ClearFocus();else Focus(element);}
        // Gesture adapters can request grid spans without owning layout or modifying prefab defaults.
        public void ResizeFocusedElement(int columnSpan,int rowSpan)
        {
            if(!focusedElement)return;
            focusedSpan=new Vector2Int(Mathf.Clamp(columnSpan,1,Mathf.Max(1,columns)),Mathf.Clamp(rowSpan,1,Mathf.Max(1,rows)));Layout();
        }

        void OnEnable() { Refresh(); Application.onBeforeRender += Layout; }
        void OnDisable()
        {
            focusedElement=null;
            Application.onBeforeRender -= Layout;
            if (elements != null) foreach(var element in elements) if(element) element.SetVisible(true);
        }
        void OnTransformChildrenChanged() => Refresh();
        void OnTransformParentChanged() { if(!sleeve) sleeve=GetComponentInParent<WristSleeve>(); }
        public void Refresh() { elements=GetComponentsInChildren<SleeveElement>(true); if(!sleeve) sleeve=GetComponentInParent<WristSleeve>(); }
        public SleeveElement AddElement(SleeveElement prefab,int column,int row,int columnSpan=1,int rowSpan=1)
        {
            var element=Instantiate(prefab,transform,false);
            element.column=column;element.row=row;element.columnSpan=columnSpan;element.rowSpan=rowSpan;
            Refresh();Layout();return element;
        }
        public void RemoveElement(SleeveElement element)
        {
            if(!element||element.GetComponentInParent<SleeveGridLayout>()!=this||element.isEmptyPlaceholder)return;
            element.gameObject.SetActive(false);
            if(Application.isPlaying)Destroy(element.gameObject);else DestroyImmediate(element.gameObject);
            Refresh();Layout();
        }
        void LateUpdate()
        {
            Layout();
        }
        [ContextMenu("Toggle exploded view")]
        public void ToggleExploded() => explosion=explosion>.5f?0:1;

        public void ScrollByDegrees(float delta)
        {
            scrollAngle=Mathf.Repeat(scrollAngle+delta*scrollSensitivity,360);
            Layout();
        }
        public void ResetScroll() { scrollAngle=0;Layout(); }

        [BeforeRenderOrder(110)]
        public void Layout()
        {
            if(!sleeve) sleeve=GetComponentInParent<WristSleeve>();
            if(!sleeve) return;
            if(elements==null) Refresh();
            if(focusedElement&&(!focusedElement.isActiveAndEnabled||focusedElement.GetComponentInParent<SleeveGridLayout>()!=this))
            {focusedElement=null;FocusChanged?.Invoke(null);}
            CurrentExplosion=explosion;
            int cols=Mathf.Max(1,columns), rowCount=Mathf.Max(1,rows);
            float circumference=Mathf.Max(.005f,sleeve.radius)*Mathf.PI*2;
            float visibleLength=Mathf.Max(.001f,sleeve.VisibleLength);
            bool hasContent=false;
            foreach(var e in elements)if(e&&!e.isEmptyPlaceholder&&e.isActiveAndEnabled){hasContent=true;break;}
            foreach(var element in elements)
            {
                if(!element) continue;
                if(element.isEmptyPlaceholder)
                {
                    bool show=!hasContent;
                    if(element.gameObject.activeSelf!=show)element.gameObject.SetActive(show);
                    if(!show)continue;
                }
                else if(!element.isActiveAndEnabled)continue;
                if(focusedElement&&focusedElement!=element){element.SetVisible(false);continue;}
                float col=Mathf.Clamp(element.column,0,cols-1), row=Mathf.Clamp(element.row,0,rowCount-1);
                float w=Mathf.Clamp(element.columnSpan,1,cols-col),h=Mathf.Clamp(element.rowSpan,1,rowCount-row);
                if(focusedElement==element)
                {
                    float angularCenter=col+w*.5f;
                    w=Mathf.Clamp(focusedSpan.x,1,cols);h=Mathf.Clamp(focusedSpan.y,1,rowCount);
                    // Keep the current angular centre; the content grows around its existing location.
                    col=angularCenter-w*.5f;row=Mathf.Clamp(row+element.rowSpan*.5f-h*.5f,0,rowCount-h);
                }
                float uStart=(centerAngle+scrollAngle-arcDegrees*.5f)/360;
                float cellAngle=arcDegrees/360/cols;
                float angularGap=Mathf.Min(gap/circumference,cellAngle*.35f);
                float cellHeight=(1-2*endMargin)/rowCount;
                float axialGap=Mathf.Min(gap/visibleLength,cellHeight*.35f);
                float u0=uStart+col*cellAngle+angularGap*.5f;
                float u1=uStart+(col+w)*cellAngle-angularGap*.5f;
                if(focusedElement==element&&focusedAngle.HasValue)
                {
                    float offset=focusedAngle.Value/360-(u0+u1)*.5f;
                    u0+=offset;u1+=offset;
                }
                float v0=endMargin+row*cellHeight+axialGap*.5f;
                float v1=endMargin+(row+h)*cellHeight-axialGap*.5f;
                if(element.isEmptyPlaceholder){u0=uStart;u1=uStart+arcDegrees/360;v0=0;v1=1;}
                u1=Mathf.Max(u0+.001f,u1);v1=Mathf.Max(v0+.001f,v1);
                float lift=surfaceOffset+element.radialLift+CurrentExplosion*explodeDistance*element.explosionWeight+element.pressOffset;
                float shift=((row+h*.5f)-rowCount*.5f)*explodeRowGap*CurrentExplosion;
                var center=sleeve.SampleSurface((u0+u1)*.5f,(v0+v1)*.5f,lift);
                var pos=center.position+Vector3.forward*shift;
                element.transform.SetPositionAndRotation(sleeve.transform.TransformPoint(pos),sleeve.transform.rotation*center.rotation);
                // The layout rectangle measures the projected surface, not the smaller projector shell.
                float width=(u1-u0)*(Mathf.Max(.005f,sleeve.radius)+lift)*Mathf.PI*2,height=(v1-v0)*visibleLength;
                element.Apply(sleeve,new Vector4(u0,v0,u1,v1),lift,shift,new Vector2(width,height),CurrentExplosion,showDebugLines);
                element.SetVisible(!Application.isPlaying||sleeve.wristAnchor);
            }
        }
    }
}
