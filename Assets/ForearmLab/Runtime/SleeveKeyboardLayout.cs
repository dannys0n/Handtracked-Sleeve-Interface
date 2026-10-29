using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForearmLab
{
    [ExecuteAlways,RequireComponent(typeof(RectTransform))]
    public class SleeveKeyboardLayout : UIBehaviour, ILayoutSelfController
    {
        protected override void OnRectTransformDimensionsChange(){base.OnRectTransformDimensionsChange();Fit();}
        protected override void OnEnable(){base.OnEnable();Fit();}
        void Fit()
        {
            var rect=(RectTransform)transform;var parent=rect.parent as RectTransform;if(!parent)return;
            // Swap the available dimensions before rotating, so both layouts stay inside the cell.
            var size=new Vector2(parent.rect.height,parent.rect.width);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
            if(rect.sizeDelta!=size)rect.sizeDelta=size;
            rect.anchoredPosition=Vector2.zero;
            rect.localRotation=Quaternion.Euler(0,0,90);
        }
        public void Refresh()=>Fit();
        public void SetLayoutHorizontal()=>Fit();
        public void SetLayoutVertical()=>Fit();
        protected override void OnTransformParentChanged(){base.OnTransformParentChanged();Fit();}
        void OnTransformChildrenChanged()=>Fit();
    }
}
