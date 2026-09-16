using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERP.infrastructure.data;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;

        public AuthController(MasterErpDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        public record LoginRequest(string? CompanyName, string? Username, string? Password);

        public record LoginResponse(
            bool Success,
            string Message,
            int CompanyId,
            string CompanyCode,
            string CompanyName,
            string PlanName,
            string Username,
            string Role,
            string Token,
            bool IsPOSAllowed,
            bool IsInventoryAllowed,
            bool IsRepairAllowed,
            bool IsSupplierAllowed
        );

        /// <summary>
        /// Authenticates a user against their specific tenant and company subscription.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName) ||
                string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Company Name, Username, and Password are all required." });
            }

            string companyInput = request.CompanyName.Trim();
            string username = request.Username.Trim();
            string password = request.Password;

            // 1. Resolve Company from Master DB (or fast fallback if offline/unreachable)
            domain.entities.Company? company = null;
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    company = await _masterDb.Companies.AsNoTracking()
                        .FirstOrDefaultAsync(c =>
                            c.CompanyName.ToLower() == companyInput.ToLower() ||
                            c.CompanyCode.ToLower() == companyInput.ToLower(), cts.Token);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Master DB query note: {ex.Message}");
                }
            }

            if (company == null)
            {
                // Fallback for standard demo tenants if offline or not yet seeded
                if (companyInput.Equals("Tenant A", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("TENANT_A", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("Morphic Computers", StringComparison.OrdinalIgnoreCase))
                {
                    company = new domain.entities.Company { CompanyId = 1, CompanyCode = "TENANT_A", CompanyName = "Tenant A", PlanName = "Micro" };
                }
                else if (companyInput.Equals("Tenant B", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("TENANT_B", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("Apex Cybernetics", StringComparison.OrdinalIgnoreCase))
                {
                    company = new domain.entities.Company { CompanyId = 2, CompanyCode = "TENANT_B", CompanyName = "Tenant B", PlanName = "SmallBusiness" };
                }
                else if (companyInput.Equals("Tenant C", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("TENANT_C", StringComparison.OrdinalIgnoreCase) || companyInput.Equals("Vanguard Tech", StringComparison.OrdinalIgnoreCase))
                {
                    company = new domain.entities.Company { CompanyId = 3, CompanyCode = "TENANT_C", CompanyName = "Tenant C", PlanName = "Enterprise" };
                }
                else
                {
                    return NotFound(new { error = $"Company '{companyInput}' was not found. Please enter a valid company name (e.g. Tenant A, Tenant B, or Tenant C)." });
                }
            }

            // 2. Validate Credentials & Roles
            string role;
            string displayName;

            if (username.Equals("cirunay", StringComparison.OrdinalIgnoreCase) && password == "09092121")
            {
                role = "Store Administrator";
                displayName = "Cirunay";
            }
            else if (username.Equals("cashier", StringComparison.OrdinalIgnoreCase) && password == "cashier123")
            {
                role = "Cashier Operations";
                displayName = "Cashier (Alex M.)";
            }
            else if (username.Equals("admin", StringComparison.OrdinalIgnoreCase) && password == "admin123")
            {
                role = "Store Administrator";
                displayName = "Admin";
            }
            else
            {
                return Unauthorized(new { error = "Invalid username or password. Please try again." });
            }

            // Generate session token
            string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            var response = new LoginResponse(
                Success: true,
                Message: "Authentication successful.",
                CompanyId: company.CompanyId,
                CompanyCode: company.CompanyCode,
                CompanyName: company.CompanyName,
                PlanName: company.PlanName,
                Username: displayName,
                Role: role,
                Token: token,
                IsPOSAllowed: company.IsPOSAllowed,
                IsInventoryAllowed: company.IsInventoryAllowed,
                IsRepairAllowed: company.IsRepairAllowed,
                IsSupplierAllowed: company.IsSupplierAllowed
            );

            return Ok(response);
        }
    }
}
