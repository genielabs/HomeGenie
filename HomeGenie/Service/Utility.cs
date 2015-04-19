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
using HomeGenie.Service.Constants;
using System.IO.Packaging;
using System.Xml.Serialization;

namespace HomeGenie.Service
{
    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            IList<T> list = null;
            if (listToClone.GetType() == typeof(TsList<T>))
            {
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
            dynamic res = new ExpandoObject();
            res.ResponseValue = value;
            return "[" + Newtonsoft.Json.JsonConvert.SerializeObject(res) + "]";
        }
    }

    public static class Utility
    {

        public static dynamic ParseXmlToDynamic(string xml)
        {
            var document = XElement.Load(new StringReader(xml));
            XElement root = new XElement("Root", document);
            return new DynamicXmlParser(root);
        }

        public static ModuleParameter ModuleParameterGet(Module module, string propertyName)
        {
            return module.Properties.Find(delegate(ModuleParameter parameter)
            {
                return parameter.Name == propertyName;
            });
        }

        public static ModuleParameter ModuleParameterSet(Module module, string propertyName, string propertyValue)
        {
            if (module == null)
                return null;
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
            ModuleParameter parameter = null;
            var start = DateTime.UtcNow.Ticks;
            var now = start;
            int maxSecWait = 10; // 10 seconds max wait
            while (parameter == null && TimeSpan.FromTicks(now - start).TotalSeconds <= maxSecWait)
            {
                // wait for maxSecWait seconds if the parameterName doesn't exit yet - it migt not have been initialized yet
                // classes that use encryption are require many messages to be exchanged
                now = DateTime.UtcNow.Ticks;
                parameter = Service.Utility.ModuleParameterGet(module, parameterName);
                if (parameter == null)
                {
                    //Console.WriteLine("Thread - " + System.Threading.Thread.CurrentThread.ManagedThreadId + " Waiting .5s for " + parameterName + ". Waited " + TimeSpan.FromTicks(now - start).TotalSeconds);
                    Thread.Sleep(500);
                }
            }

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
                // I don't think that it will ever get into the while loop because the "updated" was just reset
                // what's the change the "parameter.UpdateTime.Ticks" was updated after "updated" was reset
                // we'll accept 1 second old values as still current values
                while ((TimeSpan.FromTicks(updated - parameter.UpdateTime.Ticks).TotalSeconds > 1 /*&& (DateTime.UtcNow.Ticks - p.UpdateTime.Ticks > 5 * TimeSpan.TicksPerSecond)*/) && timeout++ < maxWait)
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
                "   \"Name\": \"" + JsonEncode(module.Name) + "\",\n" +
                "   \"Description\": \"" + JsonEncode(module.Description) + "\",\n" +
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
                        "           \"Name\": \"" + JsonEncode(parameter.Name) + "\",\n" +
                        "           \"Description\": \"" + JsonEncode(parameter.Description) + "\",\n" +
                        "           \"Value\": \"" + JsonEncode(parameter.Value) + "\",\n" +
                        "           \"UpdateTime\": \"" + parameter.UpdateTime.ToString("u") + "\"\n" +
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

        public static string JsonEncode(string fieldValue)
        {
            if (fieldValue == null)
            {
                fieldValue = "";
            }
            else
            {
                fieldValue = fieldValue.Replace("&", "&amp;");
                fieldValue = fieldValue.Replace("\"", "&quot;");
                fieldValue = fieldValue.Replace("\n", "\\n");
                //fieldValue = fieldValue.Replace("\'", "\\'");
                fieldValue = fieldValue.Replace("\r", "\\r");
                fieldValue = fieldValue.Replace("\t", "\\t");
                fieldValue = fieldValue.Replace("\b", "\\b");
                fieldValue = fieldValue.Replace("\f", "\\f");
            }
            return fieldValue;
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

        public static string GetTmpFolder()
        {
            string tempFolder = "tmp";
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
            return tempFolder;
        }

        public static void FolderCleanUp(string path)
        {
            try
            {
                // clean up directory
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
                Directory.CreateDirectory(path);
            }
            catch
            {
                // TODO: report exception
            }
        }

        #region Private helper methods

        [DllImport("winmm.dll", SetLastError = true)]
        static extern bool PlaySound(string pszSound, UIntPtr hmod, uint fdwSound);
        // buffer size for AddFileToZip
        private const long BUFFER_SIZE = 4096;
        // delegate used by RunAsyncTask
        public delegate void AsyncFunction();

        internal static void Say(string sentence, string locale, bool async = false)
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

        internal static void Say(string sentence, string locale)
        {
            try
            {
                var client = new WebClient();
                client.Encoding = UTF8Encoding.UTF8;
                client.Headers.Add("Referer", "http://translate.google.com");
                var audioData = client.DownloadData("http://translate.google.com/translate_tts?ie=UTF-8&tl=" + Uri.EscapeDataString(locale) + "&q=" + Uri.EscapeDataString(sentence));
                client.Dispose();

                var outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_tmp");
                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
                var file = Path.Combine(outputDirectory, "_synthesis_tmp.mp3");

                if (File.Exists(file))
                    File.Delete(file);
                var stream = File.OpenWrite(file);
                stream.Write(audioData, 0, audioData.Length);
                stream.Close();

                var wavFile = file.Replace(".mp3", ".wav");
                Process.Start(new ProcessStartInfo("lame", "--decode \"" + file + "\" \"" + wavFile + "\"") {
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

        internal static void Play(string wavFile)
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
                //var player = new System.Media.SoundPlayer();
                //player.SoundLocation = wavFile;
                //player.Play();
                Process.Start(new ProcessStartInfo("aplay", "\"" + wavFile + "\"") {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false
                }).WaitForExit();
                break;
            }

        }

        internal static List<string> UncompressZip(string archiveName, string destinationFolder)
        {
            List<string> extractedFiles = new List<string>();
            // Unarchive (unzip)
            using (var package = Package.Open(archiveName, FileMode.Open, FileAccess.Read))
            {
                foreach (var part in package.GetParts())
                {
                    string filePath = part.Uri.OriginalString.Substring(1);
                    string target = Path.Combine(destinationFolder, filePath);
                    if (!Directory.Exists(Path.GetDirectoryName(target)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(target));
                    }

                    if (File.Exists(target))
                        File.Delete(target);

                    using (var source = part.GetStream(FileMode.Open, FileAccess.Read))
                    using (var destination = File.OpenWrite(target))
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            destination.Write(buffer, 0, read);
                        }
                    }
                    try { extractedFiles.Add(filePath); } catch { }
                }
            }

            return extractedFiles;
        }

        internal static void AddFileToZip(string zipFilename, string fileToAdd, string storeAsName = null)
        {
            using (var zip = System.IO.Packaging.Package.Open(zipFilename, FileMode.OpenOrCreate))
            {
                string destFilename = (String.IsNullOrWhiteSpace(storeAsName) ? fileToAdd : storeAsName);
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

        internal static Thread RunAsyncTask(AsyncFunction functionBlock)
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

        #endregion

    }

    public class DynamicXmlParser : DynamicObject
    {

        XElement element;

        public DynamicXmlParser(string filename)
        {
            element = XElement.Load(filename);
        }

        public DynamicXmlParser(XElement el)
        {
            element = el;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (element == null)
            {
                result = null;
                return false;
            }

            XElement sub = element.Element(binder.Name);
            if (sub == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = new DynamicXmlParser(sub);
                return true;
            }
        }

        public static implicit operator string(DynamicXmlParser p)
        {
            return p.ToString();
        }

        public override string ToString()
        {
            if (element != null)
            {
                return element.Value;
            }
            else
            {
                return string.Empty;
            }
        }

        public string this[string attr]
        {
            get
            {
                if (element == null)
                {
                    return string.Empty;
                }
                return element.Attribute(attr).Value;
            }
        }
    }
}
