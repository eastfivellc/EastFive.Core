using BlackBarLabs.Core.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace BlackBarLabs.Core.Reflection
{
    public static class ObjectBuilder
    {
        public static Type BuildType(IDictionary<string, Type> properties)
        {
            TypeBuilder tb = GetTypeBuilder();
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            #region Build Type and get methods for popluating properties

            var setMethods = properties
                .Select(
                    kvp =>
                    {
                        var fieldName = kvp.Key;
                        var fieldType = kvp.Value;
                        var setMethod = CreateProperty(tb, fieldName, fieldType);
                        return new KeyValuePair<string, MethodBuilder>(fieldName, setMethod);
                    })
                .ToArray();

            #endregion
            
            Type objectType = tb.CreateType();
            return objectType;
        }

        public static object CompileResultType(IDictionary<string, object> properties)
        {
            var propertyTypes = properties
                .Select(kvp => new KeyValuePair<string, Type>(kvp.Key, kvp.Value.GetType()))
                .ToDictionary();
            var objectType = BuildType(propertyTypes);
            var obj = Activator.CreateInstance(objectType);
            
            PopulateType(obj, properties);
            
            return obj;
        }

        public static object PopulateType(object obj, IDictionary<string, object> properties)
        {
            var objectProperties = obj.GetType().GetProperties();

            foreach (var objectProperty in objectProperties)
            {
                var value = properties[objectProperty.Name];
                objectProperty.SetValue(obj, value);
            }

            return obj;
        }

        private static TypeBuilder GetTypeBuilder()
        {
            var typeSignature = "MyDynamicType";
            var an = new AssemblyName(typeSignature);
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
            TypeBuilder tb = moduleBuilder.DefineType(typeSignature,
                    TypeAttributes.Public |
                    TypeAttributes.Class |
                    TypeAttributes.AutoClass |
                    TypeAttributes.AnsiClass |
                    TypeAttributes.BeforeFieldInit |
                    TypeAttributes.AutoLayout,
                    null);
            return tb;
        }

        private static MethodBuilder CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField("_" + propertyName, propertyType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);
            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
            ILGenerator getIl = getPropMthdBldr.GetILGenerator();

            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

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
            return setPropMthdBldr;
        }
    }
}
