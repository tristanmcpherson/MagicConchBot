using System;

namespace MagicConchBot
{
    public static class Extensions
    {
        //public static T Get<T>(this IServiceProvider serviceProvider)
        //{
        //    var type = typeof(T);
        //    var service = serviceProvider.GetService(type);
        //    return (T) service;
        //}

		public static string MergeUrl(this string uri1, string uri2) {
			return $"{uri1.TrimEnd('/')}/{uri2.TrimStart('/')}";
		}
	}
}
