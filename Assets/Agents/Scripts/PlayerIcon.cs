using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] private Sprite icon;
    [SerializeField] private float iconSize = 12f;
    [SerializeField] private Color iconColor;

    private SpriteRenderer iconInstance;
    private Camera cam;
    
    void Awake()
    {
        iconInstance = new GameObject("Icon").AddComponent<SpriteRenderer>();
        iconInstance.sprite = icon;
        iconInstance.color = iconColor;
        iconInstance.transform.SetParent(transform, false);
        iconInstance.gameObject.layer = LayerMask.NameToLayer("Icons");
    }
    
    void Update()
    {
        float viewSize = 0;
        if (!cam)
        {
            foreach (Camera c in Camera.allCameras)
            {
                if (c.cullingMask == 1 << LayerMask.NameToLayer("Icons"))
                {
                    cam = c;
                }
            }
        }
        if (cam && cam.orthographic)
        {
            viewSize = iconSize * (cam.orthographicSize * 2f) / cam.scaledPixelHeight;
            iconInstance.transform.localScale = Vector3.one.DivideBy(iconInstance.transform.parent.lossyScale) * viewSize;
            
            iconInstance.transform.forward = -cam.transform.forward;
            iconInstance.transform.rotation = Quaternion.LookRotation(-cam.transform.forward, iconInstance.transform.parent.forward);
        }
    }
}
