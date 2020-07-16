using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Shard.Scripting
{
    internal static class ScriptCommand
    {
        private static readonly Dictionary<string, MethodInfo> commands = new Dictionary<string, MethodInfo>();

        static ScriptCommand()
        {
            foreach (var method in typeof(ScriptCommand).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Where(m => m.GetCustomAttribute<CommandNameAttribute>() != null))
            {
                commands[method.GetCustomAttribute<CommandNameAttribute>().Name] = method;
            }
        }

        internal static bool HasCommand(string name) => commands.ContainsKey(name);

        internal static object Execute(string name, List<object> args)
        {
            var command = commands[name];
            AssertArgumentCount(name, command.GetParameters().Length - 1, args.Count - 1);
            return command.Invoke(null, args.ToArray());
        }

        [CommandName("trim")]
        internal static string Trim(string source) => source.Trim();

        [CommandName("replace")]
        internal static string Replace(string source, string target, string replacement) => source.Replace(target, replacement);

        private static void AssertArgumentCount(string method, int expected, int actual)
        {
            if (expected != actual)
            {
                throw new ArgumentException($"Method {method} expected {expected} argument(s), found {actual}");
            }
        }
    }

    internal sealed class CommandNameAttribute : Attribute
    {
        public string Name { get; set; }

        public CommandNameAttribute(string name)
        {
            Name = name;
        }
    }
}