using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField]
    protected float radius = 0.5f;
    protected Vector3 position = Vector3.zero;

    // Properties
    public float Radius => radius;
    public Vector3 Position => position;

    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
