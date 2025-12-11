using Newtonsoft.Json;
using System;
using System.IO;

namespace TrucoServer.Utilities
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

                emailSettings = wrapper?.EmailSettings;

                if (emailSettings == null)
                {
                    throw new InvalidOperationException("The key 'EmailSettings' was not found or could not be deserialized");
                }
            }
            catch (FileNotFoundException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (DirectoryNotFoundException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (UnauthorizedAccessException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (JsonSerializationException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (JsonReaderException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (InvalidOperationException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (IOException ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
            catch (Exception ex)
            {
                ServerException.HandleException(ex, nameof(LoadConfiguration));
            }
        }
    }
}