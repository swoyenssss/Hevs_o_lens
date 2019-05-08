using HEVS;
using System.Linq;

/// <summary>
/// </summary>
public class DisplayTracker : ClusterTransform
{

    /// <summary>
    /// The ID for the display's config.
    /// </summary>
    public string displayID;

    /// <summary>
    /// The actual display config.
    /// </summary>
    public DisplayConfig display;

    new private void Start()
    {
        base.Start();
        display = PlatformConfig.current.displays.First(i => i.id == displayID);
    }

    void FixedUpdate()
    {
        // Update transform if there is a receiver
        if (Cluster.isMaster)
        {
            transform.position = display.transform.translate;
            transform.rotation = display.transform.rotate;
        }
    }
}