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
/// Add clip objects to a list of ObiSoftbodies when a collision occurs.
/// The softbodies that are going to be clipped must have a particle group named "ClipGroup"
/// which is the area where the clip object will be spawned.
/// The clipping is just attaching a clip to follow the average position of the particles
/// of the "ClipGroup".
/// To try to clip, use the SetClipping method and pass true as parameter.
/// </summary>
[RequireComponent(typeof(Collider), typeof(ObiCollider))]
public class ObiClipper : MonoBehaviour
{
    public ObiSolver solver;

    [Header("Clipping settings")]
    // This is a backing field, it only matters at the start of the game, because
    // it will feed the _softbodyClipStates dictionary. So, the clippable
    // softbodies must be assigned on the inspector before starting the game
    // and can't be changed at runtime. To Change the clippable softbodies at runtime,
    // modify the softbodyClipStates dictionary directly.
    [SerializeField]
    private List<ObiSoftbody> _softbodiesToClip; // Can't repeat softbodies

    // The clip state means whether the softbody is currently clipped or not.
    /// <summary>
    /// Dictionary of softbodies that can be clipped and their current clip state.
    /// The clip state means whether the softbody is currently clipped or not.
    /// </summary>
    public Dictionary<ObiSoftbody, bool> softbodyClipStates;

    [Tooltip("The clip object that will be instantiated when clipping.")]
    public GameObject clipObject;

    [Tooltip(
        "The start position of the clip object. \nFor example, the tip of" + "the clipper tool"
    )]
    public Transform clipSpawnPoint;

    public void SetClipping(bool clipping)
    {
        _isClipping = clipping;

        if (clipping == true)
            TryClipping();
        else
            _hasClipped = false; // Reset the clipping state for the next attempt.
    }

    [Header("Feedback Events")]
    /// <summary>
    /// Triggered when a soft body is clipped. Passes the Softbody that was
    /// clipped
    /// </summary>
    public UnityEvent<ObiSoftbody> Clipped;

    // Flag to indicate whether script is trying to clip a softbody
    private bool _isClipping = false;

    // Flag to indicate whether the script has already clipped a softbody in the current attempt.
    // An attempt here means that the user has pressed the button to clip.
    private bool _hasClipped = false;

    // Dictionary to associate clip objects with the softbody and its particle
    // group that they are attached to.
    private Dictionary<GameObject, (ObiSoftbody, ObiParticleGroup)> _clipByGroups;

    // Dictionary to store the previous average rotation of each particle group.
    private Dictionary<ObiParticleGroup, Quaternion> _groupPreviousAvgRotations;

    private ObiCollider _obiCollider; // Collider of the clipping tool

    private readonly string _clipGroupName = "ClipGroup";

    void Awake()
    {
        #region Validation
        // Ensure the solver is assigned
        if (solver == null)
            throw new MissingReferenceException("ObiSolver not assigned");

        if (clipObject == null)
            throw new MissingReferenceException("Clip object not assigned");

        if (clipSpawnPoint == null)
            throw new MissingReferenceException("Clip spawn point not assigned");
        #endregion

        _clipByGroups = new Dictionary<GameObject, (ObiSoftbody, ObiParticleGroup)>(
            _softbodiesToClip.Count
        );
        _groupPreviousAvgRotations = new Dictionary<ObiParticleGroup, Quaternion>(
            _softbodiesToClip.Count
        );

        _obiCollider = GetComponent<ObiCollider>();
        if (_obiCollider == null)
            throw new MissingComponentException("No ObiCollider found in the GameObject");

        // Subscribe to the solver's OnCollision event.
        // This is not used directly. Further explanation in the Solver_OnCollision method.
        solver.OnCollision += Solver_OnCollision;

        // Initialize softbodyClipStates
        softbodyClipStates = new Dictionary<ObiSoftbody, bool>(_softbodiesToClip.Count);
        foreach (ObiSoftbody sb in _softbodiesToClip)
        {
            if (sb == null)
            {
                Debug.LogError("One of the softbodies to clip is null. Please check the list.");
                continue;
            }

            // Add to dictionary with initial state false
            if (!softbodyClipStates.ContainsKey(sb))
            {
                softbodyClipStates.Add(sb, false);
            }
            else
            {
                Debug.LogWarning(
                    $"Softbody {sb.name} is already in the clipping list. Duplicates are not allowed."
                );
            }
        }
    }

    private void Update()
    {
        // Update position of each clip object to the average position of its associated particle group.
        foreach (var pair in _clipByGroups)
        {
            GameObject clipObject = pair.Key;
            ObiSoftbody softbody = pair.Value.Item1;
            ObiParticleGroup group = pair.Value.Item2;

            Pose averagePose = GetAveragePoseOfGroup(softbody, group);

            // Calculate the diff between previous and current rotations
            Quaternion deltaRotation =
                Quaternion.Inverse(_groupPreviousAvgRotations[group]) * averagePose.rotation;

            // Update relative rotation
            clipObject.transform.localPosition = averagePose.position;
            clipObject.transform.localRotation = deltaRotation * clipObject.transform.rotation;

            // Update the last known average rotation for this group
            _groupPreviousAvgRotations[group] = averagePose.rotation;
        }
    }

    private void LateUpdate()
    {
        // If didn't clip in the current attempt, stop the clipping process.
        if (_isClipping == true && _hasClipped == false)
            SetClipping(false);
    }

    /// <summary>
    /// Method that is called when the player tries to clip a softbody.
    /// Detect if the clipper is colliding with a soft body
    /// and, if possible, add the clip object to the softbody if it has a
    /// "ClipGroup".
    /// </summary>
    void TryClipping()
    {
        // Check If the stapler has already stapled a group in the current attempt.
        if (_hasClipped == true)
            return;

        ObiNativeContactList contacts = solver.colliderContacts;

        var world = ObiColliderWorld.GetInstance();

        // just iterate over all contacts in the current frame:
        foreach (Oni.Contact contact in contacts)
        {
            // if this one is an actual collision:
            if (contact.distance < 0.01)
            {
                // Get the ObiCollider involved in the collision
                // The ObiCollider is the component that represents a collider
                // of a normal object of Unity
                ObiColliderBase colB = world.colliderHandles[contact.bodyB].owner;

                // Make sure that the collider is the one of the stapler
                if (colB == null || colB != _obiCollider)
                    continue;

                // Body A is a simplex.
                // We need to read it as simplex because if surface collisions
                // are activated, the index of bodyA does not correspond directly
                // to the particles' index
                int collidedSimplexIndex = solver.simplices[contact.bodyA];

                // ObiSoftBody. From the simplex we can obtain the associated ObiSoftBody
                ObiSolver.ParticleInActor simplexInActor = solver.particleToActor[
                    collidedSimplexIndex
                ];
                ObiActor actor = simplexInActor.actor;
                ObiSoftbody softbody = actor as ObiSoftbody;

                if (softbody == null)
                    continue;

                // Search for the softbody in the states.
                if (!softbodyClipStates.TryGetValue(softbody, out bool isClipped))
                    continue; // Softbody not in the clipping list.

                if (isClipped == true)
                    continue; // Already clipped, skip this contact.

                List<ObiParticleGroup> groups = softbody.blueprint.groups;
                // Check if theres a particle group with the name "ClipGroup"
                ObiParticleGroup clipGroup = groups.Find(g => g.name == _clipGroupName);

                if (clipGroup == null)
                {
                    Debug.LogWarning(
                        $"No particle group named '{_clipGroupName}' "
                            + $"found in {softbody.name}. It can't be clipped."
                    );
                    continue; // Not a clipable softbody.
                }

                // retrieve the offset and size of the simplex in the solver.simplices array:
                int simplexStart = solver.simplexCounts.GetSimplexStartAndSize(
                    contact.bodyA,
                    out int simplexSize
                );

                // starting at simplexStart, iterate over all particles in the simplex
                // to get the particles
                for (int i = 0; i < simplexSize; ++i)
                {
                    // Get the particle index in the solver array
                    int particleIndexInSolver = solver.simplices[simplexStart + i];
                    ObiSolver.ParticleInActor particleInActor = solver.particleToActor[
                        particleIndexInSolver
                    ];

                    // Convert the particle index to the actor's local index.
                    int particleIndexInActor = particleInActor.indexInActor;

                    if (clipGroup.ContainsParticle(particleIndexInActor))
                    {
                        //ClipGroup(softbody, clipGroup);
                        SpawnClip(softbody, clipGroup);

                        // Trigger the clipping event.
                        Clipped?.Invoke(softbody);

                        softbodyClipStates[softbody] = true; // Mark the softbody as clipped.

                        _hasClipped = true;
                    }
                }
            }
        }
    }

    // Spawn clip at the average position of the particles in the clip group.
    void SpawnClip(ObiSoftbody softbody, ObiParticleGroup clipGroup)
    {
        Pose averagePose = GetAveragePoseOfGroup(softbody, clipGroup);

        // Instantiate the clip object as a child of the solver because
        // the position from getAveragePoseOfGroup is relative to the solver's
        // transform.
        GameObject clipInstance = Instantiate(clipObject, solver.transform);

        clipInstance.transform.localPosition = averagePose.position;

        // The rotation is based on the instrument's spawn point rotation
        clipInstance.transform.rotation = clipSpawnPoint.rotation;

        // Associate the clip instance with the clip group.
        _clipByGroups.Add(clipInstance, (softbody, clipGroup));

        Quaternion currentGroupRotation = averagePose.rotation;
        _groupPreviousAvgRotations.Add(clipGroup, currentGroupRotation);
    }

    /// <summary>
    /// Calculate the average position and rotation of the particles of the group.
    /// Returns a Pose that contains the average position and rotation RELATIVE
    /// to the SOLVER's transform.
    /// Be sure that the group is not empty, otherwise a division by zero will occur.
    /// </summary>
    /// <param name="softbody">The softbody that contains the particle group.</param>
    /// <param name="particleGroup">The particle group to calculate the average pose for.</param>
    Pose GetAveragePoseOfGroup(ObiSoftbody softbody, ObiParticleGroup particleGroup)
    {
        Pose averagePose = new Pose(Vector3.zero, Quaternion.identity);

        // Calculate the local average position and rotation of the particles
        // in the clip group.
        // The position and rotation are relative to the SOLVER's transform.
        foreach (int particleIndex in particleGroup.particleIndices)
        {
            // Get particle index in the solver's array.
            int solverIndex = softbody.solverIndices[particleIndex];
            averagePose.position += solver.positions.GetVector3(solverIndex);
            averagePose.rotation.x += solver.renderableOrientations[solverIndex].x;
            averagePose.rotation.y += solver.renderableOrientations[solverIndex].y;
            averagePose.rotation.z += solver.renderableOrientations[solverIndex].z;
        }

        averagePose.position /= particleGroup.particleIndices.Count;
        averagePose.rotation.x /= particleGroup.particleIndices.Count;
        averagePose.rotation.y /= particleGroup.particleIndices.Count;
        averagePose.rotation.z /= particleGroup.particleIndices.Count;

        return averagePose;
    }

    /// <summary>
    /// This method is subscribed to the solver's OnCollision event.
    /// It is not doing anything here, but we need a function subscribed to it
    /// otherwise Obi will not process and not store the collisions.
    /// </summary>
    void Solver_OnCollision(ObiSolver solver, ObiNativeContactList contacts)
    {
        // Purposefully empty.
    }

    /*
     * Add in the future????
    void ClipGroup(ObiSoftbody softbody, ObiParticleGroup clipGroup)
    {
        Vector3 positionAverage = Vector3.zero;

        // Remove the particles in the clip group from the softbody.
        foreach (int particleIndex in clipGroup.particleIndices)
        {
            positionAverage += softbody.blueprint.positions[particleIndex];
        }

        positionAverage /= clipGroup.particleIndices.Count;

        foreach (int particleIndex in clipGroup.particleIndices)
        {
            softbody.blueprint.positions[particleIndex] = positionAverage;
        }

        // Trigger the clipping event.
        onClip.Invoke();
    }
    */
}
