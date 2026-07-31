using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agora.Models;
using Agora.Services;
using Agora.Views;
namespace Agora.ViewModel
{
    
    public class LoginViewModel
    {
        public ObservableCollection<LoginForm> ListaFormularios { get; set; }

        private readonly SupabaseConnect _supabase;
        public LoginViewModel(){
            _supabase = new SupabaseConnect();
        }


    }
}
