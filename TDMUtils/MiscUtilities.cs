
using Newtonsoft.Json;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;

namespace TDMUtils
{
    public static class MiscUtilities
    {
        /// <summary>
        /// Creates a deep copy of an object using serialization.
        /// Deprecated: Use <see cref="SerializeConvert{T}(object)"/> instead.
        /// </summary>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>A deep copy of the given object.</returns>
        public static T DeepClone<T>(this T obj) => obj!.SerializeConvert<T>()!;

        /// <summary>
        /// Converts an object to a specified type using JSON serialization and deserialization.
        /// </summary>
        /// <typeparam name="T">The target type for conversion.</typeparam>
        /// <param name="source">The source object to convert.</param>
        /// <returns>The converted object of type <typeparamref name="T"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if deserialization fails.</exception>
        public static T? SerializeConvert<T>(this object source)
        {
            string serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        /// Determines if an object exists within a given list of values.
        /// </summary>
        /// <typeparam name="T">The type of objects being compared.</typeparam>
        /// <param name="obj">The object to check.</param>
        /// <param name="args">The list of objects to search within.</param>
        /// <returns>True if the object exists in the list; otherwise, false.</returns>
        public static bool In<T>(this T obj, params T[] args) => args.Contains(obj);
        /// <summary>
        /// Serializes an object and prints the formatted JSON output to the debug console.
        /// </summary>
        /// <param name="o">The object to serialize and print.</param>
        public static void PrintObjectToConsole(object o)
        {
            Debug.WriteLine(DataFileUtilities.ToFormattedJson(o));
        }
        /// <summary>
        /// Evaluates whether an object is 'truthy' or 'falsy', similar to Python.
        /// If the value cannot be determined, the provided default value is returned.
        /// </summary>
        /// <param name="value">The object to evaluate.</param>
        /// <param name="defaultValue">The default value to return if the evaluation is indeterminate.</param>
        /// <returns>True if the object is considered 'truthy', false if 'falsy', or the provided default value.</returns>
        public static bool? IsTruthy(this object? value, bool? defaultValue = null)
        {
            if (value is null) return defaultValue; // Null is undefined

            return value switch
            {
                bool b => b,                           // True and False as-is
                string s => !string.IsNullOrWhiteSpace(s) && s.Trim().ToLowerInvariant() switch
                {
                    "false" or "no" or "off" or "0" => false,  // Common falsy values
                    "true" or "yes" or "on" or "1" => true,    // Common truthy values
                    _ => true // Any other non-empty string is truthy
                },
                IConvertible convertible when TryConvert(convertible, out double result)
                    => result > 0, // Convert any number type to double for truthy evaluation
                IEnumerable<object> collection => collection.Any(), // Non-empty generic collections are truthy
                System.Collections.IEnumerable nonGenericCollection => nonGenericCollection.GetEnumerator().MoveNext(), // Non-generic collections
                _ when value.GetType().IsValueType && value.GetType().GetFields().All(f => f.GetValue(value) == null) => false, // Empty struct instances
                _ => defaultValue // Anything else falls back to the default value
            };
        }
        /// <summary>
        /// Converts an object to a 32-bit integer representation.
        /// Throws an exception if conversion is not possible.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>An integer representation of the object.</returns>
        /// <exception cref="ArgumentException">Thrown if conversion fails.</exception>
        public static int AsIntValue(this object value)
        {
            return value switch
            {
                int i => i,                 // Already an int
                bool b => b ? 1 : 0,        // Convert boolean to 1 or 0
                string s when int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
                    => result,              // Convert valid string representations of integers
                IConvertible convertible when TryConvert(convertible, out double result)
                    => (int)result,              // Convert any numeric type
                _ => throw new ArgumentException($"{value?.GetType().Name} '{value}' cannot be converted to an int.")
            };
        }

        /// <summary>
        /// Converts an object to a 64-bit integer representation.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private static bool TryConvert(IConvertible value, out double result)
        {
            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                result = 0;
                return false;
            }
        }


        /// <summary>
        /// Determines whether a given JSON string can be deserialized into the specified type.
        /// </summary>
        /// <typeparam name="T">The target type to check against.</typeparam>
        /// <param name="json">The JSON string to validate.</param>
        /// <returns>True if the JSON can be deserialized into the given type; otherwise, false.</returns>
        public static bool IsJsonTypeOf<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                _ = JsonConvert.DeserializeObject<T>(json);
                return true;
            }
            catch { return false; }
        }
        /// <summary>
        /// Checks whether a dynamic object contains a specified property.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>True if the object has the specified property; otherwise, false.</returns>
        public static bool HasProperty(dynamic obj, string propertyName)
        {
            if (obj is null || string.IsNullOrWhiteSpace(propertyName)) return false;
            return obj switch
            {
                ExpandoObject e => ((IDictionary<string, object>)e!).ContainsKey(propertyName),
                _ => obj.GetType().GetProperty(propertyName) is not null
            };
        }
        /// <summary>
        /// Checks whether a dynamic object contains a specified method.
        /// </summary>
        /// <param name="obj">The object to inspect.</param>
        /// <param name="methodName">The name of the method to check.</param>
        /// <returns>True if the object has the specified method; otherwise, false.</returns>
        public static bool HasMethod(dynamic obj, string methodName)
        {
            if (obj is null || string.IsNullOrWhiteSpace(methodName)) return false;
            return obj.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance) is not null;
        }
        /// <summary>
        /// Determines whether a thread is alive and the associated object is a valid Windows Forms control.
        /// Ensures that if the object is a Windows Forms control, its handle is created.
        /// </summary>
        /// <param name="thread">The thread to check.</param>
        /// <param name="control">The Windows Forms control to verify.</param>
        /// <returns>True if the thread is alive and the control is valid; otherwise, false.</returns>
        public static bool IsWinFormsControlAccessible(Thread? thread, dynamic control)
        {
            if (thread is null || control is null) return false;

            bool isControlValid = !HasProperty(control, "IsHandleCreated") || control.IsHandleCreated;
            return thread.IsAlive && isControlValid;
        }
    }
}
