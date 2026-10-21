using System.Reflection;

namespace _2026WRKJRNL01.Shared.Services
{
    public class AppVersionService
    {
        public string Version { get; }

        public AppVersionService()
        {
            Version =
                Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? "Unknown";
        }
    }
}
