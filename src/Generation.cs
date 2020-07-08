using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NanoEcs.Generator.Extensions;

namespace NanoEcs.Generator
{
    class Generation
    {
        static void Main(string[] args)
        {
            var settings = GetSettings();

            if (settings != null)
            {
                DisplayHint();
                if (settings.TriggerGenerationOnSourceChange)
                {
                    Watch(settings.SettingsPath, (o, e) => Generate());
                    Watch(settings.ComponentsFolderPath, (o, e) =>
                    {
                        if (e.ChangeType != WatcherChangeTypes.Created)
                        {
                            Generate();
                        }
                    });
                }
                AwaitForInput();
            }

        }

        static void Generate()
        {
            var settings = GetSettings();
            var generator = new NanoEcsGenerator(settings);
            try
            {
                generator.Generate();

#if SERIALIZE_STATE
                var state = generator.GetLastState();
                state.GenerationResults = null;
                var serializedState = Newtonsoft.Json.JsonConvert.SerializeObject(state);
                File.WriteAllText(settings.GeneratedFolderPath + "GenerationState.json", serializedState);
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine("Generation cancelled. Exception occured: " + ex);
            }
            
        }

        static void HandleInput(ConsoleKey key)
        {
            switch (key)
            {
                case ConsoleKey.G:
                    Generate();
                    DisplayHint();
                    break;
                case ConsoleKey.C:
                    Console.Clear();
                    DisplayHint();
                    break;
                default:
                    break;
            }
        }

        static void Watch(string path, Action<object, FileSystemEventArgs> onChanged)
        {
            FileSystemWatcher watcher = new FileSystemWatcher();
            
            watcher.Path = path;
            watcher.NotifyFilter = NotifyFilters.LastWrite & ~NotifyFilters.CreationTime;
            watcher.IncludeSubdirectories = true;
            watcher.Filter = "*.*";
            watcher.Changed += new FileSystemEventHandler(onChanged);
            watcher.EnableRaisingEvents = true;
        }

        static void AwaitForInput()
        {
            var input = Console.ReadKey(true);
            HandleInput(input.Key);
            AwaitForInput();
        }

        static void DisplayHint()
        {
            Console.WriteLine("");
            Console.WriteLine("\"G\" - to generate");
            Console.WriteLine("\"C\" - to clear the console");

        }

        static NanoEcsGenerator.GenerationSettings GetSettings()
        {
            bool isDebugLocation = false;

            var debugPath = "D:\\Programming\\Projects\\KnifeAway\\Library\\PackageCache\\com.nanory.nanoecs@b9da501fb5ab009d66df8058a563dec4dc9effe2\\Generator\\NanoEcsGenerator.exe";

            var executableLocation = isDebugLocation ? debugPath : System.Reflection.Assembly.GetEntryAssembly().Location;

            var isInPackageFolder = executableLocation.Contains("PackageCache");

            var separator = isInPackageFolder ? "Library\\" : "Assets\\";

            var projectPath = executableLocation
                .Split(new string[] { separator }, StringSplitOptions.None)[0];

            var nanoEcsRootPath = executableLocation
                .Split(new string[] { "Generator\\" }, StringSplitOptions.None)[0];

            var assetsPath = projectPath + "Assets\\";
            
            var snippetsPath = nanoEcsRootPath + "Generator\\Settings\\Snippets\\";

            var settingsPath = assetsPath + "Settings\\";

            var settingsName = "NanoECS Settings";

            var settingsAssetPath = settingsPath + settingsName + ".asset";

            if (File.Exists(settingsAssetPath))
            {
                var settingsAsset = File.ReadAllText(settingsAssetPath);

                var settings = new NanoEcsGenerator.GenerationSettings(settingsAsset);
                settings.CodeSnippets = new NanoEcsGenerator.CodeSnippets();

                settings.SettingsPath = settingsPath;
                settings.ProjectRootPath = assetsPath;
                settings.CodeSnippets.GeneratedHeader = snippetsPath + "GeneratedHeader.txt";
                settings.CodeSnippets.ComponentHeader = snippetsPath + "ComponentHeader.txt";
                settings.CodeSnippets.ComponentExtensions_Get_Property = snippetsPath + "ComponentExtensions_Get_Property.txt";
                settings.CodeSnippets.ComponentExtensions_Fieldless = snippetsPath + "ComponentExtensions_Fieldless.txt";
                settings.CodeSnippets.ComponentExtensions_Add_Method = snippetsPath + "ComponentExtenstions_Add_Method.txt";
                settings.CodeSnippets.ComponentReactive = snippetsPath + "ComponentReactive.txt";
                settings.CodeSnippets.Attribute = snippetsPath + "Attribute.txt";
                settings.CodeSnippets.Contexts = snippetsPath + "Contexts.txt";
                settings.CodeSnippets.Context = snippetsPath + "Context.txt";
                settings.CodeSnippets.ContextAddExtension = snippetsPath + "ContextAddExtension.txt";
                settings.CodeSnippets.Entity_ContextChild = snippetsPath + "Entity_ContextChild.txt";
                settings.CodeSnippets.Collector = snippetsPath + "Collector.txt";
                settings.CodeSnippets.CollectorFieldless = snippetsPath + "CollectorFieldless.txt";
                settings.CodeSnippets.Group = snippetsPath + "Group.txt";
                settings.CodeSnippets.Group_Builder = snippetsPath + "Group_Builder.txt";
                settings.CodeSnippets.ComponentMap = snippetsPath + "ComponentMap.txt";
                return settings;
            }
            else
            {
                Console.WriteLine($"Settings.asset not found on path : {settingsAssetPath}");
                if (!isInPackageFolder)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Make sure that Generator executable is placed in the Package Cash folder");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine($"Press any key to close.");

                var input = Console.ReadKey(true);
            }

            return null;
        }
    }
}
