namespace Uuvr;

public class VrUiCanvas: UuvrBehaviour
{
#if CPP
    public UuvrBehaviour(System.IntPtr pointer) : base(pointer)
    {
    }
#endif

    private void Start()
    {
        OnSettingChanged();
    }

    protected override void OnSettingChanged()
    {
        LayerHelper.SetLayerRecursive(transform, LayerHelper.GetVrUiLayer());
    }
}
