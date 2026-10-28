using System;
using System.Collections.Generic;
using PDollarGestureRecognizer;
using UnityEngine;
using UnityEngine.Events;

namespace ForearmLab
{
    // World-space ink, projected onto the view plane captured at the first stroke.
    // As in the original project, recognition is 2D $P, not volumetric 3D recognition.
    public class GestureDrawing : MonoBehaviour
    {
        public Camera view;
        [Range(0, 1)] public float recognitionThreshold = .8f;
        public float minimumPointDistance = .004f;
        public int maximumPoints = 4096;
        public UnityEvent<string> onRecognized = new UnityEvent<string>();
        public string Status { get; private set; } = "Draw X, T, O or M; then Recognize";
        public int PointCount => points.Count;
        public bool IsDrawing => activeLine != null;
        readonly List<Vector3> points = new List<Vector3>();
        readonly List<int> strokeIds = new List<int>();
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<Gesture> templates = new List<Gesture>();
        LineRenderer activeLine;
        Material ink;
        Quaternion projectionRotation;
        Vector3 projectionOrigin;
        int stroke;

        void Awake()
        {
            ReloadTemplates();
            ink = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            ink.SetColor("_BaseColor", new Color(.2f, 1f, .85f));
        }

        public void ReloadTemplates()
        {
            templates.Clear();
            foreach (var record in GestureRecordingStore.List()) templates.Add(record.gesture);
        }

        public void BeginStroke(Vector3 position)
        {
            EndStroke();
            if (points.Count >= maximumPoints) return;
            if (points.Count == 0)
            {
                projectionRotation = view.transform.rotation;
                projectionOrigin = position;
            }
            activeLine = new GameObject("Stroke " + stroke).AddComponent<LineRenderer>();
            activeLine.transform.SetParent(transform);
            activeLine.sharedMaterial = ink;
            activeLine.widthMultiplier = .005f;
            activeLine.numCapVertices = 4;
            activeLine.useWorldSpace = true;
            activeLine.positionCount = 0;
            lines.Add(activeLine);
            AddPoint(position);
        }

        public void AddPoint(Vector3 position)
        {
            if (!activeLine || points.Count >= maximumPoints) return;
            if (activeLine.positionCount > 0 && Vector3.Distance(points[points.Count - 1], position) < minimumPointDistance) return;
            points.Add(position);
            strokeIds.Add(stroke);
            activeLine.positionCount++;
            activeLine.SetPosition(activeLine.positionCount - 1, position);
        }

        public void EndStroke()
        {
            if (!activeLine) return;
            // A stationary pinch must not become a zero-length $P stroke.
            if (activeLine.positionCount < 2)
            {
                if (activeLine.positionCount == 1) { points.RemoveAt(points.Count - 1); strokeIds.RemoveAt(strokeIds.Count - 1); }
                lines.Remove(activeLine);
                Destroy(activeLine.gameObject);
            }
            activeLine = null;
            stroke++;
        }

        public void Recognize()
        {
            EndStroke();
            if (!TryGetRecordingPoints(out var candidate, out var error)) { Status = error; return; }
            if (templates.Count == 0) { Status = "No recognition templates available"; return; }
            var result = PointCloudRecognizer.Classify(new Gesture(candidate), templates.ToArray());
            bool accepted = result.Score >= recognitionThreshold;
            Status = $"{(accepted ? "Recognized" : "No match; closest")}: {result.GestureClass} ({result.Score:F2})";
            Debug.Log(Status);
            if (accepted) onRecognized.Invoke(result.GestureClass);
        }

        public bool TryGetRecordingPoints(out Point[] candidate, out string error)
        {
            candidate = null; error = "";
            if (points.Count < 4) { error = "Draw a longer shape first"; return false; }
            candidate = new Point[points.Count];
            var inverse = Quaternion.Inverse(projectionRotation);
            var bounds = new Bounds();
            for (int i = 0; i < points.Count; i++)
            {
                var p = inverse * (points[i] - projectionOrigin);
                bounds.Encapsulate(new Vector3(p.x, p.y, 0));
                candidate[i] = new Point(p.x, p.y, strokeIds[i]);
            }
            if (Mathf.Max(bounds.size.x, bounds.size.y) < .01f) { error = "Shape is too small in the drawing plane"; return false; }
            return true;
        }

        public void Clear()
        {
            foreach (var line in lines) if (line) Destroy(line.gameObject);
            lines.Clear(); points.Clear(); strokeIds.Clear(); activeLine = null; stroke = 0;
            Status = "Cleared; draw X, T, O or M";
        }

        void OnDestroy() { if (ink) Destroy(ink); }
    }
}
