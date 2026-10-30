using Oculus.Interaction;
using Oculus.Interaction.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ForearmLab
{
    // Disabling input never disables the world-space ink hierarchy.
    public class ShowcaseGestureDrawing : MonoBehaviour
    {
        public GestureDrawing drawing;
        public MonoBehaviour rightHandSource,leftHandSource;
        public SleeveManipulation manipulation;
        public MetaSleeveTouchInput touchInput;
        public TwoHandCameraGesture cameraGesture;
        public Toggle drawToggle;
        public Button recognizeButton,clearButton;
        public TMP_Text statusText;
        public TMP_Text predictionText;
        public bool drawingEnabled;
        [Min(.005f)] public float sleeveExclusionDistance=.04f;
        bool wasPinching,needsRelease=true,wasClearing,wasConfirming,gesturesReady;
        public string Status { get; private set; }="Ink off; drawing retained";
        IHand Hand=>rightHandSource as IHand;

        void OnEnable()
        {
            if(drawToggle){drawToggle.SetIsOnWithoutNotify(drawingEnabled);drawToggle.onValueChanged.AddListener(SetDrawingEnabled);}
            if(recognizeButton)recognizeButton.onClick.AddListener(Recognize);
            if(clearButton)clearButton.onClick.AddListener(Clear);
            needsRelease=true;
            UpdateControls();UpdateStatus();
        }
        public void SetDrawingEnabled(bool value)
        {
            drawingEnabled=value;EndStroke();needsRelease=true;
            gesturesReady=false;
            if(drawToggle)drawToggle.SetIsOnWithoutNotify(value);
            Status=value?"Right index pinch: draw":"Ink off; drawing retained";
            UpdateControls();UpdateStatus();
        }
        public void Recognize(){if(!drawingEnabled)return;EndStroke();needsRelease=true;if(drawing)drawing.Recognize();UpdateStatus();}
        public void Clear(){if(!drawingEnabled)return;EndStroke();needsRelease=true;if(drawing)drawing.Clear();UpdateStatus();}
        void UpdateControls()
        {
            if(recognizeButton)recognizeButton.interactable=drawingEnabled;
            if(clearButton)clearButton.interactable=drawingEnabled;
        }
        bool NearSleeve(Vector3 point)
        {
            if(!manipulation||!manipulation.grid)return false;
            foreach(var surface in manipulation.grid.GetComponentsInChildren<SleevePokeSurface>())
                if(surface.ClosestSurfacePoint(point,out _,sleeveExclusionDistance))return true;
            return false;
        }
        void Update()
        {
            if(!drawing)return;
            var keyboard=Keyboard.current;
            if(drawingEnabled&&keyboard!=null)
            {
                if(keyboard.enterKey.wasPressedThisFrame)Recognize();
                if(keyboard.cKey.wasPressedThisFrame)Clear();
            }
            var hand=Hand;
            bool valid=hand!=null&&hand.IsConnected&&hand.IsTrackedDataValid&&hand.IsHighConfidence;
            bool cameraFraming=cameraGesture&&(cameraGesture.Recognizer.Current==CameraFrameGesture.Phase.Framing||
                cameraGesture.Recognizer.Current==CameraFrameGesture.Phase.Closing);
            bool occupied=(manipulation&&(manipulation.CameraOwnsHands||manipulation.OwnsRightHand))||cameraFraming||
                (touchInput&&touchInput.interactor&&touchInput.interactor.State==InteractorState.Select);
            if(!drawingEnabled||!valid||occupied||!hand.GetJointPose(HandJointId.HandIndexTip,out var tip))
            {EndStroke();needsRelease=true;gesturesReady=false;UpdateStatus();return;}
            if(NearSleeve(tip.position))
            {EndStroke();needsRelease=true;gesturesReady=false;Status="Move the drawing hand away from the sleeve";UpdateStatus();return;}
            bool clearing=hand.GetFingerIsPinching(HandFinger.Middle);
            var left=leftHandSource as IHand;
            bool confirming=left!=null&&left.IsConnected&&left.IsTrackedDataValid&&left.IsHighConfidence&&left.GetIndexFingerIsPinching();
            // Require a release after enabling/tracking loss so an already-held gesture cannot clear ink.
            if(!gesturesReady)
            {
                wasClearing=clearing;wasConfirming=confirming;
                if(!clearing&&!confirming)gesturesReady=true;
                else {EndStroke();UpdateStatus();return;}
            }
            bool clearStarted=clearing&&!wasClearing,confirmStarted=confirming&&!wasConfirming;
            wasClearing=clearing;wasConfirming=confirming;
            if(clearing)
            {
                if(clearStarted)Clear();else EndStroke();
                needsRelease=true;UpdateStatus();return;
            }
            if(confirmStarted){Recognize();return;}
            bool pinch=hand.GetIndexFingerIsPinching();
            if(needsRelease){if(!pinch)needsRelease=false;UpdateStatus();return;}
            if(pinch&&!wasPinching)drawing.BeginStroke(tip.position);
            else if(pinch)drawing.AddPoint(tip.position);
            else if(wasPinching)drawing.EndStroke();
            wasPinching=pinch;
            Status=pinch?"Drawing":"Right index pinch: draw";
            UpdateStatus();
        }
        void UpdateStatus()
        {
            if(statusText)statusText.text=$"INK {(drawingEnabled?"ON":"OFF")} · {drawing.PointCount} PT\n{drawing.Status}";
            if(predictionText&&drawing)predictionText.text=drawing.Status;
        }
        void EndStroke(){if(drawing)drawing.EndStroke();wasPinching=false;}
        void OnDisable()
        {
            EndStroke();needsRelease=true;
            if(drawToggle)drawToggle.onValueChanged.RemoveListener(SetDrawingEnabled);
            if(recognizeButton)recognizeButton.onClick.RemoveListener(Recognize);
            if(clearButton)clearButton.onClick.RemoveListener(Clear);
        }
    }
}
