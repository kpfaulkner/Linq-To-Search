using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Dynamic;

namespace LinqTest
{
    // no idea what sort of performance hit we'll get doing all this dynamic conversion... but want to at least
    // give it a go. 
    class DynamicHelper
    {

        
        // create a type... for future use.
        public static Type CreateClass(Dictionary<string, string> dic )
        {

            var className = dic.GetHashCode().ToString();

            return CreateClass(dic, className);
        }

        // create a type... for future use.
        public static Type CreateClass(Dictionary<string, string> dic, string className)
        {
            AssemblyName an = new AssemblyName("DynamicAssembly");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder mb = ab.DefineDynamicModule("DynamicModule");

            //TypeBuilder tb = mb.DefineType("Class_" + dic.GetHashCode(), TypeAttributes.Public);
            TypeBuilder tb = mb.DefineType(className , TypeAttributes.Public);

            ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            ILGenerator ilg = cb.GetILGenerator();
            ilg.Emit(OpCodes.Ldarg_0);
            ilg.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            ilg.Emit(OpCodes.Ret);

            foreach (var kvp in dic)
            {
                FieldBuilder fb = tb.DefineField(kvp.Key, typeof(String), FieldAttributes.Public);
            }

            Type type = tb.CreateType();

            return type;
        }

        // create instance of a given type... then populate it.
        public static object CreateInstance(Dictionary<string, string> dic, Type type)
        {

            var result = Activator.CreateInstance(type);

            foreach (var fi in type.GetFields())
            {
                fi.SetValue(result, dic[fi.Name]);
            }

            return result;
        }

        // generate type then populate it,.
        public static object CreateInstance(Dictionary<string, string> dic)
        {

            var type = CreateClass(dic);

            var result = Activator.CreateInstance(type);

            foreach (var fi in type.GetFields())
            {
                fi.SetValue(result, dic[fi.Name]);
            }

            return result;
        }

    }

        // The class derived from DynamicObject.
        public class DynamicDictionary : DynamicObject
        {

            // hack...  just so LINQ has something to query against. 
            // shouldn't be used in reality.
            public string Query { get; set; }

            // The inner dictionary.
            Dictionary<string, object> dictionary
                = new Dictionary<string, object>();

            // This property returns the number of elements
            // in the inner dictionary.
            public int Count
            {
                get
                {
                    return dictionary.Count;
                }
            }

            // If you try to get a value of a property 
            // not defined in the class, this method is called.
            // try a fake entry to return empty or null if member var doesn't exist?
            public override bool TryGetMember(
                GetMemberBinder binder, out object result)
            {
                // Converting the property name to lowercase
                // so that property names become case-insensitive.
                string name = binder.Name.ToLower();

                // If the property name is found in a dictionary,
                // set the result parameter to the property value and return true.
                // Otherwise, return false.

                return dictionary.TryGetValue(name, out result);
            }

            // If you try to set a value of a property that is
            // not defined in the class, this method is called.
            public override bool TrySetMember(
                SetMemberBinder binder, object value)
            {
                // Converting the property name to lowercase
                // so that property names become case-insensitive.
                dictionary[binder.Name.ToLower()] = value;

                // You can always add a value to a dictionary,
                // so this method always returns true.
                return true;
            }
        }






    
}

