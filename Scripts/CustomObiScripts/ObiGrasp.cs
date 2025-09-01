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


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Obi;
using UnityEngine;

/// <summary>
/// To fully understand this code, read about simplex in
/// https://obi.virtualmethodstudio.com/manual/7.0/scriptingcollisions.html
/// Warning (BUG): if a soft body has surface collisions enabled, the
/// other grabbable objects should also have surface collisions enabled,
/// otherwise only the objects with surface collisions enabled will be grasped.
/// </summary>
[RequireComponent(typeof(ObiCollider))]
public class ObiGrasp : MonoBehaviour
{
    // Defines whether the grasper tool is trying to grasp a soft body or not.
    // Attention to the _hasGrasped property in the private section!!
    // TODO: make this private
    public bool isGrasping = false;

    // Also detach the soft body when the grasper tool is not grasping.
    public void SetGrasping(bool grasping)
    {
        isGrasping = grasping;

        if (grasping == false)
            Detach();
        else
            TryGrasping();
    }

    // Solver which has the soft body that will be grasped by this tool
    public ObiSolver solver = null;

    // Reference to the attachment component of the grasped soft body
    public ObiParticleAttachment attachment = null;

    // Reference to the ObiCollider component of this grasper tool
    private ObiCollider _obiCollider;

    // Maps each ObiSoftbody to a set of grabbed particle indices.
    // Is initialized in Awake() method.
    private Dictionary<ObiSoftbody, HashSet<int>> _grabbedParticlesByActor = null;

    // Maps each softbody to its attachment component.
    private Dictionary<ObiSoftbody, ObiParticleAttachment> _attachmentsByActor = null;

    // Indicates whether the grasper tool has grasped (and is grasping) any particles.
    private bool _hasGrasped
    {
        get
        {
            foreach (KeyValuePair<ObiSoftbody, HashSet<int>> pair in _grabbedParticlesByActor)
                if (pair.Value.Count > 0)
                    return true;
            return false;
        }
    }

    private void Start()
    {
        // Ensure the solver is assigned
        if (solver == null)
        {
            Debug.LogError("ObiSolver is not assigned. Please assign it to the GameObject.");
            return;
        }

        _obiCollider = GetComponent<ObiCollider>();

        StartCoroutine(WaitForSolver());

        // Subscribe to the solver's OnCollision event.
        // This is not used directly. Further explanation in the Solver_OnCollision method.
        solver.OnCollision += Solver_OnCollision;
    }

    IEnumerator WaitForSolver()
    {
        // Wait until the solver has been initialized
        yield return new WaitUntil(() => solver.didStart);
        InitializeDictionariesAndSubscribe();
    }

    void InitializeDictionariesAndSubscribe()
    {
        List<ObiSoftbody> softbodies = solver.actors.OfType<ObiSoftbody>().ToList();

        if (softbodies.Count == 0)
            return;

        int capacity = softbodies.Count;
        _grabbedParticlesByActor = new Dictionary<ObiSoftbody, HashSet<int>>(capacity);
        _attachmentsByActor = new Dictionary<ObiSoftbody, ObiParticleAttachment>(capacity);

        bool surfaceCollisionExpectedState = softbodies[0].surfaceCollisions;
        foreach (ObiSoftbody sb in softbodies)
        {
            if (sb.surfaceCollisions != surfaceCollisionExpectedState)
                Debug.LogWarning(
                    "If one actor has surface collisions enabled,"
                        + "all actors should have it enabled as well for this script to work properly!"
                );

            // Initialize the grabbed particles for each soft body actor
            _grabbedParticlesByActor.Add(sb, new HashSet<int>());

            // Initialize the attachment for each soft body actor
            if (sb.gameObject.TryGetComponent<ObiParticleAttachment>(out var obiParticleAttachment))
                _attachmentsByActor.Add(sb, obiParticleAttachment);
        }
    }

    private void LateUpdate()
    {
        // If didn't grasp in the current attempt, stop the grasping.
        // Attempt here means that the user has pressed the button to grasp.
        if (isGrasping == true && _hasGrasped == false)
            SetGrasping(false);
    }

    /// <summary>
    /// Method that is called when player tries to grasp.
    /// Is used to detect if the grasper tool is colliding with a soft body
    /// and, if possible, attach the soft body to the grasper tool.
    /// </summary>
    void TryGrasping()
    {
        // Check if is currently grabbing particles
        if (_hasGrasped == true)
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

                // Make sure that the collider is the one of the grasper
                if (colB == null || colB != _obiCollider)
                    continue;

                // Body A is a simplex.
                // We need to read it as simplex because if surface collisions
                // are activated, the index of bodyA does not correspond directly
                // to the particles' index
                int grabbedSimplexIndex = solver.simplices[contact.bodyA];

                // ObiSoftBody. From the simplex we can obtain the associated ObiSoftBody
                ObiSolver.ParticleInActor simplexInActor = solver.particleToActor[
                    grabbedSimplexIndex
                ];
                ObiActor actor = simplexInActor.actor;
                ObiSoftbody softbody = actor as ObiSoftbody;

                // If actor is not a softbody, the collision is not relevant for us
                if (softbody == null)
                    continue;

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

                    AddGrabbedParticle(softbody, particleIndexInActor);
                }

                // Get the point of collision between the grasper tool and the soft body.
                Vector3 contactWorldPosition = contact.pointB;
            }
        }
        Attach();
    }

    /// <summary>
    /// Add the actor's (as softbody) index of a grabbed particle to the corresponding actor.
    /// </summary>
    /// <param name="softbody">The softbody that the particle belongs to.</param>
    /// <param name="particleIndex">The index of the particle RELATIVE TO THE
    /// ACTOR (not the solver global index) that has been grasped</param>
    public void AddGrabbedParticle(ObiSoftbody softbody, int particleIndex)
    {
        _grabbedParticlesByActor[softbody].Add(particleIndex);
    }

    /// <summary>
    /// Attaches grabbed particles to this transform.
    /// </summary>
    /// <remarks>This method attempts to attach sets of particles from the each grasped actor to the current transform.
    /// If the actor does not already have an <see cref="ObiParticleAttachment"/>, the method will not perform any
    /// action. If a particle group does not exist, a new one is created. The attachment is then activated.
    /// More infor: https://obi.virtualmethodstudio.com/manual/7.0/scriptingattachments.</remarks>
    void Attach()
    {
        foreach (KeyValuePair<ObiSoftbody, HashSet<int>> pair in _grabbedParticlesByActor)
        {
            ObiSoftbody softbody = pair.Key;

            // Check if the actor has an attachment component and if there are any grabbed particles.
            if (
                !_attachmentsByActor.TryGetValue(
                    softbody,
                    out ObiParticleAttachment obiParticleAttachment
                )
                || pair.Value.Count == 0
            )
                continue;

            // Reuse the existing particle group or create a new one if it doesn't exist.
            if (obiParticleAttachment.particleGroup == null)
                obiParticleAttachment.particleGroup =
                    ScriptableObject.CreateInstance<ObiParticleGroup>();

            obiParticleAttachment.particleGroup.particleIndices.Clear();

            foreach (int i in pair.Value)
                obiParticleAttachment.particleGroup.particleIndices.Add(i);

            obiParticleAttachment.target = transform;

            // Activate the attachment
            obiParticleAttachment.enabled = true;
        }
    }

    /// <summary>
    /// Detach the particle attachments from the grasper tool, and clear the
    /// grabbed particles.
    /// </summary>
    void Detach()
    {
        foreach (KeyValuePair<ObiSoftbody, ObiParticleAttachment> pair in _attachmentsByActor)
        {
            ObiParticleAttachment attachment = pair.Value;
            if (attachment != null)
            {
                attachment.enabled = false;
                attachment = null;

                // Instead of clearing the entire dictionary, just clear the particle groups
                foreach (
                    KeyValuePair<ObiSoftbody, HashSet<int>> pairParticle in _grabbedParticlesByActor
                )
                    pairParticle.Value.Clear();
            }
        }
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
}
