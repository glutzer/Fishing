using System.Text.Json;
using System.Text.Json.Nodes;

namespace Fishing3;

public static class NodeExtensions
{
    /// <summary>
    /// Attempt to deserialize a type from a json object.
    /// </summary>
    public static T? Get<T>(this JsonObject jObject, string path, T? defaultValue)
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

public class FluidJson
{
    public required string Code { get; set; } = "NaN";

    public string Class { get; set; } = "Fluid";

    /// <summary>
    /// 0-1 glow level of the fluid.
    /// </summary>
    public float GlowLevel { get; set; } = 0;

    /// <summary>
    /// RGBA color of the fluid.
    /// </summary>
    public float[] Color { get; set; } = new float[] { 1, 1, 1, 1 };

    /// <summary>
    /// Can simply call JsonObject.Get<> for any value you need.
    /// </summary>
    public JsonObject Attributes { get; set; } = new();
}
