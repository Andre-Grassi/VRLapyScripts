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


using PathCreation;
using UnityEngine;

/// <summary>
/// Moves a ball along a path at constant speed and swings up and down.
/// Depending on the end of path instruction, will either loop, reverse, or
/// stop at the end of the path.
/// The swing motion is a customizable sine wave.
/// </summary>
public class BallMovement : MonoBehaviour
{
    /*
     * Path Following variables
     */

    public PathCreator PathCreator;
    public EndOfPathInstruction EndOfPathInstruction;
    public bool FollowPath = true;
    public float PathFollowingSpeed = 1f;

    // Chance to reverse the direction of the ball
    // It's recommended to keep this value low, since this will be tested every
    // frame
    [Range(0f, 1f)]
    public float ChanceToReverse = 0.01f;

    private float _distanceTravelled;

    /*
     * Move up and down variables
     */

    // Period of the sinusoidal movement
    public float Period = 0.5f;

    // Amplitude of the movement
    [Range(0f, 1f)]
    public float Amplitude = 0.05f;

    // Increment for the sin function in the x axis
    private float _sinIncrement = 0f;

    // y position which the ball will swing around
    private float _referenceY;

    void Start()
    {
        if (PathCreator != null)
        {
            // Subscribed to the pathUpdated event so that we're notified if the path changes during the game
            PathCreator.pathUpdated += OnPathChanged;

            // Get the distance travelled so far.
            // Actually the ball cannot have travelled considering that this
            // is the Start of the script, but considering that the ball can
            // be instantiated at any point of the path, we need to set this
            // variable so that the ball stay on its starting position when
            // the Follow function is called.
            _distanceTravelled = PathCreator.path.GetClosestDistanceAlongPath(transform.position);
        }
        _referenceY = transform.position.y; // Used in case the PathCreator is null
    }

    public void Update()
    {
        if (PathCreator != null && FollowPath == true)
            Follow();

        if (PathCreator != null)
            // Get Y of the current point
            _referenceY = PathCreator.path
                .GetPointAtDistance(_distanceTravelled, EndOfPathInstruction)
                .y;

        // Else the _referenceY is the initial Y of the ball

        MoveUpDown(_referenceY);
    }

    public void Follow()
    {
        // Randomize direction that the ball moves considering the chanceToReverse
        PathFollowingSpeed *= Random.value <= ChanceToReverse ? -1 : 1;

        // Move ball along the path
        _distanceTravelled += PathFollowingSpeed * Time.deltaTime;
        gameObject.transform.position = PathCreator.path.GetPointAtDistance(
            _distanceTravelled,
            EndOfPathInstruction
        );
    }

    // If the path changes during the game, update the distance travelled so that the follower's position on the new path
    // is as close as possible to its position on the old path
    void OnPathChanged() =>
        _distanceTravelled = PathCreator.path.GetClosestDistanceAlongPath(transform.position);

    // y is the y position which the ball will swing around
    public void MoveUpDown(float y)
    {
        // Check to avoid division by 0 when calculating the frequency
        if (Period == 0)
            return;

        _sinIncrement += Time.deltaTime;
        float frequency = 1f / Period;

        float newY = y + Mathf.Sin(_sinIncrement * frequency) * Amplitude;

        gameObject.transform.position = new Vector3(
            gameObject.transform.position.x,
            newY,
            gameObject.transform.position.z
        );
    }
}
