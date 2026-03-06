using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-time editor tool to create Bestiary prefabs and assets.
/// Run from menu: Tools > Bestiary > Setup All
/// </summary>
public static class BestiarySetupTool
{
    private const string PrefabFolder = "Assets/Prefabs/UI/Bestiary";
    private const string AssetFolder = "Assets/ScriptableObjects/Enemies";

    [MenuItem("Tools/Bestiary/Setup All")]
    public static void SetupAll()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(AssetFolder);

        CreateEnemyDatabase();
        CreateEnemyCardPrefab();
        CreateChallengeRowPrefab();
        CreateEnemyDetailsPopupPrefab();
        CreateEnemyBestiaryScreenPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Bestiary setup complete! Check:\n" +
                  $"  Prefabs: {PrefabFolder}/\n" +
                  $"  Assets:  {AssetFolder}/\n" +
                  "Next steps:\n" +
                  "  1. Assign enemy UnitDefinitions to the EnemyDatabase asset\n" +
                  "  2. Drag EnemyBestiaryScreen prefab into your HomeScreen scene\n" +
                  "  3. Add a button and wire OnClick -> EnemyBestiaryScreen.OpenFromButton()");
    }

    // ============================================================
    // ENEMY DATABASE ASSET
    // ============================================================
    [MenuItem("Tools/Bestiary/Create Enemy Database Asset")]
    public static void CreateEnemyDatabase()
    {
        string path = $"{AssetFolder}/EnemyDatabase.asset";
        if (AssetDatabase.LoadAssetAtPath<EnemyDatabase>(path) != null)
        {
            Debug.Log("EnemyDatabase already exists at " + path);
            return;
        }

        var db = ScriptableObject.CreateInstance<EnemyDatabase>();

        // Auto-find all enemy UnitDefinitions
        var guids = AssetDatabase.FindAssets("t:UnitDefinition");
        var enemies = new System.Collections.Generic.List<UnitDefinition>();
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(assetPath);
            if (def != null && def.unitTeam == Team.EnemyTeam)
                enemies.Add(def);
        }
        db.allEnemies = enemies.ToArray();

        AssetDatabase.CreateAsset(db, path);
        Debug.Log($"Created EnemyDatabase at {path} with {enemies.Count} enemies");
    }

    // ============================================================
    // ENEMY CARD PREFAB
    // ============================================================
    [MenuItem("Tools/Bestiary/Create Enemy Card Prefab")]
    public static void CreateEnemyCardPrefab()
    {
        string path = $"{PrefabFolder}/EnemyCard.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("EnemyCard prefab already exists at " + path);
            return;
        }

        // Root
        var root = CreateUIObject("EnemyCard", 200, 250);
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        var cardView = root.AddComponent<EnemyCardView>();

        // Icon
        var iconGo = CreateUIChild(root, "Icon", stretch: false);
        var iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.1f, 0.35f);
        iconRT.anchorMax = new Vector2(0.9f, 0.95f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.preserveAspect = true;
        var iconAnimator = iconGo.AddComponent<Animator>();

        // Name
        var nameGo = CreateUIChild(root, "Name", stretch: false);
        var nameRT = nameGo.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.15f);
        nameRT.anchorMax = new Vector2(1f, 0.35f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "Enemy Name";
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 18;

        // Kill Count
        var killGo = CreateUIChild(root, "KillCount", stretch: false);
        var killRT = killGo.GetComponent<RectTransform>();
        killRT.anchorMin = new Vector2(0f, 0f);
        killRT.anchorMax = new Vector2(1f, 0.15f);
        killRT.offsetMin = Vector2.zero;
        killRT.offsetMax = Vector2.zero;
        var killText = killGo.AddComponent<TextMeshProUGUI>();
        killText.text = "0";
        killText.alignment = TextAlignmentOptions.Center;
        killText.fontSize = 14;
        killText.color = new Color(0.7f, 0.7f, 0.7f);

        // Boss Icon
        var bossGo = CreateUIChild(root, "BossIcon", stretch: false);
        var bossRT = bossGo.GetComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0.75f, 0.85f);
        bossRT.anchorMax = new Vector2(0.95f, 0.98f);
        bossRT.offsetMin = Vector2.zero;
        bossRT.offsetMax = Vector2.zero;
        var bossImage = bossGo.AddComponent<Image>();
        bossImage.color = Color.red;
        bossGo.SetActive(false);

        // Undiscovered Overlay
        var overlayGo = CreateUIChild(root, "UndiscoveredOverlay", stretch: true);
        var overlayImage = overlayGo.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.7f);
        var questionGo = CreateUIChild(overlayGo, "QuestionMark", stretch: false);
        var qRT = questionGo.GetComponent<RectTransform>();
        qRT.anchorMin = new Vector2(0.25f, 0.3f);
        qRT.anchorMax = new Vector2(0.75f, 0.7f);
        qRT.offsetMin = Vector2.zero;
        qRT.offsetMax = Vector2.zero;
        var questionText = questionGo.AddComponent<TextMeshProUGUI>();
        questionText.text = "?";
        questionText.alignment = TextAlignmentOptions.Center;
        questionText.fontSize = 48;

        // Wire serialized fields
        var so = new SerializedObject(cardView);
        so.FindProperty("iconImage").objectReferenceValue = iconImage;
        so.FindProperty("iconAnimator").objectReferenceValue = iconAnimator;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("killCountText").objectReferenceValue = killText;
        so.FindProperty("bossIcon").objectReferenceValue = bossGo;
        so.FindProperty("undiscoveredOverlay").objectReferenceValue = overlayGo;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created EnemyCard prefab at " + path);
    }

    // ============================================================
    // CHALLENGE ROW PREFAB
    // ============================================================
    [MenuItem("Tools/Bestiary/Create Challenge Row Prefab")]
    public static void CreateChallengeRowPrefab()
    {
        string path = $"{PrefabFolder}/EnemyChallengeRow.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("EnemyChallengeRow prefab already exists at " + path);
            return;
        }

        var root = CreateUIObject("EnemyChallengeRow", 0, 70);
        var rootRT = root.GetComponent<RectTransform>();
        // Stretch horizontal
        rootRT.anchorMin = new Vector2(0, 0);
        rootRT.anchorMax = new Vector2(1, 0);
        rootRT.pivot = new Vector2(0.5f, 0);
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
        var rowView = root.AddComponent<EnemyChallengeRowView>();
        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        // Description
        var descGo = CreateUIChild(root, "Description", stretch: false);
        var descLE = descGo.AddComponent<LayoutElement>();
        descLE.preferredWidth = 200;
        descLE.flexibleWidth = 1;
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = "Kill 10";
        descText.fontSize = 16;
        descText.alignment = TextAlignmentOptions.MidlineLeft;

        // Progress bar container
        var barContainer = CreateUIChild(root, "ProgressBarContainer", stretch: false);
        var barLE = barContainer.AddComponent<LayoutElement>();
        barLE.preferredWidth = 120;
        barLE.preferredHeight = 20;

        // Progress bar (Slider)
        var sliderGo = CreateUIChild(barContainer, "ProgressBar", stretch: true);
        var slider = sliderGo.AddComponent<Slider>();
        slider.interactable = false;
        slider.maxValue = 10;
        slider.value = 3;

        // Slider background
        var bgGo = CreateUIChild(sliderGo, "Background", stretch: true);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f);

        // Slider fill area
        var fillAreaGo = CreateUIChild(sliderGo, "Fill Area", stretch: true);
        var fillGo = CreateUIChild(fillAreaGo, "Fill", stretch: true);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.8f, 0.2f);
        slider.fillRect = fillGo.GetComponent<RectTransform>();

        // Progress text
        var progGo = CreateUIChild(barContainer, "ProgressText", stretch: true);
        var progText = progGo.AddComponent<TextMeshProUGUI>();
        progText.text = "3/10";
        progText.fontSize = 14;
        progText.alignment = TextAlignmentOptions.Center;

        // Reward text
        var rewardGo = CreateUIChild(root, "Reward", stretch: false);
        var rewardLE = rewardGo.AddComponent<LayoutElement>();
        rewardLE.preferredWidth = 60;
        var rewardText = rewardGo.AddComponent<TextMeshProUGUI>();
        rewardText.text = "100";
        rewardText.fontSize = 16;
        rewardText.alignment = TextAlignmentOptions.Center;
        rewardText.color = Color.yellow;

        // Claim button
        var claimGo = CreateUIChild(root, "ClaimButton", stretch: false);
        var claimLE = claimGo.AddComponent<LayoutElement>();
        claimLE.preferredWidth = 70;
        claimLE.preferredHeight = 40;
        var claimImg = claimGo.AddComponent<Image>();
        claimImg.color = new Color(0.2f, 0.7f, 0.2f);
        var claimBtn = claimGo.AddComponent<Button>();
        var claimTextGo = CreateUIChild(claimGo, "Text", stretch: true);
        var claimText = claimTextGo.AddComponent<TextMeshProUGUI>();
        claimText.text = "Claim";
        claimText.fontSize = 14;
        claimText.alignment = TextAlignmentOptions.Center;
        claimGo.SetActive(false);

        // Completed checkmark
        var checkGo = CreateUIChild(root, "Checkmark", stretch: false);
        var checkLE = checkGo.AddComponent<LayoutElement>();
        checkLE.preferredWidth = 30;
        var checkText = checkGo.AddComponent<TextMeshProUGUI>();
        checkText.text = "\u2713";
        checkText.fontSize = 24;
        checkText.color = Color.green;
        checkText.alignment = TextAlignmentOptions.Center;
        checkGo.SetActive(false);

        // Wire serialized fields
        var so = new SerializedObject(rowView);
        so.FindProperty("descriptionText").objectReferenceValue = descText;
        so.FindProperty("progressText").objectReferenceValue = progText;
        so.FindProperty("progressBar").objectReferenceValue = slider;
        so.FindProperty("completedCheckmark").objectReferenceValue = checkGo;
        so.FindProperty("rewardText").objectReferenceValue = rewardText;
        so.FindProperty("claimButton").objectReferenceValue = claimBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created EnemyChallengeRow prefab at " + path);
    }

    // ============================================================
    // ENEMY DETAILS POPUP PREFAB
    // ============================================================
    [MenuItem("Tools/Bestiary/Create Enemy Details Popup Prefab")]
    public static void CreateEnemyDetailsPopupPrefab()
    {
        string path = $"{PrefabFolder}/EnemyDetailsPopup.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("EnemyDetailsPopup prefab already exists at " + path);
            return;
        }

        // Root (full screen overlay)
        var root = CreateUIObject("EnemyDetailsPopup", 0, 0);
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // Overlay background (clickable to close)
        var overlayGo = CreateUIChild(root, "Overlay", stretch: true);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        var overlayCanvasGroup = overlayGo.AddComponent<CanvasGroup>();
        var bgCloseBtn = overlayGo.AddComponent<Button>();
        bgCloseBtn.transition = Selectable.Transition.None;

        // Window panel
        var windowGo = CreateUIChild(root, "Window", stretch: false);
        var windowRT = windowGo.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.05f, 0.05f);
        windowRT.anchorMax = new Vector2(0.95f, 0.95f);
        windowRT.offsetMin = Vector2.zero;
        windowRT.offsetMax = Vector2.zero;
        var windowImg = windowGo.AddComponent<Image>();
        windowImg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

        // Close button
        var closeGo = CreateUIChild(windowGo, "CloseButton", stretch: false);
        var closeRT = closeGo.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.9f, 0.93f);
        closeRT.anchorMax = new Vector2(0.98f, 0.99f);
        closeRT.offsetMin = Vector2.zero;
        closeRT.offsetMax = Vector2.zero;
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeTextGo = CreateUIChild(closeGo, "X", stretch: true);
        var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.fontSize = 20;

        // Icon
        var iconGo = CreateUIChild(windowGo, "Icon", stretch: false);
        var iconRT = iconGo.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.05f, 0.65f);
        iconRT.anchorMax = new Vector2(0.35f, 0.92f);
        iconRT.offsetMin = Vector2.zero;
        iconRT.offsetMax = Vector2.zero;
        var iconImage = iconGo.AddComponent<Image>();
        iconImage.preserveAspect = true;
        var iconAnimator = iconGo.AddComponent<Animator>();

        // Name
        var nameGo = CreateUIChild(windowGo, "Name", stretch: false);
        var nameRT = nameGo.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0.38f, 0.85f);
        nameRT.anchorMax = new Vector2(0.88f, 0.93f);
        nameRT.offsetMin = Vector2.zero;
        nameRT.offsetMax = Vector2.zero;
        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        nameText.text = "Enemy Name";
        nameText.fontSize = 26;
        nameText.fontStyle = FontStyles.Bold;

        // Description
        var descGo = CreateUIChild(windowGo, "Description", stretch: false);
        var descRT = descGo.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0.38f, 0.78f);
        descRT.anchorMax = new Vector2(0.88f, 0.85f);
        descRT.offsetMin = Vector2.zero;
        descRT.offsetMax = Vector2.zero;
        var descText = descGo.AddComponent<TextMeshProUGUI>();
        descText.text = "Description";
        descText.fontSize = 14;
        descText.color = new Color(0.7f, 0.7f, 0.7f);

        // Boss tag
        var bossGo = CreateUIChild(windowGo, "BossTag", stretch: false);
        var bossRT = bossGo.GetComponent<RectTransform>();
        bossRT.anchorMin = new Vector2(0.38f, 0.73f);
        bossRT.anchorMax = new Vector2(0.55f, 0.78f);
        bossRT.offsetMin = Vector2.zero;
        bossRT.offsetMax = Vector2.zero;
        var bossImg = bossGo.AddComponent<Image>();
        bossImg.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        var bossTextGo = CreateUIChild(bossGo, "Text", stretch: true);
        var bossText = bossTextGo.AddComponent<TextMeshProUGUI>();
        bossText.text = "BOSS";
        bossText.alignment = TextAlignmentOptions.Center;
        bossText.fontSize = 14;
        bossGo.SetActive(false);

        // Stats area
        var statsGo = CreateUIChild(windowGo, "Stats", stretch: false);
        var statsRT = statsGo.GetComponent<RectTransform>();
        statsRT.anchorMin = new Vector2(0.05f, 0.45f);
        statsRT.anchorMax = new Vector2(0.95f, 0.65f);
        statsRT.offsetMin = Vector2.zero;
        statsRT.offsetMax = Vector2.zero;
        var statsLayout = statsGo.AddComponent<GridLayoutGroup>();
        statsLayout.cellSize = new Vector2(140, 35);
        statsLayout.spacing = new Vector2(10, 5);
        statsLayout.childAlignment = TextAnchor.UpperLeft;

        TMP_Text hpText = CreateStatRow(statsGo, "HP", "100");
        TMP_Text dmgText = CreateStatRow(statsGo, "Damage", "10");
        TMP_Text atkSpdText = CreateStatRow(statsGo, "Hit Speed", "1.0");
        TMP_Text rangeTextComp = CreateStatRow(statsGo, "Range", "Short");
        TMP_Text speedTextComp = CreateStatRow(statsGo, "Speed", "Normal");
        TMP_Text killCountText = CreateStatRow(statsGo, "Kills", "0");

        // Challenges header
        var headerGo = CreateUIChild(windowGo, "ChallengesHeader", stretch: false);
        var headerRT = headerGo.GetComponent<RectTransform>();
        headerRT.anchorMin = new Vector2(0.05f, 0.38f);
        headerRT.anchorMax = new Vector2(0.95f, 0.44f);
        headerRT.offsetMin = Vector2.zero;
        headerRT.offsetMax = Vector2.zero;
        var headerText = headerGo.AddComponent<TextMeshProUGUI>();
        headerText.text = "Challenges";
        headerText.fontSize = 20;
        headerText.fontStyle = FontStyles.Bold;

        // Challenges scroll area
        var scrollGo = CreateUIChild(windowGo, "ChallengesScroll", stretch: false);
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.03f, 0.03f);
        scrollRT.anchorMax = new Vector2(0.97f, 0.38f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var scrollImg = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0, 0, 0, 0.2f);
        var scrollMask = scrollGo.AddComponent<Mask>();
        scrollMask.showMaskGraphic = true;

        // Scroll content
        var contentGo = CreateUIChild(scrollGo, "Content", stretch: false);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;
        var vertLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        vertLayout.spacing = 8;
        vertLayout.padding = new RectOffset(5, 5, 5, 5);
        vertLayout.childForceExpandWidth = true;
        vertLayout.childForceExpandHeight = false;
        var contentSizeFitter = contentGo.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRT;

        // PopupAnimator
        var popupAnim = root.AddComponent<PopupAnimator>();
        var paSo = new SerializedObject(popupAnim);
        paSo.FindProperty("root").objectReferenceValue = root;
        paSo.FindProperty("windowRect").objectReferenceValue = windowRT;
        paSo.FindProperty("overlayCanvasGroup").objectReferenceValue = overlayCanvasGroup;
        paSo.ApplyModifiedPropertiesWithoutUndo();

        // EnemyDetailsPopupController
        var controller = root.AddComponent<EnemyDetailsPopupController>();
        var cSo = new SerializedObject(controller);
        cSo.FindProperty("root").objectReferenceValue = root;
        cSo.FindProperty("closeButton").objectReferenceValue = closeBtn;
        cSo.FindProperty("backgroundCloseButton").objectReferenceValue = bgCloseBtn;
        cSo.FindProperty("popupAnimator").objectReferenceValue = popupAnim;
        cSo.FindProperty("icon").objectReferenceValue = iconImage;
        cSo.FindProperty("iconAnimator").objectReferenceValue = iconAnimator;
        cSo.FindProperty("nameText").objectReferenceValue = nameText;
        cSo.FindProperty("descriptionText").objectReferenceValue = descText;
        cSo.FindProperty("hpText").objectReferenceValue = hpText;
        cSo.FindProperty("dmgText").objectReferenceValue = dmgText;
        cSo.FindProperty("atkSpeedText").objectReferenceValue = atkSpdText;
        cSo.FindProperty("rangeText").objectReferenceValue = rangeTextComp;
        cSo.FindProperty("speedText").objectReferenceValue = speedTextComp;
        cSo.FindProperty("killCountText").objectReferenceValue = killCountText;
        cSo.FindProperty("bossTag").objectReferenceValue = bossGo;
        cSo.FindProperty("challengesContent").objectReferenceValue = contentGo.transform;
        cSo.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created EnemyDetailsPopup prefab at " + path);
    }

    // ============================================================
    // ENEMY BESTIARY SCREEN PREFAB
    // ============================================================
    [MenuItem("Tools/Bestiary/Create Bestiary Screen Prefab")]
    public static void CreateEnemyBestiaryScreenPrefab()
    {
        string path = $"{PrefabFolder}/EnemyBestiaryScreen.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            Debug.Log("EnemyBestiaryScreen prefab already exists at " + path);
            return;
        }

        // Load dependencies
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/EnemyCard.prefab");
        var challengeRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/EnemyChallengeRow.prefab");
        var detailsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/EnemyDetailsPopup.prefab");
        var enemyDb = AssetDatabase.LoadAssetAtPath<EnemyDatabase>($"{AssetFolder}/EnemyDatabase.asset");

        // Root (full screen)
        var root = CreateUIObject("EnemyBestiaryScreen", 0, 0);
        var rootRT = root.GetComponent<RectTransform>();
        rootRT.anchorMin = Vector2.zero;
        rootRT.anchorMax = Vector2.one;
        rootRT.offsetMin = Vector2.zero;
        rootRT.offsetMax = Vector2.zero;

        // Overlay background
        var overlayGo = CreateUIChild(root, "Overlay", stretch: true);
        var overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.6f);
        var overlayCanvasGroup = overlayGo.AddComponent<CanvasGroup>();

        // Window panel
        var windowGo = CreateUIChild(root, "Window", stretch: false);
        var windowRT = windowGo.GetComponent<RectTransform>();
        windowRT.anchorMin = new Vector2(0.02f, 0.02f);
        windowRT.anchorMax = new Vector2(0.98f, 0.98f);
        windowRT.offsetMin = Vector2.zero;
        windowRT.offsetMax = Vector2.zero;
        var windowImg = windowGo.AddComponent<Image>();
        windowImg.color = new Color(0.1f, 0.1f, 0.15f, 1f);

        // Title
        var titleGo = CreateUIChild(windowGo, "Title", stretch: false);
        var titleRT = titleGo.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.05f, 0.92f);
        titleRT.anchorMax = new Vector2(0.7f, 0.98f);
        titleRT.offsetMin = Vector2.zero;
        titleRT.offsetMax = Vector2.zero;
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "Bestiary";
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyles.Bold;

        // Close button
        var closeGo = CreateUIChild(windowGo, "CloseButton", stretch: false);
        var closeRT = closeGo.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0.88f, 0.92f);
        closeRT.anchorMax = new Vector2(0.97f, 0.98f);
        closeRT.offsetMin = Vector2.zero;
        closeRT.offsetMax = Vector2.zero;
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.2f, 0.2f);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeTextGo = CreateUIChild(closeGo, "X", stretch: true);
        var closeText = closeTextGo.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.fontSize = 22;

        // Grid scroll area
        var scrollGo = CreateUIChild(windowGo, "GridScroll", stretch: false);
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.02f, 0.02f);
        scrollRT.anchorMax = new Vector2(0.98f, 0.91f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var scrollMaskImg = scrollGo.AddComponent<Image>();
        scrollMaskImg.color = new Color(0, 0, 0, 0.01f);
        var scrollMask = scrollGo.AddComponent<Mask>();
        scrollMask.showMaskGraphic = false;

        // Grid content
        var gridGo = CreateUIChild(scrollGo, "GridContent", stretch: false);
        var gridRT = gridGo.GetComponent<RectTransform>();
        gridRT.anchorMin = new Vector2(0, 1);
        gridRT.anchorMax = new Vector2(1, 1);
        gridRT.pivot = new Vector2(0.5f, 1);
        gridRT.offsetMin = Vector2.zero;
        gridRT.offsetMax = Vector2.zero;
        var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(200, 250);
        gridLayout.spacing = new Vector2(15, 15);
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        var gridFitter = gridGo.AddComponent<ContentSizeFitter>();
        gridFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = gridRT;

        // Details popup instance (nested)
        GameObject detailsInstance = null;
        EnemyDetailsPopupController detailsController = null;
        if (detailsPrefab != null)
        {
            detailsInstance = (GameObject)PrefabUtility.InstantiatePrefab(detailsPrefab);
            detailsInstance.transform.SetParent(root.transform, false);
            detailsController = detailsInstance.GetComponent<EnemyDetailsPopupController>();

            // Wire the challenge row prefab
            if (challengeRowPrefab != null && detailsController != null)
            {
                var detailsSo = new SerializedObject(detailsController);
                var rowPrefabComp = challengeRowPrefab.GetComponent<EnemyChallengeRowView>();
                detailsSo.FindProperty("challengeRowPrefab").objectReferenceValue = rowPrefabComp;
                detailsSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // PopupAnimator for the screen itself
        var popupAnim = root.AddComponent<PopupAnimator>();
        var paSo = new SerializedObject(popupAnim);
        paSo.FindProperty("root").objectReferenceValue = root;
        paSo.FindProperty("windowRect").objectReferenceValue = windowRT;
        paSo.FindProperty("overlayCanvasGroup").objectReferenceValue = overlayCanvasGroup;
        paSo.ApplyModifiedPropertiesWithoutUndo();

        // EnemyBestiaryScreen controller
        var screen = root.AddComponent<EnemyBestiaryScreen>();
        var sSo = new SerializedObject(screen);
        sSo.FindProperty("popupAnimator").objectReferenceValue = popupAnim;
        sSo.FindProperty("root").objectReferenceValue = root;
        sSo.FindProperty("gridContent").objectReferenceValue = gridGo.transform;
        if (enemyDb != null)
            sSo.FindProperty("enemyDatabase").objectReferenceValue = enemyDb;
        if (cardPrefab != null)
            sSo.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<EnemyCardView>();
        if (detailsController != null)
            sSo.FindProperty("detailsPopup").objectReferenceValue = detailsController;
        sSo.ApplyModifiedPropertiesWithoutUndo();

        // Wire close button to screen's Close method
        UnityEditor.Events.UnityEventTools.AddPersistentListener(closeBtn.onClick, screen.Close);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log("Created EnemyBestiaryScreen prefab at " + path);
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private static GameObject CreateUIObject(string name, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(width, height);
        return go;
    }

    private static GameObject CreateUIChild(GameObject parent, string name, bool stretch)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent.transform, false);

        if (stretch)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        return go;
    }

    private static TMP_Text CreateStatRow(GameObject parent, string label, string value)
    {
        var rowGo = CreateUIChild(parent, $"Stat_{label}", stretch: false);

        var labelGo = CreateUIChild(rowGo, "Label", stretch: false);
        var labelRT = labelGo.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = new Vector2(0.5f, 1);
        labelRT.offsetMin = Vector2.zero;
        labelRT.offsetMax = Vector2.zero;
        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = $"{label}:";
        labelText.fontSize = 16;
        labelText.color = new Color(0.6f, 0.6f, 0.6f);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        var valueGo = CreateUIChild(rowGo, "Value", stretch: false);
        var valueRT = valueGo.GetComponent<RectTransform>();
        valueRT.anchorMin = new Vector2(0.5f, 0);
        valueRT.anchorMax = Vector2.one;
        valueRT.offsetMin = Vector2.zero;
        valueRT.offsetMax = Vector2.zero;
        var valueText = valueGo.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.fontSize = 16;
        valueText.alignment = TextAlignmentOptions.MidlineLeft;

        return valueText;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
