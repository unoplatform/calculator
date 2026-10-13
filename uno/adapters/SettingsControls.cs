// Uno Workaround #24297 - the shipping CommunityToolkit SettingsControls package is
// built against `Uno` and `Uno.UI.Toolkit`, assemblies Uno 7 no longer produces, so it
// fails to load at runtime. These are look-alikes of the toolkit controls covering the
// surface upstream's Settings.xaml uses; their templates live in SettingsControls.xaml.
// Delete once a toolkit build targeting Uno 7 exists (#24297).

using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.WinUI.Controls;

public enum ContentAlignment
{
    Right,
    Left,
    Vertical,
}

public partial class SettingsCard : ContentControl
{
    public SettingsCard() => DefaultStyleKey = typeof(SettingsCard);

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static DependencyProperty HeaderProperty { get; } =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(SettingsCard), new PropertyMetadata(null, OnLayoutPropertyChanged));

    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static DependencyProperty DescriptionProperty { get; } =
        DependencyProperty.Register(nameof(Description), typeof(object), typeof(SettingsCard), new PropertyMetadata(null, OnLayoutPropertyChanged));

    public IconElement? HeaderIcon
    {
        get => (IconElement?)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public static DependencyProperty HeaderIconProperty { get; } =
        DependencyProperty.Register(nameof(HeaderIcon), typeof(IconElement), typeof(SettingsCard), new PropertyMetadata(null, OnLayoutPropertyChanged));

    public ContentAlignment ContentAlignment
    {
        get => (ContentAlignment)GetValue(ContentAlignmentProperty);
        set => SetValue(ContentAlignmentProperty, value);
    }

    public static DependencyProperty ContentAlignmentProperty { get; } =
        DependencyProperty.Register(nameof(ContentAlignment), typeof(ContentAlignment), typeof(SettingsCard), new PropertyMetadata(ContentAlignment.Right, OnLayoutPropertyChanged));

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateLayoutState();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateLayoutState();
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SettingsCard)d).UpdateLayoutState();

    private void UpdateLayoutState()
    {
        if (GetTemplateChild("PART_HeaderIconPresenter") is FrameworkElement icon)
        {
            icon.Visibility = HeaderIcon is null ? Visibility.Collapsed : Visibility.Visible;
        }
        if (GetTemplateChild("PART_HeaderPresenter") is FrameworkElement header)
        {
            header.Visibility = Header is null || ContentAlignment == ContentAlignment.Left ? Visibility.Collapsed : Visibility.Visible;
        }
        if (GetTemplateChild("PART_DescriptionPresenter") is FrameworkElement description)
        {
            description.Visibility = Description is null || ContentAlignment == ContentAlignment.Left ? Visibility.Collapsed : Visibility.Visible;
        }
        if (GetTemplateChild("PART_ContentPresenter") is FrameworkElement content)
        {
            content.Visibility = Content is null ? Visibility.Collapsed : Visibility.Visible;
            switch (ContentAlignment)
            {
                case ContentAlignment.Left:
                    Grid.SetColumn(content, 1);
                    Grid.SetRow(content, 0);
                    content.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case ContentAlignment.Vertical:
                    Grid.SetColumn(content, 1);
                    Grid.SetRow(content, 1);
                    content.HorizontalAlignment = HorizontalAlignment.Stretch;
                    content.Margin = new Thickness(0, 8, 0, 0);
                    break;
                default:
                    Grid.SetColumn(content, 2);
                    Grid.SetRow(content, 0);
                    content.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
            }
        }
    }
}

[Microsoft.UI.Xaml.Markup.ContentProperty(Name = nameof(Content))]
public partial class SettingsExpander : Control
{
    private Expander? _expander;

    public SettingsExpander()
    {
        DefaultStyleKey = typeof(SettingsExpander);
        Items = new ObservableCollection<object>();
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public static DependencyProperty HeaderProperty { get; } =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null));

    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public static DependencyProperty DescriptionProperty { get; } =
        DependencyProperty.Register(nameof(Description), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null));

    public IconElement? HeaderIcon
    {
        get => (IconElement?)GetValue(HeaderIconProperty);
        set => SetValue(HeaderIconProperty, value);
    }

    public static DependencyProperty HeaderIconProperty { get; } =
        DependencyProperty.Register(nameof(HeaderIcon), typeof(IconElement), typeof(SettingsExpander), new PropertyMetadata(null));

    // The toolkit's SettingsExpander is a ContentControl-like control; upstream sets Content in XAML.
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public static DependencyProperty ContentProperty { get; } =
        DependencyProperty.Register(nameof(Content), typeof(object), typeof(SettingsExpander), new PropertyMetadata(null));

    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public static DependencyProperty IsExpandedProperty { get; } =
        DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(SettingsExpander), new PropertyMetadata(false, OnIsExpandedChanged));

    public ObservableCollection<object> Items { get; }

    protected override void OnApplyTemplate()
    {
        if (_expander is not null)
        {
            _expander.Expanding -= OnExpanding;
            _expander.Collapsed -= OnCollapsed;
        }

        base.OnApplyTemplate();

        _expander = GetTemplateChild("PART_Expander") as Expander;
        if (_expander is not null)
        {
            _expander.IsExpanded = IsExpanded;
            _expander.Expanding += OnExpanding;
            _expander.Collapsed += OnCollapsed;
        }

        if (GetTemplateChild("PART_ItemsRepeater") is ItemsControl items)
        {
            items.ItemsSource = Items;
        }
    }

    private void OnExpanding(Expander sender, ExpanderExpandingEventArgs args) => IsExpanded = true;

    private void OnCollapsed(Expander sender, ExpanderCollapsedEventArgs args) => IsExpanded = false;

    private static void OnIsExpandedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (((SettingsExpander)d)._expander is { } expander && expander.IsExpanded != (bool)e.NewValue)
        {
            expander.IsExpanded = (bool)e.NewValue;
        }
    }
}
