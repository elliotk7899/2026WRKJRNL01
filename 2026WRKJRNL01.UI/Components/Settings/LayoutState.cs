using Newtonsoft.Json.Linq;
using Microsoft.JSInterop;

namespace WRKJRNL01.UI.Components.Settings
{
    public class LayoutState
    {
        private readonly IJSRuntime JS;

        public LayoutState(IJSRuntime js)
        {
            JS = js;
        }

        public bool DrawerOpen { get; private set; } = true;

        public event Action? OnChange;

        public void SetDrawer(bool open)
        {
            DrawerOpen = open;
            OnChange?.Invoke();
        }

        public async Task ToggleDrawer(CancellationToken token = default)
        {
            DrawerOpen = !DrawerOpen;

            await JS.InvokeVoidAsync("localStorage.setItem", "drawer", (!DrawerOpen).ToString());

            OnChange?.Invoke();
        }
    }
}
