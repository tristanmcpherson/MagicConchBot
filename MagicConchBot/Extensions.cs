using System;

namespace MagicConchBot
{
    public static class Extensions
    {
        public static T Get<T>(this IServiceProvider serviceProvider)
        {
            var type = typeof(T);
            var service = serviceProvider.GetService(type);
            return (T) service;
        }
    }
}
