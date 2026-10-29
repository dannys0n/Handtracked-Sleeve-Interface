using UnityEngine;
using UnityEngine.InputSystem;

namespace ForearmLab
{
    // Lab controls are outside the reusable content and sleeve prefabs.
    public class SleeveDemoControls : MonoBehaviour
    {
        public SleeveGridLayout grid;
        Texture2D dialTexture;
        float previousDialAngle;
        int activeDialControl;

        void OnDisable()
        {
            if(activeDialControl!=0&&GUIUtility.hotControl==activeDialControl)GUIUtility.hotControl=0;
            activeDialControl=0;
        }
        void OnDestroy() { if(dialTexture)Destroy(dialTexture); }
        void Update()
        {
            if(!grid)return;
            if((Keyboard.current!=null&&Keyboard.current.f6Key.wasPressedThisFrame)||
                (OVRPlugin.initialized&&OVRInput.GetDown(OVRInput.Button.Two,OVRInput.Controller.RTouch)))grid.ToggleExploded();
        }
        void OnGUI()
        {
            if(!grid||!grid.sleeve)return;
            GUILayout.BeginArea(new Rect(12,Screen.height-302,430,290),GUI.skin.box);
            GUILayout.Label("SLEEVE CONTENT  |  F6 / right controller B");
            GUILayout.Label($"Projection distance: {grid.explodeDistance*100:F1} cm");
            grid.explodeDistance=GUILayout.HorizontalSlider(grid.explodeDistance,0,.2f);
            if(GUILayout.Button(grid.explosion>.5f?"Near surface":"Project outward"))grid.ToggleExploded();
            grid.showDebugLines=GUILayout.Toggle(grid.showDebugLines,"Show debug projection lines");
            grid.sleeve.showDebugCylinder=GUILayout.Toggle(grid.sleeve.showDebugCylinder,"Show debug cylinder");
            GUILayout.Label("Close / reopen (diamond fold)");grid.sleeve.closeAmount=GUILayout.HorizontalSlider(grid.sleeve.closeAmount,0,1);
            GUILayout.Label($"Collapse threshold: {grid.sleeve.collapseThreshold:P0} folded");
            grid.sleeve.collapseThreshold=GUILayout.HorizontalSlider(grid.sleeve.collapseThreshold,0,.99f);
            GUILayout.Label($"Radius: {grid.sleeve.radius*100:F1} cm / Length: {grid.sleeve.length*100:F1} cm");
            grid.sleeve.radius=GUILayout.HorizontalSlider(grid.sleeve.radius,.025f,.075f);
            grid.sleeve.length=GUILayout.HorizontalSlider(grid.sleeve.length,.12f,.35f);
            GUILayout.EndArea();
            DrawScrollDial();
        }

        void DrawScrollDial()
        {
            var box=new Rect(454,Screen.height-238,180,226);
            GUI.Box(box,"SCROLL AROUND SLEEVE");
            var dial=new Rect(box.x+22,box.y+31,136,136);
            int control=GUIUtility.GetControlID("SleeveScrollDial".GetHashCode(),FocusType.Passive,dial);
            var e=Event.current;
            var delta=e.mousePosition-dial.center;
            bool inside=delta.sqrMagnitude<=dial.width*dial.width*.25f;
            float angle=Mathf.Atan2(delta.x,-delta.y)*Mathf.Rad2Deg;
            switch(e.GetTypeForControl(control))
            {
                case EventType.MouseDown:
                    if(e.button==0&&inside)
                    {
                        GUIUtility.hotControl=control;activeDialControl=control;
                        previousDialAngle=angle;e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if(GUIUtility.hotControl==control)
                    {
                        // Ignore the ambiguous direction very close to the dial centre.
                        if(delta.sqrMagnitude>100)
                        {
                            grid.ScrollByDegrees(Mathf.DeltaAngle(previousDialAngle,angle));
                            previousDialAngle=angle;
                        }
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if(GUIUtility.hotControl==control&&e.button==0)
                    {GUIUtility.hotControl=0;activeDialControl=0;e.Use();}
                    break;
                case EventType.ScrollWheel:
                    if(inside)
                    {grid.ScrollByDegrees(-e.delta.y*10);e.Use();}
                    break;
            }
            if(e.type==EventType.Repaint)
            {
                if(!dialTexture)CreateDialTexture();
                GUI.DrawTexture(dial,dialTexture);
                var color=GUI.color;
                for(int i=0;i<12;i++)
                {
                    float a=i*30*Mathf.Deg2Rad;
                    var direction=new Vector2(Mathf.Sin(a),-Mathf.Cos(a));
                    GUI.color=new Color(.35f,.55f,.65f);
                    DialLine(dial.center+direction*53,dial.center+direction*61,2);
                }
                float radians=grid.scrollAngle*Mathf.Deg2Rad;
                var pointer=new Vector2(Mathf.Sin(radians),-Mathf.Cos(radians));
                GUI.color=new Color(.3f,1,1);
                DialLine(dial.center+pointer*35,dial.center+pointer*61,4);
                GUI.color=color;
            }
            var style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter};
            GUI.Label(new Rect(dial.x,dial.center.y-18,dial.width,36),$"{Mathf.Repeat(grid.scrollAngle,360):F0}°",style);
            GUI.Label(new Rect(box.x+4,box.y+170,box.width-8,21),"Drag dial / mouse wheel",style);
            if(GUI.Button(new Rect(box.x+27,box.y+197,126,23),"Reset scroll"))grid.ResetScroll();
        }

        static void DialLine(Vector2 start,Vector2 end,float width)
        {
            var matrix=GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
            GUI.DrawTexture(new Rect(start.x,start.y-width*.5f,Vector2.Distance(start,end),width),Texture2D.whiteTexture);
            GUI.matrix=matrix;
        }

        void CreateDialTexture()
        {
            const int size=128;
            dialTexture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Sleeve debug dial",hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float radius=Vector2.Distance(new Vector2(x+.5f,y+.5f),Vector2.one*(size*.5f));
                float alpha=Mathf.Clamp01(63-radius);
                pixels[y*size+x]=radius>48?new Color(.12f,.3f,.38f,alpha):new Color(.025f,.07f,.1f,alpha);
            }
            dialTexture.SetPixels(pixels);dialTexture.Apply(false,true);
        }
    }
}
