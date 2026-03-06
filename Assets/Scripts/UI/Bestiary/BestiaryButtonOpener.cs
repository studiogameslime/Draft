using UnityEngine;

public class BestiaryButtonOpener : MonoBehaviour
{
    [SerializeField] private EnemyBestiaryScreen bestiaryScreen;

    public void Open()
    {
        if (bestiaryScreen != null)
            bestiaryScreen.OpenFromButton(GetComponent<RectTransform>());
    }
}
