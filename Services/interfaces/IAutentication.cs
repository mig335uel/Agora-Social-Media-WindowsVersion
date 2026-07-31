using Agora.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Supabase;
namespace Agora.Services.interfaces
{
    public interface IAuthentication
    {
        Task<bool> SignUpAsync(RegisterForm register);
        Task<bool> SignInAsync(LoginForm loginForm);
        Task<bool> SignOutAsync();
    }
}
