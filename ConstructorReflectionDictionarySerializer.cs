using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Machine.Serialization
{
  public class ConstructorReflectionDictionarySerializer : DictionarySerializerBase<object, string>
  {
    public override IDictionary<string, object> CreateMap(object value)
    {
      var type = value.GetType();
      var ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
      return ctor.GetParameters().Select(p => type.GetField("_" + p.Name, BindingFlags.Instance | BindingFlags.NonPublic)).ToDictionary(
        f => f.Name,
        f => f.GetValue(value)
      );
    }

    public override object CreateObject(Type objectType, IDictionary<string, object> map)
    {
      return Activator.CreateInstance(objectType, map.Values.ToArray());
    }

    public override void SerializeKey(BinaryWriter binary, string key)
    {
      binary.Write(key);
    }

    public override string DeserializeKey(BinaryReader binary)
    {
      return binary.ReadString();
    }
  }

  public abstract class ObjectTupleSerializer : DictionarySerializerBase<object, object>
  {
    public override IDictionary<object, object> CreateMap(object value)
    {
      var map = new SortedDictionary<object, object>();
      foreach (var entry in CreateTuple(value).Select((r, i) => new { Entry = r, Index = i }))
      {
        map[entry.Index] = entry.Entry;
      }
      return map;
    }

    public override object CreateObject(Type objectType, IDictionary<object, object> map)
    {
      return CreateObject(objectType, map.Values);
    }

    public override void SerializeKey(BinaryWriter binary, object key)
    {
    }

    public override object DeserializeKey(BinaryReader binary)
    {
      return binary.BaseStream.Position;
    }

    public abstract object CreateObject(Type objectType, IEnumerable<object> tuple);

    public abstract IEnumerable<object> CreateTuple(object value);
  }

  public class ConstructorReflectionTupleSerializer : ObjectTupleSerializer
  {
    public override object CreateObject(Type objectType, IEnumerable<object> tuple)
    {
      return Activator.CreateInstance(objectType, tuple.ToArray());
    }

    public override IEnumerable<object> CreateTuple(object value)
    {
      var type = value.GetType();
      var ctor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
      return ctor.GetParameters().Select(p => type.GetField("_" + p.Name, BindingFlags.Instance | BindingFlags.NonPublic)).Select(
        f => f.GetValue(value)
      );
    }
  }

  public class CustomTupleSerializer<T> : ObjectTupleSerializer
  {
    readonly Func<T, IEnumerable<object>> _getTuple;
    readonly Func<object[], T> _getObject;

    public CustomTupleSerializer(Func<T, IEnumerable<object>> getTuple, Func<object[], T> getObject)
    {
      _getTuple = getTuple;
      _getObject = getObject;
    }

    public override object CreateObject(Type objectType, IEnumerable<object> tuple)
    {
      return _getObject(tuple.ToArray());
    }

    public override IEnumerable<object> CreateTuple(object value)
    {
      return _getTuple((T)value);
    }
  }
}