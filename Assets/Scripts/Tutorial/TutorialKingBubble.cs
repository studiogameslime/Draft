using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialKingBubble : MonoBehaviour
{
    private float typewriterSpeed = 30f;
    private Vector2 kingSize = new Vector2(180f, 180f);
    private float bubbleFontSize = 40f;
    private float topOffset = -100f;

    private TMP_Text bubbleText;
    private Coroutine typewriterCo;

    public void Init(Sprite kingSprite)
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1002;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        BuildUI(kingSprite);
        gameObject.SetActive(false);
    }

    private void BuildUI(Sprite kingSprite)
    {
        // Container: horizontal layout anchored to top
        var containerGo = new GameObject("Container", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        containerGo.transform.SetParent(transform, false);
        var containerRect = containerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 1f);
        containerRect.anchorMax = new Vector2(1f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0f, topOffset);
        containerRect.sizeDelta = new Vector2(-40f, 0f);

        var hlg = containerGo.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 15f;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var containerFitter = containerGo.GetComponent<ContentSizeFitter>();
        containerFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // King icon
        var kingGo = new GameObject("KingIcon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        kingGo.transform.SetParent(containerGo.transform, false);
        var kingImg = kingGo.GetComponent<Image>();
        kingImg.sprite = kingSprite;
        kingImg.preserveAspect = true;
        kingImg.raycastTarget = false;
        var kingLe = kingGo.GetComponent<LayoutElement>();
        kingLe.minWidth = kingSize.x;
        kingLe.preferredWidth = kingSize.x;
        kingLe.minHeight = kingSize.y;
        kingLe.preferredHeight = kingSize.y;
        kingLe.flexibleWidth = 0;
        kingLe.flexibleHeight = 0;

        // Bubble background
        var bubbleGo = new GameObject("Bubble", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        bubbleGo.transform.SetParent(containerGo.transform, false);
        var bubbleBg = bubbleGo.GetComponent<Image>();
        bubbleBg.color = new Color(1f, 1f, 1f, 0.95f);
        bubbleBg.raycastTarget = false;
        bubbleBg.type = Image.Type.Sliced;
        var bgSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        if (bgSprite != null) bubbleBg.sprite = bgSprite;

        var bubbleVlg = bubbleGo.GetComponent<VerticalLayoutGroup>();
        bubbleVlg.padding = new RectOffset(25, 25, 20, 20);
        bubbleVlg.childAlignment = TextAnchor.MiddleLeft;
        bubbleVlg.childControlWidth = true;
        bubbleVlg.childControlHeight = true;
        bubbleVlg.childForceExpandWidth = true;
        bubbleVlg.childForceExpandHeight = false;

        var bubbleFitter = bubbleGo.GetComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var bubbleLe = bubbleGo.GetComponent<LayoutElement>();
        bubbleLe.flexibleWidth = 1f;

        // Text with TMP
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(bubbleGo.transform, false);
        bubbleText = textGo.GetComponent<TextMeshProUGUI>();
        bubbleText.fontSize = bubbleFontSize;
        bubbleText.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        bubbleText.enableWordWrapping = true;
        bubbleText.raycastTarget = false;
        bubbleText.maxVisibleCharacters = 0;
    }

    public void Show(string text)
    {
        if (bubbleText == null) return;

        gameObject.SetActive(true);
        bubbleText.text = text;
        bubbleText.maxVisibleCharacters = 0;
        bubbleText.ForceMeshUpdate();

        if (typewriterCo != null) StopCoroutine(typewriterCo);
        typewriterCo = StartCoroutine(TypewriterRoutine());
    }

    public void Hide()
    {
        if (typewriterCo != null)
        {
            StopCoroutine(typewriterCo);
            typewriterCo = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator TypewriterRoutine()
    {
        int total = bubbleText.textInfo.characterCount;
        float delay = 1f / typewriterSpeed;

        for (int i = 1; i <= total; i++)
        {
            bubbleText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(delay);
        }
        typewriterCo = null;
    }

    private void OnDestroy()
    {
        if (typewriterCo != null)
            StopCoroutine(typewriterCo);
    }
}
