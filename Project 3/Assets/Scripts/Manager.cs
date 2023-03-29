using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class Manager : MonoBehaviour
{
    [SerializeField]
    private Camera mainCam;
    private float camHeight;
    private Vector2 camExtent;

    private float gameTime = 0;
    private float zPowerTimer = 40;
    private bool zombieStrengthened = false;
    private bool isGameOver = false;
    private int gameState = 0;

    [SerializeField]
    private GameObject obstacleContainer;
    private List<Obstacle> obstacles;

    [SerializeField]
    private GameObject crateContainer;
    private List<AmmoCrate> crates;

    [SerializeField] 
    private GameObject zombiePrefab;
    private List<Zombie> zombies;

    [SerializeField]
    private GameObject playerSquad;
    private List<Human> humans;
    private Player player;

    [SerializeField]
    private Terrain gameBounds;
    private Bounds bounds;

    [SerializeField]
    private GameObject bulletPrefab;
    private List<Bullet> bulletSpace;
    private List<Bullet> bulletPool;

    private AudioSource audioSrc;
    [SerializeField] private AudioClip bgmMain;
    [SerializeField] private AudioClip sfxCrateCollected;
    [SerializeField] private AudioClip sfxZombiesStrengthened;
    [SerializeField] private AudioClip sfxGameWin;
    [SerializeField] private AudioClip sfxGameLoss;

    // Properties
    public float GameTime => gameTime;
    public bool IsGameOver => isGameOver;
    public int GameState => gameState;
    public bool ZombieStrengthened => zombieStrengthened;
    public Bounds Bounds => bounds;
    public Player Player => player;
    public List<Human> Humans => humans;
    public int LiveHumanCount
    {
        get
        {
            int liveCount = 0;
            foreach (Human h in humans)
            {
                if (h.IsAlive)
                    liveCount++;
            }

            return liveCount;
        }
    }
    public List<Zombie> Zombies => zombies;
    public List<Zombie> ZombiesOnScreen
    {
        get
        {
            List<Zombie> onScreen = new List<Zombie>();
            foreach (Zombie z in zombies)
            {
                if (z.Position.x < mainCam.transform.position.x - camExtent.x)
                    continue;

                if (z.Position.x > mainCam.transform.position.x + camExtent.x)
                    continue;

                if (z.Position.z < mainCam.transform.position.z - camExtent.y)
                    continue;

                if (z.Position.z > mainCam.transform.position.z + camExtent.y)
                    continue;

                onScreen.Add(z);
            }
            return onScreen;
        }
    }
    public List<Obstacle> Obstacles => obstacles;
    public List<AmmoCrate> Crates => crates;
    public List<Bullet> BulletPool => bulletPool;

    // Start is called before the first frame update
    void Start()
    {
        // Freeze physics until setup is finished
        Time.timeScale = 0;

        // Initialize fields
        zombies = new List<Zombie>();
        humans = new List<Human>();
        obstacles = new List<Obstacle>();
        bulletPool = new List<Bullet>();
        bulletSpace = new List<Bullet>();
        crates = new List<AmmoCrate>();
        camHeight = mainCam.transform.position.y;
        camExtent = new Vector2(mainCam.orthographicSize * mainCam.aspect, mainCam.orthographicSize);
        audioSrc = GetComponent<AudioSource>();
        audioSrc.clip = bgmMain;

        // Define game bounds
        bounds = new Bounds(
            new Vector3(gameBounds.transform.position.x + gameBounds.terrainData.size.x / 2, 0, 
            gameBounds.transform.position.z + gameBounds.terrainData.size.z / 2),
            new Vector3(gameBounds.terrainData.size.x, 0, gameBounds.terrainData.size.z));

        // Get references to obstacles and ammo crates
        obstacles.AddRange(obstacleContainer.GetComponentsInChildren<Obstacle>());
        crates.AddRange(crateContainer.GetComponentsInChildren<AmmoCrate>());

        // Instantiate and initialize humans
        for (int i = 0; i < playerSquad.transform.childCount; i++)
        {
            // Store player as both player and as human
            if (playerSquad.transform.GetChild(i).gameObject.CompareTag("Player"))
                player = playerSquad.transform.GetChild(i).GetComponent<Player>();

            humans.Add(playerSquad.transform.GetChild(i).GetComponent<Human>());
            humans[i].gameObject.SetActive(true);
            humans[i].SetManager(this);
        }
        
        // Initialize zombies and spread across map
        for (int i = 0; i < 50; i++)
        {
            // Spawn four sets of 15 zombies in concentrated locations
            Vector3 spawnCenter = bounds.center;
            if (i < 15)
            {
                spawnCenter.x += 30;
                spawnCenter.z += 30;
            }
            else if (i < 30)
            {
                spawnCenter.x -= 20;
                spawnCenter.z += 30;
            }
            else if (i < 45)
            {
                spawnCenter.x -= 20;
                spawnCenter.z -= 5;
            }
            else
            {
                spawnCenter.x += 30;
                spawnCenter.z -= 10;
            }

            CreateZombie(spawnCenter, true);
        }

        // Create bullet pool
        for (int i = 0; i < 3; i++)
        {
            bulletPool.Add(Instantiate(bulletPrefab).GetComponent<Bullet>());
            bulletPool[i].transform.position = new Vector3(500, 500, 500);
        }

        // Begin physics
        Time.timeScale = 1;
    }

    void CreateZombie(Vector3 spawnPos, bool isRandom=false)
    {
        Zombie newZomb = Instantiate(zombiePrefab).GetComponent<Zombie>();
        zombies.Add(newZomb);
        newZomb.SetManager(this);
        newZomb.OnDeath = () =>
        {
            zombies.Remove(newZomb);
        };

        if (isRandom)
            RandomizePosition(newZomb, spawnPos, 20f);
        else
            newZomb.transform.position = spawnPos;
    }

    // Update is called once per frame
    void Update()
    {
        // Skip if game is over
        if (isGameOver)
            return;

        // Update game time
        gameTime += Time.deltaTime;
        zombieStrengthened = false;

        // Update BGM
        if (!audioSrc.isPlaying)
            audioSrc.Play();

        // Assess bullets
        UpdateBullets();

        // Check if any zombies are still active
        if (zombies.Count == 0)
        {
            // Player wins
            EndGame(true);

            // Rest of code is unneccesary
            return;
        }

        // Assess human-zombie collisions
        for (int i = 0; i < humans.Count; i++)
        {
            // Skip if dead
            if (!humans[i].IsAlive)
                continue;

            foreach (Zombie z in zombies)
            {
                // If zombie collides with human
                if (RadialCollision(humans[i], z) && !z.Attacking)
                {
                    z.Attack(humans[i]);

                    // If human dies, turn into zombie
                    if (!humans[i].IsAlive)
                        Zombify(humans[i]);
                    break;
                }
            }
        }

        // Check if player is still alive and still has squad members remaining
        if (!player.IsAlive || LiveHumanCount == 1)
        {
            EndGame(false);

            // Rest of code is unneccesary
            return;
        }

        // Assess player-obstacle collisions and keep player in bounds
        foreach (Obstacle o in obstacles)
        {
            if (RadialCollision(player, o))
                FixObstacleCollision(player, o);
        }
        LockToBounds(player);

        // Determine if player has found ammo crate
        foreach (AmmoCrate crate in crates)
        {
            if (RadialCollision(player, crate))
            {
                // Grant ammo and play SFX
                player.OpenCrate();
                if (sfxCrateCollected != null)
                    audioSrc.PlayOneShot(sfxCrateCollected);

                // Remove crate
                crates.Remove(crate);
                Destroy(crate.gameObject);
                break;
            }
        }

        // Strength zombies every 40 seconds
        zPowerTimer -= Time.deltaTime;
        if (zPowerTimer <= 0)
        {
            zombieStrengthened = true;
            zPowerTimer += 40;
            foreach (Zombie z in zombies)
                z.Strengthen();

            // Play SFX
            if (sfxZombiesStrengthened != null)
                audioSrc.PlayOneShot(sfxZombiesStrengthened);
        }

        // Keep camera on player
        mainCam.transform.position = new Vector3(player.Position.x, camHeight, player.Position.z);

        // Dont let camera see past bounds
        LockCamToBounds();
    }

    /// <summary>
    /// Updates active bullets and assesses bullet-related collisions
    /// </summary>
    private void UpdateBullets()
    {
        // Move active bullets into bullet space
        for (int i = 0; i < bulletPool.Count; i++)
        {
            if (bulletPool[i].IsActive)
            {
                bulletSpace.Add(bulletPool[i]);
                bulletPool.RemoveAt(i--);
            }
        }

        // Assess active bullets
        for (int i = 0; i < bulletSpace.Count; i++)
        {
            // If bullet expires, stop and move back to pool
            if (bulletSpace[i].Lifetime <= 0)
            {
                bulletSpace[i].Stop();
                bulletPool.Add(bulletSpace[i]);
                bulletSpace.RemoveAt(i--);
                continue;
            }

            int oldI = i;
            // Assess bullet-obstacle collisions
            foreach (Obstacle o in obstacles)
            {
                if (PointCollision(bulletSpace[i], o))
                {
                    bulletSpace[i].Stop();
                    bulletPool.Add(bulletSpace[i]);
                    bulletSpace.RemoveAt(i--);
                    break;
                }
            }

            // If bullet removed, skip rest of loop
            if (oldI != i)
                continue;

            foreach (Zombie z in zombies)
            {
                if (PointCollision(bulletSpace[i], z))
                {
                    // Deal damage to zombie
                    z.TakeDamage();

                    // Stop bullet
                    bulletSpace[i].Stop();
                    bulletPool.Add(bulletSpace[i]);
                    bulletSpace.RemoveAt(i--);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Determines if two circles are intersecting using their radii
    /// </summary>
    private bool RadialCollision(Vector3 pos1, float r1, Vector3 pos2, float r2)
    {
        return Mathf.Pow(r1 + r2, 2) >= Vector3.SqrMagnitude(pos1 - pos2);
    }

    /// <summary>
    /// Determines if two vehicles are colliding
    /// </summary>
    private bool RadialCollision(Vehicle obj1, Vehicle obj2)
    {
        return RadialCollision(obj1.Position, obj1.Radius, obj2.Position, obj2.Radius);
    }

    /// <summary>
    /// Determines if a vehicle is colliding with an obstacle
    /// </summary>
    private bool RadialCollision(Vehicle v, Obstacle obs)
    {
        return RadialCollision(v.Position, v.Radius, obs.Position, obs.Radius);
    }

    /// <summary>
    /// Determines if a point lies within the area of a circle
    /// </summary>
    private bool PointCollision(Vector3 point, Vector3 center, float radius)
    {
        return radius * radius >= Vector3.SqrMagnitude(center - point);
    }

    /// <summary>
    /// Determines if a bullet hits an obstacle
    /// </summary>
    private bool PointCollision(Bullet b, Obstacle o)
    {
        return PointCollision(b.Position, o.Position, o.Radius);
    }

    /// <summary>
    /// Determines if a bullet hits a vehicle
    /// </summary>
    private bool PointCollision(Bullet b, Vehicle v)
    {
        return PointCollision(b.Position, v.Position, v.Radius);
    }

    /// <summary>
    /// Resolves radial collision between a vehicle and a circular object
    /// </summary>
    private void FixObstacleCollision(Vehicle v, Obstacle obs)
    {
        v.transform.position = obs.Position + (v.transform.position - obs.Position).normalized * (v.Radius + obs.Radius);
    }

    /// <summary>
    /// "Zombifies" a human by removing it and creating a new Zombie in its place
    /// </summary>
    private void Zombify(Human human)
    {
        CreateZombie(human.Position);
        human.transform.position = new Vector3(500, 500, 500);
    }


    private void EndGame(bool playerWin)
    {
        isGameOver = true;

        // Freeze game and deactivate input
        Time.timeScale = 0;
        player.GetComponent<PlayerInput>().enabled = false;

        // Turn off BGM
        audioSrc.Stop();
        AudioClip sfx;

        if (playerWin)
        {
            // Player wins
            gameState = 1;
            sfx = sfxGameWin;
        }
        else
        {
            // Player loses
            gameState = -1;
            sfx = sfxGameLoss;
        }

        if (sfx != null)
            audioSrc.PlayOneShot(sfx);
    }

    /// <summary>
    /// Randomly place a Vehicle within the game bounds
    /// </summary>
    private void RandomizePosition(Vehicle target)
    {
        Vector3 newPos = Vector3.zero;

        newPos.x = Random.Range(bounds.min.x + target.Radius, bounds.max.x - target.Radius);
        newPos.y = target.Radius;
        newPos.z = Random.Range(bounds.min.z + target.Radius, bounds.max.z - target.Radius);

        target.transform.position = newPos;
    }

    /// <summary>
    /// Randomly place a Vehicle within a circular area
    /// </summary>
    private void RandomizePosition(Vehicle target, Vector3 center, float maxDistance)
    {
        bool validPosition = true;

        do
        {
            // Build a unit vector in a random direction
            float randAngle = Random.Range(0, Mathf.PI * 2);
            Vector3 offset = new Vector3(Mathf.Cos(randAngle), 0, Mathf.Sin(randAngle));

            // Scale by a random distance up to maxDistance
            offset *= Random.Range(0, maxDistance);

            // Set new position
            target.transform.position = center + offset;

            // Check for possible obstacle collisions
            foreach (Obstacle o in obstacles)
            {
                if (RadialCollision(target, o))
                {
                    validPosition = false;
                    break;
                }
            }
        } while (!validPosition);
    }

    /// <summary>
    /// Randomly place a Vehicle within bounds, outside of specified area
    /// </summary>
    private void RandomizePositionExceptArea(Vehicle target, Vector3 center, float maxDistance)
    {
        bool validPosition = true;
        do
        {
            RandomizePosition(target);

            // Ensure position is outside of specified area
            if (RadialCollision(target.Position, target.Radius, center, maxDistance))
            {
                validPosition = false;
                continue;
            }

            foreach (Obstacle o in obstacles)
            {
                if (RadialCollision(target, o))
                {
                    validPosition = false;
                    break;
                }
            }

        } while (!validPosition);
    }

    /// <summary>
    /// Restricts a vehicle's position to be within the game bounds
    /// </summary>
    private void LockToBounds(Vehicle v)
    {
        Vector3 newPos = v.transform.position;

        // Clamp to X bounds
        if (v.Position.x - v.Radius < bounds.min.x)
            newPos.x = bounds.min.x + v.Radius;
        else if (v.Position.x + v.Radius > bounds.max.x)
            newPos.x = bounds.max.x - v.Radius;

        // Clamp to Z bounds
        if (v.Position.z - v.Radius < bounds.min.z)
            newPos.z = bounds.min.z + v.Radius;
        else if (v.Position.z + v.Radius > bounds.max.z)
            newPos.z = bounds.max.z - v.Radius;

        v.transform.position = newPos;
    }

    /// <summary>
    /// Keep camera from seeing beyond game bounds
    /// </summary>
    private void LockCamToBounds()
    {
        Vector3 camPos = mainCam.transform.position;

        // Clamp to X bounds
        if (camPos.x - camExtent.x < bounds.min.x)
            camPos.x = bounds.min.x + camExtent.x;
        else if (camPos.x + camExtent.x > bounds.max.x)
            camPos.x = bounds.max.x - camExtent.x;

        // Clamp to Z bounds
        if (camPos.z - camExtent.y < bounds.min.z)
            camPos.z = bounds.min.z + camExtent.y;
        else if (camPos.z + camExtent.y > bounds.max.z)
            camPos.z = bounds.max.z - camExtent.y;

        mainCam.transform.position = camPos;
    }

    /// <summary>
    /// Stops the game scene and returns to the main menu
    /// </summary>
    public void OnExitGame()
    {
        GetComponent<SceneLoader>().OnGoToMenu();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.transform.position, 25f);
    }
}
