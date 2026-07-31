using Agora.ViewModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Agora.Views.Main.Feed;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Agora.Views.Main
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Home : Page
    {
        public HomeViewModel HomeView { get; } = new HomeViewModel();
        public Home()
        {
            InitializeComponent();
            titleBar.Title = "Agoras";

            App.m_window.SetTitleBar(titleBar);
            MainNav.SelectedItem = MainNav.MenuItems[0];
            // 2. Marca visualmente "Para Ti" (el primer elemento [0]) en el menú superior
            FeedNav.SelectedItem = FeedNav.MenuItems[0];

            SubFrame.Navigate(typeof(ForYouPage));
        }
        

        private void titleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            if (MainNav.PaneDisplayMode == NavigationViewPaneDisplayMode.LeftCompact)
            {
                MainNav.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            }
            else
            {
                MainNav.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
            }
        }

        private void FeedNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var tag = args.InvokedItemContainer.Tag?.ToString();
            if (tag == "ForYou")
            {
                SubFrame.Navigate(typeof(ForYouPage));
            }
            else if (tag == "Following")
            {
                SubFrame.Navigate(typeof(Following)); // 
            }
        }    
    }
}
