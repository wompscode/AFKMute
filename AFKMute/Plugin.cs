using AFKMute.Windows;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

// This is my first ever plugin, it's far from perfect. I just wanted a solution to an issue that I had.

namespace AFKMute;
public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;

    private const string CommandName = "/afkmute";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("AFKMute");
    private ConfigWindow ConfigWindow { get; init; }


    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<Services>();
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ConfigWindow = new ConfigWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        CommandManager.AddHandler("/afkmute", new CommandInfo(ToggleStateCommand)
        {
            HelpMessage = "Toggles active state of plugin."
        });
        CommandManager.AddHandler("/amt", new CommandInfo(ToggleStateCommand)
        {
            HelpMessage = "Toggles active state of plugin."
        });
        CommandManager.AddHandler("/afkmuteconfig", new CommandInfo(ConfigWindowCommand)
        {
            HelpMessage = "Open config window."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        // This adds a button to the plugin installer entry of this plugin which allows
        // to toggle the display status of the configuration ui
        Services.Framework.Update += this.OnFrameworkTick;
        Services.PluginLog.Info("OnFrameworkTick hooked into Framework.Update");

        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
        Services.PluginLog.Info("Hello world.");
    }

    private uint lastState;

    public void OnFrameworkTick(IFramework framework)
    {
        var player = Services.ClientState.LocalPlayer;
        if (player != null)
        {
            var onsValue = player.OnlineStatus.Value.RowId;
            
            if (onsValue == lastState) return;
            lastState = onsValue;

            Services.PluginLog.Debug("Player state changed: {onsValue}: {ons}", onsValue, player.OnlineStatus.Value.Name.ToString());
            if(onsValue == 17)
            {
                Services.PluginLog.Information("Player is AFK.", onsValue, player.OnlineStatus.Value.Name.ToString());
                if (Configuration.PluginActive)
                {
                    Services.PluginLog.Information("Muting master volume.");
                    Services.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, 1);
                }
            } else
            {
                Services.PluginLog.Information("Player is not AFK.", onsValue, player.OnlineStatus.Value.Name.ToString());
                if (Configuration.PluginActive)
                {
                    Services.PluginLog.Information("Unmuting master volume.");
                    Services.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndMaster, 0);
                }
            }
        }
    }

    public void Dispose()
    {
        Services.Framework.Update -= this.OnFrameworkTick;
        Services.PluginLog.Info("OnFrameworkTick unhooked from Framework.Update");

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        CommandManager.RemoveHandler("/afkmute");
        CommandManager.RemoveHandler("/amt");
        CommandManager.RemoveHandler("/afkmuteconfig");
        Services.PluginLog.Info("Goodbye world.");
    }
   
    private void ToggleStateCommand(string command, string args)
    {
        Configuration.PluginActive = !Configuration.PluginActive;
        Configuration.Save();

        Services.PluginLog.Info("AFKMute state: {state}.", Configuration.PluginActive);
        Services.ChatGui.Print($"Toggled AFKMute {(Configuration.PluginActive ? "on" : "off")}.");
    }

    private void ConfigWindowCommand(string command, string args)
    {
        ToggleConfigUI();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
