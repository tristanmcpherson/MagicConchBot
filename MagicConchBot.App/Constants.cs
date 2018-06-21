using System;
using System.Runtime.InteropServices;

namespace MagicConchBot.App {
	public class Constants {
		public const string LocalPathVariable = "LOCAL_PATH";
		public const string HostNameVariable = "HOST_NAME";


		public static string LocalPath => Environment.GetEnvironmentVariable(LocalPathVariable) 
			?? (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\files" : "/files");
		public static string HostName => Environment.GetEnvironmentVariable(HostNameVariable)
			?? "http://magicconchbot.com/";
	}
}
