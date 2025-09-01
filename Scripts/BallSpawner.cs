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
using System.Linq;
using PathCreation;
using UnityEngine;

/// <summary>
/// Struct to store the ball prefab and the quantity of balls to spawn
/// </summary>
[System.Serializable]
public struct BallData
{
    // Remember to get the prefab from the assets and not the current scene to
    // avoid the sample being destroyed and the prefab being lost
    public GameObject ballPrefab;

    // TODO: change to unsigned int
    public int quantity;
}

/// <summary>
/// Class that spawn a list of balls on a given path (created with the
/// PathCreator asset).
/// The balls spawned contains the scripts that make them fully functional.
/// </summary>
public class BallSpawner : MonoBehaviour
{
    // Event that fires when all balls spawned by this BallSpawner are
    // destroyed. It's used by the LevelManagement script to advance to the
    // next level.
    public event System.Action BallsCleared;

    // The below events are explained in BallDestroy class
    public event System.Action ResettingLevel;
    public event System.Action IncreasingHealth;

    public BallData[] BallDatas; // Array of ball objects
    public PathCreator PathCreator;
    public PointsManagement PointsManagement;

    public bool FollowPath = true; // Indicates if the balls should

    // follow the path
    public float PathFollowingSpeed = 1f; // Speed of the balls

    // Chance to reverse the direction of the ball
    // It's recommended to keep this value low, since this will be tested every
    // frame
    [Range(0f, 1f)]
    public float ChanceToReverse = 0.01f;

    // Period of the sinusoidal movement
    public float Period = 0.5f;

    // Amplitude of the movement
    [Range(0f, 1f)]
    public float Amplitude = 0.05f;

    private int _numPoints;
    private List<GameObject> _balls;

    public void SpawnBalls()
    {
        if (PathCreator == null)
            throw new System.Exception("PathCreator is not set in the BallSpawner");

        // Initialize the list of balls
        _balls = new List<GameObject>();

        // Get the number of points in the path
        _numPoints = PathCreator.path.NumPoints;

        // List of indexes of available points in the path
        List<int> availablePoints = Enumerable.Range(0, _numPoints - 1).ToList();

        foreach (BallData ballData in BallDatas)
        {
            int j = 0;
            while (j < ballData.quantity)
            {
                // Check if there are still available points in the path
                if (availablePoints.Count == 0)
                {
                    Debug.LogWarning("Not enough points in the path to spawn all balls");
                    return;
                }

                // Get a random available point from the path
                int pointIndex = Random.Range(0, availablePoints.Count - 1);

                // Instantiate the ball at the selected point
                Vector3 position = PathCreator.path.GetPoint(availablePoints[pointIndex]);
                Quaternion rotation = Quaternion.Euler(-90, 0, 0);
                GameObject newBall = Instantiate(ballData.ballPrefab, position, rotation);

                // Check if this ball is colliding with another ball.
                // If it is, destroy it and try to instantiate the same ball
                // again.
                // If a collision occur and there are no more available points,
                // the number of balls that will be spawned will be less than
                // the quantity asked for.
                bool isBallColliding = CheckCollisionBalls(newBall);

                // If the collision didn't occur, initialize the ball
                if (isBallColliding == false)
                {
                    // Add the ball to the list
                    _balls.Add(newBall);

                    // Set BallMovement script variables
                    BallMovement ballMovement = newBall.GetComponent<BallMovement>();
                    ballMovement.ChanceToReverse = ChanceToReverse;
                    ballMovement.PathCreator = PathCreator;
                    ballMovement.FollowPath = FollowPath;
                    ballMovement.PathFollowingSpeed = PathFollowingSpeed;
                    ballMovement.Period = Period;
                    ballMovement.Amplitude = Amplitude;

                    // Subscribe to the custom events of the BallDestroy script
                    BallDestroy ballDestroy = newBall.GetComponent<BallDestroy>();
                    ballDestroy.RemovingBall += RemoveBall;
                    ballDestroy.ResettingLevel += ResetLevel;
                    ballDestroy.IncreasingHealth += IncreaseHealth;

                    // Set the PointsManagement field of the BallDestroy script
                    ballDestroy.PointsManagement = PointsManagement;

                    // Advance to instantiate another ball
                    j++;
                }
                else
                    // Destroy the ball to position it in another point
                    Destroy(newBall);

                // Remove point from the available list so it's not a possible
                // spawn point anymore
                availablePoints.RemoveAt(pointIndex);
            }
        }
    }

    // Check if the ball is colliding with another ball from the balls list
    // The ball must not be on the list when calling this function
    private bool CheckCollisionBalls(GameObject ball)
    {
        Collider currentCollider = ball.GetComponent<Collider>();
        foreach (GameObject otherBall in _balls)
            if (
                currentCollider.bounds.Intersects(otherBall.GetComponent<Collider>().bounds) == true
            )
                return true;

        return false;
    }

    private void RemoveBall(GameObject destroyedBall)
    {
        _balls.Remove(destroyedBall);

        bool cleared = true;
        // Check if all normal balls have been cleared (Left, Right and Both
        // ball types)
        foreach (GameObject ball in _balls)
        {
            BallType type = ball.GetComponent<BallDestroy>().Type;
            if (type == BallType.Both || type == BallType.Left || type == BallType.Right)
                cleared = false;
        }

        // If all balls are destroyed invoke the BallsCleared event
        if (cleared == true)
        {
            // Destroy special balls if there are any
            DestroyAndClearBalls();

            BallsCleared?.Invoke();
        }
    }

    /// <summary>
    /// Destroy all balls from the _balls list and clear the reference to
    /// those game objects
    /// </summary>
    private void DestroyAndClearBalls()
    {
        // Delete all balls
        foreach (GameObject ball in _balls)
            Destroy(ball);

        // Clear the list of balls
        _balls.Clear();
    }

    private void ResetLevel()
    {
        // Delete all balls to reinstatiate them later
        DestroyAndClearBalls();

        ResettingLevel?.Invoke();
    }

    private void IncreaseHealth()
    {
        //TODO
    }
}
