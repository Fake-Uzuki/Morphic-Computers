using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ERP.domain.entities;
using ERP.infrastructure.data;
using ERP.infrastructure.services;

namespace ERP.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;
        private readonly ITenantDbContextFactory _tenantDbFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            MasterErpDbContext masterDb,
            ITenantDbContextFactory tenantDbFactory,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _masterDb = masterDb;
            _tenantDbFactory = tenantDbFactory;
            _configuration = configuration;
            _logger = logger;
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
        /// Authenticates a user against the Master database and configured backend data.
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

            // 1. Resolve Company from Master DB (Source of Truth)
            Company? company;
            try
            {
                company = await _masterDb.Companies.AsNoTracking()
                    .FirstOrDefaultAsync(c =>
                        c.CompanyName.ToLower() == companyInput.ToLower() ||
                        c.CompanyCode.ToLower() == companyInput.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to query Master DB for company '{CompanyInput}'.", companyInput);
                return StatusCode(500, new { error = "Database connection error while verifying company." });
            }

            if (company == null)
            {
                return NotFound(new { error = $"Company '{companyInput}' was not found in the master database." });
            }

            // 2. Validate Credentials against real backend data
            bool isAuthenticated = false;
            string role = "Store Administrator";
            string displayName = username;

            // Check A: Configured Backend Administrator credentials
            var configAdminUser = _configuration["Auth:AdminUsername"];
            var configAdminPass = _configuration["Auth:AdminPassword"];
            if (!string.IsNullOrWhiteSpace(configAdminUser) &&
                username.Equals(configAdminUser, StringComparison.OrdinalIgnoreCase) &&
                password == configAdminPass)
            {
                isAuthenticated = true;
                role = "Store Administrator";
                displayName = "Administrator";
            }

            // Check B: Identity Users in Master DB
            if (!isAuthenticated)
            {
                try
                {
                    var identityUser = await _masterDb.Users.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserName != null && u.UserName.ToLower() == username.ToLower());
                    if (identityUser != null && !string.IsNullOrEmpty(identityUser.PasswordHash))
                    {
                        var hasher = new PasswordHasher<IdentityUser>();
                        var verifyResult = hasher.VerifyHashedPassword(identityUser, identityUser.PasswordHash, password);
                        if (verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            isAuthenticated = true;
                            displayName = identityUser.UserName ?? username;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Identity user check failed for user '{Username}'.", username);
                }
            }

            // Check C: Tenant Database Staff Members
            StaffMember? staff = null;
            try
            {
                await using var tenantDb = await _tenantDbFactory.CreateAsync(company.CompanyId);
                staff = await tenantDb.StaffMembers.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Username.ToLower() == username.ToLower() && s.IsActive);

                if (staff != null)
                {
                    var defaultStaffPass = _configuration["Auth:DefaultStaffPassword"];
                    if (!isAuthenticated && !string.IsNullOrWhiteSpace(defaultStaffPass) && password == defaultStaffPass)
                    {
                        isAuthenticated = true;
                    }

                    if (isAuthenticated)
                    {
                        displayName = !string.IsNullOrWhiteSpace(staff.FullName) ? staff.FullName : staff.Username;
                        role = !string.IsNullOrWhiteSpace(staff.Role) ? staff.Role : role;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tenant staff check note for company {CompanyId}: {Message}", company.CompanyId, ex.Message);
            }

            if (!isAuthenticated)
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
