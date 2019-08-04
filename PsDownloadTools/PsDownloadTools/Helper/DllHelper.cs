using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;

namespace PsDownloadTools.Helper
{
    class DllHelper
    {
        private static readonly Dictionary<String, Assembly> _loadedDlls = new Dictionary<String, Assembly>();

        public static void RegistDLL()
        {
            Regex regex = new Regex($"^*\\.dll$", RegexOptions.IgnoreCase);
            Assembly assembly = new StackTrace(0).GetFrame(1).GetMethod().Module.Assembly;
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
            String[] res = assembly.GetManifestResourceNames();
            foreach (String r in res)
            {
                if (regex.IsMatch(r))
                {
                    try
                    {
                        Stream stream = assembly.GetManifestResourceStream(r);
                        Byte[] bytes = new Byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        Assembly assemblyObject = Assembly.Load(bytes);
                        if (!_loadedDlls.ContainsKey(assemblyObject.FullName))
                        {
                            _loadedDlls[assemblyObject.FullName] = assemblyObject;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(Application.Current.TryFindResource("StrDllError1") as String + e.Message);
                    }
                }
            }
        }

        private static Assembly AssemblyResolve(Object sender, ResolveEventArgs args)
        {
            try
            {
                String assemblyName = new AssemblyName(args.Name).FullName;
                if (_loadedDlls.TryGetValue(assemblyName, out Assembly assemblyObject) && assemblyObject != null)
                {
                    _loadedDlls[assemblyName] = null;
                    return assemblyObject;
                }
                else
                {
                    throw new DllNotFoundException(assemblyName);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(Application.Current.TryFindResource("StrDllError2") as String + e.Message);
                return null;
            }
        }
    }
}
