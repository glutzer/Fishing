using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fishing3;

public static class NodeExtensions
{
    /// <summary>
    /// Attempt to deserialize a type from a json object.
    /// </summary>
    public static T? Get<T>(this JsonObject jObject, string path, T? defaultValue = default)
    {
        if (jObject.TryGetPropertyValue(path, out JsonNode? node) && node != null)
        {
            if (node is JsonObject obj)
            {
                T? objectValue = obj.Deserialize<T>() ?? defaultValue;
                return objectValue;
            }

            T desValue = node.GetValue<T>();
            return desValue;
        }

        return defaultValue;
    }
}