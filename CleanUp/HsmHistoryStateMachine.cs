using Abstractions.Attributes;

namespace CleanUp;

public enum HhsState
{
    Root,
    Menu,
    Settings,
    Settings_Audio,
    Settings_Video,
    Game,
    Game_Play,
    Game_Pause
}

public enum HhsTrigger
{
    EnterSettings,
    Back,
    StartGame,
    Pause,
    Resume,
    ExitToMenu
}

[StateMachine(typeof(HhsState), typeof(HhsTrigger))]
public partial class HsmHistoryStateMachine
{
    // Hierarchy: Root -> { Menu (initial), Settings (composite, shallow), Game (composite, deep) }
    [State(HhsState.Root)]
    [State(HhsState.Menu, Parent = HhsState.Root, IsInitial = true, OnEntry = nameof(OnEnterMenu))]
    [State(HhsState.Settings, Parent = HhsState.Root, History = HistoryMode.Shallow, OnEntry = nameof(OnEnterSettings))]
    [State(HhsState.Settings_Audio, Parent = HhsState.Settings, IsInitial = true, OnEntry = nameof(OnEnterSettingsAudio))]
    [State(HhsState.Settings_Video, Parent = HhsState.Settings, OnEntry = nameof(OnEnterSettingsVideo))]
    [State(HhsState.Game, Parent = HhsState.Root, History = HistoryMode.Deep, OnEntry = nameof(OnEnterGame))]
    [State(HhsState.Game_Play, Parent = HhsState.Game, IsInitial = true, OnEntry = nameof(OnEnterGamePlay))]
    [State(HhsState.Game_Pause, Parent = HhsState.Game, OnEntry = nameof(OnEnterGamePause))]
    private void ConfigureStates() { }

    // Transitions
    [Transition(HhsState.Menu, HhsTrigger.EnterSettings, HhsState.Settings)] // to composite → resolves to Settings_Audio
    [Transition(HhsState.Settings, HhsTrigger.Back, HhsState.Menu)]
    [Transition(HhsState.Menu, HhsTrigger.StartGame, HhsState.Game)] // to composite → resolves to Game_Play
    [Transition(HhsState.Game_Play, HhsTrigger.Pause, HhsState.Game_Pause)]
    [Transition(HhsState.Game_Pause, HhsTrigger.Resume, HhsState.Game_Play)]
    [Transition(HhsState.Game, HhsTrigger.ExitToMenu, HhsState.Menu)]
    private void ConfigureTransitions() { }

    // OnEntry callbacks
    private void OnEnterMenu() { }
    private void OnEnterSettings() { }
    private void OnEnterSettingsAudio() { }
    private void OnEnterSettingsVideo() { }
    private void OnEnterGame() { }
    private void OnEnterGamePlay() { }
    private void OnEnterGamePause() { }
}
