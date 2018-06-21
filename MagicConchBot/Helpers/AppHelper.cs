using System.Reflection;

namespace MagicConchBotApp.Helpers
{
    public static class AppHelper
    {
        public static string Version => Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;
    }
}
