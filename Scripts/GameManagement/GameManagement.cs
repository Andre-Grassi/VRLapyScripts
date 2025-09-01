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
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class GameManagement : MonoBehaviour
{
    [System.Serializable]
    public class Transition
    {
        [Tooltip(
            "Image used for the fade in/out effect.\n"
                + "If no image is provided there won't be the fade effect."
        )]
        public Image FadeImage;

        [HideInInspector]
        public bool IsFading { get; private set; } = false;

        private float _fadeIncrement = 1f;

        // Indicates that the screen is completely tinted by the FadeImage.
        private bool _isScreenBlack = false;

        /// <summary>
        /// Change the alpha value of the Fade Image's color to create a fade in
        /// and fade out effect.
        /// </summary>
        public void FadeInOut()
        {
            if (FadeImage != null)
            {
                Color color = FadeImage.color;
                // Increment and limit alpha to 0 (lower bound) and 1 (upper bound)
                color.a = Mathf.Clamp01(color.a + _fadeIncrement * Time.deltaTime);
                FadeImage.color = color;

                if (color.a <= 0)
                {
                    IsFading = false;
                    _fadeIncrement *= -1; // Revert increment for the next fade out
                    _isScreenBlack = false;
                }
                else if (color.a >= 1)
                {
                    _fadeIncrement *= -1; // Revert increment to fade in
                    _isScreenBlack = true;
                }
            }
        }

        public IEnumerator StartFadeCoroutine()
        {
            // Fade
            // Use fade in/out effect if the Fade Image is provided
            if (FadeImage != null)
            {
                // Enable fading
                IsFading = true;

                // Only move scene objects when the screen is black
                // This variable is controlled by the FadeInOut function
                yield return new WaitUntil(() => _isScreenBlack == true);
            }
        }
    }

    public static GameManagement Instance;

    public GameObject PlayerXrRig;

    public CholecystectomyLevel CholecystectomyLevel;
    public TutorialLevel TutorialLevel;

    public Transition FadeTransition;

    private XROrigin _xrOrigin;

    private Vector3 _playerInitialPosition;
    private Quaternion _playerInitialRotation;

    // Initialize tutorial level and start it
    public void StartTutorial()
    {
        FadeTransition.StartFadeCoroutine();

        // Reset player's position
        _xrOrigin.MoveCameraToWorldLocation(_playerInitialPosition);

        StartCoroutine(LoadSceneWithFade("Tutorial"));

        // Find TutorialLevel in the scene
        TutorialLevel = FindAnyObjectByType<TutorialLevel>();

        if (TutorialLevel == null)
            throw new System.Exception("TutorialLevel not found in the scene.");

        TutorialLevel.FinishedTutorial.AddListener(ReturnToMenu);
    }

    // Initialize cholecystectomy level and start it
    public void StartCholecystectomy()
    {
        FadeTransition.StartFadeCoroutine();

        // Reset player's position
        _xrOrigin.MoveCameraToWorldLocation(_playerInitialPosition);

        StartCoroutine(LoadSceneWithFade("Cholecystectomy"));

        // Find CholecystectomyLevel in the scene
        CholecystectomyLevel = FindAnyObjectByType<CholecystectomyLevel>();

        if (CholecystectomyLevel == null)
            throw new System.Exception("CholecystectomyLevel not found in the scene.");

        CholecystectomyLevel.FinishedSurgery.AddListener(ReturnToMenu);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ReturnToMenu()
    {
        FadeTransition.StartFadeCoroutine();
        StartCoroutine(LoadSceneWithFade("Menu"));
    }

    private IEnumerator HandleSceneLoaded(string sceneName)
    {
        // Wait one frame to ensure all Start() methods have been called
        yield return null;

        if (sceneName == "Cholecystectomy")
        {
            CholecystectomyLevel = FindAnyObjectByType<CholecystectomyLevel>();
            if (CholecystectomyLevel == null)
                throw new System.Exception("CholecystectomyLevel not found in the scene.");

            CholecystectomyLevel.FinishedSurgery.AddListener(ReturnToMenu);
        }
        else if (sceneName == "Tutorial")
        {
            TutorialLevel = FindAnyObjectByType<TutorialLevel>();
            if (TutorialLevel == null)
                throw new System.Exception("TutorialLevel not found in the scene.");

            TutorialLevel.FinishedTutorial.AddListener(ReturnToMenu);
        }
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        yield return StartCoroutine(FadeTransition.StartFadeCoroutine());
        _xrOrigin.MoveCameraToWorldLocation(_playerInitialPosition);
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
            Destroy(gameObject);

        if (PlayerXrRig == null)
            throw new System.Exception("PlayerXrRig is not set in the GameManagement");

        if (PlayerXrRig.TryGetComponent<XROrigin>(out _xrOrigin) == false)
            throw new System.Exception("XROrigin component wasn't found in PlayerXrRig");

        // Get player's position info.
        // It's based on the player camera's position
        Transform cameraTransform = PlayerXrRig.transform.Find("Camera Offset");
        _playerInitialPosition = cameraTransform.position;
        _playerInitialRotation = cameraTransform.rotation;
    }

    void Update()
    {
        // Check if the fade transition is active
        if (FadeTransition.IsFading == true)
        {
            FadeTransition.FadeInOut();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HandleSceneLoaded(scene.name));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
