using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    [ExecuteAlways,RequireComponent(typeof(SleeveElement))]
    public class SleeveElementFocus : MonoBehaviour
    {
        public Button button;
        public TMP_Text icon;
        SleeveElement element;
        void OnEnable(){element=GetComponent<SleeveElement>();if(button)button.onClick.AddListener(Toggle);LateUpdate();}
        void OnDisable(){if(button)button.onClick.RemoveListener(Toggle);}
        public void Toggle(){var grid=GetComponentInParent<SleeveGridLayout>();if(grid)grid.ToggleFocus(GetComponent<SleeveElement>());}
        void LateUpdate()
        {
            if(!element)element=GetComponent<SleeveElement>();
            bool visible=element&&!element.isEmptyPlaceholder;
            if(button&&button.gameObject.activeSelf!=visible)button.gameObject.SetActive(visible);
            var grid=GetComponentInParent<SleeveGridLayout>();string value=grid&&grid.FocusedElement==element?"[-]":"[+]";
            if(icon&&icon.text!=value)icon.text=value;
        }
    }
}
