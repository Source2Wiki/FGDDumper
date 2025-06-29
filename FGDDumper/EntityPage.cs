using Sledge.Formats.GameData.Objects;
using System.Net;
using System.Text.RegularExpressions;
using static FGDDumper.GameFinder;

namespace FGDDumper
{
    public class EntityPage
    {
        public required Game Game { get; init; }
        public required EntityTypeEnum EntityType { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string IconPath { get; init; } = string.Empty;
        public List<Property> Properties { get; init; } = [];
        public List<InputOutput> InputOutputs { get; init; } = [];

        public enum EntityTypeEnum
        {
            Point,
            Mesh
        }

        public string GetText()
        {
            var propertiesString = string.Empty;

            if (Properties.Count > 0)
            {
                propertiesString = 
                    $"""
                    <details open>
                    <summary><h2>Keyvalues</h2></summary>

                    """;

                foreach (var property in Properties)
                {
                    propertiesString += property.GetText();
                }

                propertiesString += "\n </details>";
            }


            var inputsString = string.Empty;
            var outputsString = string.Empty;

            var inputs = InputOutputs.Where(x => x.Type == InputOutput.TypeEnum.Input).ToList();
            var outputs = InputOutputs.Where(x => x.Type == InputOutput.TypeEnum.Output).ToList();

            if (inputs.Count > 0)
            {
                inputsString =
                    $"""
                    <details open>
                    <summary><h2>Inputs</h2></summary>

                    """;

                foreach (var input in inputs)
                {
                    inputsString += input.GetText();
                }

                inputsString += "\n </details>";
            }

            if (outputs.Count > 0)
            {
                outputsString =
                    $"""
                    <details open>
                    <summary><h2>Outputs</h2></summary>

                    """;

                foreach (var output in outputs)
                {
                    outputsString += output.GetText();
                }

                outputsString += "\n </details>";
            }


            string iconText = string.Empty;
            if (!string.IsNullOrEmpty(IconPath))
            {
                var textureImagePath = $"static/img/Entities/{Game.FileSystemName}/{Name}.png";

                if (File.Exists(Path.Combine(FGDDumper.WikiRoot, textureImagePath)))
                {
                    iconText = $"\nimport {Name}Icon from '@site/{textureImagePath}';\n\n" +
                    $"<img src={{{Name}Icon}} alt=\"{Name} icon\" style={{{{height: '80px'}}}} />\n";
                }

            }

            var MD =
            $"""
            ---
            hide_table_of_contents: true
            ---
            {iconText}
            {EntityType} Entity

            {SanitizeInput(Description)}

            {propertiesString}
            {inputsString}
            {outputsString}
            """;

            return MD;
        }

        public class InputOutput
        {
            public enum TypeEnum
            {
                Input,
                Output
            }

            public required VariableType VariableType { get; init; }
            public required TypeEnum Type { get; init; }
            public required string Name { get; init; }
            public required string Description { get; init; }

            public string GetText()
            {
                return $"- {Name} \\<`{VariableType}`\\>\\\n{Description}\n\n";
            }
        }

        public class Annotation
        {
            public enum TypeEnum
            {
                note,
                tip,
                info,
                warning,
                danger
            }

            public required string Message { get; init; }
            public required TypeEnum Type { get; init; }

            public string GetText()
            {
                return
                $"""
                   :::{Type}
                   {Message}
                   :::
                """;
            }
        };

        public class Property
        {
            public class Option
            {
                public required string Name { get; init; }
                public required string Description { get; init; }
            }

            public required string FriendlyName { get; init; }
            public required string InternalName { get; init; }
            public required VariableType Type { get; init; }
            public required string Description { get; init; }

            public List<Option> Options = [];

            public List<Annotation> Annotations { get; set; } = [];

            public string GetText()
            {
                var propertyString = string.Empty;

                // hack to get spawnflags to display properly, for some reason the friendly name is empty for these
                var friendlyName = FriendlyName;
                if (InternalName == "spawnflags")
                {
                    friendlyName = "Spawnflags";
                }

                propertyString = $"- **{friendlyName}** (`{InternalName}`) \\<`{Type}`\\>";

                if (!string.IsNullOrEmpty(Description))
                {
                    propertyString += $"\\\n{Description}";
                }

                var options = string.Empty;

                foreach (var option in Options)
                {
                    options += $"  - {option.Name} {option.Description}\n";
                }

                if (!string.IsNullOrEmpty(options))
                {
                    propertyString += "\n" + options;
                }

                foreach (var annotation in Annotations)
                {
                    propertyString += $"\n{annotation.GetText()}";
                }

                propertyString += "\n\n";

                return propertyString;
            }
        }

        public string GetPageRelativePath()
        {
            return $"{Name}-{Game.FileSystemName}.mdx";
        }

        public static EntityPage? GetEntityPage(GameDataClass Class, Game game)
        {
            // dont want base classes, users dont care about these
            if (Class.ClassType == ClassType.BaseClass)
            {
                return null;
            }

            EntityTypeEnum entityType = EntityTypeEnum.Point;
            if (Class.ClassType == ClassType.SolidClass)
            {
                entityType = EntityTypeEnum.Mesh;
            }

            string iconPath = string.Empty;

            foreach (var behavior in Class.Behaviours)
            {
                if (behavior.Name == "iconsprite")
                {
                    if (behavior.Values.Count > 0)
                    {
                        iconPath = behavior.Values[0];
                    }
                }
            }

            foreach (var dict in Class.Dictionaries)
            {
                foreach (var kv in dict)
                {
                    if (kv.Key == "image" || kv.Key == "auto_apply_material")
                    {
                        iconPath = (string)kv.Value.Value;
                    }
                }
            }

            var inputOutputs = new List<InputOutput>();
            foreach (var inputOutput in Class.InOuts)
            {
                inputOutputs.Add(new InputOutput { 
                
                    Name = inputOutput.Name,
                    Description = SanitizeInput(inputOutput.Description),
                    Type = (InputOutput.TypeEnum)Enum.Parse(typeof(InputOutput.TypeEnum), inputOutput.IOType.ToString()),
                    VariableType = inputOutput.VariableType
                });
            }

            var entityPage = new EntityPage
            {
                Game = game,
                Name = Class.Name,
                Description = Class.Description,
                IconPath = iconPath,
                EntityType = entityType
            };

            entityPage.InputOutputs.AddRange(inputOutputs);

            foreach (var property in Class.Properties)
            {
                // dont add removed keys pls
                if (property.VariableType == VariableType.RemoveKey)
                {
                    continue;
                }

                var newProperty = new Property
                {
                    FriendlyName = SanitizeInput(property.Description),
                    InternalName = SanitizeInput(property.Name),
                    Description = SanitizeInput(property.Details),
                    Type = property.VariableType
                };

                foreach (var option in property.Options)
                {
                    newProperty.Options.Add(new Property.Option
                    {
                        Name = SanitizeInput(option.Description),
                        Description = SanitizeInput(option.Details)
                    });
                }

                entityPage.Properties.Add(newProperty);
            }

            return entityPage;
        }

        private static string EscapeInvalidTags(string input, string[] allowedTags)
        {
            var allowedPattern = string.Join("|", allowedTags.Select(Regex.Escape));

            // match opening tags that are NOT in the allowed list
            var invalidOpenTagPattern = $@"<(?!/?(?:{allowedPattern})\b)[^>]*>";

            return Regex.Replace(input, invalidOpenTagPattern, match =>
                WebUtility.HtmlEncode(match.Value), RegexOptions.IgnoreCase);
        }

        private static string SanitizeInput(string details)
        {
            // make this newline so stuff displays nicely
            details = details.Replace("<br>", "\n");

            // no clue what this does in hammer, seems to be nothing
            // a lot of these are just broken so im removing them outright to avoid confusion
            details = details.Replace("<original name>", "");
            details = details.Replace("<Award Text>", "");
            details = details.Replace("<picker>", "");
            details = details.Replace("<None>", "None");

            // escape any funky tags
            var allowedTags = new[] { "b", "br", "strong" };
            details = EscapeInvalidTags(details, allowedTags);
            // escape unclosed tags at the end
            details = Regex.Replace(details, @"<([^>]*)$", "&lt;$1");
            // escape unclosed tags followed by another opening tag
            details = Regex.Replace(details, @"<([^>]*)(?=<)", "&lt;$1");
            // escape unmatched closing brackets at start
            details = Regex.Replace(details, @"^([^<]*?)>", "$1&gt;");
            // escape unmatched closing brackets after other closing brackets
            details = Regex.Replace(details, @"(?<=>)([^<]*?)>", "$1&gt;");

            details = details.Replace("{", "\\{");
            details = details.Replace("}", "\\}");

            return details;
        }
    }
}
