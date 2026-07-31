using System;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;
using TomLabs.IISBlitz.App.Behaviours;
using TomLabs.IISBlitz.App.ViewModels;

namespace TomLabs.IISBlitz.App.Views
{
    public partial class MainWindow : Window
    {
        private SearchHighlightTransformer? _logSearchHighlighter;

        public MainWindow()
        {
            InitializeComponent();
            SetupSyntaxHighlighting();
            SetupLogSearchHighlighting();
            KeyDown += OnKeyDown;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm && _logSearchHighlighter != null)
            {
                vm.SiteViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(SiteViewModel.LogHighlightTerm))
                    {
                        _logSearchHighlighter.SearchTerm = vm.SiteViewModel.LogHighlightTerm;
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            LogViewer?.TextArea.TextView.Redraw();
                        });
                    }
                };
            }
        }

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm) return;

            if (e.Key == Key.F5)
            {
                vm.SiteViewModel.RefreshSitesCmd.Execute(null);
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        vm.SiteViewModel.SaveAppSettingsCmd.Execute(null);
                        vm.SiteViewModel.SaveWebConfigCmd.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.R:
                        vm.SiteViewModel.RecyclePoolCmd.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.F:
                        var searchBox = this.FindControl<TextBox>("SearchBox");
                        searchBox?.Focus();
                        e.Handled = true;
                        break;
                }
            }
        }

        private void SetupSyntaxHighlighting()
        {
            var registryOptions = new RegistryOptions(ThemeName.DarkPlus);

            if (AppSettingsEditor != null)
            {
                var jsonInstall = AppSettingsEditor.InstallTextMate(registryOptions);
                jsonInstall.SetGrammar(registryOptions.GetScopeByLanguageId("json"));
            }

            if (WebConfigEditor != null)
            {
                var xmlInstall = WebConfigEditor.InstallTextMate(registryOptions);
                xmlInstall.SetGrammar(registryOptions.GetScopeByLanguageId("xml"));
            }

            if (ResponseBodyViewer != null)
            {
                var htmlInstall = ResponseBodyViewer.InstallTextMate(registryOptions);
                htmlInstall.SetGrammar(registryOptions.GetScopeByLanguageId("html"));
            }
        }

        private void SetupLogSearchHighlighting()
        {
            if (LogViewer == null) return;
            _logSearchHighlighter = new SearchHighlightTransformer();
            LogViewer.TextArea.TextView.LineTransformers.Add(_logSearchHighlighter);
        }
    }
}