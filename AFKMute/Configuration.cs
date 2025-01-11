using Dalamud.Configuration;
using System;

namespace AFKMute;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool PluginActive { get; set; } = true;
    
    public bool LastToggleLock { get; set; } = false;
    public uint LastKnownState { get; set; } = 0;
    // the below exist just to make saving less cumbersome
    public void Save()
    {
        Services.PluginInterface.SavePluginConfig(this);
    }
}
