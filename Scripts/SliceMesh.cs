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

public class SliceMesh : MonoBehaviour
{
    private bool _isSlicing = false;

    private GameObject _objectToSlice = null;
    private SkeletonMeshSlicer _slicerScript = null;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update() { }

    /// <summary>
    /// Set if the slice must be performed
    /// </summary>
    /// <param name="slicing"></param>
    public void SetSlicing(bool slicing)
    {
        _isSlicing = slicing;
        if (_isSlicing == true)
            Slice();
    }

    /// <summary>
    /// Slice the skinned mesh of the object to be sliced.
    /// To slice the object, the object must have a SkeletonMeshSlicer script
    /// attached to it.
    /// </summary>
    private void Slice()
    {
        Transform sliceParent = _objectToSlice.transform;

        // Get the last parent of object to be sliced
        // This means that the SkeletonMeshSlicer script should be attached to
        // the last parent of the object to be sliced.
        while (sliceParent.parent != null)
            sliceParent = sliceParent.parent;

        _slicerScript = sliceParent.gameObject.GetComponent<SkeletonMeshSlicer>();

        if (_slicerScript != null)
            _slicerScript.SliceByMeshPlane(transform.up, transform.position);

        _isSlicing = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Store the object that collided with the slicer
        _objectToSlice = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        // Reset the object to be sliced
        _objectToSlice = null;
    }
}
