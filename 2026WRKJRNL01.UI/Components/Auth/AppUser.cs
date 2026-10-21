// LOCAL AUTH SHIM
//
// This replaces the type that previously came from the private "SDPBC.Authentication"
// NuGet package so the app can build and run on a machine OUTSIDE the corporate network
// (no SAML identity provider, no Redis, no Azure services).
//
// The namespace intentionally matches the original package namespace
// (SDPBC.Authentication.BlazorUI) so existing `@using` directives and `base.UserData`
// references throughout the Razor components compile without modification.

using System.Collections.Generic;

namespace SDPBC.Authentication.BlazorUI
{
    /// <summary>
    /// Lightweight stand-in for the SDPBC authenticated user object.
    /// Populated locally from configuration (see <c>LocalUser</c> in appsettings) instead
    /// of from a SAML-authenticated principal.
    /// </summary>
    public class AppUser
    {
        /// <summary>Global identifier (for employees this is the sAMAccountName or employee id).</summary>
        public string GlobalID { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// True when the user is a recognized application user. Always true for the local dev user.
        /// </summary>
        public bool IsAppUser { get; set; } = true;

        /// <summary>Role names assigned to the user (e.g. Employee, Admin, SuperAdmin).</summary>
        public List<string> Roles { get; set; } = new();
    }
}
