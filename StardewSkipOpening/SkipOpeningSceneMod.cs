using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.SDKs;

namespace StardewSkipOpening;

public class StardewSkipOpeningMod : Mod
{
    private Stage CurrentStage = Stage.None;
    private bool IsLaunched = false;
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
    }

    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        try
        {
            // wait until game window opens
            if (Game1.ticks <= 1)
                return;

            // start intro skip on game launch
            if (!this.IsLaunched)
            {
                if (Game1.activeClickableMenu is not TitleMenu)
                    return;

                this.IsLaunched = true;
                this.CurrentStage = Stage.SkipIntro;
            }

            // apply skip logic
            if (this.CurrentStage != Stage.None)
            {
                this.CurrentStage = Game1.activeClickableMenu is TitleMenu menu
                    ? this.Skip(menu, this.CurrentStage)
                    : Stage.None;
            }
        }
        catch (Exception ex)
        {
        }
    }
    /// <summary>Skip the intro if the game is ready.</summary>
    /// <param name="menu">The title menu whose intro to skip.</param>
    /// <param name="currentStage">The current step in the mod logic.</param>
    /// <returns>Returns the next step in the skip logic.</returns>
    private Stage Skip(TitleMenu menu, Stage currentStage)
    {
        // wait until the game is ready
        if (Game1.currentGameTime == null)
            return currentStage;

        // do nothing if a confirmation box is on-screen (e.g. multiplayer disconnect error)
        if (TitleMenu.subMenu is ConfirmationDialog)
            return Stage.None;

        // apply skip step
        return currentStage switch
        {
            Stage.SkipIntro => this.SkipToTitle(menu),
            Stage.StartTransitionToCoop => this.StartTransitionToCoop(menu),
            Stage.TransitionToCoop => this.TransitionToCoop(menu),
            Stage.TransitionToCoopHost => this.TransitionToCoopHost(),
            _ => Stage.None
        };
    }
    /// <summary>Skip to the title screen.</summary>
    /// <param name="menu">The title menu.</param>
    /// <returns>Returns the next step in the skip logic.</returns>
    private Stage SkipToTitle(TitleMenu menu)
    {
        // skip to title screen
        menu.skipToTitleButtons();

        // avoid game crash since Game1.currentSong isn't set yet
        Game1.currentSong ??= Game1.soundBank.GetCue("MainTheme");

        return Stage.StartTransitionToCoop;
    }


    /// <summary>Start transitioning from the title screen to the co-op section.</summary>
    /// <param name="menu">The title menu.</param>
    /// <returns>Returns the next step in the skip logic.</returns>
    private Stage StartTransitionToCoop(TitleMenu menu)
    {
        // wait until the game client SDK is ready, which is needed to load the co-op menus
        SDKHelper sdk = this.Helper.Reflection.GetProperty<SDKHelper>(typeof(Program), "sdk").GetValue();
        if (!sdk.ConnectionFinished)
            return Stage.StartTransitionToCoop;

        // start transition
        menu.performButtonAction("Co-op");

        // need a full game update before the next step to avoid crashes
        return Stage.TransitionToCoop;
    }

    /// <summary>Finish transitioning from the title screen to the co-op section.</summary>
    /// <param name="menu">The title menu.</param>
    /// <returns>Returns the next step in the skip logic.</returns>
    private Stage TransitionToCoop(TitleMenu menu)
    {
        // skip animation
        while (TitleMenu.subMenu == null)
            menu.update(Game1.currentGameTime);

            return TransitionToCoopHost();
    }

    /// <summary>Skip from the co-op section to the host screen.</summary>
    /// <returns>Returns the next step in the skip logic.</returns>
    private Stage TransitionToCoopHost()
    {
        // not applicable
        if (TitleMenu.subMenu is not CoopMenu submenu)
            return Stage.None;

        // not connected yet
        if (submenu.hostTab == null)
            return Stage.TransitionToCoopHost;

        // select host tab
        submenu.receiveLeftClick(submenu.hostTab.bounds.X, submenu.hostTab.bounds.Y, playSound: false);
        return Stage.None;
    }
}
