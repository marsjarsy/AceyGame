using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.U2D;

public class ParallaxLayer : MonoBehaviour
{
    
    private Vector2 startPosition;
    public GameObject cam;
    public float parallaxAmountX;
    public float parallaxAmountY;
    public bool useY;
    private void Start()
    {
        startPosition = transform.position;
        if(cam == null)
        {
            cam = GameObject.Find("Main Camera");
        }
        
        CinemachineCore.CameraUpdatedEvent.AddListener(UpdateParallax);;
    }
    private void UpdateParallax(CinemachineBrain brain)
    {
        float distanceX = ((brain.transform.position.x * parallaxAmountX) + (cam.transform.position.x * parallaxAmountX)) / 2;
        float distanceY = 0;
        if(useY)
        {
            distanceY = ((brain.transform.position.y * parallaxAmountY) + (cam.transform.position.y * parallaxAmountY)) / 2;
            //distanceY = (cam.transform.position.y * parallaxAmountY);
        }
        //float distanceX = (cam.transform.position.x * parallaxAmountX);

        transform.position = new Vector3(startPosition.x + distanceX, (startPosition.y + distanceY) , transform.position.z);
        
    }
}
