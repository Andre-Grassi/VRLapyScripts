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
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/*
 * ATTENTION: due to a known bug in this script, the instrument's origin
 * and the attach point must be aligned, or almost aligned to avoid
 * a flickering when the instrument is almost parallel to the pivot point.
 */

/// <summary>
/// Constrains the movement of an instrument to pivot around a pivot point.
/// For this script to work properly, the instrument's tip should be aligned
/// with the instrument's z vector of its origin.
/// </summary>
[RequireComponent(typeof(DetectHand), typeof(XRGrabInteractable))]
public class PivotPoint : MonoBehaviour
{
    [Header("Core Setup")]
    [Tooltip(
        "The attach point on the instrument that will follow the user's hand.\n"
            + "This script assumes that the object has an attach to work"
    )]
    public Transform attach;

    [Tooltip("The fixed incision point (trocar) that the instrument will pivot around.")]
    public Transform pivotPoint;

    [Tooltip("Cannula must be a child of the instrument")]
    public Transform cannula;

    [Tooltip("The Transform representing the user's hand (e.g., VR controller, mouse).")]
    public Transform leftHandController,
        rightHandController;

    [Tooltip("Adjust if the instrument is looking away from the pivot point.")]
    public bool invertLookingDirection = false;

    [Header("Movement Limits")]
    [Tooltip("The minimum distance between the instrument can be from the pivot point.")]
    public float minDistance = 0f;

    [Tooltip("The maximum distance the instrument can be from the pivot point.")]
    public float maxDistance = 0.8f;

    [Range(10f, 90f)]
    public float maxAngleFromVertical = 70f;

    private bool _isInstrumentEquipped = false;

    private XRGrabInteractable _xrGrabInteractable;

    // The DetectHand component to determine which hand is grabbing the instrument
    private DetectHand _detectHand;

    // The hand controller that is currently grabbing the instrument
    private Transform _handController;

    // Used to freeze cannula position
    private Vector3 _cannulaStartPosition;

    private int _invertFactor => invertLookingDirection ? -1 : 1;

    private void Awake()
    {
        if (
            pivotPoint == null
            || leftHandController == null
            || rightHandController == null
            || attach == null
        )
            Debug.LogError(
                "One or more reference Transforms have not been assigned in the Inspector."
            );

        // Set trackPosition and trackRotation to false to prevent jittering.
        // These functionalities will be overwritten by the script.
        if (TryGetComponent<XRGrabInteractable>(out var interactable))
        {
            if (interactable.trackPosition || interactable.trackRotation)
                Debug.LogWarning("Setting track position and rotation to false!");

            interactable.trackPosition = false;
            interactable.trackRotation = false;
        }
    }

    private void Start()
    {
        _xrGrabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to events to detect which hand is grabbing the instrument
        _xrGrabInteractable.selectEntered.AddListener(args =>
        {
            _detectHand.Detect(args);
        });

        // Subscribe to events to enable / disable this script
        _xrGrabInteractable.selectEntered.AddListener(args =>
        {
            _isInstrumentEquipped = true;
        });
        _xrGrabInteractable.selectExited.AddListener(args =>
        {
            _isInstrumentEquipped = false;
        });

        // Get the DetectHand component to determine which hand is grabbing the instrument
        _detectHand = GetComponent<DetectHand>();

        if (_detectHand == null)
            Debug.LogError(
                "DetectHand component not found. Please ensure it is attached to the same GameObject."
            );

        if (cannula == null)
            throw new System.Exception(
                "Cannula Transform is not assigned." + " Please assign it in the Inspector."
            );
        else
        {
            // Store the initial position of the cannula
            _cannulaStartPosition.x = pivotPoint.position.x;
            _cannulaStartPosition.z = pivotPoint.position.z;

            // Preserve the y position of the cannula relative to the pivot point.
            // This way if the pivot point is too high or low,
            // the cannula will not be affected by it.
            float yOffset = cannula.position.y - pivotPoint.position.y;
            _cannulaStartPosition.y = pivotPoint.position.y + yOffset;
        }
    }

    void FixedUpdate()
    {
        if (_isInstrumentEquipped == false)
            return;

        if (_detectHand.Handedness == InteractorHandedness.Left)
            _handController = leftHandController;
        else if (_detectHand.Handedness == InteractorHandedness.Right)
            _handController = rightHandController;

        // Calculate direction from hand to pivot point
        Vector3 handDirection = pivotPoint.position - _handController.position;

        // Get sum of absolute x and z components of the handDirection
        float absSum = Mathf.Abs(handDirection.x) + Mathf.Abs(handDirection.z);

        Quaternion oldRotation = transform.rotation;
        Vector3 oldPosition = transform.position;

        // Calculate direction from instrument to pivot point
        Vector3 direction = transform.position - pivotPoint.position;
        Vector3 normalizedDirection = direction.normalized;

        // Set the instrument's rotation to look in that direction.
        transform.rotation = Quaternion.LookRotation(_invertFactor * normalizedDirection);

        // Set instrument's attach to match the hand position.
        // This is done because manually setting the position rather than
        // using the Track Position of the XR Grab Interactable component
        // prevents some flickering.

        /*****************************************************************
         * ATTENTION: this can cause a bug, that when the instrument's origin
         * is parallel to the pivot point and the attach point are not aligned
         * the instrument will start to flicker
        ******************************************************************/
        Vector3 positionOffset = transform.position - attach.position;
        transform.position = _handController.position + positionOffset;

        float instrumentPivotDistance = Vector3.Distance(transform.position, pivotPoint.position);

        // New direction from instrument to pivot point after moving the instrument
        Vector3 newDirection = transform.position - pivotPoint.position;

        // Angle between the instrument's direction and the vertical axis (y)
        float angle = Vector3.Angle(newDirection, Vector3.up);

        // If the instrument is too close or too far from the pivot point,
        // reset its position and rotation to the previous values.
        if (
            instrumentPivotDistance < minDistance
            || instrumentPivotDistance > maxDistance
            || angle > maxAngleFromVertical
        )
        {
            transform.rotation = oldRotation;
            transform.position = oldPosition;
        }

        // Freeze the cannula position
        cannula.position = _cannulaStartPosition;

        // TODO: Stop cannula x rotation
    }
}
