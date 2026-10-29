using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    public class SleeveImageContent : MonoBehaviour
    {
        public RawImage image;
        public void SetTexture(Texture texture){if(image)image.texture=texture;}
    }
}
