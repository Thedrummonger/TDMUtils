using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class EnumerableUtilities
    {
        public class ContainerItem(object? value, string display)
        {
            public object? Value { get; } = value;
            public string Display { get; } = display;
            public override string ToString() => Display;
            public static ContainerItem[] ToContainerList<T>(IEnumerable<T> items, Func<T, string> Display) =>
                [.. items.Select(i => new ContainerItem(i, Display(i)))];
            public static ContainerItem[] ToContainerList<T>(IEnumerable<T> items, Func<T, object> Tags, Func<T, string> Display) =>
                [.. items.Select(i => new ContainerItem(Tags(i), Display(i)))];
        }
        /// <summary>
        /// Converts an Enumerable of string to a single string with NewLine Characters to seperate the values
        /// </summary>
        /// <param name="s">The source string Enumerable</param>
        /// <returns></returns>
        /// 
        public static string JoinAtNewLine(this IEnumerable<string> s) => 
            string.Join(Environment.NewLine, s);
        /// <summary>
        /// Gets a value from a dictionary at the given key and casts it to the given type
        /// </summary>
        /// <typeparam name="TKey">The the object type of the dictionaries key</typeparam>
        /// <typeparam name="T">The Object the value will be cast to</typeparam>
        /// <param name="source">The source dictionary</param>
        /// <param name="Key">The key to get value of</param>
        /// <returns></returns>
        public static T? GetValueAs<TKey, T>(this IDictionary<TKey, object> source, TKey key)
        {
            if (!source.TryGetValue(key, out var value) || value is null) return default;
            if (value is T typed) return typed;
            try
            {
                var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                if (targetType.IsEnum)
                {
                    if (value is string enumText) return (T)Enum.Parse(targetType, enumText, ignoreCase: true);
                    return (T)Enum.ToObject(targetType, value);
                }
                return (T)Convert.ChangeType(value, targetType);
            }
            catch
            {
                return value.SerializeConvert<T>();
            }
        }
        /// <summary>
        /// Creates an array containing the values of the given Enum
        /// </summary>
        /// <typeparam name="T">The Enum as a type</typeparam>
        /// <returns>An array of <Enum></returns>
        public static T[] EnumAsArray<T>() where T : struct, Enum
        {
#if NET5_0_OR_GREATER
            return Enum.GetValues<T>();
#else
            return (T[])Enum.GetValues(typeof(T));
#endif
        }
        /// <summary>
        /// Creates an array containing the values of the given Enum as strings
        /// </summary>
        /// <typeparam name="T">The Enum as a type</typeparam>
        /// <returns>An array of <string></returns>
        public static string[] EnumAsStringArray<T>() where T : struct, Enum
        {
#if NET5_0_OR_GREATER
            return Enum.GetNames<T>();
#else
            return Enum.GetNames(typeof(T));
#endif
        }
#if NET6_0_OR_GREATER
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
#endif
        /// <summary>
        /// If the given key is not present in the dictionary, add the given value at the given key
        /// </summary>
        /// <typeparam name="T">The type of the key object in the dictionary</typeparam>
        /// <typeparam name="V">The type of the value object in the dictionary</typeparam>
        /// <param name="Dict">The source dictionary</param>
        /// <param name="Value">The key to check for</param>
        /// <param name="Default">The value to assign to the given key</param>
        public static TValue SetIfEmpty<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue defaultValue)
        {
#if NET5_0_OR_GREATER
            dict.TryAdd(key, defaultValue);
#else
            if (!dict.ContainsKey(key)) dict[key] = defaultValue;
#endif
            return dict[key];
        }
        /// <summary>
        /// Picks a random element from a list
        /// </summary>
        /// <typeparam name="T">Type of object the list contains</typeparam>
        /// <param name="source">Source List</param>
        /// <param name="rng">Optional Random class</param>
        /// <returns></returns>
        public static T PickRandom<T>(this IEnumerable<T> source, Random? rng = null) => 
            EnumerableUtilities.PickRandom(source, 1, rng).Single();
        /// <summary>
        /// Picks a number of random elements from the given list
        /// </summary>
        /// <typeparam name="T">Type of object the list contains</typeparam>
        /// <param name="source">The source list</param>
        /// <param name="count">The amount of items to take</param>
        /// <param name="rng">Optional Random class</param>
        /// <returns></returns>
        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count, Random? rng = null) => 
            EnumerableUtilities.Shuffle(source, rng).Take(count);
        /// <summary>
        /// Shuffles a collection
        /// </summary>
        /// <typeparam name="T">Type of objects in the collection</typeparam>
        /// <param name="source">Source collection</param>
        /// <param name="rng">Optional Random class</param>
        /// <returns></returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random? rng = null)
        {
            rng ??= new Random();
            var list = source.ToList();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
        /// <summary>
        /// Represents an item with an associated weight and available count.
        /// </summary>
        /// <typeparam name="T">The type of the item.</typeparam>
        public class WeightedItem<T>(T pool, int weight, int available = -1)
        {
            public T Pool { get; set; } = pool;
            public int Available { get; set; } = available;
            public int Weight { get; set; } = weight;
        }

        /// <summary>
        /// Converts an enumerable of items into an enumerable of weighted items using the specified weights and optional availability counts.
        /// </summary>
        /// <param name="values">The source items.</param>
        /// <param name="weights">The weights for each item. If not provided for all items, a default weight of 1 is used.</param>
        /// <param name="available">Optional availability counts for each item. If not provided for all items, a default of -1 (infinite) is used.</param>
        /// <returns>An enumerable of <see cref="WeightedItem{T}"/> with the specified weights and availability.</returns>
        public static IEnumerable<WeightedItem<T>> ToWeightedList<T>(this IEnumerable<T> values, IEnumerable<int> weights, IEnumerable<int>? available = null)
        {
            var weightList = weights.ToList();
            var availableList = available?.ToList() ?? [];
            int index = 0;
            foreach (var value in values)
            {
                int weightValue = index < weightList.Count ? weightList[index] : 1;
                int availableValue = index < availableList.Count ? availableList[index] : -1;
                yield return new WeightedItem<T>(value, availableValue, weightValue);
                index++;
            }
        }
        /// <summary>
        /// Picks a random element from an enumerable based on specified weights and optional availability counts.
        /// </summary>
        /// <typeparam name="T">The type of items in the enumerable.</typeparam>
        /// <param name="values">The source values to choose from.</param>
        /// <param name="weights">
        /// An enumerable of weights corresponding to each value. If a weight is missing for a value,
        /// a default weight of 1 is used.
        /// </param>
        /// <param name="available">
        /// An optional enumerable representing the availability of each value. If a value's availability
        /// is missing, it defaults to -1, which indicates infinite availability.
        /// </param>
        /// <param name="random">An optional <see cref="Random"/> instance for random number generation.</param>
        /// <returns>The randomly selected item.</returns>
        public static T PickRandomWeighted<T>(this IEnumerable<T> values, IEnumerable<int> weights, IEnumerable<int>? available = null, Random? random = null) =>
            values.ToWeightedList(weights, available).PickRandomWeighted(random);
        /// <summary>
        /// Picks a random item from a list of weighted items, taking into account their weight and availability.
        /// </summary>
        /// <typeparam name="T">The type of item contained in the weighted list.</typeparam>
        /// <param name="candidates">The list of weighted items to choose from.</param>
        /// <param name="random">An optional Random instance for generating random numbers.</param>
        /// <returns>The randomly selected item.</returns>
        /// <exception cref="Exception">Thrown when no valid candidates are available or an error occurs during selection.</exception>
        public static T PickRandomWeighted<T>(this IEnumerable<WeightedItem<T>> values, Random? random = null)
        {
            Random rng = random ?? new();
            var validCandidates = values.Where(c => c.Available != 0).ToList();
            if (validCandidates.Count < 1)
                throw new Exception("Ran out of valid candidates before all could be selected");

            int totalWeight = validCandidates.Sum(c => c.Weight);
            int randomValue = rng.Next(totalWeight);

            int cumulative = 0;
            WeightedItem<T>? selected = null;
            foreach (var candidate in validCandidates)
            {
                cumulative += candidate.Weight;
                if (randomValue < cumulative)
                {
                    selected = candidate;
                    break;
                }
            }

            if (selected is null)
                throw new Exception("Error while selecting a candidate");

            if (selected.Available > 0)
                selected.Available--;

            return selected.Pool;
        }

        public static T? GetAttribute<T>(this Enum value) where T : Attribute
        {
            var field = value.GetType().GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            return field?.GetCustomAttribute<T>(inherit: false);
        }

        public static T GetAttributeOrDefault<T>(this Enum value, T defaultValue) where T : Attribute => value.GetAttribute<T>() ?? defaultValue;
        public static string GetDescription(this Enum value) => value.GetAttribute<DescriptionAttribute>()?.Description ?? value.ToString();

        public static bool HasAttribute<T>(this Enum value) where T : Attribute
            => value.GetType().GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static)?.IsDefined(typeof(T), inherit: false) == true;

        public static bool HasAttributeAnyFlag<T>(this Enum value) where T : Attribute
        {
            var raw = Convert.ToUInt64(value);
            foreach (var f in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static))
                if ((raw & Convert.ToUInt64(f.GetValue(null))) != 0 &&
                    f.IsDefined(typeof(T), false))
                    return true;
            return false;
        }
        public static bool HasAttributeAllFlags<T>(this Enum value) where T : Attribute
        {
            var raw = Convert.ToUInt64(value);
            foreach (var f in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                var flag = Convert.ToUInt64(f.GetValue(null));
                if (flag != 0 && (raw & flag) == flag && !f.IsDefined(typeof(T), false))
                    return false;
            }

            return true;
        }

        public static T NextValue<T>(this IList<T> list, T current, bool reverse = false)
        {
            int index = list.IndexOf(current);
            if (index == -1)
                return list[0];

            int next = reverse ? (index - 1 + list.Count) % list.Count : (index + 1) % list.Count;

            return list[next];
        }
        public static T NextValue<T>(T current, bool reverse = false) where T : struct, Enum
        {
            var list = (T[])Enum.GetValues(typeof(T));
            int index = Array.IndexOf(list, current);
            if (index == -1)
                return list[0];

            int next = reverse ? (index - 1 + list.Length) % list.Length : (index + 1) % list.Length;

            return list[next];
        }

        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
        {
#if NET5_0_OR_GREATER
            return Enumerable.Chunk(source, size);
#else
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));

            var buffer = new List<T>(size);

            foreach (var item in source)
            {
                buffer.Add(item);
                if (buffer.Count == size)
                {
                    yield return buffer.ToArray();
                    buffer.Clear();
                }
            }

            if (buffer.Count != 0)
                yield return buffer.ToArray();
#endif
        }

        public static T[] TakePortion<T>(T[] source, int count, bool fromEnd)
        {
            if (count >= source.Length) return [.. source];
            var result = new T[count];
            if (fromEnd) Array.Copy(source, source.Length - count, result, 0, count);
            else Array.Copy(source, 0, result, 0, count);
            return result;
        }
    }
}
