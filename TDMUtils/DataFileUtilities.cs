using Newtonsoft.Json;
using System.Diagnostics;

namespace TDMUtils
{
    public static class DataFileUtilities
    {

        public enum FileStructure
        {
            unknown = 0,
            json = 1,
            yaml = 2,
            csv = 3
        }

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
        public static T LoadObjectFromFileOrDefault<T>(string FilePath, T Default, bool WriteDefaultToFileIfError, FileStructure fileType = FileStructure.unknown, bool logging = false)
        {
            T result = Default;
            bool FileError = false;

            if (fileType == FileStructure.unknown)
            {
                switch (Path.GetExtension(FilePath).ToLower())
                {
                    case ".json":
                        fileType = FileStructure.json; 
                        break;
                    case ".yaml":
                        fileType = FileStructure.yaml;
                        break;
                    case ".csv":
                        fileType = FileStructure.csv;
                        break;
                }
            }

            if (File.Exists(FilePath))
            {
                try
                {
                    switch (fileType)
                    {
                        case FileStructure.json:
                            result = DeserializeJsonFile<T>(FilePath);
                            break;
                        case FileStructure.yaml:
                            result = DeserializeYAMLFile<T>(FilePath);
                            break;
                        case FileStructure.csv:
                            result = DeserializeCSVFile<T>(FilePath);
                            break;
                        default:
                            if (logging) Console.WriteLine($"Failed to Deserialize {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                            Debug.WriteLine($"Failed to Deserialize {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                            FileError = true;
                            result = Default;
                            break;
                    }
                }
                catch (Exception E)
                {
                    if (logging) Console.WriteLine($"Failed to Deserialize {FilePath} to {typeof(T)}\n{E}");
                    Debug.WriteLine($"Failed to Deserialize {FilePath} to {typeof(T)}\n{E}");
                    FileError = true;
                    result = Default;
                }
            }
            else
            {
                if (logging) Console.WriteLine($"File did not exit {FilePath}");
                Debug.WriteLine($"File did not exit {FilePath}");
                FileError = true;
            }
            if (FileError && WriteDefaultToFileIfError)
            {
                switch (fileType)
                {
                    case FileStructure.json:
                        File.WriteAllText(FilePath, ToFormattedJson(result));
                        break;
                    case FileStructure.yaml:
                        File.WriteAllText(FilePath, ToYamlString(result));
                        break;
                    default:
                        if (logging) Console.WriteLine($"Failed to write default to file {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                        Debug.WriteLine($"Failed to write default to file {FilePath} {Path.GetExtension(FilePath)} Was not supported");
                        break;
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
