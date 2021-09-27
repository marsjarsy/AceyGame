using Elendow.SpritedowAnimator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitBox : MonoBehaviour
{
    public float time;
    public Vector2 size;
    public Vector2 position;
    public GameObject player;
    public GameObject effectPrefab;
    SpriteAnimator animator;

    public void Init(float _time, Vector2 _size, Vector2 _position, GameObject _player)
    {
        //the position thing is e meant to be offset relative to the player
        time = _time;
        size = _size;
        position = _position;
        player = _player;
        animator = GetComponent<SpriteAnimator>();

        GetComponent<SpriteRenderer>().flipX = player.GetComponent<PlayerController>().flipX;
        
        
            //particleEffect.transform.rotation = Quaternion.Euler(-60, 90, 0);
            //particleEffect.transform.position = new Vector2(_position.x - 5, _position.y - 5);
       
    }


    private void OnTriggerStay2D(Collider2D other)
    {
        Debug.Log("please for the love of fuck");
        //check to see if this item can be hit

        //prevents the hitbox from being active frame 1 so that the animation can play in sync with the player
        if (animator.CurrentFrame > 0)
            if (other.CompareTag("canHit"))
            {
                Debug.Log("hit!");
                //particleEffect.GetComponent<ParticleSystem>().Play();
                //get the hit reaction (make a seperate class later for other objects)
                Instantiate(effectPrefab).GetComponent<PlayEffect>().Init(transform.position, "SlashImpact");
                other.GetComponent<EnemyScript>().hitReaction();

            }
    }


    private void Start()
    {
        transform.localScale = size;
        if (player.GetComponent<PlayerController>().flipX)
        {
            transform.position = new Vector2(-position.x, position.y) + (Vector2)player.transform.position;
        }
        else
        {
            transform.position = position + (Vector2)player.transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        time -= Time.deltaTime;

        //GetComponent<SpriteAnimator>().onStop;


        //there is probably a way to make this cleaner, like just passing in the player's input or something
        if (player.GetComponent<PlayerController>().flipX)
        {
            transform.position = new Vector2(-position.x, position.y) + (Vector2)player.transform.position;
        }
        else
        {
            transform.position = position + (Vector2)player.transform.position;
        }
        //destroy self once the time runs out, also tell the player it can swing again
        if (time <= 0)
        {
            //player.GetComponent<PlayerController>().isSwinging = false;
            Destroy(this.gameObject);
        }
    }
}
