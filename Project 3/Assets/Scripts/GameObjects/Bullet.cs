using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Bullet : MonoBehaviour
{
    private bool isActive = false;
    private Vector3 speed = Vector3.zero;
    private float lifetime = 0;
    private AudioSource audioSrc;
    [SerializeField] private List<AudioClip> sfxFired;

    // Properties
    public bool IsActive => isActive;
    public float Lifetime => lifetime;
    public Vector3 Direction => speed.normalized;
    public Vector3 Position
    {
        get
        {
            Vector3 pos = transform.position;
            pos.y = 0;
            return pos;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        audioSrc = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isActive && lifetime > 0)
        {
            lifetime -= Time.deltaTime;
            transform.position += speed * Time.deltaTime;
        }
    }

    public void Fire(Vector3 startPos, Vector3 direction)
    {
        // Set speed and lifetime
        isActive = true;
        lifetime = 1f;
        speed = direction * 30f;

        // Move to point of fire and face direction
        startPos.y = 0.5f;
        transform.position = startPos;
        transform.rotation = Quaternion.LookRotation(Vector3.up, direction);

        // Play SFX
        if (sfxFired.Count > 0)
            audioSrc.PlayOneShot(sfxFired[Random.Range(0, sfxFired.Count)]);
    }

    public void Stop()
    {
        isActive = false;
        transform.position = new Vector3(500, 500, 500);
    }
}
