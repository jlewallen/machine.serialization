using System;
using System.IO;

namespace Machine.Serialization
{
  public static class TypeHelpers
  {
    public static bool IsInstanceOfGenericType(this Type type, Type genericType)
    {
      if (type.IsGenericType)
        return type.GetGenericTypeDefinition() == genericType;
      return false;
    }

    public static void Write(this BinaryWriter stream, TypeCode code)
    {
      stream.Write((byte)code);
    }

    public static TypeCode ReadTypeCode(this BinaryReader stream)
    {
      return (TypeCode)stream.ReadByte();
    }
  }
}