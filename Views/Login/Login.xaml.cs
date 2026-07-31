using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Agora.Services;
using Agora.Models;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Agora.Views.Login
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Login : Page
    {
        public int maxImage = 300;
        public DateTimeOffset MaxDate = DateTimeOffset.Now.AddYears(-16);
        public DateTimeOffset MinDate = DateTimeOffset.Now.AddYears(-100);
        private string? gener {  get; set; }
       
        public Login()
        {
            InitializeComponent();
            LogoImageLogin.MaxWidth = maxImage;
            LogoImageLogin.Height = maxImage;
            BienvenidaLogin.Text = "Inicie sesión en Agora";
            BienvenidaLogin.TextWrapping = TextWrapping.Wrap;

            BienvenidaRegister.Text = "Regístrate en Agora";
            BienvenidaRegister.TextWrapping = TextWrapping.Wrap;
            LogoImageRegister.MaxWidth = maxImage;
            LogoImageRegister.Height = maxImage;

            string[] genderType = ["Masculino", "Femenino"];

            ComboGenerType.ItemsSource = genderType;

        }

        private async void LoginSubmit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var loginformul = new LoginForm(
                    Email: LoginInputEmail.Text.Trim(),
                    Password: LoginInputPassword.Password.Trim());
                // 1. Llamar a Supabase para iniciar sesión de verdad
                bool loginExitoso = await AuthService.Instance.SignInAsync(loginformul);
                
                if (loginExitoso)
                {
                    // 2. Le decimos al Frame que nos contiene que cambie la página al Home
                    if (this.Frame != null)
                    {
                        this.Frame.Navigate(typeof(Main.Home));
                        
                        // 3. Limpiamos el historial para que el usuario no pueda darle "Atrás" y volver al Login
                        this.Frame.BackStack.Clear();
                    }
                }
                else
                {
                    // TODO: Mostrar un mensaje de error en la UI
                    System.Diagnostics.Debug.WriteLine("Usuario o contraseña incorrectos.");
                }
            }
            catch (Exception ex)
            {
                // Manejar error de login (ej. contraseña incorrecta)
                System.Diagnostics.Debug.WriteLine($"Error de Login: {ex.Message}");
            }
        }

        private void RegisterChange_Click(object sender, RoutedEventArgs e)
        {
            LoginStackPanel.Visibility = Visibility.Collapsed;
            RegisterStackPanel.Visibility = Visibility.Visible;
        }

       

        private void GenerarOpcionesDisplayName(object sender, TextChangedEventArgs e)
        {
            string name = RegisterInputName.Text.Trim();
            string Lastname = RegisterInputLastName.Text.Trim();
            string UserName = RegisterInputUsername.Text.Trim();
            
            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(Lastname))
            {
                ComboDisplayNames.ItemsSource = null;
                return;
            }

            var opciones = new List<string>();

            opciones.Add($"{name} {Lastname}");
            opciones.Add(name);
            opciones.Add(UserName);

            ComboDisplayNames.ItemsSource = opciones;
            
            


        }

        private void ComboGenerType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboGenerType.SelectedIndex == 1)
            {
                gener = "Female";
            }
            else if (ComboGenerType.SelectedIndex == 0)
            {
                gener = "Male";
            }
            else
            {
                gener = null;
            }
        }

        private void LoginChange_Click(object sender, RoutedEventArgs e)
        {
            RegisterStackPanel.Visibility = Visibility.Collapsed;
            LoginStackPanel.Visibility = Visibility.Visible;
        }

        private async void RegisterSubmit_Click(object sender, RoutedEventArgs e)
        {

            try
            {
                // 1. Extraer la fecha (DateTimeOffset a DateOnly)
                DateOnly? birthDate = null;
                if (RegisterInputBirthDate.Date.HasValue)
                {
                    birthDate = DateOnly.FromDateTime(RegisterInputBirthDate.Date.Value.Date);
                }


                // 2. Llenar el Record con la info visual
                var formulario = new RegisterForm(
                    Email: RegisterInputEmail.Text.Trim(),
                    Password: RegisterInputPassword.Password.Trim(),
                    Name: RegisterInputName.Text.Trim(),
                    LastName: RegisterInputLastName.Text.Trim(),
                    Username: RegisterInputUsername.Text.Trim(),
                    DisplayName: ComboDisplayNames.SelectedItem?.ToString() ?? "",
                    BirthDate: birthDate,
                    Gender: gener
                );

                // 3. Imprimimos para verificar que se capturó todo bien
                System.Diagnostics.Debug.WriteLine($"Registrando a: {formulario.Username}, Género: {formulario.Gender}");

                // 4. Enviar a Supabase a través del servicio con patrón Singleton
                bool registroExitoso = await AuthService.Instance.SignUpAsync(formulario);
                
                if (registroExitoso)
                {
                    System.Diagnostics.Debug.WriteLine("Registro en BBDD y Auth exitoso.");
                    // Opcional: Navegar al Home si quieres que entren directamente
                    // if (this.Frame != null) { this.Frame.Navigate(typeof(Views.Main.Home)); this.Frame.BackStack.Clear(); }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Fallo en el registro de Supabase.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error de registro: {ex.Message}");
            }

        }

        private void ConfirmPassword_PasswordChanging(PasswordBox sender, PasswordBoxPasswordChangingEventArgs args)
        {
            if (ConfirmPassword.Password != RegisterInputPassword.Password) {
                ConfirmPassword.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                ConfirmPassword.ClearValue(PasswordBox.BorderBrushProperty);
            }
        }
    }
}
