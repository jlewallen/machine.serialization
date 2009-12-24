using System.IO;

namespace Machine.BinarySerializer
{
  public static class CustomBinarySerializer
  {
    public static readonly SerializerMap Serializers = new SerializerMap();

    public static void Serialize(Stream stream, object value)
    {
      Serialize(new BinaryWriter(stream), value);
    }

    public static T Deserialize<T>(Stream stream)
    {
      using (var binary = new BinaryReader(stream))
      {
        return Deserialize<T>(binary);
      }
    }

    public static T Deserialize<T>(BinaryReader binary)
    {
      return (T)Deserialize(binary);
    }

    public static void Serialize(BinaryWriter binary, object value)
    {
      Serializers.Root.Serialize(Serializers.Root, binary, value);
    }

    public static object Deserialize(BinaryReader binary)
    {
      return Serializers.Root.Deserialize(Serializers.Root, binary);
    }
  }
}