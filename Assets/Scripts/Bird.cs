﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Bird : MonoBehaviour
{
    public Flock flock;
    public Rigidbody2D body;
    public Rigidbody2D poopPrefab;
	public EffectCamera cam;

	private string id;
    private bool canBoost = true;
    private float speedMod = 1f;

    // Start is called before the first frame update
    void Start()
    {
        id = System.Guid.NewGuid().ToString();
    }

    // Update is called once per frame
    void Update()
    {
        var diff = flock.transform.position - transform.position;
        var speed = Mathf.Max(Time.deltaTime * 2f, Time.deltaTime * 1f * diff.magnitude);
        //transform.position = Vector3.MoveTowards(transform.position, flock.transform.position, speed);

        var others = flock.GetBirds().Where(b => b != this).ToList();

        Vector2 home = diff.normalized * speed * 100f;
        var separate = Separate(others) * 25;
        var align = Align(others) * 0.5f;
        var cohesion = Cohesion(others) * 2.5f;

        body.AddForce((home + separate + align + cohesion) * speedMod);

        var angle = Mathf.Atan2(body.velocity.y, body.velocity.x) / Mathf.PI * 180f - 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        LimitVelocity();

        speedMod = Mathf.MoveTowards(speedMod, 1f, Time.deltaTime);
    }

    void LimitVelocity()
    {
        if(body.velocity.magnitude > 10f)
        {
            body.velocity = body.velocity.normalized * 10f * speedMod;
        }
    }

    private Vector2 Seek(Vector3 target)
    {
        return target - transform.position;  // A vector pointing from the position to the target
    }

    private Vector2 Cohesion(List<Bird> birds)
    {
        var neighbordist = 3f;
        var sum = Vector3.zero;   // Start with empty vector to accumulate all positions
        var count = 0;
        foreach(var b in birds)
        {
            var d = Vector2.Distance(transform.position, b.transform.position);
            if ((d > 0) && (d < neighbordist))
            {
                sum += b.transform.position;
                count++;
            }
        }
        if (count > 0)
        {
            sum /= count;
            return Seek(sum);  // Steer towards the position
        }
        else
        {
            return Vector2.zero;
        }
    }

    private Vector2 Align(List<Bird> birds)
    {
        var neighbordist = 3f;
        var sum = Vector2.zero;
        var count = 0;
        foreach(var b in birds)
        {
            float d = Vector2.Distance(transform.position, b.transform.position);
            if ((d > 0) && (d < neighbordist))
            {
                sum += b.body.velocity;
                count++;
            }
        }
        if (count > 0)
        {
            sum /= count;
            return sum - body.velocity;
        }

        return Vector2.zero;
    }

    private Vector2 Separate(List<Bird> birds)
    {
        float desiredseparation = 0.8f;
        Vector2 steer = new Vector2(0, 0);
        int count = 0;
        // For every boid in the system, check if it's too close
        foreach(var b in birds)
        {
            float d = Vector2.Distance(transform.position, b.transform.position);
            // If the distance is greater than 0 and less than an arbitrary amount (0 when you are yourself)
            if ((d > 0) && (d < desiredseparation))
            {
                // Calculate vector pointing away from neighbor
                Vector2 diff = transform.position - b.transform.position;
                diff.Normalize();
                diff /= d;        // Weight by distance
                steer += diff;
                count++;            // Keep track of how many
            }
        }
        // Average -- divide by how many
        if (count > 0)
        {
            steer /= count;
        }

        return steer;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Pickup")
        {
            var t = collision.gameObject.transform;
            flock.AddScore();
            flock.AddBird(t.position);

            EffectManager.Instance.AddEffect(4, t.position);

            AudioManager.Instance.PlayEffectAt(2, t.position, 1f);
            AudioManager.Instance.PlayEffectAt(3, t.position, 1f);
            AudioManager.Instance.PlayEffectAt(1, t.position, 0.397f);
            AudioManager.Instance.PlayEffectAt(7, t.position, 0.721f);
            AudioManager.Instance.PlayEffectAt(6, t.position, 0.462f);

            t.position = flock.GetPointInLevel();


            cam.BaseEffect(0.15f);

			canBoost = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "SlimeLink")
        {
            var other = collision.gameObject.GetComponent<Rigidbody2D>();

            if(other)
            {
                other.velocity = Vector2.zero;
                other.AddForce(body.velocity * 0.6f, ForceMode2D.Impulse);
            }

            EffectManager.Instance.AddEffect(0, transform.position);
            EffectManager.Instance.AddEffect(1, transform.position);
            EffectManager.Instance.AddEffect(2, transform.position);

            EffectManager.Instance.AddEffect(3, transform.position);

            AudioManager.Instance.PlayEffectAt(4, transform.position, 1.433f * 0.8f, true, 160);
            AudioManager.Instance.PlayEffectAt(12, transform.position, 1.53f * 0.8f, true, 160);
            AudioManager.Instance.PlayEffectAt(13, transform.position, 1.538f * 0.8f, true, 160);
            AudioManager.Instance.PlayEffectAt(10, transform.position, 1.676f * 0.8f, true, 160);
            AudioManager.Instance.PlayEffectAt(14, transform.position, 1.781f * 0.8f, true, 160);
            AudioManager.Instance.PlayEffectAt(16, transform.position, 1.53f * 0.8f, true, 160);

            cam.BaseEffect(0.5f);

            flock.RemoveBird(this);
            Destroy(gameObject);

            AudioManager.Instance.curMusic.pitch = 0.8f;

            var monster = collision.gameObject.GetComponentInParent<Slime>();

            monster.actualFace.Emote(Face.Emotion.Angry, Face.Emotion.Sneaky, 1f);

            if(!monster.eaten.Contains(id))
            {
                monster.eaten.Add(id);
            }

            if(monster.eaten.Count > flock.eatLimit)
            {
                monster.actualFace.Emote(Face.Emotion.Brag, Face.Emotion.Angry, 1f);

                monster.eaten.Clear();
                flock.eatLimit *= 3;
                flock.SpawnMonster();
            }
        }
    }

    public bool CanBoost()
    {
        return canBoost;
    }

    public void Boost()
    {
        if(canBoost)
        {
            AudioManager.Instance.PlayEffectAt(2, transform.position, 1f, true, 200);
            AudioManager.Instance.PlayEffectAt(5, transform.position, 1.36f, true, 200);
            AudioManager.Instance.PlayEffectAt(10, transform.position, 1.15f, true, 200);
            AudioManager.Instance.PlayEffectAt(15, transform.position, 1.174f, true, 200);
            AudioManager.Instance.PlayEffectAt(24, transform.position, 0.599f, true, 200);
            AudioManager.Instance.PlayEffectAt(25, transform.position, 1.271f, true, 200);

            canBoost = false;
            speedMod = 2f;

            var poop = Instantiate(poopPrefab, transform.position, Quaternion.identity);
            poop.transform.localScale *= Random.Range(0.8f, 1f);
            poop.velocity = -body.velocity.normalized * 20f;
        }
    }
}
