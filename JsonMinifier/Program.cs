using System.Text.Json;
using System.Text.Json.Nodes;

namespace JsonMinifier;

public class Program
{
    private static readonly string[] InlineKeys = ["north", "east", "south", "west", "up", "down", "translation", "rotation", "origin"];

    private static void Main(string[] args)
    {
        // Make the base directory 2 directories up from running directory.
        string baseDir = "D:\\VSProjects\\Fishing\\Fishing\\assets\\fishing\\";

        foreach (string file in Directory.EnumerateFiles(baseDir, "*.json", SearchOption.AllDirectories))
        {
            string json = File.ReadAllText(file);
            try
            {
                JsonNode? node = JsonNode.Parse(json);
                string formatted = FormatNode(node, 0);
                File.WriteAllText(file, formatted);
                Console.WriteLine($"Formatted: {file}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in {file}: {ex.Message}");
            }
        }
    }

    private static string FormatNode(JsonNode? node, int indent, bool inlineChildren = false)
    {
        JsonSerializerOptions options = new() { WriteIndented = false };

        if (node is JsonArray array)
        {
            // If everything in this array is a json value, don't need to condense it.
            bool isSimple = array.All(e => e is JsonValue);

            if (isSimple)
                return "[" + string.Join(", ", array.Select(e => FormatNode(e, indent))) + "]";
            else
            {
                string inner = string.Join(",\n",
                    array.Select(e => new string(' ', indent + 2) + FormatNode(e, indent + 2)));
                return "[\n" + inner + "\n" + new string(' ', indent) + "]";
            }
        }
        else if (node is JsonObject obj)
        {
            IEnumerable<string> parts = obj.Select(kvp =>
            {
                // Will inline everything if the keys contain the current key.
                bool inline = InlineKeys.Contains(kvp.Key, StringComparer.OrdinalIgnoreCase);

                string val = FormatNode(kvp.Value, indent + 2, inline);

                string keyPart = $"\"{kvp.Key}\": ";
                return new string(' ', inlineChildren ? 0 : indent + 2) + keyPart + val;
            });

            string sep = inlineChildren ? ", " : ",\n";
            string content = string.Join(sep, parts);
            return inlineChildren ? "{ " + content + " }" : "{\n" + content + "\n" + new string(' ', indent) + "}";
        }
        else if (node is JsonValue valNode)
        {
            return JsonSerializer.Serialize(valNode.GetValue<object>());
        }
        return "null";
    }
}
