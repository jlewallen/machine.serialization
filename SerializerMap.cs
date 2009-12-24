using System;
using System.Collections.Generic;

namespace Machine.BinarySerializer
{
  public class SerializerMap
  {
    readonly Dictionary<Type, ISerializer> _serializers = new Dictionary<Type, ISerializer>();
    readonly ISerializer _defaultObjectSerializer = new ConstructorReflectionTupleSerializer(); //ConstructorReflectionDictionarySerializer();
    readonly ISerializer _rootSerializer = new DefaultSerializer();

    public ISerializer Root { get { return _rootSerializer; } }
    public ISerializer ForObject(Type type)
    {
      if (_serializers.ContainsKey(type))
        return _serializers[type];
      return _defaultObjectSerializer;
    }

    public void Add<T>(Func<T, IEnumerable<object>> getTuple, Func<object[], T> getObject)
    {
      TypeSpecifier.AddType(typeof(T));
      _serializers[typeof(T)] = new CustomTupleSerializer<T>(getTuple, getObject);
    }
  }
}