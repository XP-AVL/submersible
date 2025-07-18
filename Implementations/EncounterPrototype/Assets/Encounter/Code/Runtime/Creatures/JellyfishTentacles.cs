using UnityEngine;

// ==============================================================================
// JELLYFISH TENTACLES - THE GRACEFUL DANCERS
// ==============================================================================
// Welcome to the tentacle system! This is where we bring those beautiful,
// flowing tentacles to life underneath our jellyfish bell. Think of this as
// the CHOREOGRAPHER for our underwater dance performance!
//
// The magic here is in the PROCEDURAL GENERATION - we're not hand-placing
// each tentacle. Instead, we're writing CODE that creates them automatically!
// This means we can have 8 tentacles, or 80, or 800 - just by changing
// a single parameter. That's the POWER of procedural thinking!
//
// Each tentacle is a LineRenderer that flows and sways in the current,
// creating that mesmerizing jellyfish movement we all know and love.
// ==============================================================================

namespace Encounter.Runtime.Creatures
{
    public class JellyfishTentacles : MonoBehaviour
    {
        // ==============================================================================
        // THE PARAMETERS - Our creative control panel!
        // ==============================================================================
        // These are the "knobs and sliders" that let us design our tentacles.
        // Think of this like the control panel for a synthesizer - each parameter
        // affects the final result in a unique and beautiful way!
        // ==============================================================================
    
        [Header("Tentacle Parameters")]
        // How many tentacles should we create around the bell?
        // More tentacles = more graceful, more complex movement
        public int tentacleCount = 8;
    
        // How long should each tentacle be?
        // Longer tentacles create more dramatic, flowing movements
        public float tentacleLength = 3f;
    
        // How many points make up each tentacle?
        // More segments = smoother curves, but also more processing power needed
        public int tentacleSegments = 10;
    
        // How thick should the tentacles be at the top and bottom?
        // This creates that natural "tapering" effect - thick at the bell, thin at the tip
        public float startWidth = 0.1f;
        public float endWidth = 0.02f;
    
        // The material that controls how our tentacles look
        // This could be transparent, glowing, textured - whatever fits your vision!
        public Material tentacleMaterial;
    
        [Header("Animation")]
        // How much should the tentacles sway back and forth?
        // Think of this as the "strength of the current" in our underwater world
        public float swayAmount = 0.5f;
    
        // How fast should the swaying motion be?
        // Faster = more energetic, slower = more peaceful and meditative
        public float swaySpeed = 1f;
    
        // How much random organic movement should we add?
        // This is the "secret sauce" that makes movement feel alive and natural!
        public float noiseScale = 0.1f;
    
        [Header("References")]
        // Reference to the bell - we need to know where to attach our tentacles!
        // This is a perfect example of COMPOSITION - our tentacles work WITH the bell
        public JellyfishBell bellScript;
    
        // ==============================================================================
        // THE DATA STRUCTURES - How we organize our tentacle information
        // ==============================================================================
        // Just like with the gills, we need to keep track of our tentacles efficiently.
        // Arrays are perfect for this - they're like numbered boxes where we can
        // store our tentacle information in an organized way.
        // ==============================================================================
    
        // Each tentacle is a LineRenderer component that draws the flowing line
        private LineRenderer[] _tentacles;
    
        // For each tentacle, we need to track all the points that make it up
        // This is a 2D array - the first dimension is "which tentacle",
        // the second dimension is "which point along that tentacle"
        private Vector3[][] _tentaclePositions;
    
        // ==============================================================================
        // INITIALIZATION - Setting up our tentacle system
        // ==============================================================================

        private void Start()
        {
            // Create all our tentacles when the game starts
            // This is like setting up the stage before the performance begins!
            GenerateTentacles();
        }

        private void Update()
        {
            // Every frame, update the tentacle positions to create that flowing animation
            // This is our "animation loop" - the heartbeat of our tentacle system!
            UpdateTentaclePositions();
        }
    
        // ==============================================================================
        // TENTACLE GENERATION - Creating our underwater dancers
        // ==============================================================================
        // This is where we actually CREATE each tentacle. We're not just drawing them
        // once - we're setting up the entire system that will animate them frame by frame!
        // ==============================================================================

        private void GenerateTentacles()
        {
            // Initialize our arrays to hold all the tentacle data
            // Think of this as setting up our "filing system" for tentacle information
            _tentacles = new LineRenderer[tentacleCount];
            _tentaclePositions = new Vector3[tentacleCount][];
        
            // Create each tentacle one by one
            // This is a classic "for loop" - we're doing the same operation multiple times
            for (int i = 0; i < tentacleCount; i++)
            {
                // Create a new GameObject for this tentacle
                // Each tentacle is its own separate object in the scene hierarchy
                GameObject tentacleObj = new GameObject($"Tentacle_{i}");
                tentacleObj.transform.SetParent(transform);
            
                // Add and configure LineRenderer
                // The LineRenderer is Unity's built-in component for drawing lines
                // It's perfect for our flowing tentacle effect!
                LineRenderer lr = tentacleObj.AddComponent<LineRenderer>();
                lr.material = tentacleMaterial;
                lr.startWidth = startWidth;
                lr.endWidth = endWidth;
                lr.positionCount = tentacleSegments;
                lr.useWorldSpace = true;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            
                // Store references for later use
                _tentacles[i] = lr;
                _tentaclePositions[i] = new Vector3[tentacleSegments];
            }
        }
    
        // ==============================================================================
        // REAL-TIME ANIMATION - The flowing dance of the tentacles
        // ==============================================================================
        // This is where the MAGIC happens! Every frame, we calculate new positions
        // for every point on every tentacle. It's like being a choreographer for
        // hundreds of dancers, all moving in perfect harmony!
        // ==============================================================================

        private void UpdateTentaclePositions()
        {
            // Safety check - make sure we have everything we need
            if (bellScript == null) return;
        
            // Get the current bottom ring positions from the bell
            // This is our "attachment points" - where each tentacle connects to the bell
            Vector3[] rimPositions = bellScript.GetBottomRingPositions();
        
            // Update each tentacle individually
            // This is the main loop that brings our tentacles to life!
            for (int tentacleIndex = 0; tentacleIndex < tentacleCount; tentacleIndex++)
            {
                // ==============================================================================
                // DEBUGGING SECTION - Understanding what's happening
                // ==============================================================================
                // Sometimes we need to peek under the hood to see what's going on.
                // This debug code helps us understand where our tentacles are attaching!
                // ==============================================================================
            
                // In UpdateTentaclePositions(), add right after getting rimPositions:
                if (tentacleIndex == 0) // Only debug first tentacle to avoid spam
                {
                    //Debug.Log($"Rim position 0 (local): {rimPositions[0]}");
                    //Debug.Log($"Bell transform position: {bellScript.transform.position}");
                    //Debug.Log($"Attach point (world): {bellScript.transform.TransformPoint(rimPositions[0])}");
                }

                // ==============================================================================
                // ATTACHMENT POINT CALCULATION - Where does this tentacle connect?
                // ==============================================================================
                // We need to figure out which point on the bell's rim this tentacle
                // should attach to. This is like deciding where to hang each strand
                // of a chandelier!
                // ==============================================================================
            
                // Calculate which rim position this tentacle attaches to
                // We're spreading our tentacles evenly around the bell's circumference
                float rimIndexFloat = (float)tentacleIndex / tentacleCount * rimPositions.Length;
                int rimIndex = Mathf.FloorToInt(rimIndexFloat) % (rimPositions.Length - 1);

                // Scale the rim position inward before transforming
                // This pulls the attachment point slightly toward the center
                Vector3 scaledRimPosition = rimPositions[rimIndex] * 0.5f; // 90% of original radius
                Vector3 attachPoint = bellScript.transform.TransformPoint(scaledRimPosition);

                // Moves the attachment point up slightly to get under the bell surface
                // This is a visual adjustment to make the connection look more natural
                attachPoint.y += 0.1f; // Adjust this value as needed

                // ==============================================================================
                // TENTACLE SEGMENT GENERATION - Creating the flowing curve
                // ==============================================================================
                // Now we generate each point along this tentacle, from the attachment
                // point all the way down to the tip. This is where we create that
                // beautiful, organic flowing motion!
                // ==============================================================================

                // Generate positions along the tentacle
                for (int segmentIndex = 0; segmentIndex < tentacleSegments; segmentIndex++)
                {
                    // Calculate how far along this tentacle we are (0 to 1)
                    // t = 0 means we're at the attachment point
                    // t = 1 means we're at the very tip
                    float t = (float)segmentIndex / (tentacleSegments - 1);

                    // Create a more natural hanging curve (parabolic instead of straight)
                    // This makes the tentacle "droop" more naturally, like real tentacles do!
                    float hangCurve = t * t * 0.5f; // Quadratic curve for more natural droop

                    // Basic hanging position
                    // Start at the attachment point and move downward
                    float x = attachPoint.x;
                    float z = attachPoint.z;
                    float y = attachPoint.y - t * tentacleLength;

                    // Add horizontal drift based on the curve
                    // This gives each tentacle its own "personality" and direction
                    Vector3 tentacleDirection = new Vector3(
                        Mathf.Sin(tentacleIndex * 0.7f), // Slight radial spread
                        0,
                        Mathf.Cos(tentacleIndex * 0.7f)
                    );

                    // ==============================================================================
                    // ANIMATION MAGIC - Adding life and movement
                    // ==============================================================================
                    // This is where we add the flowing, organic movement that makes our
                    // tentacles feel alive! We're using sine waves and noise to create
                    // that mesmerizing underwater dance.
                    // ==============================================================================

                    // Add some sway animation
                    // This creates the primary "back and forth" motion
                    float swayTime = Time.time * swaySpeed + tentacleIndex * 0.5f;
                    float swayOffset = Mathf.Sin(swayTime) * swayAmount * t * t; // More sway at the tip, quadratic falloff

                    // Add some noise for organic movement
                    // Perlin noise gives us smooth, natural-looking randomness
                    float noiseX = Mathf.PerlinNoise(Time.time * 0.3f + tentacleIndex, 0) * noiseScale * t;
                    float noiseZ = Mathf.PerlinNoise(Time.time * 0.3f + tentacleIndex, 100) * noiseScale * t;

                    // Combine everything
                    // This is where all our different movement components come together!
                    Vector3 swayDirection = new Vector3(
                        Mathf.Sin(swayTime + tentacleIndex),
                        0,
                        Mathf.Cos(swayTime + tentacleIndex)
                    );

                    // The final position combines ALL our movement components:
                    // - Basic hanging position
                    // - Natural droop curve
                    // - Directional spread
                    // - Rhythmic swaying
                    // - Organic noise
                    _tentaclePositions[tentacleIndex][segmentIndex] = new Vector3(
                        x + tentacleDirection.x * hangCurve + swayDirection.x * swayOffset + noiseX,
                        y,
                        z + tentacleDirection.z * hangCurve + swayDirection.z * swayOffset + noiseZ
                    );
                }

                // Update the LineRenderer with our calculated positions
                // This is what actually makes the tentacle visible in the scene!
                _tentacles[tentacleIndex].SetPositions(_tentaclePositions[tentacleIndex]);
            }
        }
    
        // ==============================================================================
        // EDITOR SUPPORT - Making development easier
        // ==============================================================================
        // This method is called when we change values in the Inspector while the
        // game is running. It's like having a "live preview" of our changes!
        // ==============================================================================

        private void OnValidate()
        {
            // Update tentacle count at runtime
            // If we change the number of tentacles, we need to regenerate the whole system
            if (Application.isPlaying && _tentacles != null && _tentacles.Length != tentacleCount)
            {
                // Clean up old tentacles
                // It's SUPER important to clean up GameObjects we create at runtime!
                for (int i = 0; i < _tentacles.Length; i++)
                {
                    if (_tentacles[i] != null)
                        DestroyImmediate(_tentacles[i].gameObject);
                }
            
                // Generate new tentacles with the updated count
                GenerateTentacles();
            }
        }
    }
}