using System.Net;

namespace TDMUtils
{
    //This file only exists for legacy support and so I don't have to go update all my other projects to point to the new namespaces :)
    public static class Utility
    {
        //DataFileUtilities
        public static T DeserializeJsonFile<T>(string Path) => DataFileUtilities.DeserializeJsonFile<T>(Path);
        public static T DeserializeJsonString<T>(string String) => DataFileUtilities.DeserializeJsonString<T>(String);
        public static T DeserializeJsonFile<T>(IEnumerable<string> String) => DataFileUtilities.DeserializeJsonString<T>(String);
        public static T DeserializeCSVFile<T>(string Path) => DataFileUtilities.DeserializeCSVFile<T>(Path);
        public static T DeserializeCSVString<T>(string String) => DataFileUtilities.DeserializeCSVString<T>(String);
        public static T DeserializeCSVString<T>(IEnumerable<string> String) => DataFileUtilities.DeserializeCSVString<T>(String);
        public static T DeserializeYAMLFile<T>(string Path) => DataFileUtilities.DeserializeYAMLFile<T>(Path);
        public static T DeserializeYAMLString<T>(string String) => DataFileUtilities.DeserializeYAMLString<T>(String);
        public static T DeserializeYAMLString<T>(IEnumerable<string> String) => DataFileUtilities.DeserializeYAMLString<T>(String);
        public static T LoadObjectFromFileOrDefault<T>(string FilePath) => DataFileUtilities.LoadObjectFromFileOrDefault<T>(FilePath);
        public static T LoadObjectFromFileOrDefault<T>(string FilePath, T Default, bool WriteDefaultToFileIfError) =>
            DataFileUtilities.LoadObjectFromFileOrDefault(FilePath, Default, WriteDefaultToFileIfError);
        public static string ToFormattedJson(this object o) => DataFileUtilities.ToFormattedJson(o);
        public static string ToYamlString(this object e) => DataFileUtilities.ToYamlString(e);

        //String Extensions
        public static string Replace(this string s, int index, int length, string replacement) => 
            StringUtilities.Replace(s, index, length, replacement);
        public static bool IsNullOrWhiteSpace(this string s) => StringUtilities.IsNullOrWhiteSpace(s);
        public static bool IsNullOrEmpty(this string s) => StringUtilities.IsNullOrEmpty(s);
        public static string[] SplitAtNewLine(this string s) => StringUtilities.SplitAtNewLine(s);
        public static string[] TrimSplit(this string s, string Val) => StringUtilities.TrimSplit(s, Val);
        public static Tuple<string, string> SplitOnce(this string input, char Split, bool LastOccurrence = false) => 
            StringUtilities.SplitOnce(input, Split, LastOccurrence);
        public static bool IsIntegerRange(this string x, out Tuple<int, int> Values) => StringUtilities.IsIntegerRange(x, out Values);
        public static string TrimSpaces(this string myString) => StringUtilities.TrimSpaces(myString);
        public static Version AsVersion(this string version) => StringUtilities.AsVersion(version);
        public static bool IsLiteralID(this string ID, out string CleanedID) => StringUtilities.IsLiteralID(ID, out CleanedID);
        public static string ConvertToCamelCase(this string Input) => StringUtilities.ConvertToCamelCase(Input);
        public static string AddWordSpacing(this string Input) => StringUtilities.AddWordSpacing(Input);
        public static bool IsIpAddress(string Input, out IPAddress IP) => StringUtilities.IsIpAddress(Input, out IP);

        //EnumerableExtensions

        public static string JoinAtNewLine(this IEnumerable<string> s) => EnumerableUtilities.JoinAtNewLine(s);
        public static T GetValueAs<Y, T>(this Dictionary<Y, object> source, Y Key) => EnumerableUtilities.GetValueAs<Y,T>(source, Key);
        public static IEnumerable<T> EnumAsArray<T>() => EnumerableUtilities.EnumAsArray<T>();
        public static IEnumerable<string> EnumAsStringArray<T>() => EnumerableUtilities.EnumAsStringArray<T>();
        public static List<T> GetRange<T>(this List<T> list, Range range) => EnumerableUtilities.GetRange(list, range);
        public static T PickRandom<T>(this IEnumerable<T> source) => EnumerableUtilities.PickRandom<T>(source);
        public static void SetIfEmpty<T, V>(this Dictionary<T, V> Dict, T Value, V Default) => 
            EnumerableUtilities.SetIfEmpty(Dict, Value, Default);
        public static IEnumerable<T> PickRandom<T>(this IEnumerable<T> source, int count) => EnumerableUtilities.PickRandom(source, count);
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => EnumerableUtilities.Shuffle(source);

        //MiscUtilities
        public static T DeepClone<T>(this T obj) => MiscUtilities.DeepClone(obj);
        public static T SerializeConvert<T>(this object source) => MiscUtilities.SerializeConvert<T>(source);
        public static bool In<T>(this T obj, params T[] args) => MiscUtilities.In(obj, args);
        public static void PrintObjectToConsole(object o) => MiscUtilities.PrintObjectToConsole(o);
        public static bool IsTruthy(this object val, bool? Default = null) => MiscUtilities.IsTruthy(val, Default);
        public static int AsIntValue(this object _Value) => MiscUtilities.AsIntValue(_Value);
        public static bool isJsonTypeOf<T>(string Json) => MiscUtilities.isJsonTypeOf<T>(Json);
        public static bool DynamicPropertyExist(dynamic Object, string name) => MiscUtilities.DynamicPropertyExist(Object, name);
        public static bool DynamicMethodExists(dynamic Object, string methodName) => MiscUtilities.DynamicMethodExists(Object, methodName);
        public static bool OBJIsThreadSafe(Thread thread, dynamic Obj) => MiscUtilities.OBJIsThreadSafe(thread, Obj);
    }
}
