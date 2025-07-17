using System.Numerics;
using System.Runtime.CompilerServices;
using UnityEngine;

public class floatUp : MonoBehaviour
{
    public float floatSpeed = 1f;           // How fast it floats up
    public float noiseScale = .00001f;         // How fast the noise changes
    public float noiseStrength = 100f;      // How much it drifts on X/Z
    
    private UnityEngine.Vector3 startPosition;
    private float noiseOffsetX;
    private float noiseOffsetZ;

    private UnityEngine.Vector3 floatForce;

    private Rigidbody rb;


    void Start()
    {


        startPosition = transform.position;

        // Random offsets so multiple objects don't move identically
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);
        floatForce = new UnityEngine.Vector3(0, 1f, 0);

        rb = GetComponent<Rigidbody>();
        rb.AddForce(UnityEngine.Vector3.up * floatSpeed, ForceMode.Impulse);
 
    }
    
    void Update() 
    {
    // Calculate target drift position
    float x = startPosition.x + (Mathf.PerlinNoise(Time.time * noiseScale + noiseOffsetX, 0) - 0.5f) * 2f * noiseStrength;
    float z = startPosition.z + (Mathf.PerlinNoise(Time.time * noiseScale + noiseOffsetZ, 1) - 0.5f) * 2f * noiseStrength;
    UnityEngine.Vector3 targetPosition = new UnityEngine.Vector3(x, transform.position.y+.05f, z);
    
    // Apply force towards the target position
    UnityEngine.Vector3 direction = (targetPosition - transform.position).normalized;
    float driftForce = 2f;
    rb.AddForce(direction * driftForce);
    
    // Add drag to prevent runaway velocity
    rb.linearVelocity *= 0.98f; // Adjust this value (0.95-0.99)

        if (transform.position.y > 20)
        {
            Destroy(gameObject);
        }
    }
}
