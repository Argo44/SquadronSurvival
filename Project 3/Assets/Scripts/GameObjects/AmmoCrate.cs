using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AmmoCrate : Obstacle
{
    // Start is called before the first frame update
    void Start()
    {
        radius = 1f;
        position = transform.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
