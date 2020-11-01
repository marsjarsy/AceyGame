using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitBox : MonoBehaviour
{
    public float time;
    public Vector2 size;
    public Vector2 position;
    public GameObject player;

    public void Init(float _time, Vector2 _size, Vector2 _position, GameObject _player)
    {
        //the position thing is e meant to be offset relative to the player
        time = _time;
        size = _size;
        position = _position;
        player = _player;
    }

   
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("please for the love of fuck");
        //check to see if this item can be hit
        if (other.CompareTag("canHit"))
        {
            Debug.Log("hit!");
            //get the hit reaction (make a seperate class later for other objects)
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
        //there is probably a way to make this cleaner, like just passing in the player's input or something
        if(player.GetComponent<PlayerController>().flipX)
        {
            transform.position = new Vector2(-position.x, position.y )+ (Vector2)player.transform.position;
        }
        else
        {
            transform.position = position + (Vector2)player.transform.position;
        }
        //destroy self once the time runs out, also tell the player it can swing again
        if(time <= 0)
        {
            player.GetComponent<PlayerController>().isSwinging = false;
            Destroy(this.gameObject);
        }
    }
}
