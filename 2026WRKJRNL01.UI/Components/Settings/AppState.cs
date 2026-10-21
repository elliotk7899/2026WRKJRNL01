using MudBlazor;
using SDPBC.Authentication.BlazorUI;
using _2026WRKJRNL01.UI.Components.Auth;

namespace _2026WRKJRNL01.UI.Components.Settings
{

    //this class is designed to store settings via StateContainer and pass around the app, similar to cascading params
    /// <summary>
    /// Application-wide state container.
    /// Scoped per Blazor circuit (per user).
    /// </summary>
    public sealed class AppState
    {
        #region Dependencies

        // OFFLINE: the SDPBC user service and employee web service were removed.
        // All user/employee data now comes from the local user (LocalUserProvider).
        private readonly LocalUserProvider _localUser;

        public AppState(LocalUserProvider localUser)
        {
            _localUser = localUser;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Task components can await to know when initialization is complete.
        /// Safe to await multiple times.
        /// </summary>
        public Task Initialization { get; private set; } = Task.CompletedTask;

        private bool _started;

        /// <summary>
        /// Triggers one-time initialization using authenticated user data.
        /// Safe to call multiple times.
        /// </summary>
        public Task InitializeAsync(AppUser user, bool isImpersonating, AppUser impersonationData)
        {
            Console.WriteLine($"AppState.InitializeAsync called for user: {user.GlobalID}, isImpersonating: {isImpersonating}");

            if (_started)
                return Initialization;

            _started = true;

            Initialization = InitializeInternalAsync(user, isImpersonating, impersonationData);

            return Initialization;
        }

        private Task InitializeInternalAsync(AppUser user, bool isImpersonating, AppUser impersonationData)
        {
            if (Loaded) return Task.CompletedTask;

            // OFFLINE: roles come from the local user; employee id/dept come from
            // LocalUserProvider instead of the SDPBC employee web service.
            var roleNames = user.Roles.ToHashSet();

            EmployeeId = _localUser.EmployeeId;
            DeptId = _localUser.DeptId;
            EmployeeDisplayName = $"{user.FirstName} {user.LastName}".Trim();
            EmployeeEmail = user.Email;
            EmployeeSam = user.GlobalID;

            // No impersonation locally.
            HistoryCreatedByEmployeeId = EmployeeId;
            ImpersonatedEmployeeId = null;
            IsImpersonating = false;

            IsSuperAdmin = roleNames.Contains("SuperAdmin");
            IsAdmin = roleNames.Contains("Admin") || roleNames.Contains("SuperAdmin");
            ReadOnly = roleNames.Contains("Read Only");

            Loaded = true;
            NotifyStateChanged();

            return Task.CompletedTask;
        }

        #endregion

        #region Roles / Authorization
        public string EmployeeId { get; set; }
        public string DeptId { get; set; }
        public string EmployeeEmail { get; set; }
        public string EmployeeSam { get; set; }
        public string EmployeeDisplayName { get; set; }

        public string HistoryCreatedByEmployeeId { get; private set; }
        public string? ImpersonatedEmployeeId { get; private set; }
        public bool IsImpersonating { get; private set; }

        public bool IsAdmin { get; private set; }
        public bool IsSuperAdmin { get; private set; }
        public bool IsTech { get; private set; }
        public bool IsSuperIntendant { get; private set; }
        public bool IsCoordinator { get; private set; }
        public bool ReadOnly { get; private set; }
        public bool NoDepts { get; private set; }
        public bool NoAccess { get; private set; }

        public string AppendImpersonationNote(string? notes)
        {
            var normalizedNotes = notes?.Trim() ?? string.Empty;

            if (!IsImpersonating || string.IsNullOrWhiteSpace(ImpersonatedEmployeeId))
                return normalizedNotes;

            var impersonationNote = $"Impersonation({ImpersonatedEmployeeId})";

            return string.IsNullOrWhiteSpace(normalizedNotes)
                ? impersonationNote
                : $"{normalizedNotes} | {impersonationNote}";
        }

        #endregion

        #region UI / Layout State

        private bool _loaded;
        public bool Loaded
        {
            get => _loaded;
            private set
            {
                _loaded = value;
                NotifyStateChanged();
            }
        }

        private string? _pageTitle;
        public string? PageTitle
        {
            get => _pageTitle;
            set
            {
                if (_pageTitle == value) return;
                _pageTitle = value;
                NotifyStateChanged();
            }
        }

        private bool _deptDropDownVisible;
        public bool DeptDropDownVisible
        {
            get => _deptDropDownVisible;
            set
            {
                if (_deptDropDownVisible == value) return;
                _deptDropDownVisible = value;
                NotifyStateChanged();
            }
        }

        #endregion

        #region Theme

        private bool _isDarkMode;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (_isDarkMode == value) return;
                _isDarkMode = value;
                NotifyStateChanged();
            }
        }

        private string? _selectedTheme;

        /// <summary>
        /// Gets or sets the key of the currently selected MudBlazor theme.
        /// </summary>
        public string? SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme == value) return;
                _selectedTheme = value;
                NotifyStateChanged();
            }
        }

        public Dictionary<string, MudTheme> Themes { get; } = new();

        #endregion

        #region Overlay

        public bool OverlayVisible { get; private set; }
        public bool OverlayDialogVisible { get; private set; }

        public void ShowOverlay(bool dialog = false)
        {
            if (dialog)
                OverlayDialogVisible = true;
            else
                OverlayVisible = true;

            NotifyStateChanged();
        }


        /// <summary>
        /// Displays an overlay while executing an asynchronous operation.
        /// Automatically hides the overlay on completion or error.
        /// </summary>
        public async Task<bool> ShowOverlayAsync(
            Func<Task<bool>> operation,
            bool dialog = false)
        {
            ShowOverlay(dialog);

            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                return false;
            }
            finally
            {
                HideOverlay();
            }
        }

        public void HideOverlay()
        {
            OverlayVisible = false;
            OverlayDialogVisible = false;
            NotifyStateChanged();
        }

        #endregion

        #region State Notification

        public event System.Action? OnChange;

        private void NotifyStateChanged() => OnChange?.Invoke();

        #endregion
    }
}