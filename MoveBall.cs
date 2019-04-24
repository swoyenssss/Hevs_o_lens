using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveBall : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (HEVS.Cluster.isMaster) transform.position += new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }
}
