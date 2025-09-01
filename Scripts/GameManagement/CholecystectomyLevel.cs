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
using Obi;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[System.Serializable]
public class CholecystLvlData
{
    public enum StepType
    {
        clip,
        cut,
        electrocauteryCut,
        removeGallbladder,
    }

    [System.Serializable]
    public struct CholecystStep
    {
        public string Name;
        public string Description;
        public ObiSoftbody softbody; // Softbody associated with this step, if any.
        public ObiStitcher stitcher; // Stitcher associated with this step, if any.
        public bool IsCompleted;
        public StepType type;
    }

    public string Objective;
    public List<CholecystStep> Steps;

    public CholecystStep GetStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= Steps.Count)
            throw new System.ArgumentOutOfRangeException(
                nameof(stepIndex),
                "Step index is out of range."
            );
        return Steps[stepIndex];
    }

    public int CurrentStep { get; set; } = 0;
    public bool IsCompleted
    {
        get
        {
            // Iterate over all steps and check if they are completed
            return Steps.TrueForAll(step => step.IsCompleted);
        }
    }
}

[System.Serializable]
public class CholecystectomyLevel : GameLevel<CholecystLvlData>
{
    public override void InitializeLevel(int levelId)
    {
        // Get step data
        CholecystLvlData.CholecystStep step = LevelsData[levelId].GetStep(0);

        Debug.Log(step.Description);
        // Put instrument involved in the step into the user hand

        _obiClipper.softbodyClipStates.Clear();

        // _stitcherCutter.targetStitcher = null; DONT DO THIS, IT CAUSES A NULL REFERENCE

        // Configure which softbodies the instrument can interact with
        if (step.type == CholecystLvlData.StepType.clip)
        {
            ScissorsInstrument.SetActive(false);
            LHookInstrument.SetActive(false);
            ClipperInstrument.SetActive(true);

            bool isSBClipped = false;
            _obiClipper.softbodyClipStates.Add(step.softbody, isSBClipped);
        }
        else if (step.type == CholecystLvlData.StepType.cut)
        {
            ClipperInstrument.SetActive(false);
            LHookInstrument.SetActive(false);
            ScissorsInstrument.SetActive(true);

            _stitcherCutter.targetStitcher = step.stitcher;
        }
        else if (step.type == CholecystLvlData.StepType.electrocauteryCut)
        {
            ClipperInstrument.SetActive(false);
            ScissorsInstrument.SetActive(false);
            LHookInstrument.SetActive(true);

            _continuousStitchCutter.targetStitcher = step.stitcher;
        }
        else if (step.type == CholecystLvlData.StepType.removeGallbladder)
        {
            ClipperInstrument.SetActive(false);
            ScissorsInstrument.SetActive(false);
            LHookInstrument.SetActive(false);
        }

        // Update the UI to show current step
        UpdateStepsUI();
    }

    public override void AdvanceLevel()
    {
        // Logic to advance to the next level
        CurrentLevel++;

        if (CurrentLevel >= LevelsData.Length)
        {
            FinishedSurgery.Invoke();
            UpdateStepsUI();
            return;
        }

        InitializeLevel(CurrentLevel);
    }

    public override void ResetLevel()
    {
        // Logic to reset the current level
        CurrentLevel = 0;
    }

    public UnityEvent FinishedSurgery;

    public ObiSolver ObiSolver;

    // Organs
    public ObiSoftbody CysticDuct;
    public ObiSoftbody CysticArtery;

    // Stitchers
    public ObiStitcher GallbladerDuctsStitcher;
    public ObiStitcher ArteryTopBottomStitcher;
    public ObiStitcher GallbladerLiverStitcher;

    // Instruments
    [Tooltip("Must be positioned on their respective incision ports.")]
    public GameObject GrasperInstrument;
    public GameObject ClipperInstrument;
    public GameObject ScissorsInstrument;
    public GameObject LHookInstrument;

    public UIDocument HUD;

#if DEBUG
    public GameObject DebugSphere = null;
#endif

    // Softbody manipulation components
    private ObiGrasp _obiGrasp; // I think there is no need for obigrasp reference

    // since its feedback is not related to any step
    private ObiClipper _obiClipper;
    private ObiStitchCutter _stitcherCutter;
    private ObiContinuousStitchCutter _continuousStitchCutter;

    private VisualElement _stepsList;
    private List<Toggle> _stepCheckboxes = new List<Toggle>();
    private List<Label> _descriptionLabels = new List<Label>();

    private void Awake()
    {
        #region Validation
        if (ObiSolver == null)
            throw new System.NullReferenceException(
                "ObiSolver is not assigned in CholecystectomyLevel."
            );
        if (CysticDuct == null)
            throw new System.NullReferenceException(
                "CysticDuct is not assigned in CholecystectomyLevel."
            );
        if (CysticArtery == null)
            throw new System.NullReferenceException(
                "CysticArtery is not assigned in CholecystectomyLevel."
            );
        if (GallbladerDuctsStitcher == null)
            throw new System.NullReferenceException(
                "GallbladerDuctsStitcher is not assigned in CholecystectomyLevel."
            );
        if (ArteryTopBottomStitcher == null)
            throw new System.NullReferenceException(
                "ArteryTopBottomStitcher is not assigned in CholecystectomyLevel."
            );
        if (GallbladerLiverStitcher == null)
            throw new System.NullReferenceException(
                "GallbladerLiverStitcher is not assigned in CholecystectomyLevel."
            );
        if (GrasperInstrument == null)
            throw new System.NullReferenceException(
                "GrasperInstrument is not assigned in CholecystectomyLevel."
            );
        if (ScissorsInstrument == null)
            throw new System.NullReferenceException(
                "ScissorsInstrument is not assigned in CholecystectomyLevel."
            );
        if (ClipperInstrument == null)
            throw new System.NullReferenceException(
                "ClipperInstrument is not assigned in CholecystectomyLevel."
            );
        if (LHookInstrument == null)
            throw new System.NullReferenceException(
                "LHookInstrument is not assigned in CholecystectomyLevel."
            );
        #endregion

        // Define levels data
        LevelsData = new CholecystLvlData[]
        {
            // Clip cystic duct step (2 clips, but only one for now)
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Clipe o ducto cístico",
                        Description = "Coloque um clipe no ducto cístico para fechá-lo.",
                        softbody = CysticDuct,
                        IsCompleted = false,
                        type = CholecystLvlData.StepType.clip,
                    },
                },
            },
            // Clip cystic artery step (2 clips, but only one for now)
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Clipe a artéria cística",
                        Description = "Coloque um clipe na artéria cística para fechá-la.",
                        IsCompleted = false,
                        softbody = CysticArtery,
                        type = CholecystLvlData.StepType.clip,
                    },
                },
            },
            // Cut cystic duct step
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Corte o ducto cístico",
                        Description = "Corte o ducto cístico para separá-lo da vesícula biliar",
                        IsCompleted = false,
                        stitcher = GallbladerDuctsStitcher,
                        type = CholecystLvlData.StepType.cut,
                    },
                },
            },
            // Cut cystic artery step
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Corte a artéria cística",
                        Description = "Corte a artéria cística para separá-la da vesícula biliar",
                        IsCompleted = false,
                        stitcher = ArteryTopBottomStitcher,
                        type = CholecystLvlData.StepType.cut,
                    },
                },
            },
            // Dissect gallbladder step
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Disseque a vesícula biliar",
                        Description = "Disseque a vesícula biliar do leito hepático",
                        IsCompleted = false,
                        stitcher = GallbladerLiverStitcher,
                        type = CholecystLvlData.StepType.electrocauteryCut,
                    },
                },
            },
            // Remove gallbladder step
            // WIP
            /*
            new CholecystLvlData
            {
                Steps = new List<CholecystLvlData.CholecystStep>
                {
                    new CholecystLvlData.CholecystStep
                    {
                        Name = "Remove Gallbladder",
                        Description = "Carefully remove the gallbladder from the abdominal cavity.",
                        IsCompleted = false,
                        type = CholecystLvlData.StepType.removeGallbladder,
                    },
                },
            },
            */
        };

        #region Get Softbody Manipulation Components
        // Get softbody manipulation components from the instruments
        _obiGrasp = GrasperInstrument.GetComponentInChildren<ObiGrasp>();
        if (_obiGrasp == null)
            throw new System.NullReferenceException(
                "ObiGrasp component is not found in GrasperInstrument."
            );
        _obiClipper = ClipperInstrument.GetComponentInChildren<ObiClipper>();
        if (_obiClipper == null)
            throw new System.NullReferenceException(
                "ObiClipper component is not found in ClipperInstrument."
            );
        _stitcherCutter = ScissorsInstrument.GetComponentInChildren<ObiStitchCutter>();
        if (_stitcherCutter == null)
            throw new System.NullReferenceException(
                "ObiStitchCutter component is not found in ScissorsInstrument."
            );
        _continuousStitchCutter =
            LHookInstrument.GetComponentInChildren<ObiContinuousStitchCutter>();
        if (_continuousStitchCutter == null)
            throw new System.NullReferenceException(
                "ContinuousStitchCutter component is not found in LHookInstrument."
            );

        #endregion

        #region Subscribe to instruments' events
        _obiClipper.Clipped.AddListener(OnClip);
        _stitcherCutter.onStitchCut.AddListener(OnCut);
        _continuousStitchCutter.onStitchCut.AddListener(OnCut);

#if DEBUG
        ObiClipper debugClipper = DebugSphere.GetComponentInChildren<ObiClipper>();
        debugClipper.Clipped.AddListener(OnClip);
        debugClipper.SetClipping(true);
#endif
        #endregion
    }

    private void Start()
    {
        // Initialize the first level
        if (LevelsData.Length > 0)
        {
            InitializeLevel(CurrentLevel);
        }
        else
        {
            Debug.LogError("No levels data available to initialize.");
        }
    }

    private void OnEnable()
    {
        PopulateStepsUI();
        UpdateStepsUI();
    }

    /// <summary>
    /// Creates checkboxes for all steps in all levels and adds them to the UI
    /// </summary>
    private void PopulateStepsUI()
    {
        VisualElement uiRoot = HUD.rootVisualElement;
        _stepsList = uiRoot.Q<VisualElement>("steps-list");

        if (_stepsList == null)
        {
            Debug.LogWarning("Steps list UI element not found!");
            return;
        }

        // Clear existing UI elements
        _stepsList.Clear();
        _stepCheckboxes.Clear();

        // Create checkboxes for each step in each level
        for (int levelIndex = 0; levelIndex < LevelsData.Length; levelIndex++)
        {
            var levelData = LevelsData[levelIndex];

            for (int stepIndex = 0; stepIndex < levelData.Steps.Count; stepIndex++)
            {
                var step = levelData.Steps[stepIndex];

                // Create a container for each step
                var stepContainer = new VisualElement();
                stepContainer.AddToClassList("step-container");
                stepContainer.AddToClassList("pill");

                // Create checkbox
                var checkbox = new Toggle(step.Name);
                checkbox.value = step.IsCompleted;
                checkbox.SetEnabled(false); // Make it read-only (can't click on it)
                checkbox.AddToClassList("step-checkbox");

                // Create description label
                var descriptionLabel = new Label(step.Description);
                descriptionLabel.AddToClassList("step-description");

                // Add elements to container
                stepContainer.Add(checkbox);
                stepContainer.Add(descriptionLabel);

                // Add container to the steps list
                _stepsList.Add(stepContainer);

                // Store reference to checkbox for later updates
                _stepCheckboxes.Add(checkbox);

                _descriptionLabels.Add(descriptionLabel);
            }
        }
    }

    /// <summary>
    /// Updates the checkbox states based on current step completion status
    /// </summary>
    private void UpdateStepsUI()
    {
        if (_stepCheckboxes == null || _stepCheckboxes.Count == 0)
            return;

        int checkboxIndex = 0;

        // Update each checkbox based on step completion status
        for (int levelIndex = 0; levelIndex < LevelsData.Length; levelIndex++)
        {
            var levelData = LevelsData[levelIndex];

            for (int stepIndex = 0; stepIndex < levelData.Steps.Count; stepIndex++)
            {
                if (checkboxIndex < _stepCheckboxes.Count)
                {
                    var step = levelData.Steps[stepIndex];
                    _stepCheckboxes[checkboxIndex].value = step.IsCompleted;

                    // Highlight current step
                    if (levelIndex == CurrentLevel && stepIndex == 0)
                        _stepsList[checkboxIndex].AddToClassList("current-step");
                    else
                        _stepsList[checkboxIndex].RemoveFromClassList("current-step");
                }
                checkboxIndex++;
            }
        }
    }

    private void OnClip(ObiSoftbody clippedSoftbody)
    {
        // Check if the clipped object is the cystic duct or artery
        if (clippedSoftbody == CysticDuct || clippedSoftbody == CysticArtery)
        {
            // Find the current step that corresponds to the clipped object
            for (int i = 0; i < LevelsData.Length; i++)
            {
                for (int j = 0; j < LevelsData[i].Steps.Count; j++)
                {
                    var step = LevelsData[i].Steps[j];
                    if (step.softbody == clippedSoftbody && !step.IsCompleted)
                    {
                        step.IsCompleted = true;
                        LevelsData[i].Steps[j] = step; // Update the step in the list
                        UpdateStepsUI(); // Update UI
                        AdvanceLevel();
                        return; // Exit after completing the first matching step
                    }
                }
            }
        }
    }

    private void OnCut(ObiStitcher stitcher)
    {
        // Check if the cut stitcher is one of the expected stitchers
        if (
            stitcher == GallbladerDuctsStitcher
            || stitcher == ArteryTopBottomStitcher
            || stitcher == GallbladerLiverStitcher
        )
        {
            // Find the current step that corresponds to the cut stitcher
            for (int i = 0; i < LevelsData.Length; i++)
            {
                for (int j = 0; j < LevelsData[i].Steps.Count; j++)
                {
                    var step = LevelsData[i].Steps[j];

                    if (
                        step.stitcher == stitcher
                        && !step.IsCompleted
                        && step.stitcher.StitchCount == 0 // Check if the stitcher has no stitches left
                    )
                    {
                        step.IsCompleted = true;
                        LevelsData[i].Steps[j] = step; // Update the step in the list
                        UpdateStepsUI(); // Update UI
                        AdvanceLevel();
                        return; // Exit after completing the first matching step
                    }
                }
            }
        }
    }
}
