
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
        /// Creates a deep copy of an object by serializing and deserializing it.
        /// </summary>
        /// <remarks>
        /// This method performs a full deep clone of the object graph.
        /// <para>
        /// If the <c>MessagePack</c> library is present at runtime, a high-performance
        /// binary clone is used automatically (contractless mode).
        /// Otherwise, the method falls back to JSON-based cloning via
        /// <see cref="SerializeConvert{T}(object)"/>.
        /// </para>
        /// </remarks>
        /// <typeparam name="T">The type of the object to clone.</typeparam>
        /// <param name="obj">The object to clone.</param>
        /// <returns>
        /// A new instance that is a deep copy of <paramref name="obj"/>.
        /// </returns>
        public static T DeepClone<T>(this T obj)
        {
            if (_messagePackClone != null)
                return (T)_messagePackClone(obj!);

            return obj!.SerializeConvert<T>()!;
        }
        private static readonly Func<object, object>? _messagePackClone = CreateMessagePackCloner();
        private static Func<object, object>? CreateMessagePackCloner()
        {
            var serializerType = Type.GetType("MessagePack.MessagePackSerializer, MessagePack");
            var resolverType = Type.GetType("MessagePack.Resolvers.ContractlessStandardResolver, MessagePack");
            var optionsType = Type.GetType("MessagePack.MessagePackSerializerOptions, MessagePack");
            if (serializerType == null || resolverType == null || optionsType == null) return null;
            try
            {
                var resolverInstance = resolverType.GetProperty("Instance")!.GetValue(null);
                var standardOptions = optionsType.GetProperty("Standard")!.GetValue(null);
                var withResolver = optionsType.GetMethod("WithResolver")!;
                var options = withResolver.Invoke(standardOptions, [resolverInstance]);
                var serialize = serializerType.GetMethod("Serialize", [typeof(object), optionsType]);
                var deserialize = serializerType.GetMethod("Deserialize", [typeof(byte[]), optionsType]);
                if (serialize == null || deserialize == null)return null;
                return obj =>
                {
                    var bytes = (byte[])serialize.Invoke(null, [obj, options])!;
                    return deserialize.Invoke(null, [bytes, options])!;
                };
            }
            catch
            {
                return null;
            }
        }


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
        /// Evaluates whether an object is truthy or falsy using Python-like semantics.
        /// </summary>
        /// <remarks>
        /// The following rules are applied:
        /// <list type="bullet">
        /// <item><description><c>null</c> is falsy.</description></item>
        /// <item><description><see cref="bool"/> values are returned as-is.</description></item>
        /// <item><description>Strings are truthy if their length is greater than zero; empty strings are falsy.</description></item>
        /// <item><description>Numeric values (via <see cref="IConvertible"/>) are falsy if equal to zero; otherwise truthy.</description></item>
        /// <item><description>Collections are falsy if empty; otherwise truthy.</description></item>
        /// <item><description>All other objects are considered truthy.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="value">The object to evaluate.</param>
        /// <returns><c>true</c> if the object is considered truthy; otherwise, <c>false</c>.</returns>
        public static bool IsTruthy(this object? value)
        {
            return value switch
            {
                null => false,
                bool b => b,
                string s => s.Length > 0,
                IConvertible convertible when convertible.TryAsDoubleValue(out double result) => result != 0,
                System.Collections.ICollection collection => collection.Count > 0,
                System.Collections.IEnumerable collection => collection.GetEnumerator().MoveNext(),
                _ => true
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
            if (value.TryAsIntValue(out int result))
                return result;

            throw new ArgumentException($"{value?.GetType().Name} '{value}' cannot be converted to an int.");
        }

        /// <summary>
        /// Converts an object to a 64-bit floating-point representation.
        /// Throws an exception if conversion is not possible.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <returns>A double representation of the object.</returns>
        /// <exception cref="ArgumentException">Thrown if conversion fails.</exception>
        public static double AsDoubleValue(this object value)
        {
            if (value.TryAsDoubleValue(out double result))
                return result;

            throw new ArgumentException($"{value?.GetType().Name} '{value}' cannot be converted to a double.");
        }

        /// <summary>
        /// Attempts to convert an object to a 32-bit integer representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="result">
        /// When this method returns, contains the converted integer value if the conversion succeeded;
        /// otherwise, contains 0.
        /// </param>
        /// <returns>True if the conversion succeeded; otherwise, false.</returns>
        public static bool TryAsIntValue(this object value, out int result)
        {
            switch (value)
            {
                case int i:
                    result = i;
                    return true;

                case bool b:
                    result = b ? 1 : 0;
                    return true;

                case string s:
                    return int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

                case IConvertible convertible:
                    try
                    {
                        result = Convert.ToInt32(convertible, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = 0;
                        return false;
                    }

                default:
                    result = 0;
                    return false;
            }
        }

        /// <summary>
        /// Attempts to convert an object to a double-precision floating-point representation.
        /// </summary>
        /// <param name="value">The object to convert.</param>
        /// <param name="result">
        /// When this method returns, contains the converted double value if the conversion succeeded;
        /// otherwise, contains 0.
        /// </param>
        /// <returns>True if the conversion succeeded; otherwise, false.</returns>
        public static bool TryAsDoubleValue(this object value, out double result)
        {
            switch (value)
            {
                case double d:
                    result = d;
                    return true;

                case bool b:
                    result = b ? 1d : 0d;
                    return true;

                case string s:
                    return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);

                case IConvertible convertible:
                    try
                    {
                        result = Convert.ToDouble(convertible, CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        result = 0;
                        return false;
                    }

                default:
                    result = 0;
                    return false;
            }
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
