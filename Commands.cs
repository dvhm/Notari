using System.Windows.Input;

namespace Notari;

public static class Commands
{
    public static readonly RoutedUICommand New = new(
        "New", nameof(New), typeof(Commands),
        [new KeyGesture(Key.N, ModifierKeys.Control)]);

    public static readonly RoutedUICommand Open = new(
        "Open", nameof(Open), typeof(Commands),
        [new KeyGesture(Key.O, ModifierKeys.Control)]);

    public static readonly RoutedUICommand Save = new(
        "Save", nameof(Save), typeof(Commands),
        [new KeyGesture(Key.S, ModifierKeys.Control)]);

    public static readonly RoutedUICommand SaveAs = new(
        "Save As", nameof(SaveAs), typeof(Commands),
        [new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift)]);

    public static readonly RoutedUICommand Exit = new(
        "Exit", nameof(Exit), typeof(Commands),
        [new KeyGesture(Key.F4, ModifierKeys.Alt)]);

    public static readonly RoutedUICommand Settings = new(
        "Settings", nameof(Settings), typeof(Commands),
        [new KeyGesture(Key.OemComma, ModifierKeys.Control)]);

    public static readonly RoutedUICommand About = new(
        "About", nameof(About), typeof(Commands),
        [new KeyGesture(Key.OemQuestion, ModifierKeys.Control)]);

    public static readonly RoutedUICommand Find = new(
        "Find", nameof(Find), typeof(Commands),
        [new KeyGesture(Key.F, ModifierKeys.Control)]);

    public static readonly RoutedUICommand Replace = new(
        "Replace", nameof(Replace), typeof(Commands),
        [new KeyGesture(Key.H, ModifierKeys.Control)]);
}
