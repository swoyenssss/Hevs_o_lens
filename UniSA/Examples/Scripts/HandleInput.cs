using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HEVS.Experimental.Pointer))]
public class HandleInput : MonoBehaviour
{
    public string tap;
    public string hold;
    public string navigateX;
    public string navigateY;
    public string navigateZ;

    private HEVS.Experimental.Pointer pointer;
    private Transform target;

    // Start is called before the first frame update
    void Start()
    {
        pointer = GetComponent<HEVS.Experimental.Pointer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (HEVS.Cluster.isMaster)
        {
            Ray ray = new Ray(transform.position, transform.forward);

            RaycastHit[] hits = Physics.RaycastAll(transform.position, transform.forward);
            if (hits != null && hits.Length > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.transform.gameObject.name == "Graphs")
                    {
                        target = hit.transform;
                        break;
                    }
                }
            }
            
            if (target != null)
            {
                
                pointer.cursor.gameObject.SetActive(false);
                
                // TODO: don't know how to make this relative
                target.Rotate(HEVS.Input.GetAxis(navigateX), HEVS.Input.GetAxis(navigateY), HEVS.Input.GetAxis(navigateZ));
            }
            else
            {
                pointer.cursor.gameObject.SetActive(true);
                // Use the screens
            }
        }

        //first raycast for the graph
        // otherwise check for a screen
    }
}
