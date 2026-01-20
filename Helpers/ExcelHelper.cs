using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace PingMonitor.Helpers
{
    public class ExcelHelper
    {
        public static DataTable ReadExcelToDataTable(string filePath)
        {
            var dt = new DataTable();
            
            try
            {
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    // 1. Read Shared Strings
                    var sharedStrings = new List<string>();
                    var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
                    if (ssEntry != null)
                    {
                        using (var stream = ssEntry.Open())
                        using (var reader = new StreamReader(stream))
                        {
                            string content = reader.ReadToEnd();
                            // Simple regex to find <t>content</t>
                            // Note: This is basic and might miss some edge cases but works for standard Excel files
                            var matches = Regex.Matches(content, @"<t[^>]*>([^<]*)</t>");
                            foreach (Match m in matches)
                            {
                                sharedStrings.Add(m.Groups[1].Value);
                            }
                        }
                    }

                    // 2. Read Sheet1 Data
                    var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
                    if (sheetEntry == null) throw new Exception("Cannot find sheet1.xml");

                    using (var stream = sheetEntry.Open())
                    using (var reader = new StreamReader(stream))
                    {
                        var xmlDoc = new XmlDocument();
                        xmlDoc.Load(reader);

                        var nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                        nsManager.AddNamespace("d", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

                        var rows = xmlDoc.SelectNodes("//d:sheetData/d:row", nsManager);
                        if (rows == null || rows.Count == 0) return dt;

                        // Identify columns from the first few rows (max col index)
                        int maxCol = 0;
                        foreach (XmlNode row in rows)
                        {
                            var cells = row.SelectNodes("d:c", nsManager);
                            if (cells == null) continue;
                            foreach (XmlNode cell in cells)
                            {
                                string r = cell.Attributes["r"]?.Value; // e.g. "A1", "B2"
                                if (!string.IsNullOrEmpty(r))
                                {
                                    int colIdx = GetColIndex(r);
                                    if (colIdx > maxCol) maxCol = colIdx;
                                }
                            }
                        }

                        // Create columns
                        for (int i = 0; i <= maxCol; i++)
                        {
                            dt.Columns.Add($"Column {i + 1}");
                        }

                        // Populate data
                        foreach (XmlNode row in rows)
                        {
                            var dataRow = dt.NewRow();
                            var cells = row.SelectNodes("d:c", nsManager);
                            if (cells != null)
                            {
                                foreach (XmlNode cell in cells)
                                {
                                    string r = cell.Attributes["r"]?.Value;
                                    string t = cell.Attributes["t"]?.Value; // type: "s" for shared string
                                    string v = cell.SelectSingleNode("d:v", nsManager)?.InnerText;

                                    if (string.IsNullOrEmpty(r)) continue;
                                    int colIdx = GetColIndex(r);

                                    string cellValue = v;
                                    if (t == "s" && int.TryParse(v, out int strIdx))
                                    {
                                        if (strIdx >= 0 && strIdx < sharedStrings.Count)
                                            cellValue = sharedStrings[strIdx];
                                    }

                                    if (colIdx < dt.Columns.Count)
                                    {
                                        dataRow[colIdx] = cellValue;
                                    }
                                }
                            }
                            dt.Rows.Add(dataRow);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Excel: {ex.Message}");
            }

            return dt;
        }

        // Convert cell reference like "AA1" to 0-based index
        private static int GetColIndex(string cellRef)
        {
            // Remove digits
            string colRef = Regex.Replace(cellRef, @"[\d]", "");
            int sum = 0;
            foreach (char c in colRef)
            {
                sum *= 26;
                sum += (c - 'A' + 1);
            }
            return sum - 1;
        }
    }
}
