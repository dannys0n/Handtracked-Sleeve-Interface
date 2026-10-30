using UnityEngine;
using UnityEngine.UI;

namespace ForearmLab
{
    public class SleeveImageContent : MonoBehaviour
    {
        public RawImage image;
        Texture ownedTexture;
        public void SetTexture(Texture texture)
        {
            if(ownedTexture&&ownedTexture!=texture)Destroy(ownedTexture);
            ownedTexture=null;
            if(image)image.texture=texture;
        }
        public void SetOwnedTexture(Texture texture){SetTexture(texture);ownedTexture=texture;}
        void OnDestroy(){if(ownedTexture)Destroy(ownedTexture);}
    }
}
