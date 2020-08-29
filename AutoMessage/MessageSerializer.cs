using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Collections;

namespace AutoMessage
{
    public class MessageSerializer
    {
        private static Dictionary<string, Action<BinaryWriter, object>> NodeSerializers = new Dictionary<string, Action<BinaryWriter, object>>();
        private static Dictionary<string, Expression<Action<BinaryWriter, object>>> NodeSerializerExpressions = new Dictionary<string, Expression<Action<BinaryWriter, object>>>();
        private static Dictionary<string, Func<object, int>> NodeSizeCalculators = new Dictionary<string, Func<object, int>>();
        private static Dictionary<string, Expression<Func<object, int>>> NodeSizeCalculatorExpressions = new Dictionary<string, Expression<Func<object, int>>>();

        public static MessageType SerializeType(Type t)
        {
            return MessageTypeSerializer.SerializeType(t);
        }

        public static Action<BinaryWriter, object> GetSerializer(Type t)
        {
            Action<BinaryWriter, object> serializer;

            lock (NodeSerializers)
            {
                if (!NodeSerializers.TryGetValue(MessageTypeSerializer.GetTypeName(t), out serializer))
                    serializer = null;
            }

            if (serializer == null)
            {
                var messageType = SerializeType(t);

                foreach (var mt in messageType.Types)
                    CreateNodeSerializer(mt);

                lock (NodeSerializers)
                {
                    if (!NodeSerializers.TryGetValue(MessageTypeSerializer.GetTypeName(t), out serializer))
                        throw new Exception("Failed to create serializer.");
                }
            }

            return serializer;
        }

        public static Func<object, int> GetSizeCalculator(Type t)
        {
            Func<object, int> sizeCalculator;

            lock (NodeSizeCalculators)
            {
                if (!NodeSizeCalculators.TryGetValue(MessageTypeSerializer.GetTypeName(t), out sizeCalculator))
                    sizeCalculator = null;
            }

            if (sizeCalculator == null)
            {
                var messageType = SerializeType(t);

                foreach (var mt in messageType.Types)
                    CreateNodeSizeCalculator(mt);

                lock (NodeSizeCalculators)
                {
                    if (!NodeSizeCalculators.TryGetValue(MessageTypeSerializer.GetTypeName(t), out sizeCalculator))
                        throw new Exception("Failed to create serializer.");
                }
            }

            return sizeCalculator;
        }

        public static void Serialize(BinaryWriter writer, object o)
        {
            var serializer = GetSerializer(o.GetType());
            serializer(writer, o);
        }

        public static void Serialize(byte[] d, int offset, object o)
        {
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(d, offset, d.Length - offset))
            using (BinaryWriter writer = new BinaryWriter(stream))
                Serialize(writer, o);
        }

        public static byte[] Serialize(object o)
        {
            Type t = o.GetType();
            byte[] d = new byte[GetSize(o)];

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(d))
            using (BinaryWriter writer = new BinaryWriter(stream))
                Serialize(writer, o);

            return d;
        }

        public static int GetSize(object o)
        {
            var sizeCalculator = GetSizeCalculator(o.GetType());
            return sizeCalculator(o);
        }

        private static MethodInfo GetBinaryWriterMethod(Type t)
        {
            if (t == typeof(sbyte))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(sbyte) });
            else if (t == typeof(short))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(short) });
            else if (t == typeof(int))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(int) });
            else if (t == typeof(long))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(long) });
            else if (t == typeof(byte))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(byte) });
            else if (t == typeof(ushort))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(ushort) });
            else if (t == typeof(uint))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(uint) });
            else if (t == typeof(ulong))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(ulong) });
            else if (t == typeof(char))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(char) });
            else if (t == typeof(float))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(float) });
            else if (t == typeof(double))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(double) });
            else if (t == typeof(bool))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(bool) });
            else if (t == typeof(decimal))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(decimal) });
            else if (t == typeof(sbyte[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(sbyte[]) });
            else if (t == typeof(short[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(short[]) });
            else if (t == typeof(int[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(int[]) });
            else if (t == typeof(long[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(long[]) });
            else if (t == typeof(byte[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(byte[]) });
            else if (t == typeof(ushort[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(ushort[]) });
            else if (t == typeof(uint[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(uint[]) });
            else if (t == typeof(ulong[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(ulong[]) });
            else if (t == typeof(char[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(char[]) });
            else if (t == typeof(float[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(float[]) });
            else if (t == typeof(double[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(double[]) });
            else if (t == typeof(bool[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(bool[]) });
            else if (t == typeof(decimal[]))
                return typeof(BinaryWriter).GetMethod("Write", new Type[] { typeof(decimal[]) });
            else
                throw new Exception("Not a base type.");
        }

        private static Expression CallNodeSerializer(string typeName, Expression writer, Expression value)
        {
            Expression<Action<BinaryWriter, object>> nodeSerializer;

            lock (NodeSerializerExpressions)
            {   
                if (!NodeSerializerExpressions.TryGetValue(typeName, out nodeSerializer))
                    throw new Exception("Types registered in wrong order, is there a dependency tree problem?");
            }

            return Expression.Invoke(nodeSerializer, writer, Expression.TypeAs(value, typeof(object)));
        }

        private static Action<BinaryWriter, object> CreateNodeSerializer(MessageTypeNode messageTypeNode)
        {
            lock (NodeSerializers)
            {   
                if (NodeSerializers.TryGetValue(messageTypeNode.TypeName, out Action<BinaryWriter, object> serializer))
                    return serializer;
            }

            Type t = MessageTypeDeserializer.GetType(messageTypeNode.TypeName);
            if (t == null)
                throw new Exception("Type not existent.");

            var writer = Expression.Parameter(typeof(BinaryWriter), "writer");  
            var value = Expression.Parameter(typeof(object), "value");
            
            List<ParameterExpression> variables = new List<ParameterExpression>();
            List<Expression> expressions = new List<Expression>();

            var castedValue = Expression.Parameter(t, "castedValue");
            variables.Add(castedValue);
            expressions.Add(Expression.Assign(castedValue, Expression.TypeAs(value, t)));

            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetSetMethod() != null && p.GetGetMethod() != null).ToArray();

            foreach (var child in messageTypeNode.Children)
            {
                Expression source = GetFieldOrProperty(properties, fields, child.Name, castedValue);

                var childTypeName = child.TypeName;
                var childType = MessageTypeDeserializer.GetType(childTypeName);

                expressions.Add(Write(childTypeName, childType, writer, source));
            }

            var expressionTree = Expression.Lambda<Action<BinaryWriter, object>>(Expression.Block(variables, expressions), writer, value);

            lock (NodeSerializerExpressions)
                NodeSerializerExpressions.Add(messageTypeNode.TypeName, expressionTree);

            var func = expressionTree.Compile();  

            lock (NodeSerializers)
                NodeSerializers.Add(messageTypeNode.TypeName, func);

            return func;
        }

        private static Expression GetFieldOrProperty(PropertyInfo[] properties, FieldInfo[] fields, string name, Expression castedValue)
        {
            var property = properties.FirstOrDefault(p => p.Name == name);
            if (property != null)
                return Expression.Property(castedValue, property);
            else
            {
                var field = fields.FirstOrDefault(p => p.Name == name);

                if (field != null)
                    return Expression.Field(castedValue, field);
                else
                    throw new Exception("Field or property does not exist.");
            }
        }
        
        private static Expression Write(string typeName, Type type, Expression writer, Expression source)
        {
            if (typeName.EndsWith("[]"))
            {
                string elementTypeName = typeName.Substring(0, typeName.Length - 2);
                var elementType = MessageTypeDeserializer.GetType(elementTypeName);

                if (MessageTypeSerializer.PrimitiveArrayTypes.Contains(type) && !IsList(source))
                    return WritePrimitiveArray(type, writer, source);
                else
                    return WriteArray(elementTypeName, elementType, writer, source);
            }
            else
                return WriteNonArray(typeName, type, writer, source);
        }

        private static bool IsList(Expression source)
        {
            try
            {
                Expression.Property(source, "Length");
                return false;
            }
            catch
            {
                Expression.Property(source, "Count");
                return true;
            }
        }

        private static Expression GetLengthExpression(Expression source, bool isList)
        {
            if (isList)
                return Expression.Property(source, "Count");
            else
                return Expression.Property(source, "Length");
        }

        private static Expression GetArrayIndex(Expression source, ParameterExpression i, bool isList)
        {
            if (isList)
            {
                var getItemMethod = source.Type.GetMethod("get_Item", new Type[] { typeof(int) });
                return Expression.Call(source, getItemMethod, i);
            }
            else
                return Expression.ArrayIndex(source, i);
        }

        private static Expression WritePrimitiveArray(Type arrayType, Expression writer, Expression source)
        {
            bool isList = IsList(source);
            var lengthExpression = GetLengthExpression(source, isList);

            var writeMethod = GetBinaryWriterMethod(arrayType);
            if (writeMethod == null)
                throw new Exception("Not a valid type.");

            return Expression.Block(
                WriteFrom(typeof(int), writer, lengthExpression),
                Expression.Call(writer, writeMethod, source)
            );
        }

        private static Expression WriteArray(string elementTypeName, Type elementType, Expression writer, Expression source)
        {
            List<Expression> expressions = new List<Expression>();

            bool isList = IsList(source);
            var lengthExpression = GetLengthExpression(source, isList);
            expressions.Add(WriteFrom(typeof(int), writer, lengthExpression));
            
            var breakLabel = Expression.Label("LoopBreak");
            var i = Expression.Parameter(typeof(int), "i");
            expressions.Add(Expression.Assign(i, Expression.Constant(0)));

            var loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, lengthExpression),
                        Expression.Block(
                            Write(elementTypeName, elementType, writer, GetArrayIndex(source, i, isList)),
                            Expression.Assign(i, Expression.Increment(i))
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel);

            expressions.Add(loop);

            return Expression.Block(new ParameterExpression[] { i }, expressions);
        }

        private static Expression WriteNonArray(string typeName, Type type, Expression writer, Expression source)
        {
            if (MessageTypeSerializer.FinalTypes.Contains(type))
            {
                if (type == typeof(string))
                {
                    var toCharArrayMethod = typeof(string).GetMethod("ToCharArray", new Type[] { });
                    return WriteArray("Char", typeof(char), writer, Expression.Call(source, toCharArrayMethod));
                }

                return WriteFrom(type, writer, source);
            }
            else
                return CallNodeSerializer(typeName, writer, source);
        }

        private static Expression WriteFrom(Type typeToWrite, Expression writer, Expression source)
        {
            var writeMethod = GetBinaryWriterMethod(typeToWrite);
            if (typeToWrite == typeof(string) || !MessageTypeSerializer.FinalTypes.Contains(typeToWrite))
                throw new Exception("Not a valid type.");

            return Expression.Call(writer, writeMethod, source);
        }

        private static Func<object, int> CreateNodeSizeCalculator(MessageTypeNode messageTypeNode)
        {
            lock (NodeSizeCalculators)
            {   
                if (NodeSizeCalculators.TryGetValue(messageTypeNode.TypeName, out var nodeSizeCalculator))
                    return nodeSizeCalculator;
            }
            
            Type t = MessageTypeDeserializer.GetType(messageTypeNode.TypeName);
            if (t == null)
                throw new Exception("Type not existent.");

            var value = Expression.Parameter(typeof(object), "value");
            
            List<ParameterExpression> variables = new List<ParameterExpression>();
            List<Expression> expressions = new List<Expression>();
            LabelTarget returnTarget = Expression.Label(typeof(int));

            var castedValue = Expression.Parameter(t, "castedValue");
            variables.Add(castedValue);
            expressions.Add(Expression.Assign(castedValue, Expression.TypeAs(value, t)));

            var sum = Expression.Parameter(typeof(int), "sum");
            variables.Add(sum);
            expressions.Add(Expression.Assign(sum, Expression.Constant(0)));

            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetSetMethod() != null && p.GetGetMethod() != null).ToArray();

            foreach (var child in messageTypeNode.Children)
            {
                Expression source = GetFieldOrProperty(properties, fields, child.Name, castedValue);

                var childTypeName = child.TypeName;
                var childType = MessageTypeDeserializer.GetType(childTypeName);

                expressions.Add(Expression.AddAssign(sum, CalculateSize(childTypeName, childType, source)));
            }

            expressions.Add(Expression.Return(returnTarget, sum));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(-1)));

            var expressionTree = Expression.Lambda<Func<object, int>>(Expression.Block(variables, expressions), value);

            lock (NodeSizeCalculatorExpressions)
                NodeSizeCalculatorExpressions.Add(messageTypeNode.TypeName, expressionTree);

            var func = expressionTree.Compile();

            lock (NodeSizeCalculators)
                NodeSizeCalculators.Add(messageTypeNode.TypeName, func);

            return func;
        }

        private static Expression CalculateSize(string typeName, Type type, Expression source)
        {
            if (typeName.EndsWith("[]"))
                return CalculateArraySize(typeName, type, source);
            else
                return CalculateNonArraySize(typeName, type, source);
        }

        private static Expression CalculateArraySize(string typeName, Type type, Expression source)
        {
            List<Expression> expressions = new List<Expression>();
            LabelTarget returnTarget = Expression.Label(typeof(int));

            string elementTypeName = typeName.Substring(0, typeName.Length - 2);
            var elementType = MessageTypeDeserializer.GetType(elementTypeName);

            bool isList = IsList(source);
            var lengthExpression = GetLengthExpression(source, isList);

            var breakLabel = Expression.Label("LoopBreak");
            var i = Expression.Parameter(typeof(int), "i");
            var sum = Expression.Parameter(typeof(int), "sum");
            expressions.Add(Expression.Assign(i, Expression.Constant(0)));
            expressions.Add(Expression.Assign(sum, Expression.Constant(sizeof(int))));

            var loop = Expression.Loop(
                Expression.IfThenElse(
                    Expression.LessThan(i, lengthExpression),
                        Expression.Block(
                            Expression.AddAssign(sum, CalculateSize(elementTypeName, elementType, GetArrayIndex(source, i, isList))),
                            Expression.Assign(i, Expression.Increment(i))
                        ),
                        Expression.Break(breakLabel)
                    ),
                breakLabel);

            expressions.Add(loop);

            expressions.Add(Expression.Return(returnTarget, sum));
            expressions.Add(Expression.Label(returnTarget, Expression.Constant(-1)));


            return Expression.Block(new ParameterExpression[] { sum, i }, expressions);
        }

        private static Expression CalculateNonArraySize(string typeName, Type type, Expression source)
        {
            if (MessageTypeSerializer.FinalTypes.Contains(type))
            {
                if (type == typeof(string))
                {
                    var toCharArrayMethod = typeof(string).GetMethod("ToCharArray", new Type[] { });
                    return CalculateArraySize("Char[]", typeof(char[]), Expression.Call(source, toCharArrayMethod));
                }

                return GetSizeOfType(type);
            }
            else
                return CallNodeSizeCalculator(typeName, source);
        }

        private static Expression GetSizeOfType(Type typeToWrite)
        {
            var writeMethod = GetWrittenSize(typeToWrite);
            if (typeToWrite == typeof(string) || !MessageTypeSerializer.FinalTypes.Contains(typeToWrite))
                throw new Exception("Not a valid type.");

            return Expression.Constant(GetWrittenSize(typeToWrite));
        }

        private static int GetWrittenSize(Type t)
        {
            if (t == typeof(sbyte))
                return sizeof(sbyte);
            else if (t == typeof(short))
                return sizeof(short);
            else if (t == typeof(int))
                return sizeof(int);
            else if (t == typeof(long))
                return sizeof(long);
            else if (t == typeof(byte))
                return sizeof(byte);
            else if (t == typeof(ushort))
                return sizeof(ushort);
            else if (t == typeof(uint))
                return sizeof(uint);
            else if (t == typeof(ulong))
                return sizeof(ulong);
            else if (t == typeof(char))
                return 1; //return sizeof(char);
            else if (t == typeof(float))
                return sizeof(float);
            else if (t == typeof(double))
                return sizeof(double);
            else if (t == typeof(bool))
                return sizeof(bool);
            else
                throw new Exception("Not a base type.");
        }

        private static Expression CallNodeSizeCalculator(string typeName, Expression value)
        {
            Expression<Func<object, int>> nodeLengthCalculator;

            lock (NodeSizeCalculatorExpressions)
            {   
                if (!NodeSizeCalculatorExpressions.TryGetValue(typeName, out nodeLengthCalculator))
                    throw new Exception("Types registered in wrong order, is there a dependency tree problem?");
            }

            return Expression.Invoke(nodeLengthCalculator, Expression.TypeAs(value, typeof(object)));
        }
    }
}
