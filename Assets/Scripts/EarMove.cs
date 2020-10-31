using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EarMove : MonoBehaviour
{
    public Transform target;
    public Rigidbody rb;
    public float moveForce = 1;
    private void Awake()
    {
       // rb = gameObject.AddComponent<Rigidbody>();
       // rb.drag = 10f;
       // rb.mass = .1f;
       // rb.interpolation = RigidbodyInterpolation.Interpolate;
       // rb.useGravity = false;
        transform.parent = null;
    }
    void Update()
    {
        //transform.position = Vector3.LerpUnclamped(transform.position, target.position, Vector3.Distance(transform.position,target.position) * 1.1f);
       // rb.AddForce((transform.position - target.position).normalized * -moveForce);
    }
}
