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


using System.Collections.Generic;
using Obi;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Detect and "cut" stitches in an ObiStitcher when activated.
/// The cutting is performed by removing the stitch that is closest to the
/// center of the cutting collider.
/// </summary>
/// <remarks>Search for ObiStitcher in https://obi.virtualmethodstudio.com/api.html</remarks>
[RequireComponent(typeof(Collider))]
public class ObiContinuousStitchCutter : ObiStitchCutter
{
    /// <summary>
    /// Continuously attempts to cut stitches while the cutting tool is active.
    /// Iterates through all stitches in the target stitcher, finds the first stitch
    /// whose closest point to the cutting collider is within the collider bounds,
    /// removes it, and immediately returns. This allows for real-time, continuous
    /// cutting of individual stitches one at a time during each FixedUpdate cycle.
    /// </summary>
    protected override void Cut()
    {
        if (
            isCutting == false || targetStitcher == null || solver.renderablePositions.capacity == 0
        )
            return;

        // Iterate over all stitches in the target stitcher component.
        int stitchIndex = 0;
        foreach (var stitch in targetStitcher.Stitches)
        {
            // Get the position of the particles connected by the stitch.
            Vector3 posParticle1 = solver.renderablePositions[stitch.particleIndex1];
            Vector3 posParticle2 = solver.renderablePositions[stitch.particleIndex2];

            // Position of the center of the cutting collider.
            Vector3 colliderCenter = _cutCollider.bounds.center;

            Vector3 closestPoint = ClosestPointToLineSegment(
                colliderCenter,
                posParticle1,
                posParticle2
            );

            if (_cutCollider.bounds.Contains(closestPoint))
            {
                targetStitcher.RemoveStitch(stitchIndex);

                if (onStitchCut != null)
                    onStitchCut.Invoke(targetStitcher);

                // Update Solver to reflect the changes.
                targetStitcher.PushDataToSolver();

                return;
            }

            stitchIndex++;
        }
    }
}
