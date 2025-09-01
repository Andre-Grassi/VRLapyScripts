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


using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Types of balls
// They indicate which hand can destroy them
// BUG: Skull and Health does not indicate a hand!!!!
// Maybe create BallType and BallHand enums????
public enum BallType
{
    Left,
    Right,
    Both,
    Skull,
    Health,
    None,
}

/// <summary>
/// Class that manages the destruction of the balls, how and what should happen
/// when it is destroyed considering the types defined by the enum BallType.
/// </summary>
public class BallDestroy : MonoBehaviour
{
    /// <summary>
    /// Custom Event that fires when the ball will be destroyed.
    /// It passes the GameObject as a parameter.
    /// It is used to remove the ball from the list of balls in the BallSpawner.
    /// If the ball that contains this script is not being spawned by a
    /// BallSpawner, then this event can be ignored.
    /// </summary>
    public event System.Action<GameObject> RemovingBall;

    /// <summary>
    /// Custom Event that fires when the ball is destroyed and it is a Skull Ball.
    /// It is used to notify the BallSpawner and then the LevelManagement to
    /// restart the level.
    /// </summary>
    public event System.Action ResettingLevel;

    /// <summary>
    /// Custom Event that fires when the ball is destroyed and it is a Health
    /// Ball.
    /// It is used to notify the BallSpawner and then the LevelManagement to
    /// increase the health of the player.
    /// </summary>
    public event System.Action IncreasingHealth;

    public BallType Type = BallType.None;

    public PointsManagement PointsManagement; // Responsible for managing

    // points and errors
    // when incorrect instrument
    // collides with the ball and
    // when the ball is destroyed

    // Indicates if l-hand instrument has collided with the ball
    private bool _hasLeftCollided = false;

    // Indicates if r-hand instrument has collided with the ball
    private bool _hasRightCollided = false;

    void OnTriggerEnter(Collider other)
    {
        // Check if the ball collided with the tip of the Laparoscopy
        // instrument
        if (other.gameObject.CompareTag("LaparoscopyCollider"))
        {
            // Get the DetectHand class that is attached to the parent of the
            // tip collider of the LaparoscopyInstrument object
            DetectHand detectHand = other.GetComponentInParent<DetectHand>();

            if (detectHand != null)
            {
                // Detect which hand is grabbing the instrument that collided
                // with the ball
                if (detectHand.Handedness == InteractorHandedness.Left)
                    _hasLeftCollided = true;
                else if (detectHand.Handedness == InteractorHandedness.Right)
                    _hasRightCollided = true;

                // Store whether the hand that is grabbing the instrument
                // is the correct one to collide with the ball
                bool isCorrect =
                    (Type == BallType.Left && _hasLeftCollided)
                    || (Type == BallType.Right && _hasRightCollided)
                    || (Type == BallType.Both && _hasLeftCollided && _hasRightCollided);

                // Check if the collision was correct or the player collided
                // with a the Health Ball
                if ((isCorrect == true) || (Type == BallType.Health))
                {
                    Destroy(gameObject);

                    // Check if any function is subscribed to the event
                    // This prevents a null reference trying to call a delegate
                    // Then invoke
                    RemovingBall?.Invoke(gameObject);

                    if (Type == BallType.Health)
                        PointsManagement.IncrementHealth();

                    if (PointsManagement != null)
                        PointsManagement.IncrementPoints();
                }
                // In the else statement the player made a mistake
                else
                {
                    // If the player collided with the Skull, the level
                    // should be restarted by the delegate function, if it
                    // exists.
                    if (Type == BallType.Skull)
                    {
                        Destroy(gameObject);

                        if (PointsManagement != null)
                        {
                            PointsManagement.DecrementHealth();

                            // If the player has no aid left, the level should be
                            // restarted
                            if (PointsManagement.Health < 0)
                                ResettingLevel?.Invoke();
                        }
                    }

                    if (PointsManagement != null)
                        PointsManagement.IncrementError();
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the ball collided with the tip of the Laparoscopy
        // instrument
        if (other.gameObject.CompareTag("LaparoscopyCollider"))
        {
            // Get the DetectHand class that is attached to the parent of the
            // tip collider of the LaparoscopyInstrument object
            DetectHand detectHand = other.GetComponentInParent<DetectHand>();
            if (detectHand != null)
            {
                // Detect which hand is grabbing the instrument that collided
                // with the ball
                if (detectHand.Handedness == InteractorHandedness.Left)
                    _hasLeftCollided = false;
                else if (detectHand.Handedness == InteractorHandedness.Right)
                    _hasRightCollided = false;

                // If player was colliding with the both ball but one
                // instrument exited before the other entered, then it's an
                // error
                if (Type == BallType.Both)
                    if (PointsManagement != null)
                        PointsManagement.IncrementError();
            }
        }
    }

    private void OnDestroy()
    {
        // Bug: both ball incrementing twice
    }
}
