using UnityEngine;
using UnityEngine.UI;

namespace Uuvr;

// TODO: make it work with new input system too.
// TODO: render an actual cursor.
// TODO: add setting to toggle this.
// TODO: change cursor visibility based on real cursor.
public class VrUiCursor: MonoBehaviour
{
    public Canvas CursorCanvas { get; private set; }
    private Image _image;

    public static void Create(Transform parent)
    {
        new GameObject(nameof(VrUiCursor))
        {
            transform =
            {
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity,
                parent = parent
            }
        }.AddComponent<VrUiCursor>();
    }
    
    private void Start()
    {
        CursorCanvas = gameObject.AddComponent<Canvas>();
        CursorCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CursorCanvas.sortingOrder = short.MaxValue;
        
        _image = new GameObject("VrUiCursorImage").AddComponent<Image>();
        _image.transform.parent = transform;
        _image.transform.localRotation = Quaternion.identity;
        _image.color = Color.white;
        _image.raycastTarget = false;
        _image.transform.localScale = Vector3.one * 0.2f;
    }

    private void Update()
    {
        if (CursorCanvas == null) return;

        _image.transform.localPosition =  Input.mousePosition - new Vector3(CursorCanvas.pixelRect.center.x, CursorCanvas.pixelRect.center.y, 0);
    }
}