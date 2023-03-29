using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Human : Vehicle
{
    protected int health = 100;
    protected float protectTime = 0;
    protected bool isAlive = true;
    protected Manager gameManager;
    private List<Zombie> urgentZombies;
    protected Animator animator;
    protected AudioSource audioSrc;
    [SerializeField] protected List<AudioClip> sfxScream;
    [SerializeField] protected List<AudioClip> sfxDamaged;
    [SerializeField] protected AudioClip sfxDeath;

    // Properties
    public int Health => health;
    public bool IsAlive => isAlive;
    public bool Damaged => animator.GetCurrentAnimatorStateInfo(0).IsName("Damaged");


    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();

        // Human-specific stats
        urgentZombies = new List<Zombie>();
        maxSpeed = 3f;
    }

    // Update is called once per frame
    new void Update()
    {
        // Do not move when hurt
        if (!Damaged)
            base.Update();

        // Update invlunerability frames
        if (protectTime > 0)
            protectTime -= Time.deltaTime;

        // Set animation state based on speed
        float vSqMag = Vector3.SqrMagnitude(velocity);
        if (vSqMag > 3f)
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isStopped", false);
        }
        else if (vSqMag > 0.0000001f)
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", true);
            animator.SetBool("isStopped", false);
        }
        else
        {
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isStopped", true);
        }
    }


    protected override void CalcSteeringForces()
    {
        if (Damaged) return;

        // Reset acceleration
        acceleration = Vector3.zero;

        Vector3 ultima = Vector3.zero;

        // Track if currently fleeing zombies
        bool isFleeing = urgentZombies.Count > 0;
        urgentZombies.Clear();

        // Steer away from bounds
        ultima += SteerFromBounds() * 3;

        // Evade nearby zombies proportional to distance away
        Vector3 zEvasion = EvadeZombies(gameManager.Zombies);
        if (zEvasion != Vector3.zero)
            ultima += EvadeZombies(gameManager.Zombies) * 2;
        else
            ultima += FollowLeader(gameManager.Player);

        // If just started fleeing, play SFX
        if (!isFleeing && urgentZombies.Count > 0)
        {
            if (sfxScream.Count > 0)
                audioSrc.PlayOneShot(sfxScream[Random.Range(0, sfxScream.Count)]);
        }

        // Separate from other humans
        ultima += Separate(gameManager.Humans);

        // Avoid obstacles
        ultima += AvoidAllObstacles(gameManager.Obstacles);

        // Scale to max speed
        ultima = ultima.normalized * maxSpeed;

        // Apply total force
        ApplyForce(ultima);
    }

    /// <summary>
    /// Steer human to arrive at point near leader if not within a certain distance
    /// </summary>
    /// <param name="leader"></param>
    /// <returns></returns>
    private Vector3 FollowLeader(Player leader)
    {
        Vector3 steerForce = Vector3.zero;

        if (Vector3.SqrMagnitude(leader.Position - Position) > maxSpeed * maxSpeed)
            steerForce = Arrive(leader.Position - (leader.Position - Position).normalized * maxSpeed * 0.75f);

        return steerForce;
    }

    /// <summary>
    /// Evade all zombies within a certain range, inversely proportional to their distance away
    /// </summary>
    /// <param name="zombies"></param>
    /// <returns></returns>
    private Vector3 EvadeZombies(List<Zombie> zombies)
    {
        Vector3 evasionForce = Vector3.zero;
        float zDistance;

        foreach (Zombie z in zombies)
        {
            zDistance = Vector3.SqrMagnitude(Position - z.Position);

            // Limit distance to prevent division errors
            if (zDistance < 0.0001f)
                zDistance = 0.0001f;
            
            // Evade zombies within 5 units, weighted by distance
            if (zDistance <= 25)
            {
                evasionForce += Evade(z) / zDistance;
                urgentZombies.Add(z); // Track zombies that are being fleed from
            }
        }

        return evasionForce.normalized * maxSpeed;
    }


    public void TakeDamage(Zombie z)
    {
        AudioClip sfx = null;

        // Check for invulnerability
        if (protectTime <= 0)
        {
            health -= z.Damage;

            // Check if dead
            if (health <= 0)
            {
                health = 0;
                isAlive = false;
                sfx = sfxDeath;
            }
            else
            {
                // Play hurt animation
                animator.SetTrigger("damaged");

                // Allow short invlunerability after hit
                protectTime = 1f;

                // Reset velocity for stun/hit effect
                velocity = Vector3.zero;

                // Pick damage SFX
                if (sfxDamaged.Count > 0)
                    sfx = sfxDamaged[Random.Range(0, sfxDamaged.Count)];
            }

            // Play hit SFX
            if (sfx != null)
            {
                audioSrc.clip = sfx;
                audioSrc.Play();
            }
        }
    }

    public void SetManager(Manager manager)
    {
        gameManager = manager;
        bounds = gameManager.Bounds;
    }


    private void OnDrawGizmos()
    {
        // Draw velocity
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Position, Position + velocity);

        // Draw lines to all target zombies
        foreach (Zombie z in urgentZombies)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawLine(Position, z.Position);

            // Draw target zombies' future positions
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(z.FuturePos, 0.1f);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Position, Radius);
    }
}
