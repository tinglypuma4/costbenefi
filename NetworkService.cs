using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;

namespace costbenefi.Services
{
    /// <summary>
    /// Servicio de red para comunicación HTTP con el servidor
    /// </summary>
    public class NetworkService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConfiguracionSistema _config;
        private string _authToken = "";
        private bool _disposed = false;

        public NetworkService()
        {
            _config = ConfiguracionSistema.Instance;

            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(_config.UrlServidor),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"CostBenefi-Terminal/{_config.NombreTerminal}");

            Debug.WriteLine($"🌐 NetworkService inicializado para {_config.UrlServidor}");
        }

        /// <summary>
        /// Actualiza el token de autenticación
        /// </summary>
        public void ActualizarToken(string token)
        {
            _authToken = token;

            // Limpiar headers de autorización previos
            _httpClient.DefaultRequestHeaders.Remove("Authorization");

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                Debug.WriteLine($"🔐 Token de autorización actualizado");
            }
        }

        /// <summary>
        /// Prueba la conectividad básica con el servidor
        /// </summary>
        public async Task<bool> ProbarConectividad()
        {
            try
            {
                var response = await GetAsync<object>("/api/sync/ping");
                return response.IsSuccess;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error de conectividad: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Realiza una petición GET
        /// </summary>
        public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
        {
            try
            {
                Debug.WriteLine($"🌐 GET {endpoint}");

                var response = await _httpClient.GetAsync(endpoint);
                return await ProcesarRespuesta<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en GET {endpoint}: {ex.Message}");
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Realiza una petición POST
        /// </summary>
        public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                Debug.WriteLine($"🌐 POST {endpoint}");

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(endpoint, content);

                return await ProcesarRespuesta<T>(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error en POST {endpoint}: {ex.Message}");
                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Procesa la respuesta HTTP
        /// </summary>
        private async Task<ApiResponse<T>> ProcesarRespuesta<T>(HttpResponseMessage response)
        {
            try
            {
                var jsonContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    Debug.WriteLine($"✅ Respuesta exitosa: {response.StatusCode}");

                    return new ApiResponse<T>
                    {
                        IsSuccess = true,
                        Data = data,
                        StatusCode = (int)response.StatusCode
                    };
                }
                else
                {
                    Debug.WriteLine($"❌ Error HTTP: {response.StatusCode} - {jsonContent}");

                    return new ApiResponse<T>
                    {
                        IsSuccess = false,
                        Error = $"HTTP {response.StatusCode}: {jsonContent}",
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error procesando respuesta: {ex.Message}");

                return new ApiResponse<T>
                {
                    IsSuccess = false,
                    Error = $"Error procesando respuesta: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Obtiene información de salud del servidor
        /// </summary>
        public async Task<ApiResponse<object>> ObtenerEstadoServidor()
        {
            return await GetAsync<object>("/api/sync/health");
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _httpClient?.Dispose();

            Debug.WriteLine("🗑️ NetworkService disposed");
        }
    }

    /// <summary>
    /// Respuesta estándar de la API
    /// </summary>
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T? Data { get; set; }
        public string Error { get; set; } = "";
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}