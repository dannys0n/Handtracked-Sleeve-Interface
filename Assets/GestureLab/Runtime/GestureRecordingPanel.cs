using System;
using TMPro;
using UnityEngine;

namespace ForearmLab
{
    public class GestureRecordingPanel : MonoBehaviour
    {
        public GestureDrawing drawing;
        public TMP_InputField nameField;
        public TMP_Text message;
        public TMP_Text drawingStatus;
        public TMP_Text count;
        public RectTransform content;
        public GameObject rowPrefab;
        public UnityEngine.UI.Button saveButton;
        public UnityEngine.UI.Button clearButton;
        public UnityEngine.UI.Button recognizeButton;

        void Start()
        {
            saveButton.onClick.AddListener(Save);
            clearButton.onClick.AddListener(drawing.Clear);
            recognizeButton.onClick.AddListener(drawing.Recognize);
            Refresh();
        }

        void Update()
        {
            drawingStatus.text = $"{drawing.PointCount} points  |  {(drawing.IsDrawing ? "Drawing" : "Ready")}\n{drawing.Status}";
        }

        public void Save()
        {
            drawing.EndStroke();
            if (!drawing.TryGetRecordingPoints(out var points, out var error)) { message.text = error; return; }
            try
            {
                GestureRecordingStore.Save(nameField.text, points);
                message.text = "Saved " + nameField.text.Trim() + ". Clear to draw another sample.";
                drawing.ReloadTemplates(); Refresh();
            }
            catch (Exception e) { message.text = "Could not save: " + e.Message; }
        }

        public void Refresh()
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var records = GestureRecordingStore.List();
            count.text = $"Recorded samples ({records.Count})";
            foreach (var record in records)
            {
                var row = Instantiate(rowPrefab, content);
                row.SetActive(true);
                row.GetComponentInChildren<TMP_Text>().text = record.gesture.Name + "\n" + record.createdUtc.ToLocalTime().ToString("g");
                row.GetComponentInChildren<UnityEngine.UI.Button>().onClick.AddListener(() => Delete(record.id));
            }
        }

        public void Delete(string id)
        {
            try { GestureRecordingStore.Delete(id); message.text = "Recording deleted"; drawing.ReloadTemplates(); Refresh(); }
            catch (Exception e) { message.text = "Could not delete: " + e.Message; }
        }
    }
}
