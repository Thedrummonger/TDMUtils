using Newtonsoft.Json;
using System.Diagnostics;

namespace TDMUtils
{
    public static class DataFileUtilities
    {

        //JSON Helpers
        /// <summary>
        /// Read the given JSON file and Deserializes it's contents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static T DeserializeJsonFile<T>(string Path)
        {
            return JsonConvert.DeserializeObject<T>(File.ReadAllText(Path));
        }
        /// <summary>
        /// Deserializes the given Json string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeJsonString<T>(string String)
        {
            return JsonConvert.DeserializeObject<T>(String);
        }
        /// <summary>
        /// Converts the given string Enumerable to a single line and Deserializes it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeJsonString<T>(IEnumerable<string> String)
        {
            return JsonConvert.DeserializeObject<T>(string.Join(Environment.NewLine, String));
        }
        /// <summary>
        /// Serialized the given object and return it as a string
        /// </summary>
        /// <param name="o">The source object</param>
        /// <returns>The serialized object</returns>
        public static string ToFormattedJson(this object o) => JsonConvert.SerializeObject(o, NewtonsoftExtensions.DefaultSerializerSettings);

        //CSV Helpers
        /// <summary>
        /// Read the given CSV file and Deserializes it's contents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static T DeserializeCSVFile<T>(string Path)
        {
            var Json = ConvertCsvFileToJsonObject(File.ReadAllLines(Path));
            return JsonConvert.DeserializeObject<T>(Json);
        }
        /// <summary>
        /// Deserializes the given CSV string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeCSVString<T>(string String)
        {
            var Json = ConvertCsvFileToJsonObject(StringUtilities.SplitAtNewLine(String));
            return JsonConvert.DeserializeObject<T>(Json);
        }
        /// <summary>
        /// Converts the given string Enumerable to a single line and Deserializes it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeCSVString<T>(IEnumerable<string> String)
        {
            var Json = ConvertCsvFileToJsonObject(String.ToArray());
            return JsonConvert.DeserializeObject<T>(Json);
        }

        //YAML Helpers
        /// <summary>
        /// Read the given Yaml file and Deserializes it's contents
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Path"></param>
        /// <returns></returns>
        public static T DeserializeYAMLFile<T>(string Path)
        {
            var Json = ConvertYamlStringToJsonString(File.ReadAllText(Path), true);
            return JsonConvert.DeserializeObject<T>(Json);
        }
        /// <summary>
        /// Deserializes the given Yaml string
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeYAMLString<T>(string String)
        {
            var Json = ConvertYamlStringToJsonString(String, true);
            return JsonConvert.DeserializeObject<T>(Json);
        }
        /// <summary>
        /// Converts the given string Enumerable to a single line and Deserializes it.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="String"></param>
        /// <returns></returns>
        public static T DeserializeYAMLString<T>(IEnumerable<string> String)
        {
            var Json = ConvertYamlStringToJsonString(string.Join(Environment.NewLine, String), true);
            return JsonConvert.DeserializeObject<T>(Json);
        }
        /// <summary>
        /// Serialized the given object and return it as a string
        /// </summary>
        /// <param name="o">The source object</param>
        /// <returns>The serialized object</returns>

        public static string ToYamlString(this object e)
        {
            var serializer = new YamlDotNet.Serialization.SerializerBuilder().Build();
            return serializer.Serialize(e);
        }

        //File Parsing
        public static T LoadObjectFromFileOrDefault<T>(string FilePath)
        {
            var Result = LoadObjectFromFileOrDefault<T>(FilePath, default, false);
            return Result;
        }
        /// <summary>
        /// Reads and Deserializes the given data file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="FilePath">Path to </param>
        /// <param name="Default">The default state of the object</param>
        /// <param name="WriteDefaultToFileIfError">Should the given default value be written to the given path if the file is missing or corrupted</param>
        /// <returns></returns>
        public static T LoadObjectFromFileOrDefault<T>(string FilePath, T Default, bool WriteDefaultToFileIfError)
        {
            T result = Default;
            bool FileError = false;
            if (File.Exists(FilePath))
            {
                try
                {
                    if (MiscUtilities.In(Path.GetExtension(FilePath).ToLower(), ".json", ".txt")) { result = DeserializeJsonFile<T>(FilePath); }
                    else if (MiscUtilities.In(Path.GetExtension(FilePath).ToLower(), ".yaml")) { result = DeserializeYAMLFile<T>(FilePath); }
                    else if (MiscUtilities.In(Path.GetExtension(FilePath).ToLower(), ".csv")) { result = DeserializeCSVFile<T>(FilePath); }
                    else
                    {
                        Debug.WriteLine($"Failed to Deserialize {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                        FileError = true;
                        result = Default;
                    }
                }
                catch
                {
                    Debug.WriteLine($"Failed to Deserialize {FilePath} to {typeof(T)}");
                    FileError = true;
                    result = Default;
                }
            }
            else
            {
                Debug.WriteLine($"File did not exit {FilePath}");
                FileError = true;
            }
            if (FileError && WriteDefaultToFileIfError)
            {
                if (MiscUtilities.In(Path.GetExtension(FilePath).ToLower(), ".json", ".txt")) { File.WriteAllText(FilePath, ToFormattedJson(result)); }
                else if (MiscUtilities.In(Path.GetExtension(FilePath).ToLower(), ".yaml")) { File.WriteAllText(FilePath, ToYamlString(result)); }
                else
                {
                    Debug.WriteLine($"Failed to write default to file {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                }
            }
            return result;
        }
        /// <summary>
        /// Deserializes the given YAML string and then Reserializes it as a JSON string
        /// </summary>
        public static string ConvertYamlStringToJsonString(string YAML, bool Format = false)
        {
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            object yamlIsDumb = deserializer.Deserialize<object>(YAML);
            if (Format) { return JsonConvert.SerializeObject(yamlIsDumb, NewtonsoftExtensions.DefaultSerializerSettings); }
            return JsonConvert.SerializeObject(yamlIsDumb);
        }
        public static string ConvertCsvFileToJsonObject(string[] lines)
        {
            var csv = new List<string[]>();

            var properties = lines[0].Split(',');

            foreach (string line in lines)
            {
                var LineData = line.Split(',');
                csv.Add(LineData);
            }

            var listObjResult = new List<Dictionary<string, string>>();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) { continue; }
                var objResult = new Dictionary<string, string>();
                for (int j = 0; j < properties.Length; j++)
                    objResult.Add(properties[j].Trim(), csv[i][j].Trim());

                listObjResult.Add(objResult);
            }

            return JsonConvert.SerializeObject(listObjResult, NewtonsoftExtensions.DefaultSerializerSettings);
        }
    }
}
