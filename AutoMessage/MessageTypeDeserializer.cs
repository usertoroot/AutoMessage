﻿using AutoMessage.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AutoMessage
{
    public static class MessageTypeDeserializer
    {
        private const string DynamicAssemblyName = "SeedNet.Shared.SerializationDynamicDefinitions";
        private static Dictionary<string, Type> m_createdTypes = new Dictionary<string, Type>();

        private static int GetTypeDepth(MessageTypeNode messageTypeNode, int count = 0)
        {
            if (messageTypeNode.TypeName != "Array")
                return count + 1;

            return GetTypeDepth(messageTypeNode.Children[0], count + 1);
        }

        private static string GetElementTypeRecursive(MessageTypeNode messageTypeNode)
        {
            if (messageTypeNode.TypeName != "Array")
                return messageTypeNode.TypeName;

            return GetElementTypeRecursive(messageTypeNode.Children[0]);
        }

        private static Type GetTypeInAssembly(string name)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var searchForGeneric = name.Contains("<") && name.Contains(">");
                var arrayTokens = name.ContextAwareSplit("[]", '<', '>', false);
                var arrayDepth = arrayTokens.Length - 1;
                var baseName = arrayTokens.Length > 0 ? arrayTokens[0] : name;

                Type foundType = null;
                foreach (var type in assembly.GetTypes())
                {
                    if (searchForGeneric && !type.IsGenericType || !searchForGeneric && type.IsGenericType)
                        continue;

                    if (type.IsGenericType)
                    {
                        var typeName = MessageTypeSerializer.GetTypeName(type);
                        if (typeName.Split('<')[0] == baseName.Split('<')[0])
                        {
                            var genericArgumentsStartIndex = baseName.IndexOf('<');
                            var genericArgumentsEndIndex = baseName.LastIndexOf('>');
                            var genericArgumentString = baseName.Substring(genericArgumentsStartIndex + 1, genericArgumentsEndIndex - genericArgumentsStartIndex - 1);
                            var genericArgumentStrings = genericArgumentString.ContextAwareSplit(",", '<', '>', true);
                            var genericArguments = genericArgumentStrings.Select(ga => GetTypeInAssembly(ga)).ToArray();
                            if (genericArguments.Contains(null))
                                throw new Exception("Failed to find all generic argument types.");

                            var genericInstantiatedType = type.MakeGenericType(genericArguments);
                            foundType = genericInstantiatedType;
                            break;
                        }
                    }
                    else
                    {
                        var typeName = MessageTypeSerializer.GetTypeName(type);
                        if (typeName == baseName)
                        {
                            foundType = type;
                            break;
                        }
                    }
                }

                if (foundType != null)
                {
                    if (arrayDepth > 0)
                    {
                        var arrayType = foundType;

                        for (var ad = 0; ad < arrayDepth; ad++)
                            arrayType = foundType.MakeArrayType();

                        return arrayType;
                    }
                    else
                        return foundType;
                }
            }

            return null;
        }

        public static Type GetType(string name)
        {
            lock (m_createdTypes)
            {
                if (m_createdTypes.TryGetValue(name, out Type type))
                    return type;
            }

            Type t = GetTypeInAssembly(name);

            if (t == null)
                t = Type.GetType($"System.{name}");

            return t;
        }

        public static Type DeserializeType(MessageType messageType)
        {
            foreach (var messageTypeNode in messageType.Types)
                DeserializeType(messageType);

            return GetType(messageType.TypeName);
        }

        public static Type DeserializeType(MessageTypeNode messageTypeNode)
        {
            var existingType = GetType(messageTypeNode.TypeName);
            if (existingType != null)
                return existingType;

            TypeBuilder tb = GetTypeBuilder(messageTypeNode.TypeName);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            foreach (var field in messageTypeNode.Children)
            {
                Type t;
                if (field.TypeName.EndsWith("[]"))
                {
                    string elementTypeName = field.TypeName.Substring(0, field.TypeName.Length - 2);
                    t = GetType(elementTypeName);
                    t = t.MakeArrayType();
                }
                else
                {
                    t = GetType(field.TypeName);
                }

                if (t == null)
                    throw new Exception($"Failed to find type '{field.TypeName}'.");

                CreateProperty(tb, field.Name, t);
            }

            TypeInfo typeInfo = tb.CreateTypeInfo();
            Type createdType = typeInfo.AsType();

            lock (m_createdTypes)
                m_createdTypes.Add(messageTypeNode.TypeName, createdType);

            return createdType;
        }

        private static TypeBuilder GetTypeBuilder(string name)
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Guid.NewGuid().ToString()), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(name, TypeAttributes.Public |TypeAttributes.Class | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit | TypeAttributes.AutoLayout, null);
            return tb;
        }

        private static void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr = tb.DefineMethod("set_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getPropMthdBldr);
            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
    }
}
