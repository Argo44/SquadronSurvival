using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField]
    private Image currentHealth;

    [SerializeField]
    private Text hpLabel;

    /// <summary>
    /// Update health bar on HUD
    /// </summary>
    public void SetHealth(int value)
    {
        if (value >= 0)
        {
            currentHealth.rectTransform.sizeDelta =
                new Vector2(value, currentHealth.rectTransform.sizeDelta.y);

            hpLabel.text = value.ToString();
        }
    }
}
