using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace SlimVML.Loader
{
    public static class SlimVMLLoader
    {
        private const string CONFIG_FILE_NAME = "SilmVML.cfg";

        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("SlimVML");

        private static readonly ConfigFile Config =
            new(Path.Combine(Paths.ConfigPath, CONFIG_FILE_NAME), true);

        private static readonly ConfigEntry<string> ModFolder = Config.Bind("General", "ModFolder",
            "$(GameRoot)/InSlimVML/Mods",
            new StringBuilder()
                .AppendLine("Folder from which to load all DLLs. Supports following template variables:")
                .AppendLine("$(BepInExRoot) - BepInEx folder")
                .AppendLine("$(GameRoot) - Game root folder")
                .ToString());

        private static readonly ConfigEntry<string> IgnoredMods = Config.Bind("General", "IgnoredMods", "0Harmony.dll",
            "List of ignored DLLs (comma separated)");

        private static readonly Dictionary<string, string> PathTemplates = new()
        {
            ["BepInExRoot"] = Paths.BepInExRootPath,
            ["GameRoot"] = Paths.GameRootPath
        };

        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        private static string ExpandPath(string template)
        {
            return Path.GetFullPath(PathTemplates.Aggregate(template,
                (current, kv) => current.Replace($"$({kv.Key})", kv.Value)));
        }

        public static void Patch(AssemblyDefinition ad)
        {
        }

        public static void Finish()
        {
            var loadPath = ExpandPath(ModFolder.Value);

            if (!Directory.Exists(loadPath))
            {
                Logger.LogInfo($"No mod folder exists, creating one at {loadPath}");
                Directory.CreateDirectory(loadPath);
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                if (!Utility.TryParseAssemblyName(args.Name, out var assName))
                    return null;
                return Utility.TryResolveDllAssembly(assName, loadPath, out var ass) ? ass : null;
            };

            Logger.LogInfo($"Loading DLLs from {loadPath}");

            var ignoredSet =
                new HashSet<string>(IgnoredMods.Value.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.InvariantCultureIgnoreCase);

            // Set InSlimVML env vars
            Environment.SetEnvironmentVariable("INSLIM_USING_BEPINEX", "true");
            Environment.SetEnvironmentVariable("INSLIM_DISPLAY_ALTERNATE", "false");

            foreach (var file in Directory.GetFiles(loadPath, "*.dll", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(file);
                if (ignoredSet.Contains(name))
                {
                    Logger.LogWarning($"Skipping {name} since it's in ignore list");
                    continue;
                }

                try
                {
                    var ass = Assembly.LoadFile(file);
                    var main = AccessTools.GetTypesFromAssembly(ass)
                        .SelectMany(a =>
                            a.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        .FirstOrDefault(m => m.Name == "Main");
                    if (main == null)
                        continue;

                    var parameters = main.GetParameters();
                    object[] args;
                    if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string[]))
                    {
                        args = new object[] {new string[0]};
                    }
                    else if (parameters.Length == 0)
                    {
                        args = null;
                    }
                    else
                    {
                        Logger.LogWarning(
                            $"Failed to correctly resolve Main for {name}, found {main.FullDescription()}");
                        continue;
                    }

                    Logger.LogInfo($"Invoking {main.FullDescription()}");
                    main.Invoke(null, args);
                }
                catch (Exception e)
                {
                    Logger.LogWarning($"Failed to load {file} because: ({e.GetType()}) {e.Message}");
                    Logger.LogDebug(e.StackTrace);
                }
            }
        }
    }
}
