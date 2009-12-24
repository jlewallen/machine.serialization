using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Machine.BinarySerializer
{
  public class DefaultSerializer : ISerializer
  {
    static bool IsEnd(Array array, Int32[] indexer)
    {
      for (var i = 0; i < array.Rank; ++i)
      {
        if (indexer[i] > array.GetUpperBound(i))
          return true;
      }
      return false;
    }

    static void Next(Array array, Int32[] indexer)
    {
      indexer[0]++;
      for (var i = 0; i < array.Rank - 1; ++i)
      {
        if (indexer[i] == array.GetLength(i))
        {
          indexer[i    ] = 0;
          indexer[i + 1]++;
        }
      }
    }

    public void Serialize(ISerializer serializer, BinaryWriter binary, object value)
    {
      var type = value.GetType();
      if (type.IsArray)
      {
        var array = (Array)value;
        binary.Write(TypeCode.Array);
        TypeSpecifier.Write(binary, type);
        binary.Write(array.Rank);
        for (var i = 0; i < array.Rank; ++i)
        {
          binary.Write(array.GetLength(i));
        }
        var indexer = new Int32[array.Rank];
        while (!IsEnd(array, indexer))
        {
          serializer.Serialize(serializer, binary, array.GetValue(indexer));
          Next(array, indexer);
        }
      }
      else if (type == typeof(string))
      {
        var stringValue = (string)value;
        binary.Write(TypeCode.String);
        binary.Write(System.Text.Encoding.ASCII.GetByteCount(stringValue));
        binary.Write(System.Text.Encoding.ASCII.GetBytes(stringValue));
      }
      else if (type == typeof(Byte))
      {
        binary.Write(TypeCode.Byte);
        binary.Write((Byte)value);
      }
      else if (type == typeof(Int16))
      {
        binary.Write(TypeCode.Int16);
        binary.Write((Int16)value);
      }
      else if (type == typeof(Int32))
      {
        binary.Write(TypeCode.Int32);
        binary.Write((Int32)value);
      }
      else if (type == typeof(Int64))
      {
        binary.Write(TypeCode.Int64);
        binary.Write((Int64)value);
      }
      else if (type == typeof(Double))
      {
        binary.Write(TypeCode.Double);
        binary.Write((Double)value);
      }
      else if (type == typeof(Single))
      {
        binary.Write(TypeCode.Single);
        binary.Write((Single)value);
      }
      else if (type.IsInstanceOfGenericType(typeof(Dictionary<,>)))
      {
        var dictionary = (IDictionary)value;
        binary.Write(TypeCode.Dictionary);
        TypeSpecifier.Write(binary, type);
        binary.Write(dictionary.Keys.Count);
        foreach (var key in dictionary.Keys)
        {
          serializer.Serialize(serializer, binary, key);
          serializer.Serialize(serializer, binary, dictionary[key]);
        }
      }
      else if (type.IsInstanceOfGenericType(typeof(List<>)))
      {
        var collection = (IList)value;
        binary.Write(TypeCode.List);
        TypeSpecifier.Write(binary, type);
        binary.Write(collection.Count);
        foreach (var el in collection)
        {
          serializer.Serialize(serializer, binary, el);
        }
      }
      else
      {
        binary.Write(TypeCode.Object);
        TypeSpecifier.Write(binary, type);
        CustomBinarySerializer.Serializers.ForObject(type).Serialize(serializer, binary, value);
      }
    }

    public object Deserialize(ISerializer serializer, BinaryReader binary)
    {
      var type = binary.ReadTypeCode();
      switch (type)
      {
        case TypeCode.Array:
          {
            var arrayType = TypeSpecifier.Read(binary);
            var rank = binary.ReadInt32();
            var lengths = new Int32[rank];
            for (var i = 0; i < rank; ++i)
            {
              lengths[i] = binary.ReadInt32();
            }
            var array = Array.CreateInstance(arrayType.GetElementType(), lengths);
            var indexer = new Int32[array.Rank];
            while (!IsEnd(array, indexer))
            {
              var value = serializer.Deserialize(serializer, binary);
              array.SetValue(value, indexer);
              Next(array, indexer);
            }
            return array;
          }
        case TypeCode.String:
          {
            var length = binary.ReadInt32();
            var bytes = binary.ReadBytes(length);
            return System.Text.Encoding.ASCII.GetString(bytes);
          }
        case TypeCode.List:
          {
            var collectionType = TypeSpecifier.Read(binary);
            var length = binary.ReadInt32();
            var collection = (IList)Activator.CreateInstance(collectionType);
            for (var i = 0; i < length; ++i)
            {
              collection.Add(serializer.Deserialize(serializer, binary));
            }
            return collection;
          }
        case TypeCode.Dictionary:
          var dictionaryType = TypeSpecifier.Read(binary);
          var numberOfPairs = binary.ReadInt32();
          var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);
          for (var i = 0; i < numberOfPairs; ++i)
          {
            var key = serializer.Deserialize(serializer, binary);
            var value = serializer.Deserialize(serializer, binary);
            dictionary[key] = value;
          }
          return dictionary;
        case TypeCode.Object:
          // XXX: HACK
          var position = binary.BaseStream.Position;
          var objectType = TypeSpecifier.Read(binary);
          binary.BaseStream.Seek(position, SeekOrigin.Begin);
          return CustomBinarySerializer.Serializers.ForObject(objectType).Deserialize(serializer, binary);
        case TypeCode.Int64:
          return binary.ReadInt64();
        case TypeCode.Double:
          return binary.ReadDouble();
        case TypeCode.Int16:
          return binary.ReadInt16();
        case TypeCode.Int32:
          return binary.ReadInt32();
        case TypeCode.Byte:
          return binary.ReadByte();
      }
      throw new ArgumentException(type.ToString());
    }
  }
}