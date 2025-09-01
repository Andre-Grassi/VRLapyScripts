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
using UnityEngine.Events;

public enum ViewType
{
    Laparoscope,
    Overview,
}

[System.Serializable]
public class TutorialLvlData
{
    public BallData[] BallDatas; // Array of ball objects
    public PathCreator PathCreator;

    public bool FollowPath; // Indicates if the balls should follow the path
    public float PathFollowingSpeed; // Speed of the balls

    [Range(0f, 1f)]
    public float ChanceToReverse; // Chance to reverse the direction of the ball
    public float Period; // Period of the sinusoidal movement

    [Range(0f, 1f)]
    public float Amplitude; // Amplitude of the movement

    [Tooltip(
        "Enable lights of the interior of the abdomen to be active.\n"
            + "This way the interior is completely visible."
    )]
    public bool EnableLights;

    public ViewType View;
}

[System.Serializable]
public class TutorialLevel : GameLevel<TutorialLvlData>
{
    public PointsManagement PointsManager;
    public UnityEvent FinishedTutorial;

    private BallSpawner _ballSpawner;

    public override void InitializeLevel(int levelId)
    {
        Debug.Log("Initializing TutorialLevel with ID: " + levelId);
        if (levelId < 0 || levelId >= LevelsData.Length)
        {
            System.ArgumentOutOfRangeException exception = new System.ArgumentOutOfRangeException(
                nameof(levelId),
                $"Level ID {levelId} is out of range. "
                    + $"Valid range is 0 to {LevelsData.Length - 1}."
            );
            throw exception;
        }

        // Get the data for the given tutorial level
        TutorialLvlData levelData = LevelsData[levelId];

        // Initialize the data of this level in the BallSpawner script
        _ballSpawner.BallDatas = levelData.BallDatas;
        _ballSpawner.PathCreator = levelData.PathCreator;
        _ballSpawner.FollowPath = levelData.FollowPath;
        _ballSpawner.PathFollowingSpeed = levelData.PathFollowingSpeed;
        _ballSpawner.ChanceToReverse = levelData.ChanceToReverse;
        _ballSpawner.Period = levelData.Period;
        _ballSpawner.Amplitude = levelData.Amplitude;

        // Spawn the balls
        _ballSpawner.SpawnBalls();
    }

    public override void AdvanceLevel()
    {
        // Logic to advance to the next level
        CurrentLevel++;

        // Check if there are more levels to play and initialize next level
        if (CurrentLevel < LevelsData.Length)
            InitializeLevel(CurrentLevel);
        else
        {
            FinishedTutorial?.Invoke();
        }
    }

    public override void ResetLevel()
    {
        InitializeLevel(CurrentLevel);
    }

    void Awake()
    {
        _ballSpawner = gameObject.AddComponent<BallSpawner>();

        if (_ballSpawner == null)
            throw new System.Exception(
                "Couldn't add BallSpawner component to " + "TutorialLevel game object"
            );

        if (PointsManager == null)
            throw new System.Exception("PointsManager is not set in TutorialLevel");

        _ballSpawner.PointsManagement = PointsManager;

        // Subscribe to the Ball Spawner events
        _ballSpawner.BallsCleared += AdvanceLevel;
        _ballSpawner.ResettingLevel += ResetLevel;
    }

    private void Start()
    {
        InitializeLevel(0);
    }
}
