using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityMcpBridge.Editor.Helpers
{
    /// <summary>
    /// Custom JSON converter for Unity types to prevent circular reference issues
    /// </summary>
    public class UnityJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Vector3) || 
                   objectType == typeof(Vector2) || 
                   objectType == typeof(Quaternion) ||
                   objectType == typeof(Color);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Vector3 v3)
            {
                var obj = new JObject
                {
                    ["x"] = v3.x,
                    ["y"] = v3.y,
                    ["z"] = v3.z
                };
                obj.WriteTo(writer);
            }
            else if (value is Vector2 v2)
            {
                var obj = new JObject
                {
                    ["x"] = v2.x,
                    ["y"] = v2.y
                };
                obj.WriteTo(writer);
            }
            else if (value is Quaternion q)
            {
                var obj = new JObject
                {
                    ["x"] = q.x,
                    ["y"] = q.y,
                    ["z"] = q.z,
                    ["w"] = q.w
                };
                obj.WriteTo(writer);
            }
            else if (value is Color c)
            {
                var obj = new JObject
                {
                    ["r"] = c.r,
                    ["g"] = c.g,
                    ["b"] = c.b,
                    ["a"] = c.a
                };
                obj.WriteTo(writer);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            
            if (objectType == typeof(Vector3))
            {
                return new Vector3(
                    obj["x"]?.ToObject<float>() ?? 0f,
                    obj["y"]?.ToObject<float>() ?? 0f,
                    obj["z"]?.ToObject<float>() ?? 0f
                );
            }
            else if (objectType == typeof(Vector2))
            {
                return new Vector2(
                    obj["x"]?.ToObject<float>() ?? 0f,
                    obj["y"]?.ToObject<float>() ?? 0f
                );
            }
            else if (objectType == typeof(Quaternion))
            {
                return new Quaternion(
                    obj["x"]?.ToObject<float>() ?? 0f,
                    obj["y"]?.ToObject<float>() ?? 0f,
                    obj["z"]?.ToObject<float>() ?? 0f,
                    obj["w"]?.ToObject<float>() ?? 1f
                );
            }
            else if (objectType == typeof(Color))
            {
                return new Color(
                    obj["r"]?.ToObject<float>() ?? 0f,
                    obj["g"]?.ToObject<float>() ?? 0f,
                    obj["b"]?.ToObject<float>() ?? 0f,
                    obj["a"]?.ToObject<float>() ?? 1f
                );
            }
            
            return null;
        }
    }
}