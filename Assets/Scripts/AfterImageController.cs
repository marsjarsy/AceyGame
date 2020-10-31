using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class AfterImageController : MonoBehaviour
{

    private Color tint = new Color();
    private float time = 0;
    private SpriteRenderer spriteRenderer;
    public Object prefab;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if(spriteRenderer == null)
        {
            spriteRenderer = this.gameObject.AddComponent<SpriteRenderer>();
        }
        
    }

    public void CreateAfterImage(Color _color, float _time, Vector3 pos, Sprite sprite, bool flip)
    {
        // GameObject newAfterImage = Instantiate(prefab) as GameObject;
        //AfterImageController newController = newAfterImage.GetComponent<AfterImageController>();
        //newController.tint = _color;
        //newController.time = _time;
        tint = _color;
        time = _time;
        spriteRenderer.material.shader = Shader.Find("GUI/Text Shader");
        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = flip;
        transform.position = pos;
        spriteRenderer.color = tint;
        if (this.time == 0)
        {
            Debug.Log("hey, you forgot to set the time in an afterimage somewhere. deleting object");
            Destroy(this);
        }
    }



    private void FixedUpdate()
    {
        tint = new Color(tint.r, tint.g, tint.b, tint.a - time * Time.deltaTime);
        spriteRenderer.color = tint;
        if (tint.a <= 0.01f)
        {
            Destroy(this.gameObject);
        }
    }
}
