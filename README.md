# Agora - Windows Native Client

Agora es la aplicación cliente nativa para Windows de la plataforma de red social moderna y dinámica, hermana de la [versión móvil en React Native/Expo](../Agora-Social-Media). Está desarrollada con **WinUI 3 (.NET 8.0)**, diseñada para ofrecer una experiencia fluida, visualmente atractiva y con un fuerte enfoque en la seguridad criptográfica y la interacción comunitaria.

## 🚀 Características Principales

-   **Experiencia Nativa Windows**: Interfaz gráfica moderna construida sobre WinUI 3 y XAML, aprovechando el rendimiento y la integración nativa de Windows 10/11.
-   **Arquitectura E2EE Zero-Trust**: Sistema de mensajería directa con cifrado de extremo a extremo que integra criptografía apoyada por hardware (TPM) a nivel de sistema operativo.
-   **Seguridad Avanzada (ASM Shield)**: Incorpora librerías nativas en C++ (`AgoraASMShield.dll`) con soporte TPM, OpenSSL y controles anti-manipulación de entorno.
-   **SSL Pinning Extremo**: Interceptor HTTP personalizado (`TpmBunkerInterceptor`) que asegura que el cliente solo se comunique con servidores autorizados, mitigando ataques de hombre en el medio (MITM).
-   **Integración Continua con Supabase**: Autenticación, persistencia de datos y eventos en tiempo real conectados a la misma base de datos PostgreSQL de la plataforma móvil.
-   **Gestión Multi-media**: Visualización de posts con imágenes y componentes UI enriquecidos.
-   **Diseño Responsivo (Desktop)**: Modos claro/oscuro y componentes (`PostCard`, `MediaGrid`, `ForYouPage`, `Following`) adaptados a la usabilidad de escritorio.

## 🛡️ Seguridad y Criptografía (El "Búnker" en Windows)

La versión de Windows replica y extiende la arquitectura de seguridad descrita en la aplicación móvil (Zero-Trust Búnker):

-   **Gestión de Llaves Nativas (`AgoraKeyManager`)**: Utiliza `CngKey` y la API Cryptography Next Generation (CNG) de Windows para crear y almacenar llaves privadas RSA vinculadas directamente al hardware (inextraíbles). Esto actúa como el equivalente al *Android Keystore* o el *iOS Keychain*.
-   **Cifrado Híbrido In-App**: Los mensajes se cifran simétricamente usando `AES-GCM` y la llave de chat (AES) viaja protegida con `RSA-OAEP-SHA256` entre los dispositivos de la red.
-   **Aislamiento**: El código C# de la interfaz (XAML) no toca ni gestiona llaves de texto plano; delega las operaciones sensibles a las APIs criptográficas nativas del sistema, garantizando que un volcado de memoria no comprometa las llaves.

## 🛠️ Stack Tecnológico

-   **Framework**: WinUI 3 / Windows App SDK (C# .NET 8.0)
-   **Base de Datos y Auth**: [Supabase](https://supabase.com/) (Vía paquete NuGet oficial `Supabase`)
-   **Seguridad de Hardware**: Windows CNG (Cryptography Next Generation), TPM (vía dependencias C++ como TSS.CPP).
-   **Patrón de Arquitectura**: MVVM (Model-View-ViewModel)

## 📂 Estructura del Proyecto

-   `/Views`: Pantallas principales de la aplicación divididas por dominio (`Login`, `Main/Feed`, `Home`).
-   `/Components`: Controles de usuario XAML reutilizables (`PostCard`, `MediaGrid`).
-   `/Models` & `/ViewModel`: Lógica de negocio y modelos de datos de la plataforma.
-   `/Services`: Servicios de conectividad (`SupabaseConnect`, `AuthService`).
-   `/Security`: Núcleo de la arquitectura Zero-Trust. Contiene el gestor de llaves `AgoraKeyManager` y el interceptor de red `TpmBunkerInterceptor`.
-   `/AgoraASMShield`: Código fuente de las DLLs nativas de protección anti-tampering y TPM escritas en C++.

## 📦 Requisitos Previos y Compilación

1. **Requisitos de Sistema**:
   - Windows 10 (Build 17763 o superior) o Windows 11.
   - Visual Studio 2022 con las cargas de trabajo de:
     - *Desarrollo de escritorio de .NET*
     - *Desarrollo de la plataforma universal de Windows (UWP)*
     - *Desarrollo de escritorio con C++*
2. **Clonar el Repositorio**:
   ```bash
   git clone https://github.com/mig335uel/Agora-Social-Media-WindowsVersion.git
   ```
3. **Restaurar Paquetes**: 
   Visual Studio debería restaurar los paquetes NuGet automáticamente (`Microsoft.WindowsAppSDK`, `Supabase`, etc.).
4. **Dependencias Nativas C++**: 
   Asegúrate de que la compilación de `AgoraASMShield` y sus dependencias (OpenSSL vía `vcpkg`, TPM `TSS.CPP`) estén presentes y linkadas correctamente (el archivo `.csproj` ya se encarga de mover las `.dll` a la ruta de salida en `PreserveNewest`).
5. **Configuración de Supabase**:
   Para iniciar el proyecto de manera local, asegúrate de configurar las credenciales y conectividad con la instancia en los servicios correspondientes (`SupabaseConnect.cs`).

## 📄 Licencia

Este proyecto es privado. Todos los derechos reservados.
