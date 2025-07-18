using UnityEngine;

namespace Encounter.Runtime.Creatures
{
    public class FloatUp : MonoBehaviour
    {
        public float floatSpeed = 1f;           // How fast it floats up
        public float noiseScale = .00001f;         // How fast the noise changes
        public float noiseStrength = 100f;      // How much it drifts on X/Z
    
        private Vector3 _startPosition;
        private float _noiseOffsetX;
        private float _noiseOffsetZ;

        private Rigidbody _rb;


        private void Start()
        {
            _startPosition = transform.position;

            // Random offsets so multiple objects don't move identically
            _noiseOffsetX = Random.Range(0f, 100f);
            _noiseOffsetZ = Random.Range(0f, 100f);

            _rb = GetComponent<Rigidbody>();
            _rb.AddForce(Vector3.up * floatSpeed, ForceMode.Impulse);
 
        }
    
        private void Update() 
        {
            // Calculate target drift position
            float x = _startPosition.x + (Mathf.PerlinNoise(Time.time * noiseScale + _noiseOffsetX, 0) - 0.5f) * 2f * noiseStrength;
            float z = _startPosition.z + (Mathf.PerlinNoise(Time.time * noiseScale + _noiseOffsetZ, 1) - 0.5f) * 2f * noiseStrength;
            Vector3 targetPosition = new Vector3(x, transform.position.y+.05f, z);
    
            // Apply force towards the target position
            Vector3 direction = (targetPosition - transform.position).normalized;
            float driftForce = 2f;
            _rb.AddForce(direction * driftForce);
    
            // Add drag to prevent runaway velocity
            _rb.linearVelocity *= 0.98f; // Adjust this value (0.95-0.99)

            if (transform.position.y > 20)
            {
                Destroy(gameObject);
            }
        }
    }
}
