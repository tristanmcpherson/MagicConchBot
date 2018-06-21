using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace MagicConchBot.App {
	public static class Program {
		public static void Main(string[] args) {
			BuildWebHost(args).Run();
		}

		public static IWebHost BuildWebHost(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseKestrel()
				.UseStartup<Startup>()
				.Build();
	}
}