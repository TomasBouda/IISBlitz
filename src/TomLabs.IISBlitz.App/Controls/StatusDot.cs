using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace TomLabs.IISBlitz.App.Controls;

/// <summary>
/// Small state indicator: a 7px dot that is green when running and grey when stopped.
/// When <see cref="IsLive"/> is set the running dot emits a soft pulsing glow ring (styled in Controls.axaml).
/// </summary>
public class StatusDot : TemplatedControl
{
    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<StatusDot, bool>(nameof(IsRunning));

    public static readonly StyledProperty<bool> IsLiveProperty =
        AvaloniaProperty.Register<StatusDot, bool>(nameof(IsLive));

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public bool IsLive
    {
        get => GetValue(IsLiveProperty);
        set => SetValue(IsLiveProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsRunningProperty || change.Property == IsLiveProperty)
        {
            PseudoClasses.Set(":running", IsRunning);
            PseudoClasses.Set(":live", IsRunning && IsLive);
        }
    }
}
