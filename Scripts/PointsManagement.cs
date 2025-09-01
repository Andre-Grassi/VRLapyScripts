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


using TMPro;
using UnityEngine;

/// <summary>
/// Class responsible for managing points and errors of the player.
/// Can display the data on the given TextMeshPro Game Objects.
/// Can be attached to a empty GameObject and be referenced by other scripts
/// that need to get the points or errors data.
/// </summary>
public class PointsManagement : MonoBehaviour
{
    public TextMeshPro PointsText; // TextMeshPro Game Object to display points
    public TextMeshPro ErrorsText; // TextMeshPro to display number of errors
    public TextMeshPro HealthText; // TextMeshPro to display health

    private int _points = 0;
    private int _errors = 0;

    /// <summary>
    /// Indicates the amount of aid the player has.
    /// This is used to avoid resetting the level when player hits a skull.
    /// The level is resetted only when the health is -1, indicating that
    /// the player had no aid (health = 0) and hit a skull.
    /// </summary>
    public int Health { get; }
    private int _health = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PointsText != null)
            PointsText.text = "0";

        if (ErrorsText != null)
            ErrorsText.text = "0";

        if (HealthText != null)
            HealthText.text = "0";
    }

    // Update is called once per frame
    void Update() { }

    public void IncrementError()
    {
        _errors++;
        if (ErrorsText != null)
            ErrorsText.text = _errors.ToString();
    }

    public void IncrementPoints()
    {
        _points++;
        if (PointsText != null)
            PointsText.text = _points.ToString();
    }

    public void IncrementHealth()
    {
        _health++;
        if (HealthText != null)
            HealthText.text = _health.ToString();
    }

    public void DecrementHealth()
    {
        _health--;
        // TODO create static function to display health???
        if (HealthText != null)
            HealthText.text = _health.ToString();
    }
}
