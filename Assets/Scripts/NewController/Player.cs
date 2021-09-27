using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{

    //makes everything easier on the brain
    public float jumpHeight = 2;
    public float timeToJumpApex = .2f;
    public float wallSlideSpeedMax = 1f;
    public float wallStickTime = .25f;
    public float friction = .025f;
    float currentFriction = 0;

    float timeToUnstick = 0;

    Controller2D controller;
    Vector3 velocity;
    float gravity = -1;
    float moveSpeed = 6;
    float jumpVelocity = 10;
    float velocityXSmoothing;

    public Vector2 wallJumpClimb;
    public Vector2 wallJumpOff;
    public Vector2 wallLeap;



    void Start()
    {
        controller = GetComponent<Controller2D>();
        gravity = -(jumpHeight * 2) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
    }

    private void Update()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        int wallDirX = (controller.collisions.left) ? -1 : 1;

        bool wallSliding = false;
        float targetX = input.x * moveSpeed;

        
        if (controller.collisions.below)
            currentFriction = 0;
        else
            currentFriction = friction;
        //adds a slight amount of momentum to movement
        velocity.x = Mathf.SmoothDamp(velocity.x, targetX, ref velocityXSmoothing, currentFriction);

        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true;
            if (velocity.y <= -wallSlideSpeedMax && ((controller.collisions.left && input.x < 0) || (controller.collisions.right && input.x > 0)) )
                velocity.y = -wallSlideSpeedMax;
        }

        

        //prevent gravity accumulation
        if (controller.collisions.above || controller.collisions.below)
            velocity.y = 0;

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (wallSliding)
            {
                if (wallDirX == input.x)
                {
                    velocity.x = -wallDirX * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
                else if (input.x == 0)
                {
                    velocity.x = -wallDirX * wallJumpOff.x;
                    velocity.y = wallJumpOff.y;
                }
                else
                {
                    velocity.x = -wallDirX * wallLeap.x;
                    velocity.y = wallLeap.y;
                }
            }
            if (controller.collisions.below)
                velocity.y = jumpVelocity;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }
}
