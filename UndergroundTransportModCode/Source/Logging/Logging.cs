using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    [GlobalDependency(RegistrationMode.AsSelf, false)]
    public static class LogWrite
    {
        private static int msgCount = 0;
        private static string Prefix = Assembly.GetExecutingAssembly().GetName().Name + " : ============> ";

        public static void Info(string message) => Mafi.Log.Info($"{Prefix} {++msgCount,4} {message}");

        public static void Warning(string message) => Mafi.Log.Warning($"{Prefix} {++msgCount,4} {message}");

        public static void WarningOnce(string message) => Mafi.Log.WarningOnce($"{Prefix} {++msgCount,4} {message}");

        public static void Error(string message) => Mafi.Log.Error($"{Prefix} {++msgCount,4} {message}");

        public static void Exception(Exception e, string message) => Mafi.Log.Exception(e, $"{Prefix} {++msgCount,4} {message}");
    }
}
