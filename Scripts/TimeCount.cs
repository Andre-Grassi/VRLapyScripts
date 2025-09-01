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
/// Class to count time elapsed since the start of the game and display it on
/// a TextMeshPro Game Object
/// </summary>
public class TimeCount : MonoBehaviour
{
    public TextMeshPro TimeText; // TextMeshPro Game Object to display time

    private float _elapsedTime = 0f; // Elapsed time since the start of the

    // game

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (TimeText != null)
            TimeText.text = "0s";
    }

    // Update is called once per frame
    void Update()
    {
        _elapsedTime += Time.deltaTime;

        // Update text with elapsed time
        if (TimeText != null)
            TimeText.text = $"{(int)_elapsedTime}s";
    }
}
