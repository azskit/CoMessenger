using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CimPlugin.Plugin
{
    //    class PluginManager
    //    {

    //        /// <summary>
    //        /// Поток для информационных сообщений
    //        /// </summary>
    //        public TextWriter InfoStream { get; set; }

    //        /// <summary>
    //        /// Поток для  сообщений об ошибках
    //        /// </summary>
    //        public TextWriter ErrorStream { get; set; }



    //        private List<IPlugin> plugins;

    //        private void LoadPlugins()
    //        {
    //            try
    //            {
    //                plugins = new List<IPlugin>();
    //                string folder = System.AppDomain.CurrentDomain.BaseDirectory;
    //                string[] files = System.IO.Directory.GetFiles(folder, "*.dll");

    //                foreach (string file in files)
    //                {
    //                    IPlugin plugin = IsPlugin(file);
    //                    if (plugin != null)
    //                        plugins.Add(plugin);
    //                }
    //            }
    //            catch (Exception e)
    //            {
    //                ErrorStream.WriteLine("{0} : An error has occured during loading plugins : {1}", DateTime.Now.ToString(), e.Message);
    //                //ErrorStream.WriteLine("{0} : An error has occured during loading plugins : {1}", DateTime.Now.ToString(), CustomUtilites.FormatException(e));
    //            }
    //        }

    //        private IPlugin IsPlugin(byte[] file)
    //        {
    //            System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(file);
    //            foreach (Type type in assembly.GetTypes())
    //            {
    //                Type iface = type.GetInterface("Plugin.IPlugin");
    //                if (iface != null)
    //                {
    //                    IPlugin plugin = (IPlugin)Activator.CreateInstance(type);
    //                    return plugin;
    //                }
    //            }
    //            return null;
    //        }

    //        private IPlugin IsPlugin(string file_url)
    //        {
    //            try
    //            {
    //                byte[] b = System.IO.File.ReadAllBytes(file_url);
    //                return IsPlugin(b);
    //            }
    //            catch (Exception e)
    //            {
    //                ErrorStream.WriteLine("{0} : An error has occured during loading file {1}: {2}", DateTime.Now.ToString(), file_url, e.Message);
    //                //ErrorStream.WriteLine("{0} : An error has occured during loading file {1}: {2}", DateTime.Now.ToString(), file_url, CustomUtilites.FormatException(e));
    //            }
    //            return null;
    //        }
    //    }
}
