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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Agora.Components.Post
{
    public sealed partial class PostCard : UserControl
    {
     
        public Posts PostData
        {
            get { return (Posts)GetValue(PostDataProperty); }
            set { SetValue(PostDataProperty, value); }
        }

        public static readonly DependencyProperty PostDataProperty = DependencyProperty.Register("PostData", typeof(Posts), typeof(PostCard), new PropertyMetadata(null));
        public PostCard()
        {
            
            InitializeComponent();
            
        }

        public ImageSource GetImage(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(url));
            }
            catch
            {
                return null;
            }
        }
    }
}
