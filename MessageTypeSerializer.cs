using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AutoMessage
{
    public class MessageTypeSerializer
    {
        public static readonly Type[] FinalTypes = new Type[] { typeof(sbyte), typeof(short), typeof(int), typeof(long), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(char), typeof(float), typeof(double), typeof(string), typeof(bool), typeof(decimal) };
        public static readonly Type[] PrimitiveArrayTypes = new Type[] { typeof(sbyte[]), typeof(short[]), typeof(int[]), typeof(long[]), typeof(byte[]), typeof(ushort[]), typeof(uint[]), typeof(ulong[]), typeof(char[]), typeof(float[]), typeof(double[]), typeof(bool[]), typeof(decimal[]) };
        private static Dictionary<Type, MessageType> SerializedTypes = new Dictionary<Type, MessageType>();

        public static MessageType SerializeType(Type t)
        {
            lock (SerializedTypes)
            {
                if (SerializedTypes.TryGetValue(t, out var matchedType))
                    return matchedType;
            }

            List<MessageTypeNode> typeList = new List<MessageTypeNode>();
            List<(string, string)> typeDependencies = new List<(string, string)>();

            MessageType messageType = new MessageType
            {
                TypeName = t.FullName,
                Hash = CalculateHash(t.Name),
                Types = typeList
            };

            SerializeTypeNode(null, t, typeList, typeDependencies);

            List<MessageTypeNode> dependencyOrderedTypeList = new List<MessageTypeNode>(typeList.Count);

            while (dependencyOrderedTypeList.Count < typeList.Count)
            {
                int dependencyOrderedTypeListCountBefore = dependencyOrderedTypeList.Count;

                foreach (var type in typeList)
                {
                    if (dependencyOrderedTypeList.Any(dotl => dotl.TypeName == type.TypeName))
                        continue;

                    List<string> dependencies = typeDependencies.Where((dependencyPair) => dependencyPair.Item1 == type.TypeName).Select((dependencyPair) => dependencyPair.Item2).ToList();
                    List<string> unwrittenDependencies = dependencies.Where(dep => !dependencyOrderedTypeList.Any(dotl => dotl.TypeName == dep)).ToList();

                    if (unwrittenDependencies.Count == 0)
                        dependencyOrderedTypeList.Add(type);
                }

                if (dependencyOrderedTypeListCountBefore == dependencyOrderedTypeList.Count)
                    throw new Exception("No progress in solving type dependencies. Is there a not allowed circular dependency?");
            }

            messageType.Types = dependencyOrderedTypeList;

            lock (SerializedTypes)
                SerializedTypes[t] = messageType;

            return messageType;
        }

        private static bool IsTypeArray(Type t)
        {
            return t.IsArray || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>));
        }

        private static Type GetArrayElementType(Type t)
        {
            Type elementType = null;

            if (t.IsArray)
                elementType = t.GetElementType();
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                elementType = t.GetGenericArguments()[0];

            return elementType;
        }

        private static Type GetArrayElementTypeRecursive(Type t)
        {
            Type elementType = GetArrayElementType(t);

            if (IsTypeArray(elementType))
                return GetArrayElementType(elementType);

            return elementType;
        }

        private static bool IsDefinedType(Type type, List<MessageTypeNode> typeList, out string typeName)
        {
            Type elementType;

            string tn;
            if (IsTypeArray(type))
                elementType = GetArrayElementTypeRecursive(type);
            else
                elementType = type;

            tn = FinalTypes.Contains(elementType) ? elementType.Name : elementType.FullName;

            typeName = tn;
            return typeList.Any(t => t.TypeName == tn);
        }

        private static MessageTypeNode SerializeTypeNode(string name, Type type, List<MessageTypeNode> typeList, List<(string, string)> typeDependencies)
        {
            if (IsTypeArray(type))
            {
                var elementType = GetArrayElementType(type);
                var tn = SerializeTypeNode(name, elementType, typeList, typeDependencies);
                if (tn == null)
                    throw new Exception("Type not serializable.");

                tn.TypeName += "[]";

                return tn;
            }
            else
            {
                if (type.IsGenericType)
                    return null;

                MessageTypeNode messageTypeNode = new MessageTypeNode()
                {
                    Name = name,
                    TypeName = type.FullName
                };

                if (FinalTypes.Contains(type))
                {
                    messageTypeNode.TypeName = type.Name;
                    return messageTypeNode;
                }

                var constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    return null;

                int index = typeList.FindIndex(t => t.TypeName == type.FullName);
                if (index < 0)
                {
                    var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                    var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetSetMethod() != null && p.GetGetMethod() != null).ToArray();

                    MessageTypeNode newTypeNode = new MessageTypeNode()
                    {
                        TypeName = type.FullName,
                        Children = new List<MessageTypeNode>(fields.Length + properties.Length)
                    };

                    typeList.Add(newTypeNode);

                    foreach (var field in fields)
                    {
                        var tn = SerializeTypeNode(field.Name, field.FieldType, typeList, typeDependencies);
                        if (tn == null)
                            throw new Exception($"Type '{field.FieldType}' of '{field.Name}' not serializable.");

                        if (IsDefinedType(field.FieldType, typeList, out string typeName))
                            typeDependencies.Add((type.FullName, typeName));

                        newTypeNode.Children.Add(tn);
                    }

                    foreach (var property in properties)
                    {
                        var tn = SerializeTypeNode(property.Name, property.PropertyType, typeList, typeDependencies);
                        if (tn == null)
                            throw new Exception($"Type '{property.PropertyType}' of '{property.Name}' not serializable.");

                        if (IsDefinedType(property.PropertyType, typeList, out string typeName))
                            typeDependencies.Add((type.FullName, typeName));

                        newTypeNode.Children.Add(tn);
                    }

                    newTypeNode.Children = newTypeNode.Children.OrderBy(c => c.Name).ToList();
                }

                return messageTypeNode;
            }
        }

        public static long CalculateHash(string text)
        {
            const int p = 31;
            const int m = 1000000000 + 9;
            long hash = 0;
            long pow = 1;

            foreach (char c in text)
            {
                hash = (hash + (c - 'a' + 1) * pow) % m;
                pow = (pow * p) % m;
            }

            return hash;
        }
    }
}
