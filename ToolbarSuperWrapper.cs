using System;
using UnityEngine;

using AppScenes = KSP.UI.Screens.ApplicationLauncher.AppScenes;


namespace DaMichelToolbarSuperWrapper
{

public class ToolbarInfo
{
    public String name;
    public String tooltip;
    public String toolbarTexture;
    public String launcherTexture;
    public GameScenes[] visibleInScenes;
};


public abstract class PluginWithToolbarSupport : UnityEngine.MonoBehaviour
{
    private bool visibleByToolbars = true;
    private bool visibleByKspGui = true;
    private bool useToolbar = true;
    private bool useAppLauncher = true;

    private IButton toolbarButton = null;
    private KSP.UI.Screens.ApplicationLauncherButton applauncherButton = null;

    protected abstract ToolbarInfo GetToolbarInfo();

    protected bool isGuiVisible
    {
        get { return visibleByKspGui && visibleByToolbars; }
    }

    protected abstract void OnGuiVisibilityChange();

    protected void SaveImmutableToolbarSettings(ConfigNode node)
    {
        node.AddValue("useToolbar", useToolbar);
        node.AddValue("useAppLauncher", useAppLauncher);
    }

    protected void SaveMutableToolbarSettings(ConfigNode node)
    {
        node.AddValue("active", visibleByToolbars);
    }

    protected void LoadImmutableToolbarSettings(ConfigNode node)
    {
        node.TryGetValue("useToolbar", ref useToolbar);
        node.TryGetValue("useAppLauncher", ref useAppLauncher);
    }

    protected void LoadMutableToolbarSettings(ConfigNode node)
    {
        node.TryGetValue("active", ref visibleByToolbars);
    }

    private void OnHideByToolbar()
    {
        visibleByToolbars = false;
        OnGuiVisibilityChange();
    }

    private void OnShowByToolbar()
    {
        visibleByToolbars = true;
        OnGuiVisibilityChange();
    }

    private void OnHideByKspGui()
    {
        visibleByKspGui = false;
        OnGuiVisibilityChange();
    }

    private void OnShowByKspGui()
    {
        visibleByKspGui = true;
        OnGuiVisibilityChange();
    }

    protected void InitializeToolbars()
    {   
        if (ToolbarManager.ToolbarAvailable && useToolbar && toolbarButton == null)
        {
            ToolbarInfo tb = GetToolbarInfo();
            toolbarButton = ToolbarManager.Instance.add(tb.name, tb.name);
            toolbarButton.TexturePath = tb.toolbarTexture;
            toolbarButton.ToolTip = tb.tooltip;
            toolbarButton.Visibility = new GameScenesVisibility(tb.visibleInScenes);
            toolbarButton.Enabled = true;
            toolbarButton.OnClick += (e) =>
            {
                visibleByToolbars = !visibleByToolbars;
                OnGuiVisibilityChange();
            };
        }

        GameEvents.onHideUI.Add(OnHideByKspGui);
        GameEvents.onShowUI.Add(OnShowByKspGui);
        if (useAppLauncher)
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
    }

    private void OnGUIAppLauncherReady()
    {

        if (applauncherButton == null)
        {
            ToolbarInfo tb = GetToolbarInfo();
            var m = new System.Collections.Generic.Dictionary<GameScenes, AppScenes>();
            m.Add(GameScenes.FLIGHT, AppScenes.FLIGHT);
            m.Add(GameScenes.EDITOR, AppScenes.SPH | AppScenes.VAB);
            m.Add(GameScenes.SPACECENTER, AppScenes.SPACECENTER);
            // and so on ...
            AppScenes v = AppScenes.NEVER;
            foreach (GameScenes s in tb.visibleInScenes)
            {
                v |= m[s];
            }

            applauncherButton = KSP.UI.Screens.ApplicationLauncher.Instance.AddModApplication(
                OnShowByToolbar,
                OnHideByToolbar,
                null,
                null,
                null,
                null,
                v,
                (Texture)GameDatabase.Instance.GetTexture(tb.launcherTexture, false));
            if (visibleByToolbars)
                applauncherButton.SetTrue(false);
        }
    }

    protected void TearDownToolbars()
    {
        // unregister, or else errors occur
        GameEvents.onHideUI.Remove(OnHideByKspGui);
        GameEvents.onShowUI.Remove(OnShowByKspGui);
        GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);

        if (applauncherButton != null)
        {
            KSP.UI.Screens.ApplicationLauncher.Instance.RemoveModApplication(applauncherButton);
            applauncherButton = null;
        }
        if (toolbarButton != null)
        {
            toolbarButton.Destroy();
            toolbarButton = null;
        }
    }
};


}