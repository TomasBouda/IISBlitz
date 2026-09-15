using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TomLabs.IISBlitz.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        public SiteViewModel SiteViewModel { get; } = new SiteViewModel();

        public CommandPaletteViewModel Palette { get; }

        /// <summary>Semantic version taken from the assembly so the UI can never drift from the build.</summary>
        public string AppVersion { get; }

        public string MachineName { get; } = Environment.MachineName;

        [ObservableProperty]
        private int _selectedTabIndex;

        /// <summary>
        /// "v0.5.0" for releases, "v0.5.0-nightly.abc1234" for continuous builds: the informational version carries
        /// the suffix the build passes in, while the "+commit" metadata the SDK appends is not worth the space.
        /// </summary>
        private static string ReadVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            var informational = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+');
                return "v" + (plus > 0 ? informational[..plus] : informational);
            }

            var version = assembly?.GetName().Version;
            return version is null ? "dev" : $"v{version.Major}.{version.Minor}.{version.Build}";
        }

        public string[] PermissionRights { get; } = { "FullControl", "Modify", "ReadAndExecute", "Read", "Write", "ListDirectory" };
        public string[] PermissionTypes { get; } = { "Allow", "Deny" };
        public string[] EventLevels { get; } = { "All", "Error", "Warning", "Info" };

        public MainWindowViewModel()
        {
            Palette = new CommandPaletteViewModel(this);

            AppVersion = ReadVersion();
        }
    }
}
