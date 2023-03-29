using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MiniMapUpdate : MonoBehaviour
{
    [SerializeField]
    private GameObject gameManager;
    [SerializeField]
    private Canvas canvas;
    private Manager manager;
    private float mapWidth;
    private bool isActive = true;

    [SerializeField] private Image entityMarkerPrefab;
    [SerializeField] private Image objectMarkerPrefab;
    private List<Image> zMarkers;
    private List<Image> hMarkers;
    private List<Image> cMarkers;
    private Image pMarker;


    // Start is called before the first frame update
    void Start()
    {
        manager = gameManager.GetComponent<Manager>();

        zMarkers = new List<Image>();
        hMarkers = new List<Image>();
        cMarkers = new List<Image>();
        pMarker = Instantiate(entityMarkerPrefab, transform);
        pMarker.color = Color.cyan;

        // Add zombie markers
        do
        {
            zMarkers.Add(Instantiate(entityMarkerPrefab, transform));
            zMarkers[zMarkers.Count - 1].color = Color.red;
        }
        while (zMarkers.Count < manager.Zombies.Count);

        // Add human markers
        do
        {
            hMarkers.Add(Instantiate(entityMarkerPrefab, transform));
            hMarkers[hMarkers.Count - 1].color = Color.yellow;
        }
        while (hMarkers.Count < manager.Humans.Count - 1);

        // Add crate markers
        do
        {
            cMarkers.Add(Instantiate(objectMarkerPrefab, transform));
            cMarkers[cMarkers.Count - 1].color = Color.green;
        }
        while (cMarkers.Count < manager.Crates.Count);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActive) return;
        MatchGameCount();
        mapWidth = gameObject.GetComponent<RectTransform>().sizeDelta.x * canvas.scaleFactor;

        // Place zombie markers at relative positions on map
        for (int i = 0; i < zMarkers.Count; i++)
        {
            Vector3 mapPos = manager.Zombies[i].Position - manager.Bounds.center;
            mapPos.y = mapPos.z;
            mapPos.z = 0;
            mapPos /= manager.Bounds.size.x;

            mapPos = mapPos * mapWidth + transform.position;
            zMarkers[i].rectTransform.position = mapPos;
        }

        // Place human markers at relative positions on map,
        // skipping player and dead humans
        int j = 0;
        for (int i = 0; i < manager.Humans.Count; i++)
        {
            if (!manager.Humans[i].IsAlive || manager.Humans[i] is Player) continue;

            Vector3 mapPos = manager.Humans[i].Position - manager.Bounds.center;
            mapPos.y = mapPos.z;
            mapPos.z = 0;
            mapPos /= manager.Bounds.size.x;

            mapPos = mapPos * mapWidth + transform.position;
            hMarkers[j].rectTransform.position = mapPos;
            j++;
        }

        // Place crate markers at relative positions on map
        for (int i = 0; i < cMarkers.Count; i++)
        {
            Vector3 mapPos = manager.Crates[i].Position - manager.Bounds.center;
            mapPos.y = mapPos.z;
            mapPos.z = 0;
            mapPos /= manager.Bounds.size.x;

            mapPos = mapPos * mapWidth + transform.position;
            cMarkers[i].rectTransform.position = mapPos;
        }

        // Place player marker
        Vector3 playerPos = manager.Player.Position - manager.Bounds.center;
        playerPos.y = playerPos.z;
        playerPos.z = 0;
        playerPos /= manager.Bounds.size.x;

        playerPos = playerPos * mapWidth + transform.position;
        pMarker.rectTransform.position = playerPos;
    }

    /// <summary>
    /// Matches map marker counts to active entity counts
    /// </summary>
    private void MatchGameCount()
    {
        // Match zombie marker count to real zombie count
        if (zMarkers.Count > manager.Zombies.Count)
        {
            do {
                Destroy(zMarkers[0].gameObject);
                zMarkers.RemoveAt(0);
            } 
            while (zMarkers.Count > manager.Zombies.Count);
        }
        else if (zMarkers.Count < manager.Zombies.Count)
        {
            do
            {
                zMarkers.Add(Instantiate(entityMarkerPrefab, transform));
                zMarkers[zMarkers.Count - 1].color = Color.red;
            }
            while (zMarkers.Count < manager.Zombies.Count);
        }

        // Match human marker count to real human count minus player
        if (hMarkers.Count > manager.LiveHumanCount - 1)
        {
            do
            {
                Destroy(hMarkers[0].gameObject);
                hMarkers.RemoveAt(0);
            }
            while (hMarkers.Count > manager.LiveHumanCount - 1);
        }

        // Match crate marker count to real crate count
        if (cMarkers.Count > manager.Crates.Count)
        {
            do
            {
                Destroy(cMarkers[0].gameObject);
                cMarkers.RemoveAt(0);
            }
            while (cMarkers.Count > manager.Crates.Count);
        }
    }
}
