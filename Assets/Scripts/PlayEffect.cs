using Elendow.SpritedowAnimator;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class PlayEffect : MonoBehaviour
{
    private SpriteAnimator animator;

    public void Init(Vector2 position, string animName)
    {
        transform.position = position;
        animator = GetComponent<SpriteAnimator>();
        animator.Play(animName);
        animator.AddCustomEventAtEnd(animName).AddListener(EndAnim);

    }

    //hopefully destroys this object once the animation has ended
    private void EndAnim(BaseAnimator caller)
    {
        Destroy(this.gameObject);
    }

}
