using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MagicConchBot.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<string> SplitByLength(this string str, int maxLength)
        {
            for (var index = 0; index < str.Length; index += maxLength)
                yield return str.Substring(index, Math.Min(maxLength, str.Length - index));
        }

        public static string Dump(this object target)
        {
            try
            {
                return JsonConvert.SerializeObject(target, new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    Converters =
                    {
                        new StringEnumConverter()
                    }
                });
            }
            catch (JsonSerializationException)
            {
                return target.ToString();
            }
        }
    }
}