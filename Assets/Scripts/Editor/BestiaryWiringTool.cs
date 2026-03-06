using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Wires the EnemyBestiaryScreen to existing scene objects.
/// Run from: Tools > Bestiary > Wire Scene Objects
///
/// Prerequisites (already done by user):
///   - "EnemiesCollectionWindow" panel exists in scene
///   - "EnemiesCollection" child with GridLayoutGroup exists
///   - "EnemiesButton" button exists in scene
///   - EnemyCard prefab exists at Assets/Prefabs/UI/EnemyCard.prefab
/// </summary>
public static class BestiaryWiringTool
{
    [MenuItem("Tools/Bestiary/Wire Scene Objects")]
    public static void WireSceneObjects()
    {
        // Find scene objects
        var windowGo = GameObject.Find("EnemiesCollectionWindow");
        if (windowGo == null)
        {
            Debug.LogError("Could not find 'EnemiesCollectionWindow' in scene!");
            return;
        }

        var gridGo = GameObject.Find("EnemiesCollection");
        if (gridGo == null)
        {
            Debug.LogError("Could not find 'EnemiesCollection' in scene!");
            return;
        }

        var buttonGo = GameObject.Find("EnemiesButton");
        if (buttonGo == null)
        {
            Debug.LogError("Could not find 'EnemiesButton' in scene!");
            return;
        }

        // Load prefab & database
        var cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/EnemyCard.prefab");
        if (cardPrefab == null)
        {
            Debug.LogError("Could not find EnemyCard prefab at Assets/Prefabs/UI/EnemyCard.prefab!");
            return;
        }

        // Create or find EnemyDatabase
        string dbPath = "Assets/ScriptableObjects/Enemies/EnemyDatabase.asset";
        var enemyDb = AssetDatabase.LoadAssetAtPath<EnemyDatabase>(dbPath);
        if (enemyDb == null)
        {
            // Create folder if needed
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects/Enemies"))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Enemies");

            enemyDb = ScriptableObject.CreateInstance<EnemyDatabase>();

            // Auto-find all enemy UnitDefinitions
            var guids = AssetDatabase.FindAssets("t:UnitDefinition");
            var enemies = new System.Collections.Generic.List<UnitDefinition>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var def = AssetDatabase.LoadAssetAtPath<UnitDefinition>(path);
                if (def != null && def.unitTeam == Team.EnemyTeam)
                    enemies.Add(def);
            }
            enemyDb.allEnemies = enemies.ToArray();
            AssetDatabase.CreateAsset(enemyDb, dbPath);
            Debug.Log($"Created EnemyDatabase with {enemies.Count} enemies");
        }

        // ---- Clean up old components from duplication ----
        var oldLRP = windowGo.GetComponent<LevelRewardsProgressController>();
        if (oldLRP != null) Undo.DestroyObjectImmediate(oldLRP);

        var oldLRW = windowGo.GetComponent<LevelRewardWindowController>();
        if (oldLRW != null) Undo.DestroyObjectImmediate(oldLRW);

        // ---- Setup PopupAnimator on window ----
        var popupAnimator = windowGo.GetComponent<PopupAnimator>();
        if (popupAnimator == null)
            popupAnimator = Undo.AddComponent<PopupAnimator>(windowGo);

        // Find the root Canvas
        var rootCanvas = windowGo.GetComponentInParent<Canvas>();

        // The window's RectTransform is the "windowRect" for PopupAnimator
        var windowRT = windowGo.GetComponent<RectTransform>();

        // Try to find or create an overlay CanvasGroup
        // Check if there's already a child that could be an overlay
        CanvasGroup overlayGroup = null;
        var existingOverlay = windowGo.transform.Find("Overlay");
        if (existingOverlay != null)
        {
            overlayGroup = existingOverlay.GetComponent<CanvasGroup>();
            if (overlayGroup == null)
                overlayGroup = Undo.AddComponent<CanvasGroup>(existingOverlay.gameObject);
        }

        // Wire PopupAnimator via SerializedObject
        var paSO = new SerializedObject(popupAnimator);
        paSO.FindProperty("root").objectReferenceValue = windowGo;
        paSO.FindProperty("windowRect").objectReferenceValue = windowRT;
        if (overlayGroup != null)
            paSO.FindProperty("overlayCanvasGroup").objectReferenceValue = overlayGroup;
        if (rootCanvas != null)
            paSO.FindProperty("rootCanvas").objectReferenceValue = rootCanvas;
        paSO.ApplyModifiedProperties();

        // ---- Setup EnemyBestiaryScreen ----
        var screen = windowGo.GetComponent<EnemyBestiaryScreen>();
        if (screen == null)
            screen = Undo.AddComponent<EnemyBestiaryScreen>(windowGo);

        var cardView = cardPrefab.GetComponent<EnemyCardView>();

        var screenSO = new SerializedObject(screen);
        screenSO.FindProperty("enemyDatabase").objectReferenceValue = enemyDb;
        screenSO.FindProperty("cardPrefab").objectReferenceValue = cardView;
        screenSO.FindProperty("gridContent").objectReferenceValue = gridGo.transform;
        screenSO.FindProperty("popupAnimator").objectReferenceValue = popupAnimator;
        screenSO.FindProperty("root").objectReferenceValue = windowGo;
        screenSO.ApplyModifiedProperties();

        // ---- Wire button ----
        var button = buttonGo.GetComponent<Button>();
        if (button != null)
        {
            // Clear existing onClick listeners
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);

            // Add new listener: EnemyBestiaryScreen.OpenFromButton(buttonRect)
            var buttonRT = buttonGo.GetComponent<RectTransform>();

            // We need a small helper since OpenFromButton takes RectTransform parameter
            // Use a wrapper component on the button
            var opener = buttonGo.GetComponent<BestiaryButtonOpener>();
            if (opener == null)
                opener = Undo.AddComponent<BestiaryButtonOpener>(buttonGo);

            var openerSO = new SerializedObject(opener);
            openerSO.FindProperty("bestiaryScreen").objectReferenceValue = screen;
            openerSO.ApplyModifiedProperties();

            // Wire button onClick to opener.Open()
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(button.onClick, opener.Open);
        }

        // Start with window hidden
        windowGo.SetActive(false);

        Undo.RecordObject(windowGo, "Wire Bestiary");
        EditorUtility.SetDirty(windowGo);

        Debug.Log("Bestiary wiring complete! Objects wired:\n" +
                  "  - EnemiesCollectionWindow: PopupAnimator + EnemyBestiaryScreen\n" +
                  "  - EnemiesCollection: assigned as grid content\n" +
                  "  - EnemiesButton: wired to open bestiary\n" +
                  "  - EnemyDatabase: created with auto-found enemies\n" +
                  "\nDon't forget to save the scene (Ctrl+S)!");
    }
}
