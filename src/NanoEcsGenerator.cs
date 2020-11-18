namespace NanoEcs.Generator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Collections.Generic;
    using System.Text;
    using System.Diagnostics;
    using System.Threading;
    using Extensions;
    using System.Text.RegularExpressions;

    public class NanoEcsGenerator : IDisposable
    {
        #region Constructor
        public NanoEcsGenerator(GenerationSettings settings)
        {
            this.settings = settings;
        }
        #endregion

        #region Settings
        GenerationSettings settings;
        #endregion

        #region Tags
        string GenaratedHeader
        {
            get
            {
                return File.ReadAllText(settings.CodeSnippets.GeneratedHeader);
            }
        }

        public const string NanoECSNameSpace = "NanoEcs";

        public const string ComponentTitlePrefix = "Component";
        public const string ComponentBaseClassName = "ComponentEcs";
        public const string UniqueAttibute = "Unique";
        public const string ReactiveAttribute = "Reactive";

        const string ComponentNameTag = "*ComponentName*";
        const string ContextNameTag = "*Context*";
        const string ComponentParametersTag = "*ComponentFields*";
        const string AddingComponentsSequenceTag = "*AddingComponentsSequence*";
        const string ClearingActionsSequenceTag = "*ClearingActionsSequence*";
        const string ComponentEnumValuesTag = "*ComponentEnumValues*";
        const string FieldPascalCaseTag = "*FieldPascalCase*";
        const string FieldCamelCaseTag = "*FieldCamelCase*";
        const string FieldCamelCase_SetTag = "*FieldCamelCase_Set*";
        const string FieldTypeTag = "*FieldType*";
        const string FieldIdTag = "*FieldId*";

        const string ComponentReactiveBlock = "@ReactiveProperty@";
        const string ComponentIndexesSequenceTag = "@ComponentIndexesSequence@";
        const string ComponentNamesSequenceTag = "@ComponentNamesSequence@";
        const string ComponentTypesSequenceTag = "@ComponentTypesSequence@";
        const string ContextUniqueComponentsSequenceTag = "@ContextUniqueComponentsSequence@";


        #endregion

        #region State
        GenerationState state;
        #endregion

        #region API
        public void Generate()
        {
            state = new GenerationState();

            var sw = new Stopwatch();
            sw.Start();

            var cache = CacheGeneratedFolder();

            state.Contexts = settings.GetContexts;

            if (state.Contexts.Count == 0)
            {
                Console.WriteLine("No context found. Please open your NanoECS Settings and specify at least one context to proceed generation.");
                return;
            }

            if (settings.ComponentsFolderPath == string.Empty || settings.ComponentsFolderPath == "" || settings.ComponentsFolderPath == null)
            {
                Console.WriteLine("Components folder is not found. Please put the folder named \"Components\" in your source folder");
                return;
            }


            var componentsPathes = IOExtensions.Searcher.GetDirectories(settings.ComponentsFolderPath)
                .SelectMany(DirectoryPath => Directory.GetFiles(DirectoryPath))
                .Where(x => x.Substring(x.Length - 3) == ".cs");

            if (componentsPathes.Count() == 0)
            {
                Console.WriteLine("No single component was found. Component-generation wont be triggered");
            }

            if (!CheckIfComponentsAreValid(componentsPathes))
            {
                return;
            }

            Console.WriteLine("Generation in progress. Please wait...");

            var cleanResult = CleanGeneratedFolder();

            if (!cleanResult)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Files are locked by another process. Generation cancelled");
                Console.ResetColor();
                Console.Write(Format.NewLine() + "Press G to generate" + Format.NewLine());
                return;
            }

            foreach (var componentPath in componentsPathes)
            {
                var text = File.ReadAllText(componentPath);
                var parsedComponent = ParseComponent(text);
                state.ParsedComponents.Add(parsedComponent);
            }

            HandleNoRelativeComponentsCase(state.ParsedComponents);

            foreach (var component in state.ParsedComponents)
            {
                ExtendComponentFieldTypes(component);

                GenerateComponentExtensions(component);
                GenerateComponentReactives(component);
                GenerateGroupBuilders(component);
            }

            CollectOveralUsings();

            GenerateGroups();

            GenerateComponentsMap(state.ParsedComponents);

            GenerateContextFiles(state.Contexts, state.ParsedComponents);

            WriteGenerationFiles(state.GenerationResults);

            sw.Stop();
            string.Format("{0} extensions (from {1} components) are successfully generated in {2} ms", state.ParsedComponents.Count * 4, state.ParsedComponents.Count, sw.Elapsed.Milliseconds).WriteToColsole(ConsoleColor.Green);
        }

        public GenerationState GetLastState() => state;

        public void Dispose()
        {
        }
        #endregion

        #region Internal

        void HandleNoRelativeComponentsCase(List<ParsedComponent> parsedComponents)
        {
            if (parsedComponents.Count == 0)
            {
                foreach (var context in state.Contexts)
                {
                    parsedComponents.Add(NoneComponent(context));
                }
            }
            else
            {
                var pool = new List<ParsedComponent>();

                foreach (var context in state.Contexts)
                {
                    if (parsedComponents.Find(component => component.Contexts.Contains(context)) == null)
                    {
                        if (pool.Find(item => item.Contexts.Contains(context)) == null)
                        {
                            pool.Add(NoneComponent(context));
                        }
                    }
                }

                parsedComponents.AddRange(pool);
            }
        }

        ParsedComponent NoneComponent(string context)
        {
            return new ParsedComponent()
            {
                ComponentName = "None",
                Contexts = state.Contexts.ToArray(),
                Fields = new FieldParsed[] { }
            };
        }

        void CollectOveralUsings()
        {
            state.OveralUsings.Add($"using {NanoECSNameSpace}; {Format.NewLine()}");

            foreach (var component in state.ParsedComponents)
            {
                if (component.Usings != string.Empty)
                {
                    state.OveralUsings.Add(component.Usings);
                }
            }
        }


        string DefaultContext
        {
            get
            {
                return state.Contexts[0];
            }
        }

        TagKeys GroupingFilter
        {
            get { return TagKeys.Context; }
        }

        void Generate(string snippet)
        {
            foreach (var component in state.ParsedComponents)
            {
                
            }
        }

        void GenerateComponentsMap(List<ParsedComponent> parsedComponents)
        {
            var lookup = parsedComponents.SelectMany(x => x.Contexts, (parsedComponent, contextName)
            => new { parsedComponent, contextName })
            .GroupBy(component => component.contextName, c => c.parsedComponent, (contextName, values) => new { ContextName = contextName, Values = values });

            foreach (var contextGroup in lookup)
            {
                var components = contextGroup.Values.ToList();
                var mapTitle = contextGroup.ContextName;
                var componentIndexesSequence = components.Select(v => string.Format("public const int {0} = {1};", v.ComponentName, components.IndexOf(v)))
                    .Aggregate((a, b) => a + Format.NewLine(1) + b);
                var componentNamesSequence = components.Select(v => string.Format("\"{0}\"", v.ComponentName))
                    .Aggregate((a, b) => a + "," + Format.NewLine(2) + b);
                var componentTypesSequence = components.Select(v => string.Format("typeof({0}Component)", v.ComponentName))
                    .Aggregate((a, b) => a + "," + Format.NewLine(2) + b);

                var snippet = File.ReadAllText(settings.CodeSnippets.ComponentMap);

                var ComponentMapFile = snippet
                    .Replace(ComponentNameTag, mapTitle)
                    .Replace(ComponentIndexesSequenceTag, componentIndexesSequence)
                    .Replace(ComponentNamesSequenceTag, componentNamesSequence)
                    .Replace(ComponentTypesSequenceTag, componentTypesSequence);

                SaveGeneration(ComponentMapFile, new Tag(TagKeys.Context, mapTitle), new Tag(TagKeys.GenerationType, GenerationTypes.ComponentMap));
            }
        }

        void ExtendComponentFieldTypes(ParsedComponent component)
        {
            component.Fields.ToList()
                .Where(field => field.Type.Contains("List")).ToList().ForEach(x => x.Type = string.Format("NanoList<{0}>", x.Type.Extract('<', '>')));
        }

        void RevertGeneratedFolder(IEnumerable<FileData> cache)
        {
            if (cache != null)
            {
                cache.ToList().ForEach(file => File.WriteAllText(file.Path, file.Content));
            }
        }

        IEnumerable<FileData> CacheGeneratedFolder()
        {
            DirectoryInfo di = new DirectoryInfo(settings.GeneratedFolderPath);
            if (di.GetFiles().Count() == 0) return null;
            return di.GetFiles().Select(file => new FileData() { Path = file.FullName, Content = File.ReadAllText(file.FullName) });
        }

        bool CheckIfComponentsAreValid(IEnumerable<string> componentsPathes)
        {

            bool result = true;
            bool OnesFailed = false;

            foreach (var componentPath in componentsPathes)
            {
                var text = File.ReadAllText(componentPath);

                var check = PreCheckComponent(text);

                if (!OnesFailed)
                {
                    if (!check.Result)
                    {
                        OnesFailed = true;
                        result = false;
                    }
                }

                string log = string.Empty;
                ConsoleColor color = ConsoleColor.White;
                check.FileName = componentPath.Split('/', '\\').Last();
                if (!check.Result)
                {
                    color = ConsoleColor.Red;
                    log = string.Format("Failed! {0} Error in file {1}: {2} {3}", Environment.NewLine, check.FileName, Environment.NewLine, check.Commentary);
                }
                else
                {
                    color = ConsoleColor.DarkGreen;
                    log = string.Format("{0} is valid", check.FileName);
                }

                log.WriteToColsole(color);
            }

            return result;
        }

        GenerationCheckResult PreCheckComponent(string componentText)
        {
            //if (!componentText.Contains(ComponentBaseClassName))
            //    return GenerationCheckResult.Failed(string.Format("Your component is not inherited from {0}.", ComponentBaseClassName));

            //if (!componentText.Contains("partial"))
            //    return GenerationCheckResult.Failed("\"partial\" modifier is missing.");

            //if (componentText.Split('{', '}')[1].Contains("public"))
            //    return GenerationCheckResult.Failed("public modifier found. Please use private fields in your component.");

            //if (componentText.Split('{', '}')[0].Contains("public"))
            //    return GenerationCheckResult.Failed("public modifier in component found. Please dont use modifiers in your components.");

            return GenerationCheckResult.Success;
        }

        void GenerateComponentReactives(ParsedComponent parsedComponent)
        {
            if (!parsedComponent.HasFields) return;

            string result = MayBakeUsings() ? parsedComponent.Usings : string.Empty;

            var snippet = File.ReadAllText(settings.CodeSnippets.ComponentReactive);

            var subSnippet = snippet.Split(new string[] { ComponentReactiveBlock }, StringSplitOptions.None)[1];

            string members = "";

            if (parsedComponent.IsReactive())
            {
                members = parsedComponent.Fields
                    .Select(field => subSnippet
                            .Replace(FieldPascalCaseTag, field.Name.VariateFirstChar())
                            .Replace(FieldCamelCaseTag, field.Name)
                            .Replace(FieldCamelCase_SetTag, field.Name == "value" ? "this." + field.Name : field.Name)
                            .Replace(FieldTypeTag, field.Type)
                            .Replace(FieldIdTag, field.Index.ToString()))
                    .Aggregate((x, y) => x + Format.NewLine(1) + y);
            } else
            {
                members = parsedComponent.Fields
                    .Select(field => $"public {field.Type} {field.Name.FirstCharToUpper()};")
                    .Aggregate((x, y) => x + Format.NewLine(1) + y);
            }

            var componentReactive = snippet
                .Replace(ComponentNameTag, parsedComponent.ComponentName)
                .Replace(subSnippet, members)
                .Replace(ComponentReactiveBlock, "");

            if (MayBakeUsings()) componentReactive = parsedComponent.Usings + componentReactive;

            SaveGeneration(componentReactive, 
                new Tag(TagKeys.Component, parsedComponent.ComponentName),
                new Tag(TagKeys.GenerationType, GenerationTypes.Reactive));
        }

        bool MayBakeUsings()
        {
            return GroupingFilter == TagKeys.Component;
        }

        void GenerateGroups()
        {
            var group_snippet = File.ReadAllText(settings.CodeSnippets.Group);

            foreach (var context in state.Contexts)
            {
                var group_file = group_snippet
                    .Replace(ContextNameTag, context);

                SaveGeneration(group_file,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Group));
            }
        }

        void GenerateGroupBuilders(ParsedComponent parsedComponent)
        {
            var group_builder_snippet = File.ReadAllText(settings.CodeSnippets.Group_Builder);

            foreach (var context in parsedComponent.Contexts)
            {
                var group_builderFile = group_builder_snippet
                    .Replace(ContextNameTag, context)
                    .Replace(ComponentNameTag, parsedComponent.ComponentName);

                SaveGeneration(group_builderFile,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.Component, parsedComponent.ComponentName),
                    new Tag(TagKeys.GenerationType, GenerationTypes.GroupBuilder));

                if (!parsedComponent.HasFields || !parsedComponent.IsReactive()) continue;

                var snippetPath = parsedComponent.Fields.Length > 1 ? settings.CodeSnippets.Collector : settings.CodeSnippets.CollectorFieldless;

                var collector_snippet = File.ReadAllText(snippetPath);

                var componentEnumValues = parsedComponent.Fields
                    .Select(field => field.Name)
                    .Aggregate((x, y) => x + "," + Format.NewLine(1) + y);

                var collector_File = collector_snippet
                    .Replace(ComponentNameTag, parsedComponent.ComponentName)
                    .Replace(ComponentEnumValuesTag, componentEnumValues)
                    .Replace(ContextNameTag, context);

                SaveGeneration(collector_File,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.Component, parsedComponent.ComponentName),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Collector));
            }
        }

        bool CleanGeneratedFolder()
        {
            DirectoryInfo di = new DirectoryInfo(settings.GeneratedFolderPath);

            foreach (FileInfo file in di.GetFiles())
            {
                if (!file.IsFileLocked())
                {
                    file.Delete();
                }
                else
                {
                    return false;
                }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }

            Console.WriteLine("Generated folder is cleaned");
            return true;
        }

        void GenerateContextFiles(List<string> contexts, List<ParsedComponent> parsedComponents)
        {

            var contextsAccumulatorSnippet =
                @"public partial class Contexts : IContext
{
    public Contexts()
    {
        values = new IContext[] { *Contexts* };
    }

	IContext[] values;

    public void HandleDalayedOperations()
    {
        for (int i = 0; i < values.Length; i++)
        {
            values[i].HandleDalayedOperations();
        }
    }
}";
            var contextAccumulator_file = contextsAccumulatorSnippet
                .Replace("*Contexts*", contexts.Aggregate((a, b) => a + ", " + b));

            SaveGeneration(contextAccumulator_file,
                new Tag(TagKeys.GenerationType, GenerationTypes.Contexts));

            foreach (var context in contexts)
            {
                var snippet_Contexts = File.ReadAllText(settings.CodeSnippets.Contexts);
                var snippet_Context = File.ReadAllText(settings.CodeSnippets.Context);
                var snippet_ContextAddExtension = File.ReadAllText(settings.CodeSnippets.ContextAddExtension);
                var snippet_Entity = File.ReadAllText(settings.CodeSnippets.Entity_ContextChild);
                var snippet_Attribute = File.ReadAllText(settings.CodeSnippets.Attribute);

                var contexts_file = snippet_Contexts
                    .Replace(ContextNameTag, context);

                var uniqueComponents = parsedComponents
                    .Where(x => x.Contexts.Contains(context))
                    .Where(x => x.IsUnique);

                var uniqueComponentsSequence = uniqueComponents.Count() > 0 ? uniqueComponents
                    .Select(x => 
                    {
                        return string.Format("public {0}Component {0};", x.ComponentName) + Format.NewLine() +
                        HandleAddMethod(x, snippet_ContextAddExtension)
                        .Replace(ComponentNameTag, x.ComponentName)
                        .Replace(ContextNameTag, context);
                    })
                    .Aggregate((a, b) => a + Format.NewLine(1) + b) : string.Empty;

                var context_file = snippet_Context
                    .Replace(ContextNameTag, context)
                    .Replace(ContextUniqueComponentsSequenceTag, uniqueComponentsSequence);

                var entity_file = snippet_Entity
                    .Replace(ContextNameTag, context);

                var attribute_file = snippet_Attribute
                    .Replace(ContextNameTag, context);

                SaveGeneration(contexts_file,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Contexts));

                SaveGeneration(context_file,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Contexts));

                SaveGeneration(entity_file,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Entity));

                SaveGeneration(attribute_file,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.Attribute));
            }
        }

        void GenerateComponentExtensions(ParsedComponent parsedComponent)
        {
            foreach (var context in parsedComponent.Contexts)
            {

                string result = MayBakeUsings() ? parsedComponent.Usings : string.Empty;

                // Header
                var snippetHeader = File.ReadAllText(settings.CodeSnippets.ComponentHeader);
                var componentHeader = snippetHeader
                    .Replace(ComponentNameTag, parsedComponent.ComponentName);

                result += Format.NewLine() + componentHeader;

                // Get Property
                var snippet_Get = File.ReadAllText(settings.CodeSnippets.ComponentExtensions_Get_Property);
                var snippet_Fieldless = File.ReadAllText(settings.CodeSnippets.ComponentExtensions_Fieldless);

                var snippet = parsedComponent.HasFields ? snippet_Get : snippet_Fieldless;

                var get_extension =
                    snippet
                    .Replace(ComponentNameTag, parsedComponent.ComponentName)
                    .Replace(ContextNameTag, context);

                result += Format.NewLine() + get_extension;

                if (parsedComponent.HasFields)
                {
                    // Add Method
                    var snippet_Add = File.ReadAllText(settings.CodeSnippets.ComponentExtensions_Add_Method);

                    string add_extension = snippet_Add;

                    add_extension = HandleAddMethod(parsedComponent, add_extension);

                    var clearActionsSequence = parsedComponent.Fields
                        .Select(x => string.Format("{0}.On{1}Change = null;", parsedComponent.ComponentName, x.Name.VariateFirstChar()))
                        .Aggregate((x, y) => x + Format.NewLine(2) + y);

                    var NanoListFieldsInitialization = parsedComponent.Fields.ToList()
                        .Where(field => field.Type.Contains("NanoList"))
                        .Select(y =>
                        Format.NewLine() + "c." +
                        (parsedComponent.IsReactive() ? y.Name.VariateFirstChar() : y.Name.FirstCharToUpper())
                        + ".Initialize(c._InternalOnValueChange, " + y.Index + ");" +
                        Format.NewLine() + "if (c." + y.Name.VariateFirstChar() + ".Count > 0) { c._InternalOnValueChange(" + y.Index + "); }" +
                        Format.NewLine()).ToList();

                    add_extension = add_extension
                        .Replace(ClearingActionsSequenceTag, parsedComponent.IsReactive() ? clearActionsSequence : "")
                        .Replace(ComponentNameTag, parsedComponent.ComponentName)
                        .Replace(ContextNameTag, context);

                    if (NanoListFieldsInitialization.Count > 0)
                    {
                        var marker = "return this;";
                        NanoListFieldsInitialization.ForEach(x => add_extension = add_extension.Insert(add_extension.IndexOf(marker), x));
                    }

                    result += Format.NewLine() + add_extension;
                }

                result = GenaratedHeader + result;

                var prefix = state.Contexts.Count > 1 ? context : string.Empty;

                SaveGeneration(result,
                    new Tag(TagKeys.Context, context),
                    new Tag(TagKeys.GenerationType, GenerationTypes.AddRemove));
            }
        }

        static string HandleAddMethod(ParsedComponent parsedComponent, string add_extension)
        {
            var parameters = parsedComponent.Fields
                .Select(x => x.Type + " " + x.Name)
                .Aggregate((x, y) => x + ", " + y);

            var fieldSequence = parsedComponent.Fields
                .Select(x => "c." + (parsedComponent.IsReactive() ? x.Name.VariateFirstChar() : x.Name.FirstCharToUpper()) + " = " + x.Name + ";")
                .Aggregate((x, y) => x + Format.NewLine(2) + y);

            add_extension = add_extension
                .Replace(ComponentParametersTag, parameters)
                .Replace(AddingComponentsSequenceTag, fieldSequence);
            return add_extension;
        }

        ParsedComponent ParseComponent(string component)
        {
            // remove all access modifiers
            component = component.ReplaceAll("", "private", "protected", "internal", "public");

            if (component.Contains("namespace"))
            {
                var startID = component.IndexOf("namespace");
                var fromNamespaceComponent = component.Substring(startID);
                var OpeningBracketID = fromNamespaceComponent.IndexOf("{");
                var ClosingGracketID = fromNamespaceComponent.FindMatchingBracket(OpeningBracketID);

                component = component.Substring(OpeningBracketID + startID + 1, ClosingGracketID - OpeningBracketID - 1);
            }

            var temp = component
                .AslineArray()
                .Where(x => x.Contains("class"))
                .FirstOrDefault();
            var id = temp.IndexOf("class");
            var temp2 = temp
                .Substring(id);

            var name = temp2
                .Split(' ')
                [1];

            var attributesTextRaw = component
                .AslineArray()
                .Where(line => line.Contains("[") || line.Contains("]"));

            var attributesText = attributesTextRaw.Count() > 0 ? 
                attributesTextRaw
                .Aggregate((a, b) => a + b)
                .RemoveWhitespace() : null;

            var attributes = attributesText != null ?
                attributesText.ReplaceAll("", "[", "]")
                .Split(',')
                .Select(x =>
                {
                    if (x.Contains("."))
                    {
                        var parts = x.Split('.');
                        return parts[parts.Length - 1];
                    }
                    return x;
                })
                : new string[] { DefaultContext };

            var isUnique = false;

            if (attributes.Contains(UniqueAttibute))
            {
                attributes = attributes.Where(x => x != UniqueAttibute);
                isUnique = true;
            }

            var usingsLines = component
                .AslineArray()
                .Where(x => x.Contains("using"));

            var usings = usingsLines.Count() > 0 ? usingsLines.Aggregate((a, b) => a + Format.NewLine() + b) : string.Empty;

            var hasFields = component
                .Split(new string[] { "{", "}" }, StringSplitOptions.None)[1]
                .RemoveWhitespace() != "";

            var fields = component
               .Split(new string[] { "{", "}" }, StringSplitOptions.None)[1]
               .Split(new string[] { ";" }, StringSplitOptions.None)
               .ToList();

            var result = new ParsedComponent();
            result.ComponentName = name;
            result.Attributes = attributes.ToArray();
            result.Contexts = attributes.Where(a => state.Contexts.Contains(a)).ToArray();
            result.IsUnique = isUnique;
            result.HasFields = hasFields;
            result.Usings = usings;

            if (fields.Any())
            {
                var r = fields
                   .Select(x => x.Split(' ').Where(y => !(y == string.Empty || y == null || y.Contains(" "))).Skip(1).ToList()).ToList();
                r = r.Take(r.Count - 1).ToList();
                result.Fields = r.Select(fieldPair => new FieldParsed() { Index = r.IndexOf(fieldPair), Type = fieldPair[0], Name = fieldPair[1] }).ToArray();
            }

            if (settings.ForseReactiveComponents)
            {
                result.ForseReactive = true;
            }
            return result;
        }

        public void SaveGeneration(string content, params Tag[] tags)
        {
            state.GenerationResults.Add(new GenerationResult(content, tags));
        }

        void WriteGenerationFiles(List<GenerationResult> generationResults)
        {
            var corePath = settings.GeneratedFolderPath;
            var ext = ".cs";

            var lookup = generationResults
                .ToLookup(
                x => x.Tags
                    .Where(z => z.Key == GroupingFilter)
                    .Select(t => t.Value)
                    .FirstOrDefault(),
                x => x);

            var usings = state.OveralUsings.Count == 0 ? "" : state.OveralUsings.Aggregate((a, b) => a + Format.NewLine() + b);

            foreach (var item in lookup)
            {
                switch (GroupingFilter)
                {
                    case TagKeys.Context:
                        var content = usings + GenaratedHeader + Format.NewLine() + item.Select(x => x.Content).Aggregate((a, b) => a + Format.NewLine() + b);
                        var title = item.Key == null ? "Common" : item.Key;
                        File.WriteAllText(corePath + title + ext, content);
                        break;
                    case TagKeys.Component:

                        break;
                    case TagKeys.GenerationType:
                        var content2 = usings + GenaratedHeader + Format.NewLine() + item.Select(x => x.Content).Aggregate((a, b) => a + Format.NewLine() + b);
                        var title2 = item.Key == null ? "Common" : item.Key;
                        File.WriteAllText(corePath + title2 + ext, content2);
                        break;
                    default:
                        break;
                }
            }

            if (settings.IsFullStateSerializationEnabled)
            {
                state.GenerationResults = null;
                var serializedState = Newtonsoft.Json.JsonConvert.SerializeObject(state);
                File.WriteAllText(corePath + "GenerationState.json", serializedState);
            }
        }

        #endregion

        #region DataContainers

        public class GenerationState
        {
            public List<string> Contexts;
            public HashSet<string> OveralUsings = new HashSet<string>();
            
            public List<ParsedComponent> ParsedComponents = new List<ParsedComponent>();

            public List<GenerationResult> GenerationResults = new List<GenerationResult>();
        }

        [Serializable]
        public class Tag
        {
            public TagKeys Key;
            public string Value;

            public Tag(TagKeys key, string value)
            {
                Key = key;
                Value = value;
            }
        }

        public enum TagKeys
        {
            Context,
            Component,
            GenerationType
        }

        public static class GenerationTypes
        {
            public const string Reactive = "Reactive";
            public const string AddRemove = "AddRemove";
            public const string Group = "Group";
            public const string GroupBuilder = "GroupBuilder";
            public const string Collector = "Collector";
            public const string Contexts = "Contexts";
            public const string Attribute = "Attribute";
            public const string Entity = "Entity";
            public const string ComponentMap = "ComponentMap";
        }

        class FileData
        {
            public string Path;
            public string Content;
        }

        [Serializable]
        public class FieldParsed
        {
            public string Type;
            public string Name;
            public int Index;
        }

        public class GenerationSettings
        {
            public string userSettings;

            public GenerationSettings(string userSettings)
            {
                this.userSettings = userSettings;
                IsFullStateSerializationEnabled = true;
            }
            public bool ForseReactiveComponents
            {
                get
                {
                    var result = userSettings.GetLines(" ForseReactiveComponents:").FirstOrDefault();
                    if (result == null) return false;
                    return result.Contains("1");
                }
            }
            public bool IsFullStateSerializationEnabled { get; private set; }
            public string ProjectRootPath;
            public string SettingsPath;
            public CodeSnippets CodeSnippets;
            public bool TriggerGenerationOnSourceChange
            {
                get
                {
                    var result = userSettings.GetLines(" TriggerGenerationOnSourceChange:").FirstOrDefault();
                    if (result == null) return false; 
                    return result.Contains("1");
                }
            }

            public List<string> GetContexts
            {
                get
                {
                    return userSettings.GetLines("- Name:");
                }
            }
            public string GeneratedFolderPath
            {
                get
                {
                    return ProjectRootPath + userSettings.GetLines(" GeneratedFolderPath:").FirstOrDefault().Replace("/", "\\");
                }
            }
            public string SourceFolderPath
            {
                get
                {
                    return ProjectRootPath + userSettings.GetLines(" SourceFolderPath:").FirstOrDefault().Replace("/", "\\");
                }
            }
            public string ComponentsFolderPath
            {
                get
                {
                    var reg = new Regex(@"comp", RegexOptions.IgnoreCase);
                    return IOExtensions.Searcher.GetDirectories(SourceFolderPath)
                        .Where(directoryName => reg.Match(directoryName).Success)
                        .FirstOrDefault();
                }
            }

        }

        public class CodeSnippets
        {
            public string GeneratedHeader;
            public string ComponentHeader;
            public string ComponentExtensions_Add_Method;
            public string ComponentExtensions_Get_Property;
            public string ComponentExtensions_Fieldless;
            public string Attribute;
            public string Contexts;
            public string Entity_ContextChild;
            public string ComponentReactive;
            public string Collector;
            public string CollectorFieldless;
            public string Group;
            public string Group_Builder;
            public string ComponentMap;
            public string Context;
            public string ContextAddExtension;
        }

        public class GenerationResult
        {
            public string Content;
            public Tag[] Tags;

            public GenerationResult(string content, Tag[] tags)
            {
                Content = content;
                Tags = tags;
            }
        }

        [Serializable]
        public class ParsedComponent
        {
            public string ComponentName;
            public string[] Contexts;
            public string[] Attributes;
            public bool IsUnique;
            public bool HasFields;
            public FieldParsed[] Fields;
            public string Usings;
            public bool ForseReactive;
        }

        public class GenerationCheckResult
        {
            public string Commentary;
            public string FileName;
            public bool Result;

            public GenerationCheckResult(string commentary, bool result)
            {
                Commentary = commentary;
                Result = result;
            }

            public GenerationCheckResult()
            {

            }

            public GenerationCheckResult(string commentary, string fileName, bool result)
            {
                Commentary = commentary;
                FileName = fileName;
                Result = result;
            }

            public static GenerationCheckResult Success
            {
                get
                {
                    return new GenerationCheckResult() { Result = true };
                }
            }

            public static GenerationCheckResult Failed(string commentary)
            {
                return new GenerationCheckResult() { Result = false, Commentary = commentary };
            }
        }
        #endregion

    }

}
