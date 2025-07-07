using System.Text.Json.Serialization;
using System.Text.Json;
using static FGDDumper.JsonStuff;

namespace FGDDumper
{
    [JsonSourceGenerationOptions(
               PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
               WriteIndented = true,
               Converters = [typeof(EntityPageJsonConverter), typeof(JsonStringEnumConverter)]
           )]
    [JsonSerializable(typeof(EntityPage))]
    [JsonSerializable(typeof(EntityPage.Property))]
    [JsonSerializable(typeof(EntityDocument))]
    public partial class JsonContext : JsonSerializerContext
    {
    }

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

                writer.WriteString("Game", value.Game?.FileSystemName);
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
    }
}
