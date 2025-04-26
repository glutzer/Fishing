using OpenTK.Mathematics;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Fishing3;

public static class AttributeExtensions2
{
    public static Vector3 GetVector3(this ITreeAttribute instance, string key, Vector3 defaultValue = default)
    {
        if (instance.TryGetAttribute(key, out IAttribute? attribute))
        {
            if (attribute is Vector3Attribute vector3Attr)
            {
                return (Vector3)vector3Attr.GetValue();
            }
        }

        return defaultValue; // Not found.
    }

    public static void SetVector3(this ITreeAttribute instance, string key, Vector3 value)
    {
        instance[key] = new Vector3Attribute(value);
    }
}

public class Vector3Attribute : IAttribute
{
    private Vector3 value;

    public Vector3Attribute(Vector3 value)
    {
        this.value = value;
    }

    public IAttribute Clone()
    {
        return new Vector3Attribute(value);
    }

    public bool Equals(IWorldAccessor worldForResolve, IAttribute attr)
    {
        return attr is Vector3Attribute vector3Attr && value.Equals(vector3Attr.value);
    }

    public void FromBytes(BinaryReader stream)
    {
        value.X = stream.ReadSingle();
        value.Y = stream.ReadSingle();
        value.Z = stream.ReadSingle();
    }

    public int GetAttributeId()
    {
        return 234;
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
    }

    public string ToJsonToken()
    {
        return $"[{value.X}, {value.Y}, {value.Z}]";
    }
}