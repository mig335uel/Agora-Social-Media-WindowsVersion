using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Agora.Models;
using Agora.Services; // Para tu SupabaseConnect
using Newtonsoft.Json;
using Supabase.Postgrest;

namespace Agora.ViewModel
{
    public class ForYouFeedViewModel
    {
        public ObservableCollection<Posts> myforYou { get; set; } = new ObservableCollection<Posts>();

        public ForYouFeedViewModel() { }

        public async Task CargarFeedAsync()
        {
            try
            {
                myforYou.Clear();
                string miUsuarioId = SupabaseConnect.Client!.Auth.CurrentUser!.Id!;

                if (string.IsNullOrWhiteSpace(miUsuarioId))
                {
                    throw new Exception("CRÍTICO: El miUsuarioId está llegando vacío o nulo desde Supabase. ¡La sesión no tiene un ID de usuario válido!");
                }

                // 1. TUS PROPIOS POSTS (Carga en paralelo para ir rápido)
                var misPostsTask = SupabaseConnect.Client!.From<Posts>()
                    .Where(x => x.user_id == miUsuarioId)
                    .Where(x => x.parent_post_id == null)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(3)
                    .Get();

                // 2. EL ALGORITMO PARA EL RESTO DEL FEED Y TU PERFIL
                var args = new Dictionary<string, object>
                {
                    { "p_limit", 20 },
                    { "p_offset", 0 },
                    { "p_personal_weight", 0.75 },
                    { "p_viral_weight", 0.25 },
                    { "p_user_id", miUsuarioId! }
                };
                var rpcTask = SupabaseConnect.Client!.Rpc("combine_feed_and_viral", args);

                // Sacamos tus datos de perfil para pegárselos a tus posts (ya que quitamos el auto-join)
                var miPerfilTask = SupabaseConnect.Client!.From<Users>()
                    .Where(x => x.Id == miUsuarioId)
                    .Single();

                // Esperamos a que terminen las tres consultas a la vez 
                await Task.WhenAll(misPostsTask, rpcTask, miPerfilTask);

                // 3. PINTAR TUS POSTS PRIMERO
                if (misPostsTask.Result?.Models != null)
                {
                    foreach (var propio in misPostsTask.Result.Models)
                    {
                        propio.user = miPerfilTask.Result; // Le pegamos tu cara y nombre
                        myforYou.Add(propio);
                    }
                }

                // 4. PINTAR LOS POSTS DEL ALGORITMO Y TRADUCIRLOS
                var postsDelAlgoritmo = JsonConvert.DeserializeObject<List<RankedPost>>(rpcTask.Result.Content!);
                if (postsDelAlgoritmo != null)
                {
                    foreach (var raw in postsDelAlgoritmo)
                    {
                        var postFinal = new Posts
                        {
                            Id = raw.id,
                            content = raw.content,
                            created_at = raw.created_at,
                            user_id = raw.user_id,
                            user = new Users
                            {
                                Id = raw.user_id,
                                Username = raw.username,
                                DisplayName = raw.display_name,
                                // AQUÍ FALTABA LA FOTO DE PERFIL
                                ProfilePictureUrl = raw.profile_picture_url
                            },
                            // Asignamos el multimedia que escupió el RPC
                            MediaList = raw.media
                        };

                        // Evitamos pintar posts duplicados (si el algoritmo te devolvió uno tuyo)
                        bool yaExiste = false;
                        foreach (var p in myforYou) { if (p.Id == postFinal.Id) yaExiste = true; }

                        if (!yaExiste)
                        {
                            myforYou.Add(postFinal);
                        }
                    }
                }

                if (myforYou.Count == 0)
                {
                    App.m_window.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                        {
                            Title = "Feed Vacío",
                            Content = "C# vio 0 posts. JSON bruto recibido del servidor:\n" + rpcTask.Result.Content,
                            CloseButtonText = "Ok",
                            XamlRoot = App.m_window.Content.XamlRoot
                        };
                        _ = await dialog.ShowAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error del algoritmo: {ex.Message}");
                try
                {
                    // Mostramos el error en la pantalla para no tener que buscar en la consola
                    var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                    {
                        Title = "Error cargando el Feed",
                        Content = ex.Message + "\n" + ex.StackTrace,
                        CloseButtonText = "Ok",
                        XamlRoot = App.m_window.Content.XamlRoot
                    };
                    _ = dialog.ShowAsync();
                }
                catch { }
            }
        }
    }
}
