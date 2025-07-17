using System;
using UnityEngine;

// ==============================================================================
// JELLYFISH BELL - THE FOUNDATION
// ==============================================================================
// Hey there! Welcome to the beautiful world of procedural mesh generation!
// This script has ONE job: create and animate a pulsing jellyfish bell.
// Think of this as the "skeleton" that other systems can attach to.
// 
// The key insight here is SEPARATION OF CONCERNS - this script doesn't need 
// to know about gills, tentacles, or any other jellyfish parts. It just 
// creates a dome and makes it pulse. Clean and simple!
// ==============================================================================

public class JellyfishBell : MonoBehaviour
{
    [Header("Bell Shape Parameters")]
    // These control the basic geometry of our dome
    public float radius = 2f;           // How wide the bell is
    public float height = 1f;           // How tall the bell is
    public float heightScale = 2f;      // Extra scaling for dramatic effect
    public int radialSegments = 16;     // How many "slices" around the circle (like pizza slices!)
    public int heightSegments = 8;      // How many "rings" from top to bottom

    [Header("Pulsing Animation")]
    // These create that mesmerizing jellyfish breathing motion
    public float pulseSpeed = 2f;       // How fast the pulse travels
    public float pulseStrength = 0.3f;  // How dramatic the pulse is
    public float phaseShift = 2f;       // Creates the wave effect from top to bottom

    [Header("Bell Core")]
    // Optional glowing core in the center - like the jellyfish's brain!
    public bool showCore = false;
    public Material coreMaterial;
    private float coreSize = 0.8f;
    private GameObject coreObject;

    // ==============================================================================
    // THE MESH DATA - This is where we store our jellyfish's "skeleton"
    // ==============================================================================
    private Mesh mesh;
    private Vector3[] vertices;         // The actual points in 3D space
    private Vector3[] originalVertices; // The "rest" positions before animation
    private int[] triangles;            // How to connect the vertices into faces

    private Vector2[] uvs;

    void Start()
    {
        // Add some organic randomness to each jellyfish
        pulseSpeed = UnityEngine.Random.Range(1, 3);

        // Build our beautiful dome!
        GenerateBell();

  

        // Create the optional glowing core
        if (showCore)
        {
            CreateCore();
        }
    }

    void Update()
    {
        // Every frame, make our jellyfish breathe!
        // This is where the magic happens - we're constantly reshaping
        // our mesh to create that hypnotic pulsing motion
        AnimatePulse();
    }

    // ==============================================================================
    // MESH GENERATION - Creating the dome from scratch!
    // ==============================================================================
    // This is like being a digital sculptor - we're defining every point
    // and every triangle that makes up our jellyfish bell. It's procedural
    // generation at its finest!
    // ==============================================================================

    void GenerateBell()
    {
        // Create a fresh mesh and attach it to our MeshFilter
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Build the geometry step by step
        CreateVertices();  // Where should each point be?
        CreateTriangles(); // How do we connect those points?
        CreateUVs(); // Create UV coordinates

        // Tell Unity about our new mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals(); // This makes lighting work properly

        // IMPORTANT: Save the original positions for our animation system
        // Think of this as our "neutral pose" that we'll modify each frame
        originalVertices = new Vector3[vertices.Length];
        System.Array.Copy(vertices, originalVertices, vertices.Length);
    }

    void CreateVertices()
    {
        // Calculate how many vertices we need:
        // Each height ring has (radialSegments + 1) vertices
        // We have (heightSegments + 1) rings
        // Plus 1 center vertex at the bottom
        vertices = new Vector3[(radialSegments + 1) * (heightSegments + 1) + 1];

        int vertIndex = 0;

        // Create the dome vertices using spherical coordinates
        // This is like unwrapping a globe - we go ring by ring from top to bottom
        for (int h = 0; h <= heightSegments; h++)
        {
            // How far down the dome are we? (0 = top, 1 = bottom edge)
            float heightProgress = (float)h / heightSegments;

            // Convert to spherical coordinates - this creates the dome shape!
            // We only use half a sphere (0 to 90 degrees)
            float heightAngle = heightProgress * Mathf.PI * 0.5f;
            float currentRadius = Mathf.Sin(heightAngle) * radius;
            float currentHeight = Mathf.Cos(heightAngle) * height * heightScale;

            // For each position around the circle at this height
            for (int r = 0; r <= radialSegments; r++)
            {
                // Angle around the Y axis (0 to 360 degrees)
                float radialAngle = (float)r / radialSegments * Mathf.PI * 2f;

                // Convert polar coordinates to Cartesian (x, y, z)
                float x = Mathf.Cos(radialAngle) * currentRadius;
                float z = Mathf.Sin(radialAngle) * currentRadius;

                vertices[vertIndex] = new Vector3(x, currentHeight, z);
                vertIndex++;
            }
        }

        // Add the center bottom vertex (useful for closing the bell)
        vertices[vertIndex] = Vector3.zero;
    }

    void CreateTriangles()
    {
        int triangleCount = (radialSegments * heightSegments * 2) * 3;
        triangles = new int[triangleCount];

        int triIndex = 0;

        for (int h = 0; h < heightSegments; h++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                int current = h * (radialSegments + 1) + r;
                int next = current + radialSegments + 1;

                // FLIPPED: First triangle (try reversing the order)
                triangles[triIndex] = current;
                triangles[triIndex + 1] = current + 1;      // SWAPPED these two
                triangles[triIndex + 2] = next;             // SWAPPED these two

                // FLIPPED: Second triangle (try reversing the order)
                triangles[triIndex + 3] = current + 1;
                triangles[triIndex + 4] = next + 1;         // SWAPPED these two  
                triangles[triIndex + 5] = next;             // SWAPPED these two

                triIndex += 6;
            }
        }
    }
void CreateUVs()
{
    uvs = new Vector2[vertices.Length];
    int vertIndex = 0;

    for (int h = 0; h <= heightSegments; h++)
    {
        float v = (float)h / heightSegments;
        
        for (int r = 0; r <= radialSegments; r++)
        {
            float u = (float)r / radialSegments;
            uvs[vertIndex] = new Vector2(u, v);
            vertIndex++;
        }
    }
    
    // UV for center vertex
    uvs[vertIndex] = new Vector2(0.5f, 0.5f);
}
    // ==============================================================================
    // ANIMATION SYSTEM - Bringing our jellyfish to life!
    // ==============================================================================
    // This is where we create that beautiful, organic pulsing motion.
    // We're not just scaling the whole thing - we're creating a wave that
    // travels from the top to the bottom, making it feel truly alive!
    // ==============================================================================

    void AnimatePulse()
    {
        if (originalVertices == null) return;

        int vertIndex = 0;

        // Go through each ring of vertices and apply the pulse effect
        for (int h = 0; h <= heightSegments; h++)
        {
            // Create a phase offset for this height ring
            // This makes the pulse travel from top to bottom like a wave!
            float heightProgress = (float)h / heightSegments;
            float timeOffset = heightProgress * phaseShift;

            // Calculate how much this ring should pulse right now
            // The sin wave creates that smooth in-and-out breathing motion
            float pulseAmount = 1f + Mathf.Sin(Time.time * pulseSpeed + timeOffset) * pulseStrength;

            // Recalculate the ring's shape with the pulse applied
            float heightAngle = heightProgress * Mathf.PI * 0.5f;
            float baseRadius = Mathf.Sin(heightAngle) * radius;
            float currentHeight = Mathf.Cos(heightAngle) * height * heightScale;

            // Apply the pulse to each vertex in this ring
            for (int r = 0; r <= radialSegments; r++)
            {
                float radialAngle = (float)r / radialSegments * Mathf.PI * 2f;

                // HERE'S THE MAGIC: multiply the radius by our pulse amount!
                float currentRadius = baseRadius * pulseAmount;

                float x = Mathf.Cos(radialAngle) * currentRadius;
                float z = Mathf.Sin(radialAngle) * currentRadius;

                vertices[vertIndex] = new Vector3(x, currentHeight, z);
                vertIndex++;
            }
        }

        // Keep the center vertex still
        vertices[vertIndex] = Vector3.zero;

        // Update the mesh with our new vertex positions
        // This is what actually makes the animation visible!
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    // ==============================================================================
    // PUBLIC INTERFACE - How other scripts can talk to us
    // ==============================================================================
    // These methods are like "ports" that other systems can plug into.
    // The gill system, tentacles, or any other jellyfish part can use these
    // to get information about our current state.
    // ==============================================================================

    // Get the current position of any vertex (for gills, tentacles, etc.)
    public Vector3 GetVertexPosition(int index)
    {
        if (vertices != null && index >= 0 && index < vertices.Length)
        {
            return transform.TransformPoint(vertices[index]); // Convert to world space
        }
        return Vector3.zero;
    }

    // Get the total number of vertices (useful for bounds checking)
    public int GetVertexCount()
    {
        return vertices != null ? vertices.Length : 0;
    }

    // Get positions along the bottom edge (for tentacle attachment)
    public Vector3[] GetBottomRingPositions()
    {
        Vector3[] bottomPositions = new Vector3[radialSegments + 1];
        int startIndex = heightSegments * (radialSegments + 1);

        for (int i = 0; i <= radialSegments; i++)
        {
            bottomPositions[i] = vertices[startIndex + i];
        }

        return bottomPositions;
    }

    // Calculate which vertex index corresponds to a specific height and radial position
    // This is the "secret sauce" that other systems need to find specific vertices!
    public int GetVertexIndex(int heightSegment, int radialSegment)
    {
        if (heightSegment < 0 || heightSegment > heightSegments ||
            radialSegment < 0 || radialSegment > radialSegments)
        {
            return -1; // Invalid index
        }

        return heightSegment * (radialSegments + 1) + radialSegment;
    }

    // ==============================================================================
    // OPTIONAL CORE SYSTEM
    // ==============================================================================

    void CreateCore()
    {
        if (coreObject != null)
        {
            DestroyImmediate(coreObject);
        }

        coreObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coreObject.transform.SetParent(transform);
        coreObject.transform.localPosition = new Vector3(0, 0.8f, 0);
        coreObject.transform.localScale = Vector3.one * coreSize;
        coreObject.name = "BellCore";

        if (coreMaterial != null)
        {
            coreObject.GetComponent<MeshRenderer>().material = coreMaterial;
        }
    }






    // ==============================================================================
    // EDITOR UTILITIES
    // ==============================================================================

    void OnValidate()
    {
        // Regenerate the mesh when parameters change in the editor
        if (Application.isPlaying && mesh != null)
        {
            GenerateBell();
        }
        
        if (coreObject != null && showCore)
        {
            CreateCore();
        }
    }
}