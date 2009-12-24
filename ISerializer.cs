using System.IO;

namespace Machine.Serialization
{
  public interface ISerializer
  {
    void Serialize(ISerializer serializer, BinaryWriter binary, object value);
    object Deserialize(ISerializer serializer, BinaryReader binary);
  }
}