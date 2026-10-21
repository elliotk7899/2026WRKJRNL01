using _2026WRKJRNL01.UI.Components.Settings;
using Microsoft.AspNetCore.Components;
using SDPBC.Authentication.BlazorUI;

namespace _2026WRKJRNL01.UI.Components.Layout
{
    // For regular pages
    public class AppComponentBase : AuthorizedComponentBase, IDisposable
    {
        [Inject]
        protected AppState AppState { get; set; } = default!;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            AppState.OnChange += StateHasChanged;
        }

        public virtual void Dispose()
        {
            AppState.OnChange -= StateHasChanged;
        }
    }

    // For layouts
    public class AppLayoutBase : LayoutComponentBase, IDisposable
    {
        [Inject]
        protected AppState AppState { get; set; } = default!;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            AppState.OnChange += StateHasChanged;
        }

        public virtual void Dispose()
        {
            AppState.OnChange -= StateHasChanged;
        }
    }
}