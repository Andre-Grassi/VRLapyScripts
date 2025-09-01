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
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Class that has a method to detect which hand is grabbing a GameObject that
/// has the XR Grab Interactable component attached to it.
/// </summary>
public class DetectHand : MonoBehaviour
{
    // Define which hand is grabbing the object
    // It is set automatically by the Detect method
    public InteractorHandedness Handedness = InteractorHandedness.None;

    // Detect which hand grabbed the object.
    // This method should be called by the Select Entered event of the
    // XRGrabInteractable component that belongs to the object of which we want
    // to detect the handedness.
    // DEBUG ver o que acontece se eu chamar detect no onSelectExit
    public void Detect(SelectEnterEventArgs args)
    {
        // Get the interactor object
        XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;

        if (interactor != null)
        {
            // Set the hand that grabbed the object
            Handedness = interactor.handedness;
        }
    }
}
