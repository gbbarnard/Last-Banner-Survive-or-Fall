using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryManagerTMP : MonoBehaviour
{
    public string jsonResourceName = "Act1";

    public TMP_Text passageText;
    public Transform choicesContainer;
    public Button choiceButtonTemplate;

    public Image backgroundImage;
    public string backgroundFolder = "Backgrounds";

    private int mercy = 0;
    private int fear = 0;
    private int supplies = 0;

    private StoryData data;
    private Dictionary<string, Passage> map;
    private Passage current;

    [Serializable]
    private class StoryData
    {
        public string startPassageId;
        public Passage[] passages;
    }

    [Serializable]
    private class Passage
    {
        public string id;
        public string bg;
        public string text;
        public Choice[] choices;
    }

    [Serializable]
    private class Choice
    {
        public string label;
        public string nextId;
        public int mercyDelta;
        public int fearDelta;
        public int suppliesDelta;
    }

    private void Start()
    {
        Load();
        StartStory();
    }

    private void Load()
    {
        var asset = Resources.Load<TextAsset>(jsonResourceName);
        if (asset == null)
        {
            Debug.LogError($"Missing Resources/{jsonResourceName}.json");
            return;
        }

        data = JsonUtility.FromJson<StoryData>(asset.text);
        if (data == null || data.passages == null || data.passages.Length == 0)
        {
            Debug.LogError("JSON has no passages.");
            return;
        }

        map = new Dictionary<string, Passage>();
        foreach (var p in data.passages)
        {
            if (!string.IsNullOrWhiteSpace(p.id) && !map.ContainsKey(p.id))
                map.Add(p.id, p);
        }
    }

    private void StartStory()
    {
        if (data == null || map == null) return;

        string startId = !string.IsNullOrWhiteSpace(data.startPassageId)
            ? data.startPassageId
            : data.passages[0].id;

        GoTo(startId);
    }

    private void GoTo(string id)
    {
        if (!map.ContainsKey(id))
        {
            Debug.LogError($"Passage not found: {id}");
            return;
        }

        current = map[id];
        Render();
    }

    private void Render()
    {
        SetBackground(current.bg);

        if (passageText != null)
            passageText.text = current.text ?? "";

        ClearChoiceButtons();
        RenderChoices();
    }

    private void SetBackground(string bgKey)
    {
        if (backgroundImage == null) return;
        if (string.IsNullOrWhiteSpace(bgKey)) return;

        var sprite = Resources.Load<Sprite>($"{backgroundFolder}/{bgKey}");
        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.preserveAspect = false;
        }
    }

    private void ClearChoiceButtons()
    {
        if (choicesContainer == null || choiceButtonTemplate == null) return;

        for (int i = choicesContainer.childCount - 1; i >= 0; i--)
        {
            var child = choicesContainer.GetChild(i).gameObject;
            if (child != choiceButtonTemplate.gameObject)
                Destroy(child);
        }
    }

    private void RenderChoices()
    {
        if (choicesContainer == null || choiceButtonTemplate == null) return;

        var choices = current.choices;
        if (choices == null || choices.Length == 0) return;

        foreach (var c in choices)
        {
            var btn = Instantiate(choiceButtonTemplate, choicesContainer);
            btn.gameObject.SetActive(true);

            var t = btn.GetComponentInChildren<TMP_Text>();
            if (t != null) t.text = c.label ?? "Continue";

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                mercy += c.mercyDelta;
                fear += c.fearDelta;
                supplies += c.suppliesDelta;
                if (supplies < 0) supplies = 0;

                if (!string.IsNullOrWhiteSpace(c.nextId))
                    GoTo(c.nextId);
            });
        }
    }
}
