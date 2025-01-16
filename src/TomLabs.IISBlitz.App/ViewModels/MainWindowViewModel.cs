using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData.Binding;
using Microsoft.Web.Administration;
using MsBox.Avalonia;
using ReactiveUI;

namespace TomLabs.IISBlitz.App.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {

        public SiteViewModel SiteViewModel { get; } = new SiteViewModel();
        
        public MainWindowViewModel()
        {
         
        }
    }
}