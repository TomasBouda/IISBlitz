namespace TomLabs.IISBlitz.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public SiteViewModel SiteViewModel { get; } = new SiteViewModel();
    }
}