#region Header Licence
//  ---------------------------------------------------------------------
// 
//  Copyright (c) 2009 Alexandre Mutel and Microsoft Corporation.  
//  All rights reserved.
// 
//  This code module is part of NShader, a plugin for visual studio
//  to provide syntax highlighting for shader languages (hlsl, glsl, cg)
// 
//  ------------------------------------------------------------------
// 
//  This code is licensed under the Microsoft Public License. 
//  See the file License.txt for the license details.
//  More info on: http://nshader.codeplex.com
// 
//  ------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace NShader
{

    public class EnumMap<T> : Dictionary<string, T>
    {
        public void Load(string resource)
        {
            Stream file = typeof(T).Assembly.GetManifestResourceStream(typeof(T).Assembly.GetName().Name + "." + resource);
            TextReader textReader = new StreamReader(file);
            Load(textReader);

            // If the user file exists, then we merge it with the embedded settings.
            var customResourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NShader", resource);
            if (File.Exists(customResourcePath))
            {
                textReader = new StreamReader(customResourcePath);
                Load(textReader, true);
            }
        }

        private void Load(TextReader textReader, bool merge = false)
        {
            string line;
            while ((line = textReader.ReadLine()) != null )
            {
                int indexEqu = line.IndexOf('=');
                if ( indexEqu > 0 )
                {
                    string enumName = line.Substring(0, indexEqu);
                    string value = line.Substring(indexEqu + 1, line.Length - indexEqu-1).Trim();
                    string[] values = Regex.Split(value, @"[\t ]+");
                    T enumValue = (T)Enum.Parse(typeof(T), enumName);
                    foreach (string token in values)
                    {
                        if (merge && ContainsKey(token))
                        {
                            Remove(token);
                        }

                        if (!ContainsKey(token))
                        {
                            Add(token, enumValue);
                        }
                        else
                        {
                            Trace.WriteLine(string.Format("Warning: token {0} for enum {1} already added for {2}", token, enumValue, this[token]));
                        }
                    }
                }

            }
        }
    }
}