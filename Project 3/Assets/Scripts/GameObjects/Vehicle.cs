using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Vehicle : MonoBehaviour
{
    protected Vector3 vPosition;
    protected Vector3 direction = Vector3.right;
    protected Vector3 velocity = Vector3.zero;
    protected Vector3 acceleration = Vector3.zero;
    protected Vector3 lastAccel;
    protected Vector3 right = Vector3.right;
    protected Bounds bounds;
    
    protected float maxSpeed = 2f;
    protected float radius = 0.5f;
    protected float mass = 1;
    protected float separationSpace = 2f;
    protected float viewDistance = 3f;
    protected float wanderAngle;
    protected float frictionCoEff = 0.05f;
    protected bool hasFriction = false;

    // Properties
    public Vector3 Acceleration => lastAccel;
    public Vector3 Velocity => velocity;
    public Vector3 Direction => direction;
    public Vector3 Position => vPosition;
    public Vector3 FuturePos => vPosition + velocity;
    public bool HasFriction => hasFriction;
    public float Mass
    {
        get { return mass; }
        set { mass = value; }
    }
    public float Radius
    {
        get { return radius; }
        set { radius = value; }
    }
    
    // Start is called before the first frame update
    protected void Start()
    {
        wanderAngle = Random.Range(0, 2 * Mathf.PI);
    }

    // Use FixedUpdate for CalcSteeringForces to maintain consistency with varying framerates
    protected void FixedUpdate()
    {
        // Calculate acceleration
        CalcSteeringForces();
    }

    // Update is called once per frame
    protected void Update()
    {
        // Update position
        vPosition = transform.position;

        // Change velocity by acceleration
        velocity += acceleration * Time.deltaTime;

        velocity.y = 0;

        // Send velocity to 0 when extremely small
        if (velocity.magnitude < 0.005f)
            velocity = Vector3.zero;

        // Apply velocity to position
        vPosition += velocity * Time.deltaTime;

        vPosition.y = 0;

        // Set object position and direction
        transform.position = vPosition;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Set direction and clear acceleration
        if (velocity != Vector3.zero)
        {
            direction = velocity.normalized;
            right = Vector3.Cross(direction, Vector3.up);
        }
        lastAccel = acceleration;
    }


    protected abstract void CalcSteeringForces();

    /// <summary>
    /// Applies a force to the object changes its acceleration, with respect to its mass
    /// </summary>
    /// <param name="force"></param>
    public void ApplyForce(Vector3 force)
    {
        acceleration += force / mass;
    }

    /// <summary>
    /// Toggles whether a friction force is applied each frame
    /// </summary>
    public void ToggleFriction()
    {
        hasFriction = !hasFriction;
    }

    /// <summary>
    /// Applies a dissipating force on the object
    /// </summary>
    protected void ApplyFriction()
    {
        // Use normalized reverse of velocity vector & scale by friction coefficient
        acceleration += -1 * velocity.normalized * frictionCoEff;
    }

    /// <summary>
    /// Seeks a target by moving towards its current position
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    protected Vector3 Seek(Vector3 targetPos)
    {
        Vector3 desiredVel = targetPos - vPosition;
        desiredVel = desiredVel.normalized * maxSpeed;

        return desiredVel - velocity;
    }


    protected Vector3 Seek(Vehicle target)
    {
        return Seek(target.transform.position);
    }

    /// <summary>
    /// Flees a target by moving away from it current position
    /// </summary>
    /// <param name="targetPos"></param>
    /// <returns></returns>
    protected Vector3 Flee(Vector3 targetPos)
    {
        Vector3 desiredVel = vPosition - targetPos;
        desiredVel = desiredVel.normalized * maxSpeed;

        return desiredVel - velocity;
    }


    protected Vector3 Flee(Vehicle target)
    {
        return Flee(target.transform.position);
    }

    /// <summary>
    /// Pursues a target by seeking its future position
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    protected Vector3 Pursue(Vehicle target, float dt = 1)
    {
        // If target is closer to this object than to its future position, flee target's current position
        if (Vector3.SqrMagnitude(target.Position - target.GetFuturePosition(dt)) > Vector3.SqrMagnitude(target.Position - Position))
            return Seek(target);

        return Seek(target.GetFuturePosition(dt));
    }

    /// <summary>
    /// Evades a target by fleeing from its future position
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    protected Vector3 Evade(Vehicle target, float dt = 1)
    {
        // If target is closer to this object than to its future position, flee target's current position
        if (Vector3.SqrMagnitude(target.Position - target.GetFuturePosition(dt)) > Vector3.SqrMagnitude(target.Position - Position))
            return Flee(target);

        return Flee(target.GetFuturePosition(dt));
    }

    /// <summary>
    /// Arrives at a target by seeking its position while far away, then slowing down as it approaches
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    protected Vector3 Arrive(Vector3 target)
    {
        Vector3 desiredVel = target - vPosition;

        if (desiredVel.magnitude < separationSpace)
            desiredVel = desiredVel.normalized * maxSpeed * desiredVel.magnitude / separationSpace;
        else
            desiredVel = desiredVel.normalized * maxSpeed;

        return desiredVel - velocity;
    }

    /// <summary>
    /// Arrives at a target by seeking its position while far away, then slowing down as it approaches
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    protected Vector3 Arrive(Vehicle target)
    {
        Vector3 desiredVel = target.Position - vPosition;

        if (desiredVel.magnitude < target.Radius * 4)
            desiredVel = desiredVel.normalized * maxSpeed * desiredVel.magnitude / (target.Radius * 4);
        else
            desiredVel = desiredVel.normalized * maxSpeed;

        return desiredVel - velocity;
    }

    /// <summary>
    /// Moves randomly by picking a semi-random point near its future position to seek
    /// </summary>
    /// <returns></returns>
    protected Vector3 Wander()
    {
        float wanderRadius = 2f;
        float wanderDist = 3f;
        wanderAngle += Random.Range(-Mathf.PI/12, Mathf.PI/12);
        Vector3 wanderPoint = new Vector3(
            vPosition.x + (direction * wanderDist).x + Mathf.Cos(wanderAngle) * wanderRadius, 
            0,
            vPosition.z + (direction * wanderDist).z + Mathf.Sin(wanderAngle) * wanderRadius);

        return Seek(wanderPoint);
    }

    /// <summary>
    /// Separates itself from nearby neighbors within a certain radius
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="neighbors"></param>
    /// <returns></returns>
    protected Vector3 Separate<T>(List<T> neighbors) where T : Vehicle
    {
        Vector3 separationForce = Vector3.zero;
        float neighborDistance;

        foreach (Vehicle neighbor in neighbors)
        {
            neighborDistance = Vector3.SqrMagnitude(Position - neighbor.Position);

            // Skip self
            if (neighborDistance < Mathf.Epsilon) 
                continue;

            // Limit distance to prevent division errors
            if (neighborDistance < 0.0001f)
                neighborDistance = 0.0001f;

            // Flee any neighbors within radius of separationSpace
            // Flee force is inversely proportional to distance from neighbor
            if (neighborDistance < Mathf.Pow(separationSpace, 2))
                separationForce += Flee(neighbor) * (1 / neighborDistance);
        }

        return separationForce.normalized * maxSpeed;
    }

    /// <summary>
    /// Calculates the vehicle's future position in dt seconds based on its current velocity
    /// </summary>
    /// <param name="dt"></param>
    /// <returns></returns>
    public Vector3 GetFuturePosition(float dt)
    {
        return vPosition + (velocity * dt);
    }

    /// <summary>
    /// Generates a force that steers the vehicle away from boundaries when it is within a certain distance
    /// </summary>
    /// <returns></returns>
    protected Vector3 SteerFromBounds()
    {
        Vector3 fPos = FuturePos;

        // if too close to wall, desired velocity is max speed in opposite direction of wall
        Vector3 steerForce = Vector3.zero;
        float boundAllowance = 2.5f;

        if (bounds.max.x - Position.x < boundAllowance || Position.x - bounds.min.x < boundAllowance)
        {
            
            Vector3 desired = new Vector3(
                Mathf.Sqrt(maxSpeed * maxSpeed - velocity.z * velocity.z) * (bounds.center.x - Position.x)/Mathf.Abs(bounds.center.x - Position.x),
                0, velocity.z);
            steerForce += (desired - velocity).normalized * maxSpeed;
        }

        if (bounds.max.z - Position.z < boundAllowance || Position.z - bounds.min.z < boundAllowance)
        {
            Vector3 desired = new Vector3(
                velocity.x, 0,
                Mathf.Sqrt(maxSpeed * maxSpeed - velocity.x * velocity.x) * (bounds.center.z - Position.z) / Mathf.Abs(bounds.center.z - Position.z));
            steerForce += (desired - velocity).normalized * maxSpeed;
        }

        return steerForce.normalized * maxSpeed;
    }

    protected Vector3 AvoidObstacle(Vector3 obstaclePos, float obstacleRadius)
    {
        Vector3 vToObj = obstaclePos - Position;

        // Ignore objects behind vehicle
        float dotFwdToObj = Vector3.Dot(direction, vToObj);
        if (dotFwdToObj < 0)
            return Vector3.zero;

        // Ignore objects too far left or right
        float dotRightToObj = Vector3.Dot(right, vToObj);
        if (Mathf.Abs(dotRightToObj) > radius + obstacleRadius) 
            return Vector3.zero;

        // Ignore objects farther than view distance
        if (dotFwdToObj > viewDistance)
            return Vector3.zero;

        // Obstacle will now be avoided
        // Calculate weight based on distance to object
        float weight = viewDistance / Mathf.Max(dotFwdToObj, 0.001f);

        // Desired velocity is direction away from obstacle
        Vector3 desired;
        if (dotRightToObj > 0)
            desired = right * -maxSpeed;
        else
            desired = right * maxSpeed;

        // Return steering force to desired velocity
        return (desired - velocity) * weight;
    }

    protected Vector3 AvoidObstacle(Obstacle objToAvoid)
    {
        return AvoidObstacle(objToAvoid.Position, objToAvoid.Radius);
    }

    protected Vector3 AvoidAllObstacles(List<Obstacle> obstables)
    {
        Vector3 ultima = Vector3.zero;

        foreach (Obstacle o in obstables)
            ultima += AvoidObstacle(o);

        return ultima;
    }

    /// <summary>
    /// "Bounces" the object off of the spatial bounds when in contact with them
    /// </summary>
    protected void Bounce()
    {
        Debug.Log("Bounds are " + bounds);

        // Horizontal bounds
        if (vPosition.x + radius > bounds.max.x)
        {
            vPosition.x = bounds.max.x - radius;
            velocity.x *= -1;
        }
        else if (vPosition.x - radius < bounds.min.x)
        {
            vPosition.x = bounds.min.x + radius;
            velocity.x *= -1;
        }

        // Vertical bounds
        if (vPosition.z + radius > bounds.max.z)
        {
            vPosition.z = bounds.max.z - radius;
            velocity.z *= -1;
        }
        else if (vPosition.z - radius < bounds.min.z)
        {
            vPosition.z = bounds.min.z + radius;
            velocity.z *= -1;
        }
    }

    /// <summary>
    /// Wraps the object around the screen when travelling off-screen
    /// </summary>
    protected void ScreenWrap()
    {
        // Horizontal bounds
        if (vPosition.x > bounds.max.x)
        {
            vPosition.x -= bounds.size.x;
        }
        else if (vPosition.x < bounds.min.x)
        {
            vPosition.x += bounds.size.x;
        }

        // Vertical bounds
        if (vPosition.z > bounds.max.z)
        {
            vPosition.z -= bounds.size.z;
        }
        else if (vPosition.z < bounds.min.z)
        {
            vPosition.z += bounds.size.z;
        }
    }
}
