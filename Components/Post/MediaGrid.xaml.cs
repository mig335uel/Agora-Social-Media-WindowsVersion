using Agora.Models;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Agora.Components.Post
{
    public sealed partial class MediaGrid : UserControl
    {
        
        public List<MediaFeature> ListaDeFotos
        {
            get { return (List<MediaFeature>)GetValue(ListaDeFotosProperty); }
            set { SetValue(ListaDeFotosProperty, value); }
        }
        public static readonly DependencyProperty ListaDeFotosProperty =
            DependencyProperty.Register("ListaDeFotos", typeof(List<MediaFeature>), typeof(MediaGrid), new PropertyMetadata(null));

        public MediaGrid()
        {
            InitializeComponent();
        }

        private void Image_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // 1. Sabemos qué imagen han clicado
            var imagenClicada = sender as Image;

            // 2. Creamos una ventana de Windows 11 completamente nueva
            var nuevaVentana = new Window();
            nuevaVentana.Title = "Visor de Imágenes - Agora";
            // 3. Le metemos un Grid negro de fondo con la foto a pantalla completa
            var fondo = new Grid { Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black) };
            var fotoGrande = new Image
            {
                Source = imagenClicada!.Source,
                Stretch = Microsoft.UI.Xaml.Media.Stretch.Uniform
            };

            fondo.Children.Add(fotoGrande);
            nuevaVentana.Content = fondo;
            // 4. ¡Mostramos la ventana!
            nuevaVentana.Activate();
        }
    }
}
