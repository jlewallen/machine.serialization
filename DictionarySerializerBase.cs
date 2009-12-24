using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Machine.BinarySerializer
{
  public abstract class DictionarySerializerBase<T, K> : ISerializer
  {
    public void Serialize(ISerializer serializer, BinaryWriter binary, object value)
    {
      var map = CreateMap((T)value);
      binary.Write((byte)map.Count());
      foreach (var field in map)
      {
        SerializeKey(binary, field.Key);
        serializer.Serialize(serializer, binary, field.Value);
      }
    }

    public object Deserialize(ISerializer serializer, BinaryReader binary)
    {
      var objectType = TypeSpecifier.Read(binary);
      var numberOfFields = binary.ReadByte();
      var map = new SortedDictionary<K, object>();
      for (var i = 0; i < numberOfFields; ++i)
      {
        var key = DeserializeKey(binary);
        var value = serializer.Deserialize(serializer, binary);
        map.Add(key, value);
      }
      return CreateObject(objectType, map);
    }

    public abstract IDictionary<K, object> CreateMap(T value);
    public abstract T CreateObject(Type objectType, IDictionary<K, object> map);
    public abstract void SerializeKey(BinaryWriter binary, K key);
    public abstract K DeserializeKey(BinaryReader binary);
  }
}