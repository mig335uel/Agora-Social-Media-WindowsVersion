using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Agora.Security
{
    public class TpmBunkerInterceptor : DelegatingHandler
    {
        private const string HoneyPotKey = "PedrosanchezComemelosHuevos";

        // Hashes válidos para tu SSL Pinning extraídos de la app de Android
        private readonly HashSet<string> _validPins = new HashSet<string>
        {
            "VUUMLi+n7iaAw+X9sfcsrHMs8MqLG+WewadiiymYWEA=", // Nuevo certificado
            "PD6PUu2Rh7NKN+wAgWFnGn5Hw+IELlAOn9so3A32QYE="  // Anterior
        };

        public TpmBunkerInterceptor()
        {
            var innerHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    if (cert == null) return false;
                    byte[] hashBytes = SHA256.HashData(cert.GetPublicKey());
                    string serverPin = Convert.ToBase64String(hashBytes);
                    return _validPins.Contains(serverPin);
                }
            };
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 1. Seguridad Activa: Verificamos que el entorno no haya sido comprometido 
            // justo en el milisegundo antes de lanzar la petición de red
            if (AgoraBunker.AgoraIsEnvironmentCompromised())
            {
                Environment.Exit(unchecked((int)0xDEAD));
            }
            // (Eliminado el intercambio del Honeypot, ya que la llave real ahora se inyecta directamente 
            // en SupabaseConnect para soportar Realtime nativamente)

            // 2. Dejamos que la petición continúe su camino hacia internet protegida por SSL Pinning
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
