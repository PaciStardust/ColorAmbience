using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace ColorAmbience
{
    public static class Config
    {
        public static ConfigModel Data { get; private set; } = new();
        public static ConfigCaptureModel Capture => Data.Capture;
        public static ConfigApiModel Api => Data.Api;
        public static ConfigLoggerModel Debug => Data.Debug;

        //todo: implement
        //public static string Github => "https://github.com/PaciStardust/HOSCY";
        //public static string GithubLatest => "https://api.github.com/repos/pacistardust/hoscy/releases/latest";

        public static string ResourcePath { get; private set; }
        public static string ConfigPath { get; private set; }
        public static string LogPath { get; private set; }

        #region Saving and Loading
        static Config()
        {
            var assemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();

            ResourcePath = Path.GetFullPath(Path.Combine(assemblyDirectory, "config"));
            ConfigPath = Path.GetFullPath(Path.Combine(ResourcePath, "config.json"));
            LogPath = Path.GetFullPath(Path.Combine(ResourcePath, "log.txt"));

            try
            {
                if (!Directory.Exists(ResourcePath))
                    Directory.CreateDirectory(ResourcePath);

                string configData = File.ReadAllText(ConfigPath, Encoding.UTF8);
                Data = JsonConvert.DeserializeObject<ConfigModel>(configData) ?? new();
            }
            catch
            {
                Data = GetDefaultConfig();
                SaveConfig();
            }
        }

        /// <summary>
        /// Saves the config file
        /// </summary>
        public static void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Data ?? new(), Formatting.Indented));
                Logger.PInfo("Saved config file at " + ConfigPath);
            }
            catch (Exception e)
            {
                Logger.Error(e, "The config file was unable to be saved.");
            }
        }
        #endregion

        #region Utility
        /// <summary>
        /// A combination of floor and ceil for comparables
        /// </summary>
        /// <typeparam name="T">Type to compare</typeparam>
        /// <param name="value">Value to compare</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <returns>Value, if within bounds. Min, if value smaller than min. Max, if value larger than max. If max is smaller than min, min has priority</returns>
        public static T MinMax<T>(T value, T min, T max) where T : IComparable
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;
            return value;
        }

        public static string GetVersion()
        {
            var assembly = Assembly.GetEntryAssembly();
            return "v." + (assembly != null ? FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion : "???");
        }
        #endregion

        #region Question asking
        /// <summary>
        /// This exists as newtonsoft seems to have an issue with my way of creating a default config
        /// </summary>
        private static ConfigModel GetDefaultConfig()
        {
            var config = new ConfigModel();

            config.Capture.CaptureName =        AskString("What is the name of the window? (Leave blank for whole screen)");
            config.Capture.CapturePercentage =  AskComparable<int>("How much % of the region should be captured? (5 - 100 %)") / 100f;
            config.Capture.CaptureInterval =    AskComparable<int>("How often do you want captures to happen? (500 - 60000 ms)");
            config.Capture.UseVirtualScreen =   !string.IsNullOrWhiteSpace(AskString("Use virtual screen (all screens) as fallback instead of primary? (Blank for no)"));
            config.Capture.IgnoreBlackPixels =  string.IsNullOrWhiteSpace(AskString("Ignore black pixels in processing? (Blank for yes)"));

            return config;
        }

        private static string AskString(string question)
        {
            Console.Write($"{question}\n > ");
            return Console.ReadLine() ?? string.Empty;
        }

        private static T AskComparable<T>(string question) where T : IComparable
        {
            while (true)
            {
                var answer = AskString(question);

                try
                {
                    var converted = TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(answer);
                    if (converted == null)
                        continue;

                    return (T)converted;
                }
                catch
                {
                    continue;
                }
            }
        }
        #endregion

        #region Models
        /// <summary>
        /// Model for storing all config data
        /// </summary>
        public class ConfigModel
        {
            public ConfigCaptureModel Capture { get; init; } = new();
            public ConfigApiModel Api { get; init; } = new();
            public ConfigLoggerModel Debug { get; init; } = new();
        }

        /// <summary>
        /// Model for storing all Capture related data
        /// </summary>
        public class ConfigCaptureModel
        {
            public string CaptureName { get; set; } = string.Empty;
            public float CapturePercentage
            {
                get { return _capturePercentage; }
                set { _capturePercentage = MinMax(value, 0.05f, 1); }
            }
            private float _capturePercentage = 1;

            public int CaptureInterval
            {
                get { return _captureInterval; }
                set { _captureInterval = MinMax(value, 500, 60000); }
            }
            private int _captureInterval = 1000;

            public bool UseVirtualScreen { get; set; } = false;
            public bool IgnoreBlackPixels { get; set; } = true;

            public int ResolutionWidth
            {
                get { return _resolutionWidth; }
                set { _resolutionWidth = MinMax(value, 128, 1024); }
            }
            private int _resolutionWidth = 512;

            public int ResolutionHeight
            {
                get { return _resolutionHeight; }
                set { _resolutionHeight = MinMax(value, 72, 576); }
            }
            private int _resolutionHeight = 288;

            public float KMeansThreshold
            {
                get { return _kMeansThreshold; }
                set { _kMeansThreshold = MinMax(value, 0, 20); }
            }
            private float _kMeansThreshold = 5;
        }

        /// <summary>
        /// Model for all API related data
        /// </summary>
        public class ConfigApiModel
        {
            //todo: implement api settings
        }

        /// <summary>
        /// Model for all Logging related data, this can currently only be changed in the file
        /// </summary>
        public class ConfigLoggerModel
        {
            public bool CheckUpdates { get; set; } = true;
            public bool Error { get; set; } = true;
            public bool Warning { get; set; } = true;
            public bool Info { get; set; } = true;
            public bool PrioInfo { get; set; } = true;
            public bool Log { get; set; } = true;
            public bool Debug { get; set; } = false;
            public List<string> LogFilter { get; set; } = new();
        }
        #endregion
    }
}