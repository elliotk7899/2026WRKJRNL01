// LOCAL AUTH SHIM
//
// Replaces SDPBC.Authentication.BlazorUI.AuthorizedComponentBase (from the private NuGet
// package) with a local equivalent so the app builds/runs off-network. The namespace and
// member surface (UserData / IsImpersonating / ImpersonatorUserData) match what the Razor
// components already consume, so no component markup needs to change.

using Microsoft.AspNetCore.Components;
using _2026WRKJRNL01.UI.Components.Auth;

namespace SDPBC.Authentication.BlazorUI
{
    /// <summary>
    /// Base component that exposes the current (local) user. Impersonation is always off
    /// in the local build.
    /// </summary>
    public abstract class AuthorizedComponentBase : ComponentBase
    {
        [Inject]
        protected LocalUserProvider LocalUser { get; set; } = default!;

        /// <summary>The current application user.</summary>
        protected AppUser UserData => LocalUser.User;

        /// <summary>Always false locally (no impersonation support off-network).</summary>
        protected bool IsImpersonating => false;

        /// <summary>Same as <see cref="UserData"/> locally since impersonation is disabled.</summary>
        protected AppUser ImpersonatorUserData => LocalUser.User;
    }
}
