using System.Collections.Generic;
using UnityEngine;

// ==============================================================================
// JELLYFISH GILL SYSTEM - THE NERVOUS SYSTEM
// ==============================================================================
// Welcome to the gill system! This is a perfect example of MODULAR DESIGN.
// Instead of cramming everything into one massive script, we've created a 
// specialized component that does ONE thing really well: create glowing gill
// lines that follow the contours of a jellyfish bell.
//
// The beautiful thing about this approach is COMPOSITION over inheritance.
// Any object with a JellyfishBell can have gills just by adding this component!
// It's like building with LEGO blocks - each piece has a specific function.
// ==============================================================================

[RequireComponent(typeof(JellyfishBell))]
public class JellyfishGill : MonoBehaviour
{
    [Header("Gill Configuration")]
    // How many radial segments to skip between each gill
    // gillSpacing = 2 means every other segment gets a gill
    // gillSpacing = 4 means every fourth segment gets a gill
    public int gillSpacing = 2;
    
    [Header("Visual Properties")]
    // The material that controls how our gills look and behave
    // This could be a simple colored material, or something fancy like
    // a reactive material that responds to audio input!
    public Material gillMaterial;
    
    // How thick should our gill lines be?
    public float gillWidth = 0.02f;
    
    // How far should the gills float above the bell surface?
    // This prevents z-fighting (that ugly flickering when two surfaces overlap)
    public float gillOffset = 0.05f;
    
    // ==============================================================================
    // THE DATA STRUCTURES - How we organize our gill information
    // ==============================================================================
    // Think of these as our "filing system" for keeping track of all the gills
    // ==============================================================================
    
    // Reference to the bell we're attached to
    private JellyfishBell bellReference;
    
    // Each gill is a LineRenderer component that draws a curved line
    private List<LineRenderer> gillLines;
    
    // For each gill, we need to know which vertices to follow
    // This is an array of arrays - each inner array contains the vertex indices
    // for one gill line (from top of bell to bottom)
    private List<int[]> gillColumnIndices;

    // ==============================================================================
    // INITIALIZATION - Setting up our gill system
    // ==============================================================================
    
    void Start()
    {
        // Get a reference to the bell we're attached to
        // This is our "data source" - we'll be reading vertex positions from it
        bellReference = GetComponent<JellyfishBell>();
        
        if (bellReference == null)
        {
            Debug.LogError("JellyfishGill requires a JellyfishBell component!");
            return;
        }
        
        // Build our gill system!
        InitializeGillSystem();
    }

    void Update()
    {
        // Every frame, update our gill positions to match the bell's current shape
        // Since the bell is constantly pulsing and changing, our gills need to
        // follow along to maintain that organic, connected feeling
        UpdateGillPositions();
    }

    // ==============================================================================
    // GILL SYSTEM INITIALIZATION - The planning phase
    // ==============================================================================
    // This is where we decide which radial segments will have gills and set up
    // all the data structures we need to track them efficiently.
    // ==============================================================================
    
    void InitializeGillSystem()
    {
        // First, figure out which radial segments should have gills
        // We're essentially creating a "selection pattern" around the bell
        List<int> selectedRadialIndices = SelectGillPositions();
        
        // Initialize our storage collections
        gillLines = new List<LineRenderer>();
        gillColumnIndices = new List<int[]>();
        
        // Create a gill line for each selected position
        for (int i = 0; i < selectedRadialIndices.Count; i++)
        {
            CreateSingleGill(selectedRadialIndices[i], i);
        }
        
        //Debug.Log($"Created {gillLines.Count} gill lines around the jellyfish bell");
    }
    
    List<int> SelectGillPositions()
    {
        // This method decides which radial segments get gills
        // Think of it like deciding where to place the meridian lines on a globe
        
        List<int> positions = new List<int>();
        
        // Start at 0 and jump by gillSpacing each time
        // This creates an even distribution around the bell
        for (int r = 0; r < bellReference.radialSegments; r += gillSpacing)
        {
            positions.Add(r);
        }
        
        return positions;
    }

    // ==============================================================================
    // INDIVIDUAL GILL CREATION - Building each gill line
    // ==============================================================================
    // Each gill is its own GameObject with a LineRenderer component.
    // This gives us maximum flexibility - each gill could potentially have
    // different properties, animations, or behaviors if we wanted!
    // ==============================================================================
    
    void CreateSingleGill(int radialIndex, int gillIndex)
    {
        // Create a new GameObject to hold this gill
        // Naming it clearly helps with debugging and scene organization
        GameObject gillObject = new GameObject($"Gill_{gillIndex}_Radial_{radialIndex}");
        gillObject.transform.SetParent(transform);
        gillObject.transform.localPosition = Vector3.zero;
        
        // Add the LineRenderer component - this is what actually draws the line
        LineRenderer lineRenderer = gillObject.AddComponent<LineRenderer>();
        
        // Configure the visual properties
        SetupLineRendererProperties(lineRenderer);
        
        // Calculate which vertices this gill should follow
        // This is the "secret sauce" - we're creating a column of vertex indices
        // that represents a vertical slice through the bell mesh
        int[] columnIndices = CalculateColumnIndices(radialIndex);
        
        // Set up the LineRenderer to have the right number of points
        lineRenderer.positionCount = columnIndices.Length;
        
        // Store everything for later updates
        gillLines.Add(lineRenderer);
        gillColumnIndices.Add(columnIndices);
    }
    
    void SetupLineRendererProperties(LineRenderer lineRenderer)
    {
        // Use sharedMaterial so all gills share the same material instance
        // This is crucial for materials that change properties (like audio-reactive ones)
        lineRenderer.material = gillMaterial;
        
        // Set the line width - same thickness from start to end for now
        lineRenderer.startWidth = gillWidth;
        lineRenderer.endWidth = gillWidth;
        
        // Use local space coordinates relative to our jellyfish
        // This means if the whole jellyfish moves, the gills move with it
        lineRenderer.useWorldSpace = false;
        
        // Optional: disable shadows and light receiving for performance
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }
    
    int[] CalculateColumnIndices(int radialIndex)
    {
        // This is the mathematical heart of our gill system!
        // We need to find all the vertex indices that form a vertical column
        // at the specified radial position.
        
        // Each height level has (radialSegments + 1) vertices
        // So to find the vertex at height H and radial position R:
        // index = H * (radialSegments + 1) + R
        
        int[] indices = new int[bellReference.heightSegments + 1];
        
        for (int h = 0; h <= bellReference.heightSegments; h++)
        {
            indices[h] = bellReference.GetVertexIndex(h, radialIndex);
        }
        
        return indices;
    }

    // ==============================================================================
    // REAL-TIME UPDATES - The animation loop
    // ==============================================================================
    // This runs every frame and is responsible for keeping our gills in sync
    // with the pulsing bell. It's like a dance where the bell leads and the
    // gills follow!
    // ==============================================================================
    
    void UpdateGillPositions()
    {
        if (gillLines == null || bellReference == null) return;
        
        // Update each gill line
        for (int gillIndex = 0; gillIndex < gillLines.Count; gillIndex++)
        {
            UpdateSingleGill(gillIndex);
        }
    }
    
    void UpdateSingleGill(int gillIndex)
    {
        LineRenderer lineRenderer = gillLines[gillIndex];
        int[] columnIndices = gillColumnIndices[gillIndex];
        
        // Update each point along this gill line
        for (int pointIndex = 0; pointIndex < columnIndices.Length; pointIndex++)
        {
            // Get the current position of this vertex from the bell
            Vector3 bellPosition = bellReference.GetVertexPosition(columnIndices[pointIndex]);
            
            // Convert from world space back to local space (since we're using local space)
            Vector3 localPosition = transform.InverseTransformPoint(bellPosition);
            
            // Push the gill point slightly outward to avoid z-fighting
            // We calculate the normal by getting the direction from center to vertex
            Vector3 normal = localPosition.normalized;
            Vector3 gillPosition = localPosition + (normal * gillOffset);
            
            // Set this position on our LineRenderer
            lineRenderer.SetPosition(pointIndex, gillPosition);
        }
    }

    // ==============================================================================
    // PUBLIC INTERFACE - Methods other scripts might want to use
    // ==============================================================================
    
    // Get the number of gills we've created
    public int GetGillCount()
    {
        return gillLines != null ? gillLines.Count : 0;
    }
    
    // Get a specific gill's LineRenderer (useful for individual gill effects)
    public LineRenderer GetGill(int index)
    {
        if (gillLines != null && index >= 0 && index < gillLines.Count)
        {
            return gillLines[index];
        }
        return null;
    }
    
    // Regenerate the entire gill system (useful when bell parameters change)
    public void RegenerateGills()
    {
        CleanupGills();
        InitializeGillSystem();
    }

    // ==============================================================================
    // CLEANUP AND MEMORY MANAGEMENT
    // ==============================================================================
    // It's super important to clean up GameObjects we create at runtime!
    // Memory leaks are the enemy of smooth performance.
    // ==============================================================================
    
    void CleanupGills()
    {
        if (gillLines != null)
        {
            // Destroy each gill GameObject
            foreach (LineRenderer lr in gillLines)
            {
                if (lr != null && lr.gameObject != null)
                {
                    if (Application.isPlaying)
                        Destroy(lr.gameObject);
                    else
                        DestroyImmediate(lr.gameObject);
                }
            }
            
            // Clear our collections
            gillLines.Clear();
            gillColumnIndices.Clear();
        }
    }
    
    void OnDestroy()
    {
        // Always clean up when this component is destroyed
        CleanupGills();
    }
    
    // ==============================================================================
    // EDITOR SUPPORT
    // ==============================================================================
    
    void OnValidate()
    {
        // When parameters change in the editor, regenerate the gills
        if (Application.isPlaying && gillLines != null)
        {
            RegenerateGills();
        }
    }
    
    // ==============================================================================
    // DEBUGGING AND VISUALIZATION
    // ==============================================================================
    
    void OnDrawGizmosSelected()
    {
        // Draw debug information when this object is selected
        if (bellReference == null) return;
        
        // Draw small spheres at gill attachment points
        Gizmos.color = Color.cyan;
        for (int r = 0; r < bellReference.radialSegments; r += gillSpacing)
        {
            Vector3 bottomPos = bellReference.GetVertexPosition(
                bellReference.GetVertexIndex(bellReference.heightSegments, r)
            );
            Gizmos.DrawSphere(bottomPos, 0.05f);
        }
    }
}