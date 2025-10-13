using OpenTK.Mathematics;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing;

public static class AttributeExtensions3
{
    public static Vector4 GetVector4(this ITreeAttribute instance, string key, Vector4 defaultValue = default)
    {
        if (instance.TryGetAttribute(key, out IAttribute? attribute))
        {
            if (attribute is Vector4Attribute vector4Attr)
            {
                return (Vector4)vector4Attr.GetValue();
            }
        }

        return defaultValue; // Not found.
    }

    public static void SetVector4(this ITreeAttribute instance, string key, Vector4 value)
    {
        instance[key] = new Vector4Attribute(value);
    }
}

public class Vector4Attribute : IAttribute
{
    private Vector4 value;

    public Vector4Attribute(Vector4 value)
    {
        this.value = value;
    }

    public IAttribute Clone()
    {
        return new Vector4Attribute(value);
    }

    public bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
    {
        return attr is Vector4Attribute vector4Attr && value.Equals(vector4Attr.value);
    }

    public void FromBytes(BinaryReader stream)
    {
        value.X = stream.ReadSingle();
        value.Y = stream.ReadSingle();
        value.Z = stream.ReadSingle();
        value.W = stream.ReadSingle();
    }

    public int GetAttributeId()
    {
        return 235;
    }

    public object GetValue()
    {
        return value;
    }

    public void ToBytes(BinaryWriter stream)
    {
        stream.Write(value.X);
        stream.Write(value.Y);
        stream.Write(value.Z);
        stream.Write(value.W);
    }

    public string ToJsonToken()
    {
        return $"[{value.X}, {value.Y}, {value.Z}, {value.W}]";
    }
}