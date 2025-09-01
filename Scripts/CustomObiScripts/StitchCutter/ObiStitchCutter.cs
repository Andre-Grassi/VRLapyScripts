/*
 * MIT License

 * Copyright (c) 2025 Andre Grassi de Jesus

 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */


using Obi;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detect and "cut" stitches in an ObiStitcher when activated.
/// The cutting is performed by removing the stitch that is closest to the
/// center of the cutting collider.
/// </summary>
/// <remarks>Search for ObiStitcher in https://obi.virtualmethodstudio.com/api.html</remarks>
[RequireComponent(typeof(Collider))]
public abstract class ObiStitchCutter : MonoBehaviour
{
    public ObiSolver solver;

    [Header("Cutting settings")]
    public ObiStitcher targetStitcher;

    [Tooltip("Toggle cutting. Use a script to control this, e.g., a VR controller button.")]
    public bool isCutting = false;

    public void SetCutting(bool cutting)
    {
        isCutting = cutting;
    }

    [Header("Feedback")]
    public UnityEvent<ObiStitcher> onStitchCut;

    protected Collider _cutCollider; // Collider of the cutting tool

    void Awake()
    {
        // Ensure the collider is trigger
        _cutCollider = GetComponent<Collider>();
        if (_cutCollider != null)
            _cutCollider.isTrigger = true;
    }

    void FixedUpdate()
    {
        Cut();
    }

    /// <summary>
    /// Function to cut stitches.
    /// </summary>
    // Inlining because this is the only thing called in FixedUpdate.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void Cut();

    /// <summary>
    /// Get the closest point on the line segment AB to the point P.
    /// </summary>
    /// <param name="p">Arbitraty point</param>
    /// <param name="a">Point at the start of the line segment</param>
    /// <param name="b">Point at the end of the line segment</param>
    /// <returns>Closest point to the line</returns>
    public Vector3 ClosestPointToLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        // Line segment vector from A to B.
        Vector3 ab = b - a;

        // Vector from A to P.
        Vector3 ap = p - a;

        // Square length of the segment. If it's zero, a and b are the same point.
        float abSqrMagnitude = ab.sqrMagnitude;

        float tolerance = 1e-6f; // Tolerance to avoid division by zero.
        if (abSqrMagnitude < tolerance)
        {
            return a; // Closest point == A == B
        }

        // Project the vector AP onto the vector AB to find how far along the segment
        // the projection of our point is. 't' is a value from 0 to 1.
        float t = Vector3.Dot(ap, ab) / abSqrMagnitude;

        Vector3 closestPoint;
        if (t < 0.0f)
            // The projection is "before" point A, so the closest point on the segment is A.
            closestPoint = a;
        else if (t > 1.0f)
            // The projection if "after" point B, so the closest point on the segment is B.
            closestPoint = b;
        else
            // The projection is between A and B, so the closest point is on the middle of the segment.
            closestPoint = a + ab * t;

        return closestPoint;
    }
}
