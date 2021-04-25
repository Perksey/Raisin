using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tallinn.Models.Json
{
    internal class PolymorphicConverter<TBaseType, TTypeEnum> : JsonConverter<TBaseType>
        where TTypeEnum : Enum where TBaseType : class
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeof(TBaseType).IsAssignableFrom(typeToConvert);

        public override TBaseType? Read(
            ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected StartObject.");
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName.");
            }

            var propertyName = reader.GetString();
            if (propertyName != "Type")
            {
                throw new JsonException("Expected PropertyName to be \"Type\".");
            }

            reader.Read();
            if (reader.TokenType != JsonTokenType.Number)
            {
                throw new JsonException();
            }

            var typeDiscriminator = (TTypeEnum) Enum.ToObject(typeof(TTypeEnum), reader.GetInt32());
            var typeForEnumValue = typeof(TTypeEnum)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.GetValue(null)?.Equals(typeDiscriminator) ?? false)?
                .GetCustomAttribute<ForTypeAttribute>()?.Type;
            if (typeForEnumValue is null)
            {
                throw new JsonException("Failed to get type for polymorphic element");
            }

            propertyName = reader.GetString();
            if (propertyName != "Value")
            {
                throw new JsonException("Expected PropertyName to be \"Value\".");
            }

            return JsonSerializer.Deserialize(ref reader, typeForEnumValue) as TBaseType;
        }

        public override void Write(
            Utf8JsonWriter writer, TBaseType obj, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("Type", Convert.ToInt32(typeof(TTypeEnum)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.GetCustomAttribute<ForTypeAttribute>()?.Type == obj.GetType())?
                .GetValue(null)));
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, obj);
            writer.WriteEndObject();
        }
    }
}