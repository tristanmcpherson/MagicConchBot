using System.Reflection;

namespace MagicConchBot.Helpers
{
    public static class AppHelper
    {
        public static string Version => Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;
    }
}
