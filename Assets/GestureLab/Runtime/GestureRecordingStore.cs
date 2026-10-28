using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using PDollarGestureRecognizer;
using UnityEngine;

namespace ForearmLab
{
    public static class GestureRecordingStore
    {
        public sealed class Record
        {
            public string id;
            public Gesture gesture;
            public DateTime createdUtc;
        }

        const string AssetFolder = "Assets/GestureLab/Resources/CustomGestures";
        public static string Folder => Path.Combine(Application.dataPath, "GestureLab/Resources/CustomGestures");

        static string FilePath(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id == "." || id == ".." ||
                id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || id.Contains("/") || id.Contains("\\"))
                throw new ArgumentException("Invalid recording ID");
            return Path.Combine(Folder, id + ".xml");
        }

        public static List<Record> List()
        {
            var records = new List<Record>();
#if UNITY_EDITOR
            if (!Directory.Exists(Folder)) return records;
            foreach (var path in Directory.GetFiles(Folder, "*.xml"))
            {
                string id = Path.GetFileNameWithoutExtension(path);
                try
                {
                    records.Add(new Record { id = id, gesture = Read(path), createdUtc = File.GetCreationTimeUtc(path) });
                }
                catch (Exception e) { Debug.LogWarning($"Skipped unreadable gesture recording {id}: {e.Message}"); }
            }
            records.Sort((a, b) => b.createdUtc.CompareTo(a.createdUtc));
#else
            // Resources are packaged assets in a player; editing the source library is an Editor workflow.
            foreach (var xml in Resources.LoadAll<TextAsset>("CustomGestures"))
            {
                try { records.Add(new Record { id = xml.name, gesture = ReadXml(xml.text) }); }
                catch (Exception e) { Debug.LogWarning($"Skipped unreadable gesture {xml.name}: {e.Message}"); }
            }
#endif
            return records;
        }

        static Gesture Read(string path)
        {
            return ReadXml(File.ReadAllText(path));
        }

        static Gesture ReadXml(string text)
        {
            var points = new List<Point>();
            int stroke = -1; string name = "";
            using (var xml = XmlReader.Create(new StringReader(text), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 2000000 }))
            {
                while (xml.Read())
                {
                    if (xml.NodeType != XmlNodeType.Element) continue;
                    if (xml.Name == "Gesture") name = xml.GetAttribute("Name");
                    else if (xml.Name == "Stroke") stroke++;
                    else if (xml.Name == "Point")
                    {
                        if (points.Count >= 4096) throw new InvalidDataException("Recording exceeds point limit");
                        float x = float.Parse(xml.GetAttribute("X"), CultureInfo.InvariantCulture);
                        float y = float.Parse(xml.GetAttribute("Y"), CultureInfo.InvariantCulture);
                        if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y)) throw new InvalidDataException("Invalid point");
                        points.Add(new Point(x,y,stroke));
                    }
                }
            }
            return new Gesture(points.ToArray(), name);
        }

        public static string Save(string name, Point[] points)
        {
#if !UNITY_EDITOR
            throw new NotSupportedException("Record gestures in Unity Play Mode to edit the project's gesture library");
#else
            name = name?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 64) throw new ArgumentException("Enter a gesture name (1-64 characters)");
            if (points == null || points.Length < 4) throw new ArgumentException("Draw a longer shape first");
            // Validate normalization before persisting a template.
            var validated = new Gesture(points, name);
            if (Array.Exists(validated.Points, p => p == null || float.IsNaN(p.X) || float.IsNaN(p.Y) || float.IsInfinity(p.X) || float.IsInfinity(p.Y)))
                throw new ArgumentException("Drawing has invalid points");
            Directory.CreateDirectory(Folder);
            string id = Guid.NewGuid().ToString("N");
            string target = FilePath(id), temp = target + ".tmp";
            try
            {
                using (var xml = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true }))
                {
                    xml.WriteStartElement("Gesture"); xml.WriteAttributeString("Name", name);
                    int stroke = -1;
                    foreach (var p in points)
                    {
                        if (p.StrokeID != stroke)
                        {
                            if (stroke != -1) xml.WriteEndElement();
                            xml.WriteStartElement("Stroke"); stroke = p.StrokeID;
                        }
                        xml.WriteStartElement("Point");
                        xml.WriteAttributeString("X", p.X.ToString("R", CultureInfo.InvariantCulture));
                        xml.WriteAttributeString("Y", p.Y.ToString("R", CultureInfo.InvariantCulture));
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement(); xml.WriteEndElement();
                }
                File.Move(temp, target);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            UnityEditor.AssetDatabase.ImportAsset(AssetFolder + "/" + id + ".xml");
            return id;
#endif
        }

        public static void Delete(string id)
        {
#if UNITY_EDITOR
            FilePath(id); // Validate that this is a direct child of the library.
            if (!UnityEditor.AssetDatabase.DeleteAsset(AssetFolder + "/" + id + ".xml"))
                throw new IOException("Could not delete gesture XML asset");
#else
            throw new NotSupportedException("Delete gestures in Unity Play Mode to edit the project's gesture library");
#endif
        }
    }
}
