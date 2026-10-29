using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace ForearmLab
{
    [ExecuteAlways,DefaultExecutionOrder(1200)]
    public class SleeveKeyboard : MonoBehaviour
    {
        public TMP_Text output;
        public RectTransform compactKeys;
        public RectTransform expandedKeys;
        public UnityEvent<string> onTextChanged=new UnityEvent<string>();
        public UnityEvent<string> onCommand=new UnityEvent<string>();
        [HideInInspector] public int layoutVersion;
        public string Text {get;private set;}="";
        public int Caret {get;private set;}
        public bool Shift {get;private set;}
        public bool CapsLock {get;private set;}
        bool overwrite;
        SleeveGridLayout grid;
        SleeveElement element;
        void OnEnable(){element=GetComponent<SleeveElement>();RefreshLayout();}
        void OnDisable(){if(grid)grid.FocusChanged-=FocusChanged;grid=null;}
        void LateUpdate()=>RefreshLayout();
        void FocusChanged(SleeveElement focused)=>RefreshLayout();
        public void RefreshLayout()
        {
            var owner=GetComponentInParent<SleeveGridLayout>();
            if(owner!=grid){if(grid)grid.FocusChanged-=FocusChanged;grid=owner;if(grid)grid.FocusChanged+=FocusChanged;}
            if(!element)element=GetComponent<SleeveElement>();
            // Grid layout resizes the canvas first; fit the rotated viewport to its new bounds.
            if(compactKeys&&compactKeys.parent.TryGetComponent<SleeveKeyboardLayout>(out var layout))layout.Refresh();
            bool expanded=grid&&grid.FocusedElement==element;
            if(compactKeys&&compactKeys.gameObject.activeSelf==expanded)compactKeys.gameObject.SetActive(!expanded);
            if(expandedKeys&&expandedKeys.gameObject.activeSelf!=expanded)expandedKeys.gameObject.SetActive(expanded);
        }
        public void Key(string value)
        {
            switch(value.ToUpperInvariant())
            {
                case "SHIFT":Shift=!Shift;return;
                case "CAPS":case "CAPSLOCK":CapsLock=!CapsLock;return;
                case "BACKSPACE":case "DEL":Backspace();return;
                case "DELETE":if(Caret<Text.Length)SetText(Text.Remove(Caret,1),Caret);return;
                case "CLEAR":Clear();return;
                case "LEFT":MoveCaret(Caret-1);return;
                case "RIGHT":MoveCaret(Caret+1);return;
                case "HOME":MoveCaret(Caret==0?0:Text.LastIndexOf('\n',Caret-1)+1);return;
                case "END":int end=Text.IndexOf('\n',Caret);MoveCaret(end<0?Text.Length:end);return;
                case "UP":MoveLine(-1);return;
                case "DOWN":MoveLine(1);return;
                case "INSERT":overwrite=!overwrite;onCommand.Invoke(value);return;
                case "SPACE":Insert(" ");return;
                case "ENTER":Insert("\n");return;
                case "TAB":Insert("\t");return;
                case "ESC":Shift=false;onCommand.Invoke(value);return;
            }
            if(value.Length!=1){onCommand.Invoke(value);return;}
            char c=value[0];
            if(char.IsLetter(c))c=Shift^CapsLock?char.ToUpperInvariant(c):char.ToLowerInvariant(c);
            else if(Shift)
            {
                const string plain="`1234567890-=[]\\;',./";
                const string shifted="~!@#$%^&*()_+{}|:\"<>?";
                int index=plain.IndexOf(c);if(index>=0)c=shifted[index];
            }
            Insert(c.ToString());Shift=false;
        }
        void Insert(string value)
        {
            if(Text.Length+value.Length>2048)return;
            string text=overwrite&&Caret<Text.Length?Text.Remove(Caret,1):Text;
            SetText(text.Insert(Caret,value),Caret+value.Length);
        }
        public void Backspace(){if(Caret>0)SetText(Text.Remove(Caret-1,1),Caret-1);}
        public void Clear()=>SetText("",0);
        void MoveCaret(int index){Caret=Mathf.Clamp(index,0,Text.Length);Render();}
        void MoveLine(int direction)
        {
            int start=Caret==0?0:Text.LastIndexOf('\n',Caret-1)+1;int column=Caret-start;
            if(direction<0){if(start==0)return;int previous=start<2?0:Text.LastIndexOf('\n',start-2)+1;MoveCaret(Mathf.Min(previous+column,start-1));}
            else{int end=Text.IndexOf('\n',Caret);if(end<0)return;int nextEnd=Text.IndexOf('\n',end+1);MoveCaret(Mathf.Min(end+1+column,nextEnd<0?Text.Length:nextEnd));}
        }
        void Render(){if(output)output.text=string.IsNullOrEmpty(Text)?"TYPE HERE":Text.Insert(Caret,"|");}
        void SetText(string value,int caret){Text=value;Caret=caret;Render();onTextChanged.Invoke(value);}
    }
}
