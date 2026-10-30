using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForearmLab
{
    // Presentation helpers for the showcase; all sleeve/hand interactions use the lab adapters.
    public class ShowcaseCapture : MonoBehaviour
    {
        public Camera view;
        public Transform captureStage;
        public string LastSnapshotPath { get; private set; }
        public string Status { get; private set; } = "F8: snapshot | R: recenter capture objects | P: VR/passthrough";
        bool capturing, ready;

        IEnumerator Start()
        {
            float deadline=Time.unscaledTime+10;
            while(!OVRPlugin.initialized&&Time.unscaledTime<deadline)yield return null;
            yield return null;yield return null;yield return null;
            RecenterStage();
            ready=true;
        }
        void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard==null)return;
            if(keyboard.f8Key.wasPressedThisFrame)CaptureSnapshot();
            if(keyboard.rKey.wasPressedThisFrame)RecenterStage();
        }
        [ContextMenu("Recenter capture objects")]
        public void RecenterStage()
        {
            if(!view||!captureStage)return;
            var forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up);
            if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            var rotation=Quaternion.LookRotation(forward.normalized,Vector3.up);
            captureStage.SetPositionAndRotation(view.transform.position+rotation*new Vector3(0,-.12f,1.2f),rotation);
        }
        [ContextMenu("Capture showcase snapshot")]
        public void CaptureSnapshot()
        {
            if(!Application.isPlaying||capturing)return;
            StartCoroutine(SaveSnapshot());
        }
        IEnumerator SaveSnapshot()
        {
            capturing=true;
            while(!ready)yield return null;
#if UNITY_EDITOR
            string folder=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Artifacts","ShowcaseCaptures");
#else
            string folder=Path.Combine(Application.persistentDataPath,"ShowcaseCaptures");
#endif
            Directory.CreateDirectory(folder);
            LastSnapshotPath=Path.Combine(folder,"showcase-"+System.DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(LastSnapshotPath);
            Status="Saving snapshot…";
            float deadline=Time.unscaledTime+10;
            while(!File.Exists(LastSnapshotPath)&&Time.unscaledTime<deadline)yield return null;
            Status=File.Exists(LastSnapshotPath)?"Snapshot saved: "+LastSnapshotPath:"Snapshot did not finish saving";
            Debug.Log(Status,this);capturing=false;
        }
    }
}
