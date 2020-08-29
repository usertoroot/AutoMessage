using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoMessage
{
    public class MessageDeserializer
    {
        private static Dictionary<string, Func<BinaryReader, object>> NodeDeserializers = new Dictionary<string, Func<BinaryReader, object>>();
        private static Dictionary<string, Expression<Func<BinaryReader, object>>> NodeDeserializerExpressions = new Dictionary<string, Expression<Func<BinaryReader, object>>>();

        public static Type DeserializeType(MessageType messageType)
        {
            Type existingType = MessageTypeDeserializer.GetType(messageType.TypeName);
            if (existingType != null)
                return existingType;

            foreach (var messageTypeNode in messageType.Types)
                MessageTypeDeserializer.DeserializeType(messageTypeNode);

            return MessageTypeDeserializer.GetType(messageType.TypeName);
        }

        public static Func<BinaryReader, object> GetDeserializer(Type t)
        {
            Func<BinaryReader, object> deserializer;

            lock (NodeDeserializers)
            {
                if (!NodeDeserializers.TryGetValue(MessageTypeSerializer.GetTypeName(t), out deserializer))
                    deserializer = null;
            }

            if (deserializer == null)
            {
                var messageType = MessageTypeSerializer.SerializeType(t);

                foreach (var mt in messageType.Types)
                    CreateNodeDeserializer(mt);

                lock (NodeDeserializers)
                {
                    if (!NodeDeserializers.TryGetValue(MessageTypeSerializer.GetTypeName(t), out deserializer))
                        throw new Exception("Failed to create deserializer.");
                }
            }

            return deserializer;
        }

        public static object Deserialize(BinaryReader reader, Type t)
        {
            var deserializer = GetDeserializer(t);
            return deserializer(reader);
        }

        public static T Deserialize<T>(BinaryReader reader)
        {
            var deserializer = GetDeserializer(typeof(T));
            return (T)deserializer(reader);
        }

        public static object Deserialize(byte[] d, Type t)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(d))
            using (BinaryReader reader = new BinaryReader(stream))
                return Deserialize(reader, t);
        }

        public static T Deserialize<T>(byte[] d)
        {
            return (T)Deserialize(d, typeof(T));
        }

        private static MethodInfo GetBinaryReaderMethod(Type t)
        {
            if (t == typeof(sbyte))
                return typeof(BinaryReader).GetMethod("ReadSByte");
            else if (t == typeof(short))
                return typeof(BinaryReader).GetMethod("ReadInt16");
            else if (t == typeof(int))
                return typeof(BinaryReader).GetMethod("ReadInt32");
            else if (t == typeof(long))
                return typeof(BinaryReader).GetMethod("ReadInt64");
            else if (t == typeof(byte))
                return typeof(BinaryReader).GetMethod("ReadByte");
            else if (t == typeof(ushort))
                return typeof(BinaryReader).GetMethod("ReadUInt16");
            else if (t == typeof(uint))
                return typeof(BinaryReader).GetMethod("ReadUInt32");
            else if (t == typeof(ulong))
                return typeof(BinaryReader).GetMethod("ReadUInt64");
            else if (t == typeof(char))
                return typeof(BinaryReader).GetMethod("ReadChar");
            else if (t == typeof(float))
                return typeof(BinaryReader).GetMethod("ReadSingle");
            else if (t == typeof(double))
                return typeof(BinaryReader).GetMethod("ReadDouble");
            else if (t == typeof(bool))
                return typeof(BinaryReader).GetMethod("ReadBoolean");
            else if (t == typeof(sbyte[]))
                return typeof(BinaryReader).GetMethod("ReadSBytes");
            else if (t == typeof(short[]))
                return typeof(BinaryReader).GetMethod("ReadInt16s");
            else if (t == typeof(int[]))
                return typeof(BinaryReader).GetMethod("ReadInt32s");
            else if (t == typeof(long[]))
                return typeof(BinaryReader).GetMethod("ReadInt64s");
            else if (t == typeof(byte[]))
                return typeof(BinaryReader).GetMethod("ReadBytes");
            else if (t == typeof(ushort[]))
                return typeof(BinaryReader).GetMethod("ReadUInt16s");
            else if (t == typeof(uint[]))
                return typeof(BinaryReader).GetMethod("ReadUInt32s");
            else if (t == typeof(ulong[]))
                return typeof(BinaryReader).GetMethod("ReadUInt64s");
            else if (t == typeof(char[]))
                return typeof(BinaryReader).GetMethod("ReadChars");
            else if (t == typeof(float[]))
                return typeof(BinaryReader).GetMethod("ReadSingles");
            else if (t == typeof(double[]))
                return typeof(BinaryReader).GetMethod("ReadDoubles");
            else if (t == typeof(bool[]))
                return typeof(BinaryReader).GetMethod("ReadBooleans");
            else
                throw new Exception("Not a base type.");
        }

        private static Expression CallNodeDeserializer(string typeName, Expression reader, Expression target)
        {
            Expression<Func<BinaryReader, object>> nodeDeserializer;

            lock (NodeDeserializerExpressions)
            {   
                if (!NodeDeserializerExpressions.TryGetValue(typeName, out nodeDeserializer))
                    throw new Exception("Types registered in wrong order, is there a dependency tree problem?");
            }

            return Expression.Assign(target, Expression.TypeAs(Expression.Invoke(nodeDeserializer, reader), MessageTypeDeserializer.GetType(typeName)));
        }

        private static Func<BinaryReader, object> CreateNodeDeserializer(MessageTypeNode messageTypeNode)
        {
            lock (NodeDeserializers)
            {   
                if (NodeDeserializers.TryGetValue(messageTypeNode.TypeName, out Func<BinaryReader, object> deserializer))
                    return deserializer;
            }

            Type t = MessageTypeDeserializer.GetType(messageTypeNode.TypeName);
            if (t == null)
                throw new Exception("Type not existent.");

            var reader = Expression.Parameter(typeof(BinaryReader), "reader");  

            List<ParameterExpression> variables = new List<ParameterExpression>();
            List<Expression> expressions = new List<Expression>();

            LabelTarget returnTarget = Expression.Label(typeof(object));

            var value = Expression.Parameter(t, "value");
            variables.Add(value);
            expressions.Add(Expression.Assign(value, Expression.New(t)));

            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetSetMethod() != null && p.GetGetMethod() != null).ToArray();

            foreach (var child in messageTypeNode.Children)
            {
                Expression target = GetFieldOrProperty(properties, fields, child.Name, value);

                var childTypeName = child.TypeName;
                var childType = MessageTypeDeserializer.GetType(childTypeName);

                expressions.Add(Read(childTypeName, childType, reader, target));
            }

            expressions.Add(Expression.Return(returnTarget, Expression.TypeAs(value, typeof(object))));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(null)));

            var expressionTree = Expression.Lambda<Func<BinaryReader, object>>(Expression.Block(variables, expressions), reader);

            lock (NodeDeserializerExpressions)
                NodeDeserializerExpressions.Add(messageTypeNode.TypeName, expressionTree);

            var func = expressionTree.Compile();

            lock (NodeDeserializers)
                NodeDeserializers.Add(messageTypeNode.TypeName, func);

            return func;  
        }

        private static Expression Read(string typeName, Type type, Expression reader, Expression target)
        {
            if (typeName.EndsWith("[]"))
            {
                string elementTypeName = typeName.Substring(0, typeName.Length - 2);
                var elementType = MessageTypeDeserializer.GetType(elementTypeName);

                if (MessageTypeSerializer.PrimitiveArrayTypes.Contains(type) && !IsList(target))
                    return ReadPrimitiveArray(type, reader, target);
                else
                    return ReadArray(elementTypeName, elementType, reader, target);
            }
            else
                return ReadNonArray(typeName, type, reader, target);
        }

        private static bool IsList(Expression target)
        {
            try
            {
                Expression.Property(target, "Length");
                return false;
            }
            catch
            {
                Expression.Property(target, "Count");
                return true;
            }
        }

        private static Expression MakeArray(Type elementType, Expression length, bool isList)
        {
            if (isList)
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var constructor = listType.GetConstructor(new Type[] { elementType.MakeArrayType() });
                return Expression.New(constructor, Expression.NewArrayBounds(elementType, new Expression[] { length }));
            }
            else
                return Expression.NewArrayBounds(elementType, new Expression[] { length });
        }

        private static Expression ArrayAccess(Type elementType, bool isList, Expression target, Expression i)
        {
            if (isList)
            {
                var listType = typeof(List<>).MakeGenericType(elementType);
                var indexer = listType.GetProperty("Item", new Type[] { typeof(int) });
                return Expression.Property(target, indexer, new Expression[] { i });
            }
            else
                return Expression.ArrayAccess(target, i);
        }

        private static Expression ReadPrimitiveArray(Type arrayType, Expression reader, Expression target)
        {
            var length = Expression.Parameter(typeof(int), "length");

            var readMethod = GetBinaryReaderMethod(arrayType);
            if (readMethod == null)
                throw new Exception("Not a valid type.");

            return Expression.Block(
                new ParameterExpression[]
                {
                    length
                },
                new Expression[]
                {
                    ReadInto(typeof(int), reader, length),
                    Expression.Assign(target, Expression.Call(reader, readMethod, length))
                }
            );
        }

        private static Expression ReadArray(string elementTypeName, Type elementType, Expression reader, Expression target)
        {
            List<Expression> expressions = new List<Expression>();

            bool isList = IsList(target);

            var length = Expression.Parameter(typeof(int), "length");
            expressions.Add(ReadInto(typeof(int), reader, length));
            expressions.Add(Expression.Assign(target, MakeArray(elementType, length, isList)));

            var breakLabel = Expression.Label("LoopBreak");
            var i = Expression.Parameter(typeof(int), "i");
            expressions.Add(Expression.Assign(i, Expression.Constant(0)));

            var loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, length),
                        Expression.Block(
                            Read(elementTypeName, elementType, reader, ArrayAccess(elementType, isList, target, i)),
                            Expression.Assign(i, Expression.Increment(i))
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel);

            expressions.Add(loop);

            return Expression.Block(new ParameterExpression[] { i, length }, expressions);
        }

        private static Expression ReadNonArray(string typeName, Type type, Expression reader, Expression target)
        {
            if (MessageTypeSerializer.FinalTypes.Contains(type))
            {
                if (type == typeof(string))
                {
                    var charArray = Expression.Parameter(typeof(char[]), "charArray");

                    return Expression.Block(new [] { charArray }, new Expression[]
                    {
                        ReadArray("Char", typeof(char), reader, charArray),
                        Expression.Assign(target, Expression.New(typeof(string).GetConstructor(new Type[] { typeof(char[]), typeof(int), typeof(int) }), new Expression[] { charArray, Expression.Constant(0), Expression.Property(charArray, "Length") }))
                    });
                }

                return ReadInto(type, reader, target);
            }
            else
                return CallNodeDeserializer(typeName, reader, target);
        }

        private static Expression GetFieldOrProperty(PropertyInfo[] properties, FieldInfo[] fields, string name, Expression value)
        {
            var property = properties.FirstOrDefault(p => p.Name == name);
            if (property != null)
                return Expression.Property(value, property);
            else
            {
                var field = fields.FirstOrDefault(p => p.Name == name);

                if (field != null)
                    return Expression.Field(value, field);
                else
                    throw new Exception("Field or property does not exist.");
            }
        }

        private static Expression ReadInto(Type typeToRead, Expression reader, Expression target)
        {
            var readMethod = GetBinaryReaderMethod(typeToRead);
            if (typeToRead == typeof(string) || !MessageTypeSerializer.FinalTypes.Contains(typeToRead))
                throw new Exception("Not a valid type.");

            return Expression.Assign(target, Expression.Call(reader, readMethod));
        }
    }
}
