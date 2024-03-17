using BepInEx.Configuration;

namespace Uuvr.VrTogglers;

public abstract class VrToggler
{
    public bool IsVrEnabled { get; private set; }

    private bool _isSetUp;

    protected abstract bool SetUp();
    protected abstract bool EnableVr();
    protected abstract bool DisableVr();

    public void SetVrEnabled(bool nextVrEnabled)
    {
        if (!_isSetUp)
        {
            _isSetUp = SetUp();
        }

        if (nextVrEnabled)
        {
            IsVrEnabled = EnableVr();
        }
        else if (DisableVr())
        {
            IsVrEnabled = false;
        }
    }
}
