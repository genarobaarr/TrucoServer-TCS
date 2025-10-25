using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrucoServer
{
    public static class ConfigurationReader
    {
        private static EmailSettings emailSettings;

        private class ConfigWrapper
        {
            public EmailSettings EmailSettings { get; set; }
        }

        public static EmailSettings EmailSettings
        {
            get
            {
                if (emailSettings == null)
                {
                    string filePath = "appSettings.private.json"; 

                    try
                    {
                        string jsonText = File.ReadAllText(filePath);
                        var wrapper = JsonConvert.DeserializeObject<ConfigWrapper>(jsonText);

                        emailSettings = wrapper.EmailSettings;

                        if (emailSettings == null)
                        {
                            throw new InvalidOperationException("La clave 'EmailSettings' no se encontró o no pudo ser deserializada.");
                        }
                    }
                    catch (FileNotFoundException)
                    {
                        throw new FileNotFoundException($"El archivo de configuración '{filePath}' no se encuentra en el directorio de ejecución (bin/Debug).");
                    }
                    catch (JsonException ex)
                    {
                        throw new InvalidOperationException($"Error al parsear el archivo JSON {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Error al cargar la configuración: {ex.Message}");
                    }
                }
                return emailSettings;
            }
        }
    }
}