using UnityEngine;
using UnityEngine.UI;

public class GamePreference : MonoBehaviour
{
    public Image gameTintOverlay;

    public Color tintColor = new Color(1f, 0.75f, 0.27f, 1f);

    [Range(0f, 0.5f)]
    public float maxTintStrength = 0.3f;

    public void OnSlider(float value)
    {
        if (gameTintOverlay == null)
            return;

        Color color = tintColor;
        color.a = value * maxTintStrength;

        gameTintOverlay.color = color;
    }
}