/*
    This file is part of HomeGenie Project source code.

    HomeGenie is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    HomeGenie is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with HomeGenie.  If not, see <http://www.gnu.org/licenses/>.  
*/

/*
 *     Author: Generoso Martello <gene@homegenie.it>
 *     Project Homepage: http://homegenie.it
 */

#define LOGGING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Linq;
using System.Dynamic;

using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

using HomeGenie.Data;

using System.IO.Packaging;
using System.Xml.Serialization;
using HomeGenie.Service.Constants;

namespace HomeGenie.Service
{

    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            IList<T> list = null;
            if (listToClone.GetType() == typeof(TsList<T>))
            {
                var tslist = ((TsList<T>)listToClone);
                list = listToClone.Select(item => (T)item.Clone()).ToList();
            }
            else
            {
                list = listToClone.Select(item => (T)item.Clone()).ToList();
            }
            return list;
        }
    }

    [Serializable()]
    public class TsList<T> : System.Collections.Generic.List<T>
    {
        
        private object syncLock = new object();

        public TsList()
        {
        }

        public object LockObject
        {
            get { return syncLock; }
        }

        new public void Clear()
        {

            lock (syncLock)
                base.Clear();
        }
        new public void Add(T value)
        {

            lock (syncLock)
                base.Add(value);
        }
        new public void RemoveAll(Predicate<T> predicate)
        {
            lock (syncLock)
                base.RemoveAll(predicate);
        }
        new public void Remove(T item)
        {
            lock (syncLock)
                base.Remove(item);
        }

        new public void Sort(Comparison<T> comparison)
        {
            lock (syncLock)
                base.Sort(comparison);
        }
        
    }


    public static class JsonHelper
    {
        public static string GetSimpleResponse(string value)
        {
            return "[{ ResponseValue : '" + value + "' }]";
        }
    }


    public static class Utility
    {
        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(string pszSound, UIntPtr hmod, uint fdwSound);

        // buffer size for AddFileToZip
        private const long BUFFER_SIZE = 4096;
        // delegate used by RunAsyncTask
        public delegate void AsyncFunction();

        public static void Say(string sentence, string locale, bool async = false)
        {
            if (async)
            {
                var t = new Thread(() =>
                {
                    Say(sentence, locale);
                });
                t.Start();
            }
            else
            {
                Say(sentence, locale);
            }
        }

        public static Process StartUpdater(bool restart)
        {
            File.Copy("HomeGenieUpdater.exe", "HomeGenieUpdaterC.exe", true);
            var updater = new Process();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows)
            {
                updater.StartInfo.FileName = "HomeGenieUpdaterC.exe";
                updater.StartInfo.Arguments = restart ? "-r" : "";
                //updater.StartInfo.UseShellExecute = true;
                updater.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //updater.StartInfo.RedirectStandardOutput = true;
            }
            else
            {
                updater.StartInfo.FileName = "mono";
                updater.StartInfo.Arguments = "HomeGenieUpdaterC.exe " + (restart ? "-r" : "");
                updater.StartInfo.UseShellExecute = false;
                updater.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                //updater.StartInfo.RedirectStandardOutput = true;
            }
            updater.Start();
            return updater;
        }

        public static void Say(string sentence, string locale)
        {
            try
            {
                var client = new WebClient();
                client.Headers.Add("Referer", "http://translate.google.com");
                var audioData = client.DownloadData("http://translate.google.com/translate_tts?tl=" + locale + "&q=" + sentence);
                client.Dispose();

                var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_tmp");
                if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
                var file = Path.Combine(outputDirectory, "_synthesis_tmp.mp3");

                if (File.Exists(file)) File.Delete(file);
                var stream = File.OpenWrite(file);
                stream.Write(audioData, 0, audioData.Length);
                stream.Close();

                var wavFile = file.Replace(".mp3", ".wav");
                Process.Start(new ProcessStartInfo("lame", "--decode \"" + file + "\" \"" + wavFile + "\"")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false
                }).WaitForExit();

                Play(wavFile);
            }
            catch (Exception)
            {
                // TODO: add error logging 
            }
        }

        public static void Play(string wavFile)
        {

            var os = Environment.OSVersion;
            var platform = os.Platform;
            //
            switch (platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    PlaySound(wavFile, UIntPtr.Zero, (uint)(0x00020000 | 0x00000000));
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                default:
                    var player = new System.Media.SoundPlayer();
                    player.SoundLocation = wavFile;
                    player.Play();
                    break;
            }

        }

        public static void AddFileToZip(string zipFilename, string fileToAdd)
        {
            using (var zip = System.IO.Packaging.Package.Open(zipFilename, FileMode.OpenOrCreate))
            {
                string destFilename = fileToAdd;
                var uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri))
                {
                    zip.DeletePart(uri);
                }
                var part = zip.CreatePart(uri, "", CompressionOption.Normal);
                using (var fileStream = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (var outputStream = part.GetStream())
                    {
                        CopyStream(fileStream, outputStream);
                    }
                }
            }
        }

        private static void CopyStream(System.IO.FileStream inputStream, System.IO.Stream outputStream)
        {
            long bufferSize = inputStream.Length < BUFFER_SIZE ? inputStream.Length : BUFFER_SIZE;
            long bytesWritten = 0;
            int bytesRead = 0;
            byte[] buffer = new byte[bufferSize];
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                bytesWritten += bufferSize;
            }
        }

        public static dynamic ParseXmlToDynamic(string xml)
        {
            var document = XDocument.Load(new StringReader(xml));
            dynamic root = new ExpandoObject();
            XmlToDynamic.Parse(root, document.Elements().First());
            return root;
        }

        public static Thread RunAsyncTask(AsyncFunction functionBlock)
        {
            var asyncTask = new Thread(() =>
            {
                try
                {
                    functionBlock();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Service.Utility.RunAsyncTask", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
            });
            asyncTask.Start();
            return asyncTask;
        }

        public static ModuleParameter ModuleParameterGet(Module module, string propertyName)
        {
            return module.Properties.Find(delegate(ModuleParameter parameter) { return parameter.Name == propertyName; });
        }

        public static ModuleParameter ModuleParameterSet(Module module, string propertyName, string propertyValue)
        {
            if (module == null) return null;
            //
            var parameter = module.Properties.Find(mpar => mpar.Name == propertyName);
            if (parameter == null)
            {
                parameter = new ModuleParameter() { Name = propertyName, Value = propertyValue };
                module.Properties.Add(parameter);
            }
            parameter.Value = propertyValue;
            return parameter;
        }

        public static string WaitModuleParameterChange(Module module, string parameterName)
        {
            string value = "";
            // TODO make it as a function _waitModuleParameterChange(mod, parname, timeout)
            ModuleParameter parameter = Service.Utility.ModuleParameterGet(module, parameterName);
            if (parameter != null)
            {
                var updated = DateTime.UtcNow.Ticks; //p.UpdateTime.Ticks - (TimeSpan.TicksPerSecond * 1); 
                //
                Thread.Sleep(500);
                //
                int timeout = 0;
                int maxWait = 50; //(50 * 100ms ticks = 5000 ms)
                int tickFrequency = 100;
                //
                while ((TimeSpan.FromTicks(parameter.UpdateTime.Ticks - updated).TotalSeconds > 1 /*&& (DateTime.UtcNow.Ticks - p.UpdateTime.Ticks > 5 * TimeSpan.TicksPerSecond)*/) && timeout++ < maxWait)
                {
                    Thread.Sleep(tickFrequency);
                }
                //
                if (timeout < maxWait)
                {
                    value = parameter.Value;
                }
            }
            return value;
        }

        public class StringValueAttribute : System.Attribute
        {

            private string attributeValue;

            public StringValueAttribute(string value)
            {
                attributeValue = value;
            }

            public string Value
            {
                get { return attributeValue; }
            }

        }

        public static DateTime JavaTimeStampToDateTime(double javaTimestamp)
        {
            // Java timestamp is millisecods past epoch
            var timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            timestamp = timestamp.AddMilliseconds(javaTimestamp);
            return timestamp;
        }

        public static string Module2Json(Module module, bool hideProperties)
        {
            string json = "{\n" +
                        "   \"Name\": \"" + XmlEncode(module.Name) + "\",\n" +
                        "   \"Description\": \"" + XmlEncode(module.Description) + "\",\n" +
                        "   \"DeviceType\": \"" + module.DeviceType + "\",\n" +
                        "   \"Domain\": \"" + module.Domain + "\",\n" +
                        "   \"Address\": \"" + module.Address + "\",\n";
            if (!hideProperties)
            {
                json += "   \"Properties\": [ \n";
                //
                for (int i = 0; i < module.Properties.Count; i++)
                {
                    var parameter = module.Properties[i];
                    json += "       {\n" +
                            "           \"Name\": \"" + XmlEncode(parameter.Name) + "\",\n" +
                            "           \"Description\": \"" + XmlEncode(parameter.Description) + "\",\n" +
                            "           \"Value\": \"" + XmlEncode(parameter.Value) + "\",\n" +
                            "           \"UpdateTime\": \"" + parameter.UpdateTime.ToString("u") + "\",\n" +
                            "           \"ValueIncrement\": \"" + parameter.ValueIncrement.ToString() + "\",\n" +
                            "           \"LastValue\": \"" + XmlEncode(parameter.LastValue) + "\",\n" +
                            "           \"LastUpdateTime\": \"" + parameter.LastUpdateTime.ToString("u") + "\"\n" +
                            "       },\n";
                }
                json = json.TrimEnd(',', '\n');
                //
                json += "   ],\n";
            }
            json += "   \"RoutingNode\": \"" + (module.RoutingNode != null ? module.RoutingNode : "") + "\"\n";
            json += "}";
            //
            return json;
        }

        public static string XmlEncode(string fieldValue)
        {
            if (fieldValue == null)
            {
                fieldValue = "";
            }
            else //if (s.IndexOf("&") >= 0 && s.IndexOf("\"") >= 0)
            {
                fieldValue = fieldValue.Replace("&", "&amp;");
                fieldValue = fieldValue.Replace("\"", "&quot;");
            }
            return fieldValue;
        }

    }

    public class XmlToDynamic
    {
        public static void Parse(dynamic parent, XElement node)
        {
            if (node.HasElements)
            {
                if (node.Elements(node.Elements().First().Name.LocalName).Count() > 1)
                {
                    //list
                    var item = new ExpandoObject();
                    var list = new List<dynamic>();
                    foreach (var element in node.Elements())
                    {
                        Parse(list, element);
                    }

                    AddProperty(item, node.Elements().First().Name.LocalName, list);
                    AddProperty(parent, node.Name.ToString(), item);
                }
                else
                {
                    var item = new ExpandoObject();

                    foreach (var attribute in node.Attributes())
                    {
                        AddProperty(item, attribute.Name.ToString(), attribute.Value.Trim());
                    }

                    //element
                    foreach (var element in node.Elements())
                    {
                        Parse(item, element);
                    }

                    AddProperty(parent, node.Name.ToString(), item);
                }
            }
            else
            {
                AddProperty(parent, node.Name.ToString(), node.Value.Trim());
            }
        }

        private static void AddProperty(dynamic parent, string name, object value)
        {
            if (parent is List<dynamic>)
            {
                (parent as List<dynamic>).Add(value);
            }
            else
            {
                (parent as IDictionary<String, object>)[name] = value;
            }
        }
    }

}
