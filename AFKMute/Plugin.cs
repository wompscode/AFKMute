using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

// This is my first plugin, it's not perfect. I just wanted a solution to an issue that I had.

namespace AFKMute;
public sealed class Plugin : IDalamudPlugin
{
    public Configuration Configuration { get; init; }

    private uint lastOnlineState;
    private bool toggleLock; 
    
    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        Configuration = Services.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Services.CommandManager.AddHandler("/afkmute", new CommandInfo(ToggleStateCommand)
        {
            HelpMessage = "Toggles active state of plugin."
        });
        Services.CommandManager.AddHandler("/am", new CommandInfo(ToggleStateCommand)
        {
            HelpMessage = "Toggles active state of plugin."
        });

        lastOnlineState = Configuration.LastKnownState;
        
        Services.Framework.Update += this.OnFrameworkTick;
        Services.PluginLog.Info("OnFrameworkTick hooked into Framework.Update");
    }

    public void OnFrameworkTick(IFramework framework)
    {
        var localPlayer = Services.ClientState.LocalPlayer; // Grab the current LocalPlayer
        if (localPlayer != null) // If it exists, we can go ahead:
        {
            var onlineState = localPlayer.OnlineStatus.Value.RowId; // Get the current OnlineState as a Lumina RowId
            Services.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, out uint sndMasterMuted); // Get the current state of the Master Volume mute

            if (onlineState == lastOnlineState) return; // Don't do anything if the state hasn't changed.
            if (lastOnlineState != 17)                  // If _lastOnlineState wasn't AFK last, then go ahead:
            {
                toggleLock = sndMasterMuted == 1; // If it was already muted before we go AFK, lock all mute/unmute operations. Don't tamper with the player's master volume.
                Configuration.LastToggleLock = toggleLock; // Save toggleLock to LastToggleLock
            }
            Services.PluginLog.Debug("LastKnownState: {ConfigurationLastKnownState}, lastOnlineState: {lastOnlineState} onlineState: {onlineState}", Configuration.LastKnownState, lastOnlineState, onlineState);
            Services.PluginLog.Debug("Player state updated from {lastOnlineState} to {onlineState}: {localised}",lastOnlineState, onlineState, localPlayer.OnlineStatus.Value.Name.ToString());
            lastOnlineState = onlineState; // Set _lastOnlineState to whatever onlineState now is.
            Configuration.LastKnownState = onlineState; // Save onlineState to LastKnownState
            Configuration.Save(); // Save this.
            
            if(lastOnlineState == 17) // If AFK, go ahead:
            {
                Services.PluginLog.Information("Player is AFK.");
                if (Configuration.PluginActive && toggleLock == false && sndMasterMuted == 0) // If the plugin is actually active and _toggleLock isn't true, go ahead:
                {
                    Services.PluginLog.Information("Muting master volume.");
                    Services.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, 1); // Mute
                }
            } else
            {
                Services.PluginLog.Information("Player is no longer AFK.");
                if (Configuration.PluginActive && toggleLock == false && sndMasterMuted == 1) // If the plugin is actually active and _toggleLock isn't true, go ahead:
                {
                    Services.PluginLog.Information("Unmuting master volume.");
                    Services.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, 0); // Unmute
                }
            }
        }
        else // If there is no active localPlayer
        {
            Services.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, out uint sndMasterMuted); // Get the current state of the Master Volume mute

            if (Configuration.LastToggleLock == false && sndMasterMuted == 1) // If last known time game was running, the plugin did not have toggle lock on, and the volume is muted:
            {
                if (Configuration.LastKnownState == 17) // If the last state was AFK, the game was likely muted.
                {
                    Services.PluginLog.Information("Unmuting master volume.");
                    Services.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, 0); // Unmute
                    Configuration.LastKnownState = 0; // Set LastKnownState to 0, as now it doesn't really matter.
                    Configuration.Save();
                }
            }
        }
    }

    public void Dispose()
    {
        Services.Framework.Update -= this.OnFrameworkTick;
        Services.PluginLog.Info("OnFrameworkTick unhooked from Framework.Update");

        Services.CommandManager.RemoveHandler("/afkmute");
        Services.CommandManager.RemoveHandler("/am");
    }
   
    private void ToggleStateCommand(string command, string args)
    {
        Configuration.PluginActive = !Configuration.PluginActive;
        Configuration.Save();

        Services.PluginLog.Info("AFKMute state: {state}.", Configuration.PluginActive);
        Services.ChatGui.Print($"Toggled AFKMute {(Configuration.PluginActive ? "on" : "off")}.");
    }

}
