using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace WzComparerR2.Network.Contracts
{
    class ByteArrayConverter : JsonConverter
    {
        static readonly Type supportedType = typeof(byte[]);

        public override bool CanConvert(Type objectType)
        {
            return objectType == supportedType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string value = reader.Value as string;

            return value != null ? Convert.FromBase64String(value) : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(Convert.ToBase64String((byte[])value));
        }
    }
}
