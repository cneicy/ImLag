using Godot;

namespace ImLag.GUI.Scripts.UI.Head;

public partial class VersionLabel : Label
{
    public override void _Ready()
    {
        Text = (string)ProjectSettings.GetSetting("application/config/version");
    }
}
