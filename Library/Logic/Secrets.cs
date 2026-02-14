using System;
using System.Collections.Generic;
using System.Reflection;


namespace VedAstro.Library
{

    /// <summary>
    /// OBS SECURITY PROTOCOL
    /// </summary>
    public static partial class Secrets
    {
        /// <summary>
        /// OBS SECURITY PROTOCOL
        /// </summary>
        public static string Get(string key)
        {
            //keys are expected to be in private mode, accessed only via this method
            var field = typeof(Secrets).GetField(key, BindingFlags.Static | BindingFlags.NonPublic);
            if (field != null)
            {
                var value = (string)field.GetValue(null);
                if (!string.IsNullOrEmpty(value)) return value;
            }

            // Fallback: read from environment (Azure Functions loads local.settings.json Values as env vars)
            var envValue = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(envValue)) return envValue;

            Console.WriteLine($"The key --> '{key}' is missing sweetheart! Contact us for a testing Key --> vedastro.org/Contact.html");
            throw new Exception($"The key --> '{key}' is missing sweetheart! Contact us for a testing Key --> vedastro.org/Contact.html");
        }
    }
}
