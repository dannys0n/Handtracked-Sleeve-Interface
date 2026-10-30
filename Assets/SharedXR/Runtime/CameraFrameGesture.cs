using UnityEngine;

namespace ForearmLab
{
    // Temporal recognition is independent of SDK polling, rendering, and UI.
    public sealed class CameraFrameGesture
    {
        public enum Phase { Idle, Framing, Closing, Cooldown }
        public Phase Current {get;private set;}
        public Rect Frame {get;private set;}
        public float holdTime = .22f;
        float candidateSince = -1, cooldownUntil;
        public void Cancel(){Current=Phase.Idle;candidateSince=-1;}
        public bool Step(bool valid, bool bothOpen, bool bothClosed, Rect rectangle, float time)
        {
            if(!valid){Cancel();return false;}
            if(Current==Phase.Cooldown)
            {
                if(time<cooldownUntil || !bothOpen)return false;
                Cancel();
            }
            if(Current==Phase.Idle)
            {
                if(!bothOpen || rectangle.width<.08f || rectangle.height<.08f){candidateSince=-1;return false;}
                if(candidateSince<0)candidateSince=time;
                if(time-candidateSince>=holdTime){Frame=rectangle;Current=Phase.Framing;}
                return false;
            }
            if(bothOpen){if(rectangle.width<.08f||rectangle.height<.08f){Cancel();return false;}Frame=rectangle;Current=Phase.Framing;return false;}
            if(Current==Phase.Framing)Current=Phase.Closing;
            if(!bothClosed)return false;
            Current=Phase.Cooldown;cooldownUntil=time+.8f;return true;
        }
    }
}
