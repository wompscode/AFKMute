using Dalamud.Configuration;
using System;

namespace AFKMute;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool PluginActive { get; set; } = true;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Services.PluginInterface.SavePluginConfig(this);
    }
}
