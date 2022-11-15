using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Json.Utilities
{
    public class PolymorphicJsonConverter<T> : JsonConverter<T>
    {
        public static readonly string TYPE_NAME_JSON_PROPERTY_NAME = "typeName";
        public static readonly string TYPE_VALUE_JSON_PROPERTY_NAME = "typeValue";

        public override bool CanConvert(Type type)
        {
            if (type.Name.IndexOf("List", StringComparison.Ordinal) == 0)
            {
                return typeof(T).GenericTypeArguments[0].IsAssignableFrom(type.GenericTypeArguments[0]);
            }

            return typeof(T).IsAssignableFrom(type);
        }

        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            // It's important to get assembly from generic of the attribute since we don't want to create types of unknown assemblies
            var typeAssemblyName = typeof(T).Assembly.FullName;

            if (typeToConvert.Name.IndexOf("List", StringComparison.Ordinal) == 0)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException("StartArray expected");
                }

                typeAssemblyName = typeof(T).GenericTypeArguments[0].Assembly.FullName;

                reader.Read();

                var list = (T)Activator.CreateInstance(typeof(T));

                while (reader.TokenType != JsonTokenType.EndArray)
                {
                    var element = ReadElement(ref reader, typeAssemblyName, options);
                    (list as dynamic).Add((dynamic)element);

                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        reader.Read();
                    }
                }

                return list;
            }

            var obj = (T)ReadElement(ref reader, typeAssemblyName, options);

            return obj;
        }

        protected object ReadElement(
            ref Utf8JsonReader reader,
            string typeAssemblyName,
            JsonSerializerOptions options
        )
        {
            options.PropertyNamingPolicy ??= JsonNamingPolicy.CamelCase;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException();
            }

            if (!reader.Read()
                    || reader.TokenType != JsonTokenType.PropertyName
                    || reader.GetString() != TYPE_NAME_JSON_PROPERTY_NAME)
            {
                throw new JsonException($"{TYPE_NAME_JSON_PROPERTY_NAME} should be first property in json");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"{TYPE_NAME_JSON_PROPERTY_NAME} value should be string");
            }

            var typeName = reader.GetString();

            if (!reader.Read()
                    || reader.TokenType != JsonTokenType.PropertyName
                    || reader.GetString() != TYPE_VALUE_JSON_PROPERTY_NAME)
            {
                throw new JsonException($"{TYPE_VALUE_JSON_PROPERTY_NAME} should go right after {TYPE_NAME_JSON_PROPERTY_NAME} property value");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"{TYPE_VALUE_JSON_PROPERTY_NAME} value should be an object");
            }

            reader.Read();

            var instance = Activator.CreateInstance(Type.GetType($"{typeName}, {typeAssemblyName}"));

            var properties = instance.GetType().GetProperties();
            var propertiesFromJson = new Dictionary<string, object>();

            for (var i = 0; i < properties.Length; i++)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var jsonPropertyName = reader.GetString();
                    reader.Read();

                    var propertyType = properties.FirstOrDefault(
                        p => options.PropertyNamingPolicy.ConvertName(p.Name) == jsonPropertyName
                    );

                    if (propertyType != null)
                    {
                        try
                        {
                            var jsonPropertyValue = JsonSerializer.Deserialize(ref reader, propertyType.PropertyType, options);

                            if (propertyType.SetMethod != null)
                            {
                                propertyType.SetValue(instance, jsonPropertyValue);
                            }
                        }
                        catch (NotSupportedException ex)
                        {
                            throw;
                        }

                        reader.Read();
                    }
                }
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException();
            }

            return instance;
        }

        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            if (typeof(T).Name.IndexOf("List", StringComparison.Ordinal) == 0)
            {
                writer.WriteStartArray();

                foreach (var element in (IEnumerable<object>)value)
                {
                    WriteElement(writer, element, options);
                }

                writer.WriteEndArray();
            }
            else
            {
                WriteElement(writer, value, options);
            }
        }

        protected void WriteElement(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy ??= JsonNamingPolicy.CamelCase;

            writer.WriteStartObject();

            writer.WriteString(TYPE_NAME_JSON_PROPERTY_NAME, value.GetType().FullName);
            writer.WritePropertyName(TYPE_VALUE_JSON_PROPERTY_NAME);

            writer.WriteStartObject();
            var properties = value.GetType().GetProperties();
            foreach (var prop in properties)
            {
                writer.WritePropertyName(
                    options.PropertyNamingPolicy.ConvertName(prop.Name)
                );

                if (typeof(T).IsAssignableFrom(prop.PropertyType))
                {
                    WriteElement(writer, prop.GetValue(value), options);
                }
                else
                {
                    JsonSerializer.Serialize(writer, prop.GetValue(value), options);
                }
            }
            writer.WriteEndObject();

            writer.WriteEndObject();
        }
    }
}
