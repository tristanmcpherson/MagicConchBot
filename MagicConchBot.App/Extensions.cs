using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MagicConchBot.App {
	public static class Extensions {
		public static string Combine(this string uri1, string uri2) => $"{uri1.TrimEnd('/')}/{uri2.TrimStart('/')}";

	}
}
