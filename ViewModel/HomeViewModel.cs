using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agora.Views.Main;
using Agora.Models;
using System.Collections.ObjectModel;
namespace Agora.ViewModel
{
    public class HomeViewModel
    {
        public ObservableCollection<Posts> myforYou { get; set; }   

        public HomeViewModel()
        {
            myforYou = new ObservableCollection<Posts>();
        }
    }
}
