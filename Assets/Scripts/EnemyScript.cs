using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    public int health = 1;
    private Collider2D collide;

    public Sprite leftSplit;
    public Sprite rightSplit;
    GameObject gayObject;
    GameObject hayObject;

    private bool beenHit = false;

    private float moveTimer = 0;
    private Vector2 moveTo;
    private void Start()
    {
        collide = GetComponent<Collider2D>();
        
    }


    private void FixedUpdate()
    {
        if (beenHit)
        {
            moveTimer -= Time.deltaTime;
            gayObject.transform.position = new Vector2(Mathf.Lerp(gayObject.transform.position.x, moveTo.x + .5f, .15f), transform.position.y);
            hayObject.transform.position = new Vector2(Mathf.Lerp(hayObject.transform.position.x, moveTo.x, .15f), transform.position.y);
        }
    }

    public void hitReaction()
    {
        if(!beenHit)
        {
            beenHit = true;
            moveTo = new Vector2(transform.position.x - .25f, transform.position.y);
            Destroy(GetComponent<Collider2D>());
            Destroy(GetComponent<Rigidbody2D>());
            GetComponent<SpriteRenderer>().sprite = null;
            gayObject = new GameObject();
            hayObject = new GameObject();
            gayObject.transform.position = transform.position;
            hayObject.transform.position = transform.position;
            var h = hayObject.AddComponent<SpriteRenderer>();
            h.sprite = leftSplit;
            h.sortingOrder = -1;
            var g = gayObject.AddComponent<SpriteRenderer>();
            g.sprite = rightSplit;
            g.sortingOrder = 5; 
            moveTimer = 1;
        }
    }
    
}
