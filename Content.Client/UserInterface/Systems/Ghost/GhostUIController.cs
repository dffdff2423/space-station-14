using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.Ghost;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

// AXOLOTL: imports for ghostrespawn
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed class GhostUIController : UIController, IOnSystemChanged<GhostSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    [UISystemDependency] private readonly GhostSystem? _system = default;

    private GhostGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostGui>();

    // AXOLOTL: Systems required for ghostrespawn
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    private void OnScreenLoad()
    {
        LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void OnSystemLoaded(GhostSystem system)
    {
        system.PlayerRemoved += OnPlayerRemoved;
        system.PlayerUpdated += OnPlayerUpdated;
        system.PlayerAttached += OnPlayerAttached;
        system.PlayerDetached += OnPlayerDetached;
        system.GhostWarpsResponse += OnWarpsResponse;
        system.GhostRoleCountUpdated += OnRoleCountUpdated;
    }

    public void OnSystemUnloaded(GhostSystem system)
    {
        system.PlayerRemoved -= OnPlayerRemoved;
        system.PlayerUpdated -= OnPlayerUpdated;
        system.PlayerAttached -= OnPlayerAttached;
        system.PlayerDetached -= OnPlayerDetached;
        system.GhostWarpsResponse -= OnWarpsResponse;
        system.GhostRoleCountUpdated -= OnRoleCountUpdated;
    }

    public void UpdateGui()
    {
        if (Gui == null)
        {
            return;
        }

        Gui.Visible = _system?.IsGhost ?? false;
        // AXOLOTL: Send death and respawn time information to client for ghostrespawn
        Gui.Update(_system?.AvailableGhostRoleCount, _system?.Player?.CanReturnToBody,
                _system?.Player?.TimeOfDeath, _cfg.GetCVar(CCVars.RespawnTime));
    }

    private void OnPlayerRemoved(GhostComponent component)
    {
        Gui?.Hide();
    }

    private void OnPlayerUpdated(GhostComponent component)
    {
        AxolotlUpdateRespawn(component.TimeOfDeath);
        UpdateGui();
    }

    private void OnPlayerAttached(GhostComponent component)
    {
        if (Gui == null)
            return;

        Gui.Visible = true;
        AxolotlUpdateRespawn(component.TimeOfDeath);
        UpdateGui();
    }

    private void OnPlayerDetached()
    {
        Gui?.Hide();
    }

    private void OnWarpsResponse(GhostWarpsResponseEvent msg)
    {
        if (Gui?.TargetWindow is not { } window)
            return;

        window.UpdateWarps(msg.Warps);
        window.Populate();
    }

    private void OnRoleCountUpdated(GhostUpdateGhostRoleCountEvent msg)
    {
        UpdateGui();
    }

    private void OnWarpClicked(NetEntity player)
    {
        var msg = new GhostWarpToTargetRequestEvent(player);
        _net.SendSystemNetworkMessage(msg);
    }

    private void OnGhostnadoClicked()
    {
        var msg = new GhostnadoRequestEvent();
        _net.SendSystemNetworkMessage(msg);
    }

    public void LoadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed += RequestWarps;
        Gui.ReturnToBodyPressed += ReturnToBody;
        Gui.GhostRolesPressed += GhostRolesPressed;
        Gui.TargetWindow.WarpClicked += OnWarpClicked;
        Gui.TargetWindow.OnGhostnadoClicked += OnGhostnadoClicked;
        // AXOLOTL: ghostrespawn button
        Gui.AxolotlGhostRespawnPressed += AxolotlGuiOnGhostRespawnPressed;

        UpdateGui();
    }

    public void UnloadGui()
    {
        if (Gui == null)
            return;

        Gui.RequestWarpsPressed -= RequestWarps;
        Gui.ReturnToBodyPressed -= ReturnToBody;
        Gui.GhostRolesPressed -= GhostRolesPressed;
        Gui.TargetWindow.WarpClicked -= OnWarpClicked;

        Gui.Hide();
    }

    private void ReturnToBody()
    {
        _system?.ReturnToBody();
    }

    private void RequestWarps()
    {
        _system?.RequestWarps();
        Gui?.TargetWindow.Populate();
        Gui?.TargetWindow.OpenCentered();
    }

    private void GhostRolesPressed()
    {
        _system?.OpenGhostRoles();
    }

    private void AxolotlUpdateRespawn(TimeSpan? timeOfDeath)
    {
        Gui?.AxolotlUpdateRespawn(timeOfDeath);
    }

    private void AxolotlGuiOnGhostRespawnPressed()
    {
        _consoleHost.ExecuteCommand("ghostrespawn");
    }
}
