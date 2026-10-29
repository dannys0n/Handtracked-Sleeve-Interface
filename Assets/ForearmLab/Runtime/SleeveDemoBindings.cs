using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    // Example app behavior lives on the demo variant, never on the generic element.
    public class SleeveDemoBindings : MonoBehaviour
    {
        public Slider volume;
        public Toggle passthrough;
        public Button project;
        public TMP_Text status;
        public SleeveGridLayout grid;
        LabEnvironmentMode environment;
        float nextUpdate;
        void OnEnable()
        {
            environment=FindAnyObjectByType<LabEnvironmentMode>();
            if(volume){volume.SetValueWithoutNotify(AudioListener.volume);volume.onValueChanged.AddListener(SetVolume);}
            if(passthrough)passthrough.onValueChanged.AddListener(SetPassthrough);
            if(project)project.onClick.AddListener(ToggleProjection);
        }
        void OnDisable()
        {
            if(volume)volume.onValueChanged.RemoveListener(SetVolume);
            if(passthrough)passthrough.onValueChanged.RemoveListener(SetPassthrough);
            if(project)project.onClick.RemoveListener(ToggleProjection);
        }
        void SetVolume(float value)=>AudioListener.volume=value;
        void ToggleProjection(){if(!grid)return;if(grid.explodeDistance<=0){grid.explodeDistance=.085f;grid.explosion=1;}else grid.ToggleExploded();}
        void SetPassthrough(bool value){if(!environment)return;if(value)environment.UsePassthrough();else environment.UseVR();}
        void Update()
        {
            if(Time.unscaledTime<nextUpdate)return;nextUpdate=Time.unscaledTime+1;
            if(passthrough&&environment)passthrough.SetIsOnWithoutNotify(environment.CurrentMode==LabEnvironmentMode.Mode.Passthrough||environment.IsStartingPassthrough);
            // Link runs on the PC: SystemInfo there is not the headset's battery.
            float battery=Application.platform==RuntimePlatform.Android?SystemInfo.batteryLevel:-1;
            if(status)status.text=System.DateTime.Now.ToString("HH:mm")+"\nPOWER "+(battery<0?"N/A":$"{battery:P0}");
        }
    }
}
