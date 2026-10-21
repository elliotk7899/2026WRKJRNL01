// LOCAL AUTH SHIM
//
// Supplies a single, hardcoded/configurable "logged in" user so the app can run locally
// without the SDPBC SAML authentication stack. Values are read from the "LocalUser"
// section of appsettings (with sensible defaults) so you can tweak the dev identity
// without recompiling.

using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using SDPBC.Authentication.BlazorUI;

namespace _2026WRKJRNL01.UI.Components.Auth
{
    /// <summary>
    /// Holds the local development user that replaces the SAML-authenticated user.
    /// Registered as a singleton.
    /// </summary>
    public class LocalUserProvider
    {
        public AppUser User { get; }

        /// <summary>Employee id used to stamp/filter journal records.</summary>
        public string EmployeeId { get; }

        /// <summary>Department id used to scope categories/records.</summary>
        public string DeptId { get; }

        public LocalUserProvider(IConfiguration configuration)
        {
            var section = configuration.GetSection("LocalUser");

            var globalId = section["GlobalID"] ?? "localdev";
            var firstName = section["FirstName"] ?? "Local";
            var lastName = section["LastName"] ?? "Developer";
            var email = section["Email"] ?? "localdev@example.local";

            EmployeeId = section["EmployeeId"] ?? "000000";
            DeptId = section["DeptId"] ?? "0000";

            // Roles default to full access so every page/feature is reachable while
            // running locally. Override via LocalUser:Roles in appsettings if desired.
            var roles = section.GetSection("Roles").Get<string[]>();
            var roleList = (roles is { Length: > 0 })
                ? new List<string>(roles)
                : new List<string> { "Employee", "Admin", "SuperAdmin" };

            User = new AppUser
            {
                GlobalID = globalId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                IsAppUser = true,
                Roles = roleList
            };
        }
    }
}
