using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : Human
{
    private int ammo = 30;
    private float shootCD = 0;
    private bool[] keyStates;
    [SerializeField] protected List<AudioClip> sfxFootstep;
    protected float footstepTimer;
    protected float footstepRate = 0;
    protected const float FOOTSTEP_RUN_RATE = 0.25f;
    protected const float FOOTSTEP_WALK_RATE = 0.5f;

    // Properties
    public int Ammo => ammo;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        keyStates = new bool[4];
        animator = GetComponent<Animator>();
        audioSrc = GetComponent<AudioSource>();
        maxSpeed = 3f;
    }

    // Update is called once per frame
    new void Update()
    {
        // Calls Vehicle.Update, NOT Human.Update
        base.Update();

        // Slow to a stop when not moving
        if (acceleration == Vector3.zero)
            velocity *= 0.8f;

        // Update invlunerability frames
        if (protectTime > 0)
            protectTime -= Time.deltaTime;

        // Update shoot cooldown
        if (shootCD > 0)
            shootCD -= Time.deltaTime;

        // Update footsteps if moving
        if (footstepRate != 0)
        {
            // Play SFX based on movement rate
            if (footstepTimer >= footstepRate)
            {
                if (sfxFootstep.Count > 0)
                    audioSrc.PlayOneShot(sfxFootstep[Random.Range(0, sfxFootstep.Count)]);
                footstepTimer = 0;
            }
            else
                footstepTimer += Time.deltaTime;
        }

        // Set animation state based on speed
        float vSqMag = Vector3.SqrMagnitude(velocity);
        if (vSqMag > 5f)
        {
            footstepRate = FOOTSTEP_RUN_RATE;
            animator.SetBool("isRunning", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isStopped", false);
        }
        else if (vSqMag > 0.0000001f)
        {
            footstepRate = FOOTSTEP_WALK_RATE;
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", true);
            animator.SetBool("isStopped", false);
        }
        else
        {
            footstepRate = 0;
            animator.SetBool("isRunning", false);
            animator.SetBool("isWalking", false);
            animator.SetBool("isStopped", true);
        }
    }

    protected override void CalcSteeringForces()
    {
        // Reset acceleration
        acceleration = Vector3.zero;

        Vector3 ultima = Vector3.zero;

        // Follow mouse
        // ultima += FollowMouse();

        // Move using WASD keys
        ultima += MovementInput();

        // Scale to max speed
        ultima = ultima.normalized * maxSpeed;

        // Apply total force
        ApplyForce(ultima);
    }

    /// <summary>
    /// Steers the player towards the mouse's world position
    /// </summary>
    private Vector3 FollowMouse()
    {
        Vector3 mousePoint = Mouse.current.position.ReadValue();
        mousePoint = Camera.main.ScreenToWorldPoint(mousePoint);
        mousePoint.y = 0;

        return Arrive(mousePoint);
    }

    /// <summary>
    /// Steers the player towards cardinal directions based on key presses
    /// </summary>
    private Vector3 MovementInput()
    {
        Vector3 targetPos = vPosition;

        // Calculate target position using each active key
        for (int i = 0; i < keyStates.Length; i++)
        {
            if (keyStates[i]) // Offset target position in N/E/S/W directions respectively
                targetPos += (new Vector3(Mathf.Sign(2 - i) * maxSpeed * (i % 2), 0, Mathf.Sign(1 - i) * maxSpeed * ((i + 1) % 2)));
        }

        // Seek target position
        // If no keys pressed, seek self and slow to a stop
        if (targetPos == vPosition)
            return Vector3.zero;

        return Seek(targetPos);
    }

    void OnMoveNorth(InputValue value)
    {
        keyStates[0] = value.isPressed;
    }

    void OnMoveEast(InputValue value)
    {
        keyStates[1] = value.isPressed;
    }

    void OnMoveSouth(InputValue value)
    {
        keyStates[2] = value.isPressed;
    }

    void OnMoveWest(InputValue value)
    {
        keyStates[3] = value.isPressed;
    }

    /// <summary>
    /// Shoots a bullet in the direction of the mouse if ammo is available
    /// </summary>
    void OnShoot()
    {
        // Can a bullet be shot?
        if (ammo > 0 && shootCD <= 0)
        {
            // Reset cooldown and remove ammo
            shootCD += 0.4f;
            ammo--;

            // Send bullet towards mouse
            Vector3 mousePoint = Mouse.current.position.ReadValue();
            mousePoint = Camera.main.ScreenToWorldPoint(mousePoint);
            mousePoint.y = 0;

            // Fire first free bullet
            gameManager.BulletPool[0].Fire(Position, (mousePoint - Position).normalized);
        }
    }

    // Grants player ammo and health
    public void OpenCrate()
    {
        ammo += 20;
        health += 10;
        if (health > 100)
            health = 100;
    }

    private void OnDrawGizmos()
    {
        // Mouse point
        Gizmos.color = Color.red;
        Vector3 mousePoint = Mouse.current.position.ReadValue();
        mousePoint = Camera.main.ScreenToWorldPoint(mousePoint);
        mousePoint.y = 0;
        Gizmos.DrawSphere(mousePoint, 0.1f);
        Gizmos.DrawLine(Position, mousePoint);

        // Velocity
        Gizmos.color = Color.white;
        Gizmos.DrawLine(Position, Position + velocity);

        // Acceleration
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Position + velocity, Position + velocity + lastAccel);

        // Max speed
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(Position + direction * maxSpeed, 0.1f);

        // Area that humans will seek to when far enough from leader
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Position, maxSpeed);
    }
}
