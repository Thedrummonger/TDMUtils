using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class MiscUtilities
    {
        /// <summary>
        /// Deprecated, Use SerializeConvert.
        /// </summary>
        /// <returns></returns>
        public static T DeepClone<T>(this T obj) => MiscUtilities.SerializeConvert<T>(obj);
        /// <summary>
        /// Converts and object to a given type using serialization
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="source">The source Object</param>
        /// <returns></returns>
        public static T SerializeConvert<T>(this object source)
        {
            string Serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(Serialized);
        }
        /// <summary>
        /// Checks if the given object is in the given list of values
        /// </summary>
        /// <typeparam name="T">The type of objects being compared</typeparam>
        /// <param name="obj">The object to check</param>
        /// <param name="args">The list of objects that may contain the given object</param>
        /// <returns></returns>
        public static bool In<T>(this T obj, params T[] args) => args.Contains(obj);
        /// <summary>
        /// Serialized the given object and prints it to the debug window
        /// </summary>
        /// <param name="o">Source object</param>
        public static void PrintObjectToConsole(object o)
        {
            Debug.WriteLine(DataFileUtilities.ToFormattedJson(o));
        }
        /// <summary>
        /// Interprets the given object as a 'truthy' or 'Falsey' value and return the value as a bool
        /// </summary>
        /// <param name="val">The value to parse</param>
        /// <param name="Default">vlaue to return if the object can not be interpreted as truthy or falsey. Throws an error if null and conversion fails</param>
        /// <returns>the boolean interpretation of the given object</returns>
        /// <exception cref="Exception"></exception>
        public static bool IsTruthy(this object val, bool? Default = null)
        {
            var result = Default;
            if (val is bool boolVal) { result = (boolVal); }
            else if (val is int IntBoolVal) { result = (IntBoolVal > 0); }
            else if (val is Int64 Int64BoolVal) { result = (Int64BoolVal > 0); }
            else if (val is float FloatBoolVal) { result = (FloatBoolVal > 0); }
            else if (val is double DoubleBoolVal) { result = (DoubleBoolVal > 0); }
            else if (val is decimal DecimalBoolVal) { result = (DecimalBoolVal > 0); }
            else if (val is string StringBoolVal && bool.TryParse(StringBoolVal, out bool rb)) { result = (rb); }

            if (result is null) { throw new Exception($"{val.GetType().Name} {val} Was not a valid truthy Value"); }
            return (bool)result;
        }
        /// <summary>
        /// Attempts to parse an integer 32 representation of the given object
        /// </summary>
        /// <param name="_Value"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static int AsIntValue(this object _Value)
        {
            if (_Value is int i1) { return (i1); }
            else if (_Value is Int64 i64) { return (Convert.ToInt32(i64)); }
            else if (_Value is string str && int.TryParse(str, out int istr)) { return (istr); }
            else if (_Value is bool bval) { return (bval ? 1 : 0); }
            else { throw new Exception($"{_Value.GetType().Name} {_Value} Could not be applied to an int option"); }
        }
        /// <summary>
        /// Checks if the given serialized object can deserialize to the given type
        /// </summary>
        /// <typeparam name="T">Type to test the object against</typeparam>
        /// <param name="Json">The source json string</param>
        /// <returns></returns>
        public static bool isJsonTypeOf<T>(string Json)
        {
            try
            {
                T test = JsonConvert.DeserializeObject<T>(Json, NewtonsoftExtensions.DefaultSerializerSettings);
                return test != null;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Checks if the given dynamic object has the given property
        /// </summary>
        /// <param name="Object">The source object</param>
        /// <param name="name">The name of the property to check for</param>
        /// <returns></returns>
        public static bool DynamicPropertyExist(dynamic Object, string name)
        {
            if (Object is null) { return false; }
            if (Object is ExpandoObject)
                return ((IDictionary<string, object>)Object).ContainsKey(name);

            var type = Object.GetType();
            return type.GetProperty(name) != null;
        }
        /// <summary>
        /// Checks if the given dynamic object has the given method
        /// </summary>
        /// <param name="Object">The source object</param>
        /// <param name="methodName">The name of the method to check for</param>
        /// <returns></returns>
        public static bool DynamicMethodExists(dynamic Object, string methodName)
        {
            if (Object is null) { return false; }
            var type = Object.GetType();
            return type.GetMethod(methodName) != null;
        }
        /// <summary>
        /// Check if the given thread is alive and the object still exists
        /// </summary>
        /// <param name="thread">The source thread</param>
        /// <param name="Obj">The source object</param>
        /// <returns></returns>
        public static bool OBJIsThreadSafe(Thread thread, dynamic Obj)
        {
            bool IsWinformSafe = Obj is not null && (!MiscUtilities.DynamicPropertyExist(Obj, "IsHandleCreated") || Obj.IsHandleCreated);
            return thread is not null && thread.IsAlive && Obj is not null && IsWinformSafe;
        }
    }
}
