using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class EnumerableUtilities
    {
        /// <summary>
        /// Converts an Enumerable of string to a single string with NewLine Characters to seperate the values
        /// </summary>
        /// <param name="s">The source string Enumerable</param>
        /// <returns></returns>
        /// 
        public static string JoinAtNewLine(this IEnumerable<string> s) => string.Join(Environment.NewLine, s);
        /// <summary>
        /// Gets a value from a dictionary at the given key and casts it to the given type
        /// </summary>
        /// <typeparam name="Y">The the object type of the dictionaries key</typeparam>
        /// <typeparam name="T">The Object the value will be cast to</typeparam>
        /// <param name="source">The source dictionary</param>
        /// <param name="Key">The key to get value of</param>
        /// <returns></returns>
        public static T GetValueAs<Y, T>(this Dictionary<Y, object> source, Y Key)
        {
            if (!source.TryGetValue(Key, out object value)) { return default; }
            return MiscUtilities.SerializeConvert<T>(value);
        }
        /// <summary>
        /// Creates an array containing the values of the given Enum
        /// </summary>
        /// <typeparam name="T">The Enum as a type</typeparam>
        /// <returns>An array of <Enum></returns>
        public static IEnumerable<T> EnumAsArray<T>() => Enum.GetValues(typeof(T)).Cast<T>().ToArray();
        /// <summary>
        /// Creates an array containing the values of the given Enum as strings
        /// </summary>
        /// <typeparam name="T">The Enum as a type</typeparam>
        /// <returns>An array of <string></returns>
        public static IEnumerable<string> EnumAsStringArray<T>() => EnumAsArray<T>().Select(x => x.ToString()).ToArray();
        /// <summary>
        /// Gets the values from a list in a certain range
        /// </summary>
        /// <typeparam name="T">Type of values in the list</typeparam>
        /// <param name="list">The source list</param>
        /// <param name="range">The range of objects to grab</param>
        /// <returns></returns>
        public static List<T> GetRange<T>(this List<T> list, Range range)
        {
            var (start, length) = range.GetOffsetAndLength(list.Count);
            return list.GetRange(start, length);
        }
        /// <summary>
        /// Picks a random element from a list
        /// </summary>
        /// <typeparam name="T">Type of object the list contains</typeparam>
        /// <param name="source">Source List</param>
        /// <returns></returns>
        public static T PickRandom<T>(this IEnumerable<T> source) => EnumerableUtilities.PickRandom(source, 1).Single();
        /// <summary>
        /// If the given key is not present in the dictionary, add the given value at the given key
        /// </summary>
        /// <typeparam name="T">The type of the key object in the dictionary</typeparam>
        /// <typeparam name="V">The type of the value object in the dictionary</typeparam>
        /// <param name="Dict">The source dictionary</param>
        /// <param name="Value">The key to check for</param>
        /// <param name="Default">The value to assign to the given key</param>
        public static void SetIfEmpty<T, V>(this Dictionary<T, V> Dict, T Value, V Default)
        {
            if (!Dict.ContainsKey(Value)) { Dict[Value] = Default; }
        }
        /// <summary>
        /// Picks a number of random elements from the given list
        /// </summary>
        /// <typeparam name="T">Type of object the list contains</typeparam>
        /// <param name="source">The source list</param>
        /// <param name="count">The amount of items to take</param>
        /// <returns></returns>
        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count) => EnumerableUtilities.Shuffle(source).Take(count);
        /// <summary>
        /// Shuffles an collection
        /// </summary>
        /// <typeparam name="T">Type of objects in the collection</typeparam>
        /// <param name="source">Source collection</param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => source.OrderBy(x => Guid.NewGuid());
    }
}
