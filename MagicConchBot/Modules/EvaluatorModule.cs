using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Helpers;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace MagicConchBot.Modules
{
    public class Globals
    {
        public ConchCommandContext Context;

        public Globals(ConchCommandContext context)
        {
            Context = context;
        }
    }

    public class EvaluatorModule : ModuleBase<ConchCommandContext>
    {
        private static readonly ScriptOptions ScriptOptions;
        private static CancellationTokenSource tokenSource;

        static EvaluatorModule()
        {
            ScriptOptions = ScriptOptions.Default
                .WithImports("System", "System.Diagnostics", "System.Text", "System.Collections.Generic",
                    "System.Linq", "System.Net", nameof(Modules), nameof(Services), "Newtonsoft.Json",
                    "Newtonsoft.Json.Linq")
                .WithReferences(nameof(MagicConchBot), "System.Linq", "Newtonsoft.Json");
        }

        private static readonly string[] BlockedTokens =
        {
            "Process.",
            "Process(",
            "Thread.",
            "Thread(",
            "File.",
            "Directory.",
            "StreamReader(",
            "StreamWriter(",
            "Environment.",
            "WebClient(",
            "HttpClient(",
            "WebRequest.",
            "Activator.CreateInstance",
            "Invoke(",
            "unsafe",
            "DllImport(",
            "Assembly.Load",
            ".DynamicInvoke",
            ".CreateDelegate",
            "Expression.Call",
            ".Compile()",
            ".Emit(",
            "MethodRental."
        };

        [Command("eval")]
        public async Task Eval([Remainder] string code)
        {
            tokenSource?.Cancel();

            tokenSource = new CancellationTokenSource();

            if (BlockedTokens.Any(t => code.Contains(t)))
            {
                await ReplyAsync("Failed. You've used a blocked keyword.");
                return;
            }

            if (!code.Contains("return"))
                code = "return " + code;
            if (!code.EndsWith(";"))
                code = code + ";";

            var cleanCode = "var stopwatch = new Stopwatch();" +
                            "stopwatch.Start();\n" +
                            code.Replace("```cs", "").Replace("```", "") + "\n" +
                            "stopwatch.Stop();";

            var result = await CSharpScript.RunAsync(cleanCode, ScriptOptions, new Globals(Context), typeof(Globals),
                tokenSource.Token);

            var watch = (Stopwatch) result.Variables.First(d => d.Name == "stopwatch").Value;

            watch.Stop();

            if (result.ReturnValue == null)
                throw new NullReferenceException("No return value.");

            var embed = new EmbedBuilder
            {
                Title = result.ReturnValue.GetType().Name,
                Description = $"```json\n{result.ReturnValue.Dump()}```",
                Footer = new EmbedFooterBuilder
                {
                    Text = $"Took {watch.ElapsedMilliseconds}ms"
                },
                Color = new Color(0, 200, 0)
            };

            await ReplyAsync("", false, embed.Build());
        }
    }
}