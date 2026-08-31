// Fork-owned. Disambiguates a type that is unambiguous on UWP but not on WinUI/Uno,
// without touching upstream sources. Upstream TitleBar.xaml.cs:190 handles Window.Activated,
// whose args type is Microsoft.UI.Xaml.WindowActivatedEventArgs on WinUI.
global using WindowActivatedEventArgs = Microsoft.UI.Xaml.WindowActivatedEventArgs;
global using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
