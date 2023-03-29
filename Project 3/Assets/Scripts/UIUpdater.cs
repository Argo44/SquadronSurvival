using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIUpdater : MonoBehaviour
{
    [SerializeField]
    private GameObject gameManager;
    private Manager manager;

    [SerializeField]
    private Text gameClockLabel;

    [SerializeField]
    private Text zombieAlertLabel;
    private float zAlertTime = 0;

    [SerializeField]
    private Image pHealthBar;
    private HealthBar pHP;

    [SerializeField]
    private Text ammoLabel;

    [SerializeField]
    private List<Image> humanHealthBars;
    private List<HealthBar> humanHPs;

    [SerializeField]
    private Image fadeScreen;
    private float screenFadeTimer = 0;
    private Text endGameLabel;
    private Text returnText;

    [SerializeField]
    private GameObject zLabelParent;
    private List<Image> zHealthLabels;
    private List<Zombie> zOnScreen;

    [SerializeField]
    private GameObject minimap;

    // Start is called before the first frame update
    void Start()
    {
        manager = gameManager.GetComponent<Manager>();
        pHP = pHealthBar.GetComponent<HealthBar>();
        returnText = fadeScreen.GetComponentInChildren<Text>(true);
        
        // Load squad health bars
        humanHPs = new List<HealthBar>();
        foreach (Image hpBar in humanHealthBars)
        {
            humanHPs.Add(hpBar.GetComponent<HealthBar>());
        }

        // Create pool of zombie health labels
        zHealthLabels = new List<Image>();
        for (int i = 0; i < 15; i++)
        {
            zHealthLabels.Add(Instantiate(fadeScreen, zLabelParent.transform));
            zHealthLabels[zHealthLabels.Count - 1].color = Color.red;
            zHealthLabels[zHealthLabels.Count - 1].rectTransform.sizeDelta = new Vector2(40, 8);
            zHealthLabels[zHealthLabels.Count - 1].rectTransform.position = new Vector3(-1000, -1000, 0);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Skip if game end screen is visible
        if (screenFadeTimer < 0)
        {
            // Show return message
            returnText.gameObject.SetActive(true);
            return;
        }

        // If game is over, fade out
        if (manager.IsGameOver && screenFadeTimer == 0)
        {
            screenFadeTimer = 2.5f;
            endGameLabel = Instantiate(ammoLabel, transform);
            endGameLabel.alignment = TextAnchor.MiddleCenter;
            endGameLabel.fontSize = 50;
            endGameLabel.rectTransform.position = transform.position;
            endGameLabel.rectTransform.sizeDelta = new Vector2(800, 300);
            endGameLabel.color = new Color(255, 255, 255, 0);

            // Set text based on condition
            if (manager.GameState > 0)
                endGameLabel.text = "You Win";
            else
                endGameLabel.text = "Game Over";

            // Remove all zombie health bars from screen
            foreach (Image zBar in zHealthLabels)
            {
                zBar.rectTransform.position = new Vector3(-1000, -1000, 0);
            }
        }
        if (screenFadeTimer > 0)
        {
            screenFadeTimer -= Time.unscaledDeltaTime;
            fadeScreen.color = new Color(0, 0, 0, (2.5f - screenFadeTimer) / 2.5f);
            endGameLabel.color = new Color(255, 255, 255, (2.5f - screenFadeTimer) / 2.5f);
            if (screenFadeTimer == 0) screenFadeTimer--;
        }

        // Update clock
        int seconds = (int)Mathf.Floor(manager.GameTime);
        gameClockLabel.text = seconds/60 + ":" + (seconds % 60 < 10 ? "0" : "") + seconds % 60;

        // Update player stats
        pHP.SetHealth(manager.Player.Health);
        ammoLabel.text = " Ammo: " + manager.Player.Ammo;
        if (manager.Player.Ammo <= 3)
            ammoLabel.color = Color.red;
        else if (manager.Player.Ammo <= 10)
            ammoLabel.color = Color.yellow;
        else
            ammoLabel.color = Color.white;

        // Update non-player human hp
        int j = 0;
        for (int i = 0; i < humanHPs.Count; i++)
        {
            if (manager.Humans[i] is Player) j++;
            humanHPs[i].SetHealth(manager.Humans[j].Health);
            j++;
        }

        // Place HP indicators above zombies on screen
        zOnScreen = manager.ZombiesOnScreen;
        for (int i = 0; i < zOnScreen.Count; i++)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(zOnScreen[i].Position);
            if (i >= zHealthLabels.Count)
            {
                // Add new label if necessary
                zHealthLabels.Add(Instantiate(fadeScreen, zLabelParent.transform));
            zHealthLabels[zHealthLabels.Count - 1].rectTransform.sizeDelta = new Vector2(40, 8);
                zHealthLabels[zHealthLabels.Count - 1].color = Color.yellow;
            }
            else
            {
                // Place label above zombie
                screenPoint.y += 30;
                zHealthLabels[i].rectTransform.position = screenPoint;
                zHealthLabels[i].rectTransform.sizeDelta = new Vector2(zOnScreen[i].Health, 3);
            }
        }

        // Move remaining labels off screen
        if (zHealthLabels.Count > zOnScreen.Count)
        {
            for (int i = zOnScreen.Count; i < zHealthLabels.Count; i++)
                zHealthLabels[i].rectTransform.position = new Vector3(-1000, -1000, 0);
        }

        // Show alert if zombies were just strengthened
        if (manager.ZombieStrengthened)
            zAlertTime = 3f;
        if (zAlertTime > 0)
        {
            // Fade alert in and out
            zAlertTime -= Time.deltaTime;
            zombieAlertLabel.color = new Color(
                zombieAlertLabel.color.r,
                zombieAlertLabel.color.g,
                zombieAlertLabel.color.b,
                (1.5f - Mathf.Abs(zAlertTime - 1.5f)) / 1.5f);
        }
    }

    public void OnMapToggle()
    {
        minimap.SetActive(!minimap.activeSelf);
    }
}
