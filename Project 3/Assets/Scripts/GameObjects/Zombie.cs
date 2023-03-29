using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(AudioSource))]
public class Zombie : Vehicle
{
    // Game Fields
    private Manager gameManager;
    private Human target;
    protected Animator animator;
    private int health = 40;
    private int damage = 20;
    private float detectionRadius = 10f;
    private bool isDead = false;
    private float stunTimer = 0f;

    // Audio Fields
    private AudioSource audioSrc;
    [SerializeField] private List<AudioClip> sfxHunting;
    [SerializeField] private List<AudioClip> sfxDamaged;
    [SerializeField] private AudioClip sfxDeath;

    // Properties
    public int Health => health;
    public int Damage => damage;
    public UnityAction OnDeath { get; set; }
    public bool Attacking => animator.GetCurrentAnimatorStateInfo(0).IsName("Z_Attack");

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();

        // Zombie-specific stats
        mass *= 1.5f;
        viewDistance = 2.5f;
    }

    // Update is called once per frame
    new void Update()
    {
        // Do not update when dead
        if (isDead) return;

        // Update stun timer
        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;

            // When stun ends, update animator
            if (stunTimer <= 0) animator.SetBool("isStunned", false);
            return;
        }
        else if (!Attacking) // If not attacking, update movement
            base.Update();

        // Set animation state based on speed
        float vSqMag = Vector3.SqrMagnitude(velocity);
        if (vSqMag > 1.5f)
        {
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isStopped", false);
        }
        else if (vSqMag > 0.000001f)
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
        // Skip calculations when attacking, stunned, or dead
        if (Attacking || isDead || stunTimer > 0) return;

        // Reset acceleration
        acceleration = Vector3.zero;

        Vector3 ultima = Vector3.zero;

        // Steer away from bounds
        ultima += SteerFromBounds() * 3;

        // Pursue closest human
        Human closest = FindClosestHuman(gameManager.Humans);

        // If switching from wandering to pursuing, play SFX
        if (target == null && closest != null)
        {
            if (sfxHunting.Count > 0) 
            {
                audioSrc.clip = sfxHunting[Random.Range(0, sfxHunting.Count)];
                audioSrc.Play();
            }
        }

        // If there is at least one human, pursue it
        target = closest;
        if (target != null)
            ultima += Pursue(target) * 2;
        else
            ultima += Wander();

        // Separate from other zombies
        ultima += Separate(gameManager.Zombies);

        // Avoid obstacles
        ultima += AvoidAllObstacles(gameManager.Obstacles) * 4;

        // Scale to max speed
        ultima = ultima.normalized * maxSpeed;

        // Apply total force
        ApplyForce(ultima);
    }

    /// <summary>
    /// Searches for the closest Human, returns null if no humans exist within range
    /// </summary>
    private Human FindClosestHuman(List<Human> humans)
    {
        Human closest = null;
        float closestDistance = float.MaxValue;

        foreach (Human h in humans)
        {
            // Skip dead humans
            if (!h.IsAlive)
                continue;

            float sqDistance = Vector3.SqrMagnitude(Position - h.Position);

            if (sqDistance <= detectionRadius * detectionRadius)
            {
                if (closest == null || sqDistance < closestDistance)
                {
                    closest = h;
                    closestDistance = sqDistance;
                }
            }
        }

        return closest;
    }

    public void Attack(Human target)
    {
        // Deal damage to target
        target.TakeDamage(this);

        // Stop movement and play animation
        velocity = Vector3.zero;
        animator.SetTrigger("attack");
    }

    /// <summary>
    /// Decrease health and apply knockback
    /// </summary>
    public void TakeDamage()
    {
        // Deal damage
        health -= 25;

        // Select and play appropriate FX
        AudioClip sfx;
        if (health > 0)
        {
            // Stun zombie on hit
            stunTimer = 0.2f;

            // Reset velocity for stun/hit effect
            velocity = Vector3.zero;

            sfx = sfxDamaged[Random.Range(0, sfxDamaged.Count)];
            animator.SetBool("isStunned", true);
        }
        else
        {
            isDead = true;
            sfx = sfxDeath;
            animator.Play("Z_Dead");
            OnDeath();
        }

        if (sfx != null)
        {
            audioSrc.clip = sfx;
            audioSrc.Play();
        }
    }

    /// <summary>
    /// Increase zombies stats
    /// </summary>
    public void Strengthen()
    {
        health += 5;
        damage += 5;
        detectionRadius += 2.5f;
        maxSpeed += 0.2f;
    }

    public void SetManager(Manager manager)
    {
        gameManager = manager;
        bounds = gameManager.Bounds;
    }


    private void OnDrawGizmos()
    {
        // Draw velocity
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Position, Position + velocity);

        // Draw detection radius
        Gizmos.DrawWireSphere(Position, detectionRadius);

        // Draw line to target human
        Gizmos.color = Color.yellow;
        if (target != null)
            Gizmos.DrawLine(Position, target.Position);

        // Draw collision area
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Position, radius);
    }
}
