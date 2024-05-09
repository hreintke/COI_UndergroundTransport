using Mafi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UndergroundTransportMod
{
    public static class Utils
    {
        public static void OutputValuesToLog<T>(T instance)
        {
            LogWrite.Info("[COIE]: Printing out all values from " + ObjectExtensions.ToStringSafe((object)instance));
            if ((object)instance == null)
            {
                LogWrite.Info("[COIE]: Instance is null.");
            }
            else
            {
                Type type = instance.GetType();
                PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                LogWrite.Info("[COIE]: Properties of " + type.Name + ":");
                foreach (PropertyInfo propertyInfo in properties)
                {
                    try
                    {
                        object obj = propertyInfo.GetValue((object)instance);
                        LogWrite.Info(string.Format("[COIE]: {0} = {1}", (object)propertyInfo.Name, obj));
                    }
                    catch (Exception ex)
                    {
                        LogWrite.Info("[COIE]: " + propertyInfo.Name + " could not be read: " + ex.Message);
                    }
                }
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (FieldInfo fieldInfo in fields)
                {
                    try
                    {
                        object obj = fieldInfo.GetValue((object)instance);
                        LogWrite.Info(string.Format("[COIE]: {0} = {1}", (object)fieldInfo.Name, obj));
                    }
                    catch (Exception ex)
                    {
                        LogWrite.Info("[COIE]: " + fieldInfo.Name + " could not be read: " + ex.Message);
                    }
                }
            }
        }
    }
}
