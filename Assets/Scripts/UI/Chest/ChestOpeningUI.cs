using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChestOpeningUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Animator chestAnimator;
    [SerializeField] private Button collectButton;
    [SerializeField] private GameObject rewardsDisplay;

    [Header("Chest Click")]
    [SerializeField] private RectTransform chestRect;
    [SerializeField] private float chestShakeDuration = 0.2f;
    [SerializeField] private float chestShakeStrength = 15f;

    [Header("VFX / SFX")]
    [SerializeField] private ParticleSystem openParticles;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip openSound;

    [Header("Timing")]
    [SerializeField] private float openAnimationDuration = 1.1f;

    private ChestRewardsSpawner rewardsSpawner;
    private ChestOpener chestOpener;
    private Image backgroundImage;

    private bool isOpened = false;
    private bool isOpening = false;

    private void Awake()
    {
        chestOpener = GetComponent<ChestOpener>();
        rewardsSpawner = GetComponent<ChestRewardsSpawner>();
        backgroundImage = GetComponent<Image>();

        if (rewardsDisplay != null)
            rewardsDisplay.SetActive(false);

        if (collectButton != null)
        {
            collectButton.gameObject.SetActive(false);
            collectButton.interactable = false;

            // Make sure Collect triggers the opener
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(() =>
            {
                if (chestOpener != null)
                    chestOpener.OnCollectPressed();
            });
        }

        if (rewardsSpawner != null)
            rewardsSpawner.OnRewardsFinished += HandleRewardsFinished;
    }

    private void OnDestroy()
    {
        if (rewardsSpawner != null)
            rewardsSpawner.OnRewardsFinished -= HandleRewardsFinished;
    }

    public void Show(ChestDefinition chest)
    {
        if (chest == null)
        {
            Debug.LogWarning("ChestOpeningUI.Show: chest is null");
            return;
        }

        isOpened = false;
        isOpening = false;

        gameObject.SetActive(true);

        if (chestOpener != null)
            chestOpener.SetChest(chest);

        if (titleText != null)
            titleText.text = chest.displayName;

        if (rewardsDisplay != null)
            rewardsDisplay.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.sprite = chest.backgroundImage;

        if (chestAnimator != null && chest.animatorController != null)
            chestAnimator.runtimeAnimatorController = chest.animatorController;

        if (collectButton != null)
        {
            collectButton.gameObject.SetActive(false);
            collectButton.interactable = false;
        }
    }

    public void OnChestClicked()
    {
        if (isOpened || isOpening) return;

        if (chestRect != null)
            StartCoroutine(ChestClickShakeThenOpen());
        else
            StartOpen();

        // Missions example (optional)
        // MissionsManager.Instance.ReportAction(MissionAction.OpenChests, 1);
    }

    private IEnumerator ChestClickShakeThenOpen()
    {
        isOpening = true;

        Vector2 originalPos = chestRect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < chestShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / chestShakeDuration;

            float strength = chestShakeStrength * (1f - t);
            Vector2 offset = Random.insideUnitCircle * strength;

            chestRect.anchoredPosition = originalPos + offset;
            yield return null;
        }

        chestRect.anchoredPosition = originalPos;
        StartOpen();
    }

    private void StartOpen()
    {
        if (isOpened) return;

        isOpened = true;

        if (chestAnimator != null)
            chestAnimator.SetTrigger("Open");

        if (openParticles != null)
            openParticles.Play();

        if (sfxSource != null && openSound != null)
            sfxSource.PlayOneShot(openSound);

        StartCoroutine(OpenSequence());
    }

    private IEnumerator OpenSequence()
    {
        yield return new WaitForSeconds(openAnimationDuration);

        if (chestOpener != null)
            chestOpener.OpenChest();
        else
            Debug.LogWarning("ChestOpeningUI.OpenSequence: chestOpener is null.");

        if (rewardsDisplay != null)
            rewardsDisplay.SetActive(true);

        // Collect becomes available only when rewards finished animating
        isOpening = false;
    }

    private void HandleRewardsFinished()
    {
        if (collectButton == null) return;

        var text = collectButton.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = "Collect";

        collectButton.gameObject.SetActive(true);
        collectButton.interactable = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isOpened || isOpening) return;
        OnChestClicked();
    }
}
