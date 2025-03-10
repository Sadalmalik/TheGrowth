using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GeekyHouse.Architecture.CSV
{
    public static class CSVUtils
    {
        private static string SPLIT_RE = @",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        private static string LINE_SPLIT_RE = @"(?:\r\n|\n\r|\n|\r)(?=(?:[^""]*""[^""]*"")*(?![^""]*""))";
        private static char[] TRIM_CHARS = { '\"', ' ', '\n', '\r' };
        private static char[] SPECIAL_CHAR = new char[] { ',', '\n', '"', '«', '»' };

        public static List<string[]> FromStrint(string text, bool forceFillColls=true)
        {
            var lines = Regex.Split(text, LINE_SPLIT_RE);

            var table = new List<string[]>();

            if (lines.Length <= 1) return table;

            var maxLength = 0;

            for (var i = 0; i < lines.Length; i++)
            {
                var values = Regex.Split(lines[i], SPLIT_RE);
                if (values.Length == 0) continue;
                bool allEmpty = true;
                for (var j = 0; j < values.Length; j++)
                {
                    var item = values[j];
                    if (item.StartsWith("\"") && item.EndsWith("\""))
                        item = item.Substring(1, item.Length - 2);
                    values[j] = item.Replace("\"\"", "\"").Trim(TRIM_CHARS);
                    allEmpty &= string.IsNullOrEmpty(values[j]);
                }
                if (allEmpty) continue;
                maxLength = Mathf.Max(maxLength, values.Length);
                table.Add(values);
            }
            
            if (forceFillColls)
            {
                for (int i=0;i<table.Count;i++)
                {
                    var row = table[i];
                    Array.Resize(ref row, maxLength);
                    table[i] = row;
                }
            }

            return table;
        }

        public static string ToString(List<string[]> table)
        {
            StringBuilder builder = new StringBuilder();
            bool firstLine = true;
            foreach (string[] line in table)
            {
                if (!firstLine)
                    builder.Append("\n\r");
                firstLine = false;

                bool firstItem = true;
                foreach (var item in line)
                {
                    if (!firstItem)
                        builder.Append(",");
                    firstItem = false;

                    if (item.IndexOfAny(SPECIAL_CHAR) != -1)
                        builder.AppendFormat("\"{0}\"", item.Replace("\"", "\"\""));
                    else
                        builder.Append(item);
                }
            }
            return builder.ToString();
        }
    }
}