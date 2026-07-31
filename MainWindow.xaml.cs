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
using System.Runtime.InteropServices; // <-- Este es vital para que funcione el DllImport
using Windows.Foundation;
using Windows.Foundation.Collections;
using Agora.Services;
using Agora.Security;
using System.Security.Cryptography.X509Certificates;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Agora
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            App.m_window = this;
            InitializeComponent();
            
            // 1. Obtenemos el ID de la ventana y llamamos al Búnker nativo para bloquear capturas
            //IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            //AgoraBunker.AgoraProtectWindow(hWnd);
            
            AppWindow.SetIcon("Assets/AgorasLogo.ico");
            if (SupabaseConnect.IsUserAuthenticated())
            {
                ExtendsContentIntoTitleBar = true;
                SessionControl.Navigate(typeof(Views.Main.Home));


            }
            else
            {
                // Si no está autenticado, mostramos la página de login
                SessionControl.Navigate(typeof(Views.Login.Login));
            }

        }
    }
}
