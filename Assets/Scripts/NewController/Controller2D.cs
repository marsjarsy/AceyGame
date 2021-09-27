using Boo.Lang.Runtime.DynamicDispatching;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor.Experimental.GraphView;
using UnityEngine;


//this is following the Sebastian Lague Character controller 2d tutorial because my brain is too small for raycast collisoon

public class Controller2D : RaycastController
{

    float maxClimbAngle = 80;
    float maxDescendAngle = 90;
    //float SlideDownMaxSlope = 89;
    public CollisionInfo collisions;
    public bool standingOnPlatform;

    //override the RaycastControlelr start, base.Start() is the original star that gets called first. at this point you can add your own stuff
    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1;
    }
    public void Move(Vector3 velocity, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();
       // checkGround(collisions.faceDir);
        collisions.Reset();
        collisions.velocityOld = velocity;

        if (velocity.x != 0)
            collisions.faceDir = (int)Mathf.Sign(velocity.x);

        if (collisions.fallen && !collisions.below && !standingOnPlatform && velocity.y < 0)
        {
            GroundSnap(ref velocity);
        }

        if (velocity.y < 0)
            DescendSlope(ref velocity);

        HorizontalCollision(ref velocity);
        if (velocity.y != 0)
            VerticalCollision(ref velocity);


        

        

        transform.Translate(velocity);
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    void GroundSnap(ref Vector3 velocity)
    {
        Vector3 rayOrigin = (collisions.faceDir == 1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, skinWidth * 2, collisionMask);

        if (hit)
        {
            velocity.y = -hit.distance;
            collisions.below = true;
        }

    }

    //ref passes the same variable, any change to a ref will change it where it was passed in
    //Move(){velocity = 1;}
    //VerticalCollision(ref v3 velocity){ velocity = 0;} 
    //Move's velocity now == 0
    void VerticalCollision(ref Vector3 velocity)
    {
        float directionY = Mathf.Sign(velocity.y);
        //add skinwidth because you removed it earlier
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        for (int i = 0; i < verticalRayCount; i++)
        {
            //if moving down, start bottom left, if up start in top left
            //the question mark functions like a sort of inline if statement. the parenthesis is the condition, and the colon separates the two responses
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            //check rays based on the left/right velocity to make sure you dont clip through stuff if you're moving left or right
            rayOrigin += Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            //change velocity by distance to hit
            if (hit)
            {
                //hit.distance is always positive
                velocity.y = (hit.distance - skinWidth) * directionY;
                if (collisions.climbingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                //set the collision things
                collisions.above = directionY == 1;
                collisions.below = directionY == -1;
                //change the length so that the last ray does not get priority, if this isnt done it would snap to whatever ground the last ray touches
                rayLength = hit.distance;
            }

            Debug.DrawRay(rayOrigin, Vector2.up * directionY * rayLength, Color.red);
        }

        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(velocity.x);
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * directionX;
                    collisions.slopeAngle = slopeAngle;
                }
            }

        }
    }
    void HorizontalCollision(ref Vector3 velocity)
    {
        float directionX = collisions.faceDir;
        //add skinwidth because you removed it earlier
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;

        if (Mathf.Abs(velocity.x) < skinWidth)
        {
            rayLength = skinWidth * 2;
        }

        for (int i = 0; i < horizontalRayCount; i++)
        {
            //if moving down, start bottom left, if up start in top left
            //the question mark functions like a sort of inline if statement. the parenthesis is the condition, and the colon separates the two responses
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            //change velocity by distance to hit
            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (i == 0 && slopeAngle <= maxClimbAngle)
                {
                    //helps prevent slowdown when transitioning between 2 slopes
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        velocity = collisions.velocityOld;
                    }
                    float distanceToSlopeStart = 0;
                    //this makes sure you actually attatch to the slope as you climb it, otherwise you just float slightly next to it
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        velocity.x -= distanceToSlopeStart * directionX;
                    }
                    ClimbSlope(ref velocity, slopeAngle);
                    velocity.x += distanceToSlopeStart * directionX;
                }

                if (!collisions.climbingSlope || slopeAngle > maxClimbAngle)
                {
                    //hit.distance is always positive
                    velocity.x = (hit.distance - skinWidth) * directionX;

                    if (collisions.climbingSlope)
                    {
                        velocity.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad * Mathf.Abs(velocity.x));
                    }
                    collisions.right = directionX == 1;
                    collisions.left = directionX == -1;
                    //change the length so that the last ray does not get priority, if this isnt done it would snap to whatever ground the last ray touches
                    rayLength = hit.distance;
                }
            }

            Debug.DrawRay(rayOrigin, Vector2.right * directionX * rayLength * -2, Color.red);
        }
    }

    void ClimbSlope(ref Vector3 velocity, float slopeAngle)
    {//treat the x velocity more like the distance up a slope, then do trig of nomatry
        float moveDistance = Mathf.Abs(velocity.x);
        float climbVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
        //make sure you're not juming
        if (velocity.y <= climbVelocityY)
        {
            velocity.y = climbVelocityY;
            velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
        }
    }

    void DescendSlope(ref Vector3 velocity)
    {
        float directionX = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        if (hit)
        {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (slopeAngle != 0 && slopeAngle <= maxDescendAngle)
            {
                if (Mathf.Sign(hit.normal.x) == directionX)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocityY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
                        velocity.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocityY;

                        collisions.slopeAngle = slopeAngle;
                        collisions.descendingSlope = true;
                        collisions.below = true;
                    }
                }
            }
        }
    }




    //if there's collision in these directions

    public struct CollisionInfo
    {
        public bool above, below;
        public bool left, right;

        public bool fallen;

        public bool climbingSlope, descendingSlope;

        public float slopeAngle, slopeAngleOld;

        public Vector2 slopeNormal;

        public RaycastHit2D vHit;

        public Vector3 velocityOld;

        public int faceDir;
        public void Reset()
        {
            fallen = below;
            above = below = false;
            left = right = false;
            climbingSlope = descendingSlope = false;
            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
            slopeNormal = Vector2.zero ;
        }
    }




}
