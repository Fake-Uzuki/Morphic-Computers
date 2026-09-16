using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ERP.domain.entities;

namespace ERP.winforms.Services
{
    /// <summary>
    /// Secure HTTP Client for communicating with ERP.api.
    /// Uses internal API key authentication to safely interact with backend services without exposing DB credentials.
    /// Uses ConfigureAwait(false) throughout to prevent Windows Forms UI thread deadlocks.
    /// </summary>
    public class ApiClient
    {
        private static ApiClient? _instance;
        public static ApiClient Instance => _instance ??= new ApiClient();

        public const string ApiKey = "MorphicErp_SecureKey_2026";
        public const string HttpsBaseUrl = "https://localhost:7021";
        public const string HttpBaseUrl = "http://localhost:5156";
        public static string CurrentBaseUrl { get; set; } = HttpsBaseUrl;

        private HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;

        public class LoginResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public int CompanyId { get; set; } = 1;
            public string CompanyCode { get; set; } = "TENANT_A";
            public string CompanyName { get; set; } = "Tenant A";
            public string PlanName { get; set; } = "Micro";
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
            public bool IsPOSAllowed { get; set; } = true;
            public bool IsInventoryAllowed { get; set; } = true;
            public bool IsRepairAllowed { get; set; }
            public bool IsSupplierAllowed { get; set; }
        }

        private ApiClient()
        {
            _http = CreateHttpClient(CurrentBaseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private static HttpClient CreateHttpClient(string baseUrl)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var client = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(3)
            };

            client.DefaultRequestHeaders.Add("X-API-KEY", ApiKey);
            return client;
        }

        /// <summary>
        /// Authenticates user against ERP.api for a given company.
        /// </summary>
        public async Task<LoginResult> LoginAsync(string companyName, string username, string password)
        {
            // Try CurrentBaseUrl first; if connection fails, try HttpBaseUrl
            var result = await TryLoginAsync(CurrentBaseUrl, companyName, username, password).ConfigureAwait(false);
            if (!result.Success && result.Message.StartsWith("API Connection Error") && CurrentBaseUrl != HttpBaseUrl)
            {
                var fallbackResult = await TryLoginAsync(HttpBaseUrl, companyName, username, password).ConfigureAwait(false);
                if (fallbackResult.Success || !fallbackResult.Message.StartsWith("API Connection Error"))
                {
                    CurrentBaseUrl = HttpBaseUrl;
                    _http = CreateHttpClient(CurrentBaseUrl);
                    return fallbackResult;
                }
            }
            return result;
        }

        private async Task<LoginResult> TryLoginAsync(string baseUrl, string companyName, string username, string password)
        {
            try
            {
                using var client = CreateHttpClient(baseUrl);
                var payload = new { companyName, username, password };
                var response = await client.PostAsJsonAsync("/api/auth/login", payload).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResult>(_jsonOptions).ConfigureAwait(false);
                    return result ?? new LoginResult { Success = false, Message = "Empty response from API server." };
                }

                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    using var doc = JsonDocument.Parse(errorContent);
                    if (doc.RootElement.TryGetProperty("error", out var errProp))
                    {
                        return new LoginResult { Success = false, Message = errProp.GetString() ?? "Authentication failed." };
                    }
                }
                catch { }

                return new LoginResult { Success = false, Message = "Invalid credentials or unauthorized." };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"API Connection Error: Could not connect to {baseUrl} ({ex.Message}). Make sure ERP.api is running."
                };
            }
        }

        /// <summary>
        /// Retrieves all products for a specific tenant from ERP.api.
        /// </summary>
        public async Task<List<Product>?> GetProductsAsync(int companyId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Product>>($"/api/tenant/{companyId}/products", _jsonOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient GetProducts error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a new product in the tenant database via ERP.api.
        /// </summary>
        public async Task<Product?> AddProductAsync(int companyId, Product product)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"/api/tenant/{companyId}/products", product).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Product>(_jsonOptions).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient AddProduct error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Updates an existing product and inventory via ERP.api.
        /// </summary>
        public async Task<bool> UpdateProductAsync(int companyId, Product product)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/tenant/{companyId}/products/{product.ProductId}", product).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient UpdateProduct error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a product via ERP.api.
        /// </summary>
        public async Task<bool> DeleteProductAsync(int companyId, int productId)
        {
            try
            {
                var response = await _http.DeleteAsync($"/api/tenant/{companyId}/products/{productId}").ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient DeleteProduct error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Archives a product (soft delete) via ERP.api.
        /// </summary>
        public async Task<bool> ArchiveProductAsync(int companyId, int productId)
        {
            try
            {
                var response = await _http.PutAsync($"/api/tenant/{companyId}/products/{productId}/archive", null).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient ArchiveProduct error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores an archived product via ERP.api.
        /// </summary>
        public async Task<bool> RestoreProductAsync(int companyId, int productId)
        {
            try
            {
                var response = await _http.PutAsync($"/api/tenant/{companyId}/products/{productId}/restore", null).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient RestoreProduct error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves all product categories for a specific tenant from ERP.api.
        /// </summary>
        public async Task<List<Category>?> GetCategoriesAsync(int companyId)
        {
            try
            {
                return await _http.GetFromJsonAsync<List<Category>>($"/api/tenant/{companyId}/categories", _jsonOptions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient GetCategories error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Adds a new product category in the tenant database via ERP.api.
        /// </summary>
        public async Task<Category?> AddCategoryAsync(int companyId, Category category)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"/api/tenant/{companyId}/categories", category).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Category>(_jsonOptions).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient AddCategory error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Updates/renames a product category in the tenant database via ERP.api.
        /// </summary>
        public async Task<Category?> UpdateCategoryAsync(int companyId, int categoryId, Category category)
        {
            try
            {
                var response = await _http.PutAsJsonAsync($"/api/tenant/{companyId}/categories/{categoryId}", category).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<Category>(_jsonOptions).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient UpdateCategory error: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Deletes a custom category from the tenant database via ERP.api and reassigns products.
        /// </summary>
        public async Task<bool> DeleteCategoryAsync(int companyId, int categoryId, string? reassignTo = null)
        {
            try
            {
                string url = $"/api/tenant/{companyId}/categories/{categoryId}";
                if (!string.IsNullOrWhiteSpace(reassignTo))
                {
                    url += $"?reassignTo={Uri.EscapeDataString(reassignTo)}";
                }
                var response = await _http.DeleteAsync(url).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient DeleteCategory error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Retrieves all orders for the specified tenant from ERP.api / MonsterASP DB.
        /// </summary>
        public async Task<List<Order>> GetOrdersAsync(int companyId)
        {
            try
            {
                var response = await _http.GetAsync($"/api/tenant/{companyId}/orders").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var orders = await response.Content.ReadFromJsonAsync<List<Order>>(_jsonOptions).ConfigureAwait(false);
                    return orders ?? new List<Order>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient GetOrders error: {ex.Message}");
            }
            return new List<Order>();
        }

        /// <summary>
        /// Processes a POS checkout order and deducts inventory on the server via ERP.api.
        /// </summary>
        public async Task<bool> ProcessOrderAsync(int companyId, Order order)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"/api/tenant/{companyId}/orders", order).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient ProcessOrder error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dynamically upgrades the company's subscription plan on the server via ERP.api.
        /// </summary>
        public async Task<bool> UpgradePlanAsync(int companyId, string planName)
        {
            try
            {
                var response = await _http.PostAsJsonAsync($"/api/companies/{companyId}/upgrade-plan", new { planName }).ConfigureAwait(false);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApiClient UpgradePlan error: {ex.Message}");
                return false;
            }
        }
    }
}
