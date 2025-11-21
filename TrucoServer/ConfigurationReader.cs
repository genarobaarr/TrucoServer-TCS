using Newtonsoft.Json;
using System;
using System.IO;

namespace TrucoServer
{
    public static class ConfigurationReader
    {
        private const string CONFIGURATION_FILE_NAME = "appSettings.private.json";
        private static EmailSettings emailSettings;

        public static EmailSettings EmailSettings
        {
            get
            {
                if (emailSettings == null)
                {
                    LoadConfiguration();
                }
                return emailSettings;
            }
        }

        private static void LoadConfiguration()
        {
            string filePath = CONFIGURATION_FILE_NAME;

            try
            {
                string jsonText = File.ReadAllText(filePath);
                var wrapper = JsonConvert.DeserializeObject<ConfigurationFileWrapper>(jsonText);

                emailSettings = wrapper.EmailSettings;

                if (emailSettings == null)
                {
                    throw new InvalidOperationException("La clave 'EmailSettings' no se encontró o no pudo ser deserializada.");
                }
            }
            catch (FileNotFoundException ex)
            {
                LogManager.LogFatal(ex, nameof(LoadConfiguration));
            }
            catch (JsonException ex)
            {
                LogManager.LogFatal(ex, nameof(LoadConfiguration));
            }
            catch (Exception ex)
            {
                LogManager.LogFatal(ex, nameof(LoadConfiguration));
            }
        }
    }
}