using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class MovingPlatform : RaycastController
{
    public LayerMask passengerMask;
    public float speed;
    public bool loop = false;
    public float waitTime;
    [Range(0,2)]
    public float easeAmount;


    public Vector3[] localWaypoints;
    Vector3[] globalWaypoints;
    float nextMoveTime;


    List<PassengerMovement> passengers;
    //so i think. dictionary are things that let you reference objects with bobjects
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>();



    int fromWaypointIndex;
    float percentBetweenWaypoints;

    public override void Start()
    {
        base.Start();

        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < localWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }
    private void Update()
    {
        UpdateRaycastOrigins();

        Vector3 velocity = CalcPlatformMovement();

        CalcPassengerMovement(velocity);

        MovePassengers(true);
        transform.Translate(velocity);
        MovePassengers(false);

    }

    Vector3 CalcPlatformMovement()
    {
        if(Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp(1,0 , percentBetweenWaypoints);
        float easedPercent = Ease(percentBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercent);

        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0;
            fromWaypointIndex++;
            //make sure it loops to the begining
            if (!loop)
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            nextMoveTime = Time.time + waitTime;
        }
        //amount desired to move this turn
        return newPos - transform.position;

    }

    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1-x, a));
    }



    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengers)
        {
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }
            if (passenger.moveBefore == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.onPlatform);
            }
        }
    }


    void CalcPassengerMovement(Vector3 velocity)
    {
        //hash sets are good at adding and checking for containers. apparently
        HashSet<Transform> movedPassengers = new HashSet<Transform>();
        passengers = new List<PassengerMovement>();
        float directionX = Mathf.Sign(velocity.x);
        float directionY = Mathf.Sign(velocity.y);

        //vertically moving platform
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth;
            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;

                //check rays based on the left/right velocity to make sure you dont clip through stuff if you're moving left or right
                rayOrigin += Vector2.right * (verticalRaySpacing * i);
                //cast rays to see any possible passengers
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask);
                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = (directionY == 1) ? velocity.x : 0;
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY;
                        Debug.Log("in the way vertically");
                        //direction y because it is only standing if the platforms going up
                        passengers.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), (directionY == 1), true));

                    }
                }
            }
        }

        //horizontally moving platform
        if (velocity.x != 0)
        {
            //add skinwidth because you removed it earlier
            float rayLength = Mathf.Abs(velocity.x) + skinWidth;
            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask);

                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengers.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));

                    }
                }
            }

        }

        // passenger on a horizontal or downward platform
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 4;
            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i);
                //cast rays to see any possible passengers
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask);
                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = velocity.x;
                        float pushY = velocity.y;

                        Debug.Log("catch a ride!");

                        passengers.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
                Debug.DrawRay(rayOrigin, (skinWidth * 4 - Mathf.Abs(velocity.y)) * Vector2.up);
            }
        }
    }

    struct PassengerMovement
    {
        public Transform transform;
        public Vector3 velocity;
        public bool onPlatform;
        public bool moveBefore;

        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _onPlatform, bool _moveBefore)
        {
            transform = _transform;
            velocity = _velocity;
            onPlatform = _onPlatform;
            moveBefore = _moveBefore;
        }
    }
    private void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red;

            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypointPosition = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypointPosition - Vector3.up * size, globalWaypointPosition + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPosition - Vector3.right * size, globalWaypointPosition + Vector3.right * size);
            }
        }
    }
}
