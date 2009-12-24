using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Machine.Serialization
{
  public class TypeSpecifier
  {
    static readonly Dictionary<string, Type> _cache = new Dictionary<string, Type>();
    static readonly ReaderWriterLock _lock = new ReaderWriterLock();

    public static void Write(BinaryWriter binary, Type type)
    {
      var typeCode = TypeCodeFor(type);
      binary.Write(typeCode);
      switch (typeCode)
      {
        case TypeCode.Array:
          Write(binary, type.GetElementType());
          break;
        case TypeCode.List:
          Write(binary, type.GetGenericArguments().First());
          break;
        case TypeCode.Dictionary:
          Write(binary, type.GetGenericArguments().First());
          Write(binary, type.GetGenericArguments().Last());
          break;
        case TypeCode.Object:
          binary.Write(type.FullName);
          break;
      }
    }

    public static Type Read(BinaryReader binary)
    {
      var typeCode = binary.ReadTypeCode();
      switch (typeCode)
      {
        case TypeCode.Array:
          var arrayType = Read(binary);
          return Array.CreateInstance(arrayType, 0).GetType();
        case TypeCode.List:
          var collectionType = Read(binary);
          return typeof(List<>).MakeGenericType(collectionType);
        case TypeCode.Dictionary:
          var keyType = Read(binary);
          var valueType = Read(binary);
          return typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        case TypeCode.String:
          return typeof(string);
        case TypeCode.Byte:
          return typeof(byte);
        case TypeCode.Int16:
          return typeof(Int16);
        case TypeCode.Int32:
          return typeof(Int32);
        case TypeCode.Int64:
          return typeof(Int64);
        case TypeCode.Single:
          return typeof(Single);
        case TypeCode.Double:
          return typeof(Double);
        case TypeCode.Object:
          var typeName = binary.ReadString();
          _lock.AcquireReaderLock(Timeout.Infinite);
          try
          {
            if (_cache.ContainsKey(typeName))
              return _cache[typeName];
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
              var type = assembly.GetType(typeName);
              if (type != null)
              {
                _lock.UpgradeToWriterLock(Timeout.Infinite);
                _cache[typeName] = type;
                return type;
              }
            }
          }
          finally
          {
            _lock.ReleaseLock();
          }
          throw new ArgumentException("Unable to resolve type: " + typeName);
      }
      throw new ArgumentException(typeCode.ToString());
    }

    static TypeCode TypeCodeFor(Type type)
    {
      if (type.IsArray)
        return TypeCode.Array;
      if (type == typeof(string))
        return TypeCode.String;
      if (type == typeof(Byte))
        return TypeCode.Byte;
      if (type == typeof(Int16))
        return TypeCode.Int16;
      if (type == typeof(Int32))
        return TypeCode.Int32;
      if (type == typeof(Int64))
        return TypeCode.Int64;
      if (type == typeof(Double))
        return TypeCode.Double;
      if (type == typeof(Single))
        return TypeCode.Single;
      if (type.IsInstanceOfGenericType(typeof(Dictionary<,>)))
        return TypeCode.Dictionary;
      if (type.IsInstanceOfGenericType(typeof(List<>)))
        return TypeCode.List;
      return TypeCode.Object;
    }

    public static void AddType(Type type)
    {
      _lock.AcquireWriterLock(Timeout.Infinite);
      try
      {
        _cache[type.FullName] = type;
      }
      finally
      {
        _lock.ReleaseLock();
      }
    }
  }
}