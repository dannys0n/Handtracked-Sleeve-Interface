using TMPro;
using UnityEngine;

namespace ForearmLab
{
    // Warp fresh TMP glyph vertices onto the same cylindrical map as the backing tile.
    // Subscribe before mesh upload so redraws never accumulate deformation.
    [ExecuteAlways, RequireComponent(typeof(TextMeshProUGUI))]
    public class SleeveCurvedText : MonoBehaviour, UnityEngine.UI.IMaterialModifier
    {
        TMP_Text text;
        WristSleeve sleeve;
        Vector4 region, previousShape;
        float lift, shift;

        void OnEnable()
        {
            text=GetComponent<TMP_Text>();
            text.OnPreRenderText+=Curve;
            text.SetVerticesDirty();text.SetMaterialDirty();
        }
        void OnDisable()
        {
            if(text){text.OnPreRenderText-=Curve;text.SetVerticesDirty();text.SetMaterialDirty();}
        }

        public void Configure(WristSleeve source,Vector4 bounds,float offset,float axialShift)
        {
            var shape=new Vector4(source.radius,source.EffectiveLength,offset,axialShift);
            if(sleeve==source&&region==bounds&&previousShape==shape)return;
            sleeve=source;region=bounds;lift=offset;shift=axialShift;previousShape=shape;
            if(!text)text=GetComponent<TMP_Text>();
            text.SetVerticesDirty();text.SetMaterialDirty();
        }

        public Material GetModifiedMaterial(Material baseMaterial) =>
            sleeve&&isActiveAndEnabled?sleeve.ProjectionMaterial(baseMaterial):baseMaterial;

        void Curve(TMP_TextInfo info)
        {
            if(!sleeve||!isActiveAndEnabled)return;
            float middleU=(region.x+region.z)*.5f,middleV=(region.y+region.w)*.5f;
            float r=Mathf.Max(.005f,sleeve.radius)+lift;
            var center=text.rectTransform.rect.center;
            for(int i=0;i<info.characterCount;i++)
            {
                var character=info.characterInfo[i];if(!character.isVisible)continue;
                var vertices=info.meshInfo[character.materialReferenceIndex].vertices;
                for(int j=0;j<4;j++)
                {
                    int index=character.vertexIndex+j;var point=vertices[index];
                    // The prefab canvas convention is 1000 UI units per metre.
                    // Map from the label's actual rect centre into the tile's angular/axial space.
                    // Rect dimensions are measured at the projected radius, matching the backing.
                    float u=middleU+(point.x-center.x)*.001f/(Mathf.PI*2*r);
                    float v=middleV-(point.y-center.y)*.001f/Mathf.Max(.001f,sleeve.EffectiveLength);
                    var position=sleeve.SampleSurface(u,v,lift+.0008f).position+Vector3.forward*shift;
                    vertices[index]=transform.InverseTransformPoint(sleeve.transform.TransformPoint(position));
                }
            }
        }
    }
}
