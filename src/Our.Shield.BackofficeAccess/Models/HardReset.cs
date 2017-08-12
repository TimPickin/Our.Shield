﻿using Our.Shield.Core.Models;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Umbraco.Core.Logging;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Our.Shield.BackofficeAccess.Models.HardReset), nameof(Our.Shield.BackofficeAccess.Models.HardReset.Start))]
namespace Our.Shield.BackofficeAccess.Models
{
    internal class HardReset
    {
        public static void Start()
        {
            var resetter = new HardResetFileHandler();

            try
            {
                if (resetter.SoftLocation == null)
                {
                    return;
                }

                var curUmbVersion = Umbraco.Core.Configuration.UmbracoVersion.GetSemanticVersion().ToString();

                if(!curUmbVersion.Equals(ApplicationSettings.UmbracoVersion, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                if (!Directory.Exists(resetter.HardLocation))
                {
                    throw new FileNotFoundException($"Unable to find directory at location {resetter.HardLocation} for renaming to {Path.GetFileName(resetter.SoftLocation)}");
                }

                if (Directory.Exists(resetter.SoftLocation))
                {
                    Directory.Delete(resetter.SoftLocation, true);
                }

                var webConfig = new WebConfigFileHandler();

                webConfig.UmbracoPath = "~/" + Path.GetFileName(resetter.SoftLocation);
                ConfigurationManager.AppSettings.Set("umbracoPath", webConfig.UmbracoPath);

                var paths = webConfig.UmbracoReservedPaths;

                var regex = new Regex("^(~?)(/?)" + Path.GetFileName(resetter.HardLocation) + "(/?)$", RegexOptions.IgnoreCase);

                for (var i = 0; i < paths.Length; i++)
                {
                    if (regex.IsMatch(paths[i]))
                    {
                        paths[i] = regex.Replace(paths[i], webConfig.UmbracoPath);
                    }
                }

                webConfig.UmbracoReservedPaths = paths;
                ConfigurationManager.AppSettings.Set("umbracoReservedPaths", string.Join(",", paths));

                webConfig.SetLocationPath(Path.GetFileName(resetter.HardLocation).Trim('/'), webConfig.UmbracoPath.TrimStart('~', '/').TrimEnd('/'));

                Directory.Move(resetter.HardLocation, resetter.SoftLocation);
                try
                {
                    webConfig.Save();
                }
                catch(Exception saveEx)
                {
                    throw saveEx;
                }
                
                resetter.Delete();
            }
            catch (Exception ex)
            {
                LogHelper.Error<HardReset>($"Unexpected error occurred renaming folder {Path.GetFileName(resetter.HardLocation)} to {Path.GetFileName(resetter.SoftLocation)} OR error setting correct app key values in web.config for umbracoPath / umbracoReservedPaths", ex);
                throw ex;
            }
        }

        internal class WebConfigFileHandler
        {
            private const string file = "web.config";

            private string filePath
            {
                get
                {
                    return AppDomain.CurrentDomain.BaseDirectory + file;
                }
            }

            private XDocument webConfig;

            public WebConfigFileHandler()
            {
                webConfig = XDocument.Load(filePath);
            }


            public string UmbracoPath
            {
                get
                {
                    return webConfig.XPathSelectElement("/configuration/appSettings/add[@key='umbracoPath']").Attribute("value").Value;
                }
                set
                {
                    webConfig.XPathSelectElement("/configuration/appSettings/add[@key='umbracoPath']").Attribute("value").Value = value;
                }
            }
            public string[] UmbracoReservedPaths
            {
                get
                {
                    return webConfig.XPathSelectElement("/configuration/appSettings/add[@key='umbracoReservedPaths']").Attribute("value").Value.Split(',');
                }
                set
                {
                    webConfig.XPathSelectElement("/configuration/appSettings/add[@key='umbracoReservedPaths']").Attribute("value").Value = string.Join(",", value);
                }
            }

            public void SetLocationPath(string currentLocation, string newLocation)
            {
                var element = webConfig.XPathSelectElement("/configuration/location[@path='" + currentLocation + "']");
                if (element != null)
                {
                    element.Attribute("path").Value = newLocation;
                }
            }

            public void Save()
            {
                using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    fileStream.SetLength(0);
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.UTF8))
                    {
                        streamWriter.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" + webConfig.ToString());
                    }
                }
            }
        }
    }
}
