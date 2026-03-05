using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class HomeSettingsModal : MonoBehaviour
{
    public static HomeSettingsModal Instance { get; private set; }

    private Canvas rootCanvas;
    private GameObject modalRoot;
    private Toggle soundToggle;
    private Toggle musicToggle;
    private Toggle vibrationToggle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;

        var go = new GameObject("HomeSettingsModal");
        Instance = go.AddComponent<HomeSettingsModal>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "HomeScreen")
            WireSettingsButton();
    }

    private void WireSettingsButton()
    {
        var go = GameObject.Find("SettingsButton");
        if (go == null) return;

        var btn = go.GetComponent<Button>();
        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            SoundManager.Instance?.PlayButtonClick();
            Open();
        });
    }

    public void Open()
    {
        if (modalRoot == null)
            BuildUI();

        SyncToggles();
        modalRoot.SetActive(true);
    }

    public void Close()
    {
        if (modalRoot != null)
            modalRoot.SetActive(false);
    }

    private void SyncToggles()
    {
        if (SoundManager.Instance == null) return;
        soundToggle.SetIsOnWithoutNotify(SoundManager.Instance.SoundEnabled);
        musicToggle.SetIsOnWithoutNotify(SoundManager.Instance.MusicEnabled);
        vibrationToggle.SetIsOnWithoutNotify(SoundManager.Instance.VibrationEnabled);
    }

    // ============================
    // BUILD UI
    // ============================

    private void BuildUI()
    {
        // Find the active canvas
        rootCanvas = FindFirstObjectByType<Canvas>();
        if (rootCanvas == null) return;

        // Root overlay (full screen, on top)
        modalRoot = CreateObject("SettingsModalRoot", rootCanvas.transform);
        var rootRt = AddFullStretchRect(modalRoot);

        // Ensure it renders on top
        var canvas = modalRoot.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        modalRoot.AddComponent<GraphicRaycaster>();

        // Dark overlay background (blocks clicks, tap to close)
        var overlayGo = CreateObject("Overlay", modalRoot.transform);
        AddFullStretchRect(overlayGo);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.6f);
        var overlayBtn = overlayGo.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(Close);

        // Panel
        var panel = CreateObject("Panel", modalRoot.transform);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.sizeDelta = new Vector2(700f, 750f);

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.14f, 0.14f, 0.22f, 0.97f);

        // Title
        var title = CreateText("Settings", panel.transform, 52f, FontStyles.Bold);
        var titleRt = title.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(600f, 70f);

        // Divider line
        var divider = CreateObject("Divider", panel.transform);
        var divRt = divider.AddComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0.1f, 1f);
        divRt.anchorMax = new Vector2(0.9f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -120f);
        divRt.sizeDelta = new Vector2(0f, 3f);
        var divImg = divider.AddComponent<Image>();
        divImg.color = new Color(1f, 1f, 1f, 0.2f);

        // Toggle rows
        float startY = -160f;
        float rowSpacing = 140f;

        soundToggle = CreateToggleRow("Sound Effects", panel.transform, startY,
            val => SoundManager.Instance?.SetSoundEnabled(val));

        musicToggle = CreateToggleRow("Music", panel.transform, startY - rowSpacing,
            val => SoundManager.Instance?.SetMusicEnabled(val));

        vibrationToggle = CreateToggleRow("Vibration", panel.transform, startY - rowSpacing * 2,
            val => SoundManager.Instance?.SetVibrationEnabled(val));

        // Close button
        CreateButton("Close", panel.transform, new Vector2(0f, -600f),
            new Vector2(300f, 85f), new Color(0.85f, 0.25f, 0.25f, 1f), Close);

        modalRoot.SetActive(false);
    }

    // ============================
    // UI FACTORY HELPERS
    // ============================

    private Toggle CreateToggleRow(string label, Transform parent, float yPos,
        UnityEngine.Events.UnityAction<bool> onChanged)
    {
        var row = CreateObject(label + "Row", parent);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 1f);
        rowRt.anchorMax = new Vector2(0.5f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, yPos);
        rowRt.sizeDelta = new Vector2(560f, 100f);

        // Label
        var txt = CreateText(label, row.transform, 38f, FontStyles.Normal);
        var txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = new Vector2(0f, 0f);
        txtRt.anchorMax = new Vector2(0.65f, 1f);
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        txt.alignment = TextAlignmentOptions.MidlineLeft;

        // Toggle track
        var toggleGo = CreateObject("Toggle", row.transform);
        var toggleRt = toggleGo.AddComponent<RectTransform>();
        toggleRt.anchorMin = new Vector2(1f, 0.5f);
        toggleRt.anchorMax = new Vector2(1f, 0.5f);
        toggleRt.pivot = new Vector2(1f, 0.5f);
        toggleRt.anchoredPosition = Vector2.zero;
        toggleRt.sizeDelta = new Vector2(120f, 60f);

        var trackImg = toggleGo.AddComponent<Image>();
        trackImg.color = new Color(0.4f, 0.4f, 0.4f, 1f);

        // Checkmark fill
        var checkGo = CreateObject("Checkmark", toggleGo.transform);
        var checkRt = checkGo.AddComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0f, 0f);
        checkRt.anchorMax = new Vector2(1f, 1f);
        checkRt.offsetMin = new Vector2(6f, 6f);
        checkRt.offsetMax = new Vector2(-6f, -6f);
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.3f, 0.85f, 0.4f, 1f);

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = trackImg;
        toggle.graphic = checkImg;
        toggle.isOn = true;
        toggle.onValueChanged.AddListener(onChanged);

        return toggle;
    }

    private void CreateButton(string label, Transform parent, Vector2 pos, Vector2 size,
        Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var btnGo = CreateObject(label + "Button", parent);
        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = btnGo.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(onClick);

        var txt = CreateText(label, btnGo.transform, 36f, FontStyles.Bold);
        var txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
    }

    private TMP_Text CreateText(string content, Transform parent, float fontSize, FontStyles style)
    {
        var go = CreateObject(content + "Text", parent);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private GameObject CreateObject(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private RectTransform AddFullStretchRect(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }
}
