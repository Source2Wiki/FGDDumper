using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;
using Sledge.Formats.GameData.Objects;

namespace FGDDumper
{
    public static class JsonStuff
    {
        public class EntityPageJsonConverter : JsonConverter<EntityPage>
        {
            public override EntityPage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Expected StartObject token");
                }

                GameFinder.Game? game = null;
                EntityPage.EntityTypeEnum entityType = EntityPage.EntityTypeEnum.Default;
                string? name = string.Empty;
                string description = string.Empty;
                string iconPath = string.Empty;
                List<EntityPage.Property> properties = [];
                List<EntityPage.InputOutput> inputOutputs = [];

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("Expected PropertyName token");
                    }

                    string? propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Game":
                            game = GameFinder.GetGameByFileSystemName(reader.GetString());
                            break;
                        case "EntityType":
                            entityType = Enum.Parse<EntityPage.EntityTypeEnum>(reader.GetString() ?? string.Empty);
                            break;
                        case "Name":
                            name = reader.GetString();
                            break;
                        case "Description":
                            description = reader.GetString() ?? string.Empty;
                            break;
                        case "IconPath":
                            iconPath = reader.GetString() ?? string.Empty;
                            break;
                        case "Properties":
                            properties = JsonSerializer.Deserialize<List<EntityPage.Property>>(ref reader, options) ?? [];
                            break;
                        case "InputOutputs":
                            inputOutputs = JsonSerializer.Deserialize<List<EntityPage.InputOutput>>(ref reader, options) ?? [];
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                if
                (   game == null ||
                    (entityType == EntityPage.EntityTypeEnum.Default) ||
                    string.IsNullOrEmpty(name)
                )
                {
                    throw new InvalidOperationException("Failed to deserialise json object!");
                }

                return new EntityPage
                {
                    Game = game,
                    EntityType = entityType,
                    Name = name,
                    Description = description,
                    IconPath = iconPath,
                    Properties = properties,
                    InputOutputs = inputOutputs
                };

            }

            public override void Write(Utf8JsonWriter writer, EntityPage value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("Game", value.Game.FileSystemName);
                writer.WriteString("EntityType", value.EntityType.ToString());
                writer.WriteString("Name", value.Name);
                writer.WriteString("Description", value.Description);
                writer.WriteString("IconPath", value.IconPath);

                writer.WritePropertyName("Properties");
                JsonSerializer.Serialize(writer, value.Properties, options);

                writer.WritePropertyName("InputOutputs");
                JsonSerializer.Serialize(writer, value.InputOutputs, options);

                writer.WriteEndObject();
            }
        }

        public class EntityPropertyJsonConverter : JsonConverter<EntityPage.Property>
        {
            public override EntityPage.Property Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions jsonOptions)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Expected StartObject token");
                }

                string? friendlyName = null;
                string? internalName = null;
                string description = string.Empty;
                VariableType? type = null;
                List<EntityPage.Property.Option> options = [];
                List<EntityPage.Annotation> annotations = [];

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("Expected PropertyName token");
                    }

                    string? propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "FriendlyName":
                            friendlyName = reader.GetString();
                            break;
                        case "InternalName":
                            internalName = reader.GetString();
                            break;
                        case "Description":
                            description = reader.GetString() ?? string.Empty;
                            break;
                        case "Type":
                            type = Enum.Parse<VariableType>(reader.GetString() ?? string.Empty);
                            break;
                        case "Options":
                            options = JsonSerializer.Deserialize<List<EntityPage.Property.Option>>(ref reader, jsonOptions) ?? [];
                            break;
                        case "Annotations":
                            annotations = JsonSerializer.Deserialize<List<EntityPage.Annotation>>(ref reader, jsonOptions) ?? [];
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                if
                (   string.IsNullOrEmpty(friendlyName) ||
                    string.IsNullOrEmpty(internalName) || 
                    (type == null)
                )
                {
                    throw new InvalidOperationException("Failed to deserialise json object!");
                }

                return new EntityPage.Property
                {
                    FriendlyName = friendlyName,
                    InternalName = internalName,
                    Description = description,
                    Type = (VariableType)type,
                    Options = options,
                    Annotations = annotations

                };

            }

            public override void Write(Utf8JsonWriter writer, EntityPage.Property value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("FriendlyName", value.FriendlyName);
                writer.WriteString("InternalName", value.InternalName);
                writer.WriteString("Type", value.Type.ToString());
                writer.WriteString("Description", value.Description);

                writer.WritePropertyName("Options");
                JsonSerializer.Serialize(writer, value.Options, options);

                writer.WritePropertyName("Annotations");
                JsonSerializer.Serialize(writer, value.Annotations, options);

                writer.WriteEndObject();
            }
        }

        public class DocumentJsonConverter : JsonConverter<EntityDocument>
        {
            public override EntityDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException("Expected StartObject token");
                }
            
                string? name = string.Empty;
                List<EntityPage> pages = [];

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("Expected PropertyName token");
                    }

                    string? propertyName = reader.GetString();
                    reader.Read();

                    switch (propertyName)
                    {
                        case "Name":
                            name = reader.GetString() ?? string.Empty;
                            break;
                        case "Pages":
                            pages = JsonSerializer.Deserialize<List<EntityPage>>(ref reader, options) ?? [];
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }

                if (string.IsNullOrEmpty(name) || pages.Count == 0)
                {
                    throw new InvalidOperationException("Failed to deserialise json object!");
                }

                return new EntityDocument
                {
                    Name = name,
                    Pages = pages,
                };
            }

            public override void Write(Utf8JsonWriter writer, EntityDocument value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WriteString("Name", value.Name);
                
                writer.WritePropertyName("Pages");
                JsonSerializer.Serialize(writer, value.Pages, options);

                writer.WriteEndObject();
            }
        }

        public static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                Converters = { new EntityPageJsonConverter() }
            };

            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

    }
}
