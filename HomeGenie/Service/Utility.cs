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
            IList<T> retlist = null;
            if (listToClone.GetType() == typeof(TsList<T>))
            {
                TsList<T> tslist = ((TsList<T>)listToClone);
                retlist = listToClone.Select(item => (T)item.Clone()).ToList();
            }
            else
            {
                retlist = listToClone.Select(item => (T)item.Clone()).ToList();
            }
            return retlist;
        }
    }

    //// source: http://msdn.microsoft.com/en-us/library/ee722116(v=vs.110).aspx
    //public class Latch
    //{
    //    // 0 = unset, 1 = set 
    //    private volatile int m_state = 0;
    //    private ManualResetEvent m_ev = new ManualResetEvent(false);

    //    public void Set()
    //    {
    //        m_state = 1;
    //        m_ev.Set();
    //    }

    //    public void Wait()
    //    {
    //        Wait(Timeout.Infinite);
    //    }

    //    public bool Wait(int timeout)
    //    {
    //        // Allocated on the stack.
    //        SpinWait spinner = new SpinWait();
    //        Stopwatch watch;

    //        while (m_state == 0)
    //        {

    //            // Lazily allocate and start stopwatch to track timeout.
    //            watch = Stopwatch.StartNew();

    //            // Spin only until the SpinWait is ready 
    //            // to initiate its own context switch. 
    //            if (!spinner.NextSpinWillYield)
    //            {
    //                spinner.SpinOnce();

    //            }
    //            // Rather than let SpinWait do a context switch now, 
    //            //  we initiate the kernel Wait operation, because 
    //            // we plan on doing this anyway. 
    //            else if (timeout != Timeout.Infinite)
    //            {
    //                //totalKernelWaits++;
    //                // Account for elapsed time. 
    //                int realTimeout = timeout - (int)watch.ElapsedMilliseconds;

    //                // Do the wait. 
    //                if (realTimeout <= 0 || !m_ev.WaitOne(realTimeout))
    //                {
    //                    return false;
    //                }
    //            }
    //        }

    //        // Take the latch.
    //        m_state = 0;

    //        return true;
    //    }
    //}


    //[Serializable()]
    //public class TsList<T> : System.Collections.Generic.List<T>
    //{
    //    [NonSerialized]
    //    private Latch _latch = new Latch();
    //    //private List<T> _list = new List<T>();
    //    private object _sync = new object();

    //    public TsList()
    //    {
    //        _latch.Set();
    //    }

    //    [XmlIgnore]
    //    public Latch ConcurrentLock
    //    {
    //        get { return _latch; }
    //    }

    //    new public void Add(T value)
    //    {

    //        _latch.Wait();
    //        base.Add(value);
    //        _latch.Set();
    //    }
    //    new public void RemoveAll(Predicate<T> predicate)
    //    {
    //        _latch.Wait();
    //        base.RemoveAll(predicate);
    //        _latch.Set();
    //    }
    //    new public void Remove(T item)
    //    {
    //        _latch.Wait();
    //        base.Remove(item);
    //        _latch.Set();
    //    }

    //    //new public T Find(Predicate<T> predicate)
    //    //{
    //    //    T retobj = default(T);
    //    //    _latch.Wait();
    //    //    retobj = base.Find(predicate);
    //    //    _latch.Set();
    //    //    return retobj;
    //    //}

    //    new public void Sort(Comparison<T> comparison)
    //    {
    //        _latch.Wait();
    //        base.Sort(comparison);
    //        _latch.Set();
    //    }

    //}



    [Serializable()]
    public class TsList<T> : System.Collections.Generic.List<T>
    {
        private object _sync = new object();

        public TsList()
        {
        }

        public object LockObject
        {
            get { return _sync; }
        }
        new public void Clear()
        {

            lock (_sync)
                base.Clear();
        }
        new public void Add(T value)
        {

            lock (_sync)
                base.Add(value);
        }
        new public void RemoveAll(Predicate<T> predicate)
        {
            lock (_sync)
                base.RemoveAll(predicate);
        }
        new public void Remove(T item)
        {
            lock (_sync)
                base.Remove(item);
        }

        new public void Sort(Comparison<T> comparison)
        {
            lock (_sync)
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


        public static void Say(string sentence, string locale, bool async = false)
        {
            if (async)
            {
                Thread t = new Thread(new ThreadStart(delegate()
                {
                    Say(sentence, locale);
                }));
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
            Process updater = new Process();
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
                WebClient wc = new WebClient();
                wc.Headers.Add("Referer", "http://translate.google.com");
                byte[] audiodata = wc.DownloadData("http://translate.google.com/translate_tts?tl=" + locale + "&q=" + sentence);
                wc.Dispose();

                string outdir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "_tmp");
                if (!Directory.Exists(outdir)) Directory.CreateDirectory(outdir);
                string file = Path.Combine(outdir, "_synthesis_tmp.mp3");

                if (File.Exists(file)) File.Delete(file);
                FileStream fs = File.OpenWrite(file);
                fs.Write(audiodata, 0, audiodata.Length);
                fs.Close();

                string filewav = file.Replace(".mp3", ".wav");
                Process.Start(new ProcessStartInfo("lame", "--decode \"" + file + "\" \"" + filewav + "\"")
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                    UseShellExecute = false
                }).WaitForExit();

                Play(filewav);
            }
            catch (Exception)
            {
                // TODO: add error logging 
            }
        }

        public static void Play(string filewav)
        {

            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;
            //
            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    PlaySound(filewav, UIntPtr.Zero, (uint)(0x00020000 | 0x00000000));
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                default:
                    System.Media.SoundPlayer p = new System.Media.SoundPlayer();
                    p.SoundLocation = filewav;
                    p.Play();
                    break;
            }

        }







        private const long BUFFER_SIZE = 4096;

        public static void AddFileToZip(string zipFilename, string fileToAdd)
        {
            using (Package zip = System.IO.Packaging.Package.Open(zipFilename, FileMode.OpenOrCreate))
            {
                string destFilename = fileToAdd;
                Uri uri = PackUriHelper.CreatePartUri(new Uri(destFilename, UriKind.Relative));
                if (zip.PartExists(uri))
                {
                    zip.DeletePart(uri);
                }
                PackagePart part = zip.CreatePart(uri, "", CompressionOption.Normal);
                using (FileStream fileStream = new FileStream(fileToAdd, FileMode.Open, FileAccess.Read))
                {
                    using (Stream dest = part.GetStream())
                    {
                        CopyStream(fileStream, dest);
                    }
                }
            }
        }

        private static void CopyStream(System.IO.FileStream inputStream, System.IO.Stream outputStream)
        {
            long bufferSize = inputStream.Length < BUFFER_SIZE ? inputStream.Length : BUFFER_SIZE;
            byte[] buffer = new byte[bufferSize];
            int bytesRead = 0;
            long bytesWritten = 0;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) != 0)
            {
                outputStream.Write(buffer, 0, bytesRead);
                bytesWritten += bufferSize;
            }
        }






        public static dynamic XmlParseToDynamic(string xml)
        {
            var xDoc = XDocument.Load(new StringReader(xml));
            dynamic root = new ExpandoObject();
            XmlToDynamic.Parse(root, xDoc.Elements().First());
            return root;
        }






        public delegate void AsyncFunction();
        public static Thread RunAsyncTask(AsyncFunction fnblock)
        {
            Thread at = new Thread(new ThreadStart(() =>
            {
                try
                {
                    fnblock();
                }
                catch (Exception ex)
                {
                    HomeGenieService.LogEvent(Domains.HomeAutomation_HomeGenie, "Service.Utility.RunAsyncTask", ex.Message, "Exception.StackTrace", ex.StackTrace);
                }
            }));
            at.Start();
            return at;
        }



        public static ModuleParameter ModuleParameterGet(Module module, string prop)
        {
            return module.Properties.Find(delegate(ModuleParameter mp) { return mp.Name == prop; });
        }

        public static ModuleParameter ModuleParameterSet(Module m, string prop, string value)
        {
            if (m == null) return null;
            //
            ModuleParameter mp = m.Properties.Find(mpar => mpar.Name == prop);
            if (mp == null)
            {
                mp = new ModuleParameter() { Name = prop, Value = value };
                m.Properties.Add(mp);
            }
            mp.Value = value;
            return mp;
        }

        public static string WaitModuleParameterChange(Module mod, string parameter)
        {
            string val = "";
            // TODO make it as a function _waitModuleParameterChange(mod, parname, timeout)
            ModuleParameter p = Service.Utility.ModuleParameterGet(mod, parameter);
            if (p != null)
            {
                long updated = DateTime.UtcNow.Ticks; //p.UpdateTime.Ticks - (TimeSpan.TicksPerSecond * 1); 
                //
                System.Threading.Thread.Sleep(500);
                //
                int timeout = 0;
                int maxwait = 50; //(50 * 100ms ticks = 5000 ms)
                int tickfreq = 100;
                //
                while ((TimeSpan.FromTicks(p.UpdateTime.Ticks - updated).TotalSeconds > 1 /*&& (DateTime.UtcNow.Ticks - p.UpdateTime.Ticks > 5 * TimeSpan.TicksPerSecond)*/) && timeout++ < maxwait)
                {
                    System.Threading.Thread.Sleep(tickfreq);
                }
                //
                if (timeout < maxwait)
                {
                    val = p.Value;
                }
            }
            return val;
        }





        public class StringValueAttribute : System.Attribute
        {

            private string _value;

            public StringValueAttribute(string value)
            {
                _value = value;
            }

            public string Value
            {
                get { return _value; }
            }

        }

        public static DateTime JavaTimeStampToDateTime(double javaTimeStamp)
        {
            // Java timestamp is millisecods past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(javaTimeStamp);
            return dtDateTime;
        }



        public static string Module2Json(Module m, bool hideprops)
        {
            string json = "{\n" +
                        "   \"Name\": \"" + XmlEncode(m.Name) + "\",\n" +
                        "   \"Description\": \"" + XmlEncode(m.Description) + "\",\n" +
                        "   \"Type\": \"" + m.Type + "\",\n" +
                        "   \"DeviceType\": \"" + m.DeviceType + "\",\n" +
                        "   \"Domain\": \"" + m.Domain + "\",\n" +
                        "   \"Address\": \"" + m.Address + "\",\n";
            if (!hideprops)
            {
                json += "   \"Properties\": [ \n";
                //
                for (int i = 0; i < m.Properties.Count; i++)
                {
                    ModuleParameter p = m.Properties[i];
                    // if (p.Name == Properties.VirtualModule_ParentId) continue;
                    json += "       {\n" +
                            "           \"Name\": \"" + XmlEncode(p.Name) + "\",\n" +
                            "           \"Description\": \"" + XmlEncode(p.Description) + "\",\n" +
                            "           \"Value\": \"" + XmlEncode(p.Value) + "\",\n" +
                            "           \"UpdateTime\": \"" + p.UpdateTime.ToString("u") + "\",\n" +
                            "           \"ValueIncrement\": \"" + p.ValueIncrement.ToString() + "\",\n" +
                            "           \"LastValue\": \"" + XmlEncode(p.LastValue) + "\",\n" +
                            "           \"LastUpdateTime\": \"" + p.LastUpdateTime.ToString("u") + "\"\n" +
                            "       },\n";
                    //System.Threading.Thread.Sleep(1);
                }
                json = json.TrimEnd(',', '\n');
                //
                json += "   ],\n";
            }
            json += "   \"RoutingNode\": \"" + (m.RoutingNode != null ? m.RoutingNode : "") + "\"\n";
            json += "}";
            //
            return json;
        }

        public static string XmlEncode(string s)
        {
            if (s == null)
            {
                s = "";
            }
            else //if (s.IndexOf("&") >= 0 && s.IndexOf("\"") >= 0)
            {
                s = s.Replace("&", "&amp;");
                s = s.Replace("\"", "&quot;");
            }
            return s;
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
