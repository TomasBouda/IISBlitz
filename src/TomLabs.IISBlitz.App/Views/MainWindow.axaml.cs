using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TomLabs.IISBlitz.App.Behaviours;
using TomLabs.IISBlitz.App.Models;
using TomLabs.IISBlitz.App.ViewModels;

namespace TomLabs.IISBlitz.App.Views
{
    public partial class MainWindow : Window
    {
        private SearchHighlightTransformer? _logSearchHighlighter;
        private TextMate.Installation? _jsonTextMate;
        private TextMate.Installation? _xmlTextMate;
        private readonly Dictionary<OpenFileViewModel, TabItem> _fileTabs = new();
        private SiteInfo? _tabsSite;

        public MainWindow()
        {
            InitializeComponent();
            SetupSyntaxHighlighting();
            SetupLogSearchHighlighting();

            // Tunnel so the palette gets Up/Down/Enter/Esc before the text box does.
            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            KeyDown += OnKeyDown;
            Loaded += OnLoaded;
            ActualThemeVariantChanged += (_, _) => ApplyEditorTheme();

            LogFilesList.SelectionChanged += OnLogFileSelected;
            LogResultsList.SelectionChanged += OnLogResultSelected;
            LogSearchBox.KeyDown += OnLogSearchKeyDown;
            PaletteList.PointerReleased += OnPaletteItemPointerReleased;
        }

        private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            ApplyEditorTheme();

            // The window is up: a freshly installed update may drop its rollback backup.
            TomLabs.AutoUpdate.Updater.Current?.MarkHealthy();

            if (Vm is { } vm)
            {
                vm.OpenFilePicker = () => _ = OpenFilePickerAsync();
                vm.FocusFile = file => { if (_fileTabs.TryGetValue(file, out var tab)) Tabs.SelectedItem = tab; };
                vm.SiteViewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(SiteViewModel.SelectedSite))
                        BindFileTabs(vm.SiteViewModel.SelectedSite);
                };
                BindFileTabs(vm.SiteViewModel.SelectedSite);
            }

            if (Vm is { } vm2 && _logSearchHighlighter != null)
            {
                vm2.SiteViewModel.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(SiteViewModel.LogHighlightTerm))
                    {
                        _logSearchHighlighter.SearchTerm = vm2.SiteViewModel.LogHighlightTerm;
                        Dispatcher.UIThread.Post(() => LogViewer?.TextArea.TextView.Redraw());
                    }
                };
            }
        }

        // ----- Keyboard -----

        private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (Vm is not { } vm) return;

            if (vm.Palette.IsOpen)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        vm.Palette.Close();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        vm.Palette.MoveSelection(1);
                        e.Handled = true;
                        break;
                    case Key.Up:
                        vm.Palette.MoveSelection(-1);
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        vm.Palette.RunSelected();
                        e.Handled = true;
                        break;
                }
                return;
            }

            if (e.KeyModifiers == KeyModifiers.Control && e.Key == Key.K)
            {
                OpenPalette();
                e.Handled = true;
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (Vm is not { } vm || vm.Palette.IsOpen) return;

            if (e.Key == Key.F5)
            {
                vm.SiteViewModel.RefreshSitesCmd.Execute(null);
                e.Handled = true;
            }
            else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.L)
            {
                vm.SiteViewModel.ToggleThemeCmd.Execute(null);
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        vm.SiteViewModel.SaveAppSettingsCmd.Execute(null);
                        vm.SiteViewModel.SaveWebConfigCmd.Execute(null);
                        vm.SiteViewModel.SaveOpenFiles();
                        e.Handled = true;
                        break;
                    case Key.R:
                        vm.SiteViewModel.RecyclePoolCmd.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.F:
                        SearchBox.Focus();
                        SearchBox.SelectAll();
                        e.Handled = true;
                        break;
                }
            }
        }

        // ----- Command palette -----

        private void OpenPalette()
        {
            if (Vm is not { } vm) return;
            vm.Palette.Open();
            Dispatcher.UIThread.Post(() =>
            {
                PaletteBox.Focus();
                PaletteBox.SelectAll();
            }, DispatcherPriority.Input);
        }

        private void OnPaletteButtonClick(object? sender, RoutedEventArgs e) => OpenPalette();

        /// <summary>The header doubles as the title bar: drag moves the window, double-click toggles maximize.</summary>
        private void OnHeaderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.Source is Visual source && source.FindAncestorOfType<Button>(true) != null) return;
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

            if (e.ClickCount == 2)
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            else
                BeginMoveDrag(e);
        }

        private void OnScrimPressed(object? sender, PointerPressedEventArgs e) => Vm?.Palette.Close();

        private async void OnCopyEventClick(object? sender, RoutedEventArgs e)
        {
            if (Vm?.SiteViewModel.SelectedEvent is { } ev && Clipboard is { } clipboard)
                await clipboard.SetTextAsync($"{ev.TimeGenerated:yyyy-MM-dd HH:mm:ss} [{ev.Level}] {ev.Source}{Environment.NewLine}{ev.Message}");
        }

        private void OnPaletteItemPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (e.Source is Visual source && source.FindAncestorOfType<ListBoxItem>(true) != null)
                Vm?.Palette.RunSelected();
        }

        // ----- Extra file tabs -----

        /// <summary>Mirrors the selected site's OpenFiles collection into TabItems after the fixed tabs.</summary>
        private void BindFileTabs(SiteInfo? site)
        {
            if (_tabsSite != null)
                _tabsSite.OpenFiles.CollectionChanged -= OnOpenFilesChanged;

            foreach (var tab in _fileTabs.Values)
                Tabs.Items.Remove(tab);
            _fileTabs.Clear();

            _tabsSite = site;
            if (site == null) return;

            site.OpenFiles.CollectionChanged += OnOpenFilesChanged;
            foreach (var file in site.OpenFiles)
                AddFileTab(file);
        }

        private void OnOpenFilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            foreach (var file in e.OldItems?.OfType<OpenFileViewModel>() ?? Enumerable.Empty<OpenFileViewModel>())
            {
                if (_fileTabs.Remove(file, out var tab))
                {
                    var wasSelected = ReferenceEquals(Tabs.SelectedItem, tab);
                    Tabs.Items.Remove(tab);
                    if (wasSelected) Tabs.SelectedIndex = 0;
                }
            }

            // Tabs restored while a site loads stay in the background; a tab the user opens gets focus.
            var focus = Vm?.SiteViewModel.IsLoadingSite != true;
            foreach (var file in e.NewItems?.OfType<OpenFileViewModel>() ?? Enumerable.Empty<OpenFileViewModel>())
            {
                var tab = AddFileTab(file);
                if (focus) Tabs.SelectedItem = tab;
            }
        }

        private TabItem AddFileTab(OpenFileViewModel file)
        {
            var name = new TextBlock { Text = file.FileName, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            var dirty = new TextBlock { Text = "●", FontSize = 8, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, DataContext = file };
            dirty.Bind(TextBlock.IsVisibleProperty, new Avalonia.Data.Binding(nameof(OpenFileViewModel.IsDirty)));
            if (this.TryFindResource("AccentBrush", ActualThemeVariant, out var accent) && accent is IBrush accentBrush)
                dirty.Foreground = accentBrush;

            var close = new Button { Classes = { "icon", "small" }, Width = 18, Height = 18, Padding = new Thickness(0) };
            close.Content = new Projektanker.Icons.Avalonia.Icon { Value = "fa-solid fa-xmark", FontSize = 10 };
            ToolTip.SetTip(close, "Close tab");
            close.Click += (_, _) => Vm?.SiteViewModel.CloseFileCmd.Execute(file);

            var header = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
            header.Children.Add(name);
            header.Children.Add(dirty);
            header.Children.Add(close);
            ToolTip.SetTip(header, file.Path);

            var tab = new TabItem { Header = header, Content = new FileEditorView { DataContext = file } };
            _fileTabs[file] = tab;
            Tabs.Items.Add(tab);
            return tab;
        }

        private void OnOpenFileClick(object? sender, RoutedEventArgs e) => _ = OpenFilePickerAsync();

        private async System.Threading.Tasks.Task OpenFilePickerAsync()
        {
            if (Vm?.SiteViewModel.SelectedSite is not { } site) return;
            var options = new FilePickerOpenOptions { Title = $"Open a file of {site.Name}", AllowMultiple = true };
            try { options.SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(site.PhysicalPath); } catch { /* keep default */ }

            var picked = await StorageProvider.OpenFilePickerAsync(options);
            foreach (var item in picked)
            {
                var path = item.TryGetLocalPath();
                if (path != null) Vm.SiteViewModel.OpenFile(path);
            }
        }

        // ----- Logs -----

        private void OnLogFileSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (LogFilesList.SelectedItem is string file)
                Vm?.SiteViewModel.ViewLogCmd.Execute(file);
        }

        private void OnLogResultSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (LogResultsList.SelectedItem is LogSearchResult result)
                Vm?.SiteViewModel.ViewSearchResultCmd.Execute(result.FullPath);
        }

        private void OnLogSearchKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Vm?.SiteViewModel.SearchLogCmd.Execute(null);
                e.Handled = true;
            }
        }

        // ----- Editors -----

        private void SetupSyntaxHighlighting()
        {
            var registry = new RegistryOptions(ThemeName.DarkPlus);

            _jsonTextMate = AppSettingsEditor.InstallTextMate(registry);
            _jsonTextMate.SetGrammar(registry.GetScopeByLanguageId("json"));

            _xmlTextMate = WebConfigEditor.InstallTextMate(registry);
            _xmlTextMate.SetGrammar(registry.GetScopeByLanguageId("xml"));

        }

        /// <summary>
        /// Keeps the TextMate colour theme in step with the app theme, then re-applies our own editor chrome —
        /// TextMate writes the theme's background and line-number colours straight onto the editor.
        /// </summary>
        private void ApplyEditorTheme()
        {
            var dark = ActualThemeVariant == ThemeVariant.Dark;
            var registry = new RegistryOptions(dark ? ThemeName.DarkPlus : ThemeName.LightPlus);
            var theme = registry.LoadTheme(dark ? ThemeName.DarkPlus : ThemeName.LightPlus);

            try
            {
                _jsonTextMate?.SetTheme(theme);
                _xmlTextMate?.SetTheme(theme);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to switch editor colour theme: {ex.Message}");
            }

            foreach (var editor in new[] { AppSettingsEditor, WebConfigEditor, LogViewer })
                ApplyEditorChrome(editor);
        }

        private void ApplyEditorChrome(TextEditor editor)
        {
            if (TryBrush("BgEditorBrush", out var bg)) editor.Background = bg;
            if (TryBrush("TextBrush", out var fg)) editor.Foreground = fg;
            if (TryBrush("TextFaintBrush", out var ln)) editor.LineNumbersForeground = ln;
        }

        private bool TryBrush(string key, out IBrush brush)
        {
            if (this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush found)
            {
                brush = found;
                return true;
            }

            brush = Brushes.Transparent;
            return false;
        }

        private void SetupLogSearchHighlighting()
        {
            _logSearchHighlighter = new SearchHighlightTransformer();
            LogViewer.TextArea.TextView.LineTransformers.Add(_logSearchHighlighter);
        }
    }
}
