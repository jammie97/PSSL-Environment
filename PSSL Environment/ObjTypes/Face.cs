﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSL_Environment.ObjTypes
{
    public class Face : Type
    {
        public const int MinimumDataLength = 4;
        public const string Prefix = "f";

        public int IndicesPerFace = 0;
        public string UseMtl { get; set; }
        public int[] VertexIndexList { get; set; }
        public int[] NormalIndexList { get; set; }
        public int[] TextureVertexIndexList { get; set; }

        public void LoadFromStringArray(string[] input)
        {
            string[] data = input.ToArray();
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = data[i].Replace(@"//", @"/0/");
            }

            if (data.Length < MinimumDataLength)
                throw new ArgumentException("Input array must be of minimum length " + MinimumDataLength, "data");

            if (!data[0].ToLower().Equals(Prefix))
                throw new ArgumentException("Data prefix must be '" + Prefix + "'", "data");

            int vcount = data.Count() - 1;
            VertexIndexList = new int[vcount];
            TextureVertexIndexList = new int[vcount];
            NormalIndexList = new int[vcount];

            bool success;

            //Find empty face indexing and make them equal 0
            //string[] temp = new String[data.Length];
            //for (int i = 0; i < temp.Length; i++)
            //{
            //    temp[i] = temp[i].Replace("//", "/0/");
            //}

            for (int i = 0; i < vcount; i++)
            {
                string[] parts = data[i + 1].Split('/');

                int vindex;
                success = int.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out vindex);
                if (!success) throw new ArgumentException("Could not parse parameter as int");
                VertexIndexList[i] = vindex;
                

                if (parts.Count() > 1)
                {
                    success = int.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out vindex);
                    if (success)
                    {
                        TextureVertexIndexList[i] = vindex;
                    }
                }

                if (parts.Count() > 2)
                {
                    success = int.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out vindex);
                    if (success)
                    {
                        NormalIndexList[i] = vindex;
                    }
                }
            }

            IndicesPerFace = vcount;
        }

        // HACKHACK this will write invalid files if there are no texture vertices in
        // the faces, need to identify that and write an alternate format
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append("f");

            for (int i = 0; i < VertexIndexList.Count(); i++)
            {
                if (i < TextureVertexIndexList.Length)
                {
                    b.AppendFormat(" {0}/{1}", VertexIndexList[i], TextureVertexIndexList[i]);
                }
                else
                {
                    b.AppendFormat(" {0}", VertexIndexList[i]);
                }
            }

            return b.ToString();
        }
    }
}
