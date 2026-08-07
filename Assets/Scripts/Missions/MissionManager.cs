using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Possible states for any mission in the linear mission system.
/// </summary>
public enum MissionState
{
    Locked,     // Not yet available (prerequisite not met)
    Available,  // Can be started by the player
    Active,     // Currently in progress
    Completed   // Finished
}

/// <summary>
/// Central, persistent brain of the linear mission system.
///
/// Scalability: mission states are stored in a dictionary keyed by an integer
/// mission id, so future missions only need (a) their own scene objects and
/// (b) a registered state here. This file deliberately implements ONLY Mission 1
/// ("Lost Package"); Mission 2 exists solely as an internal locked/unlocked flag.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    // Mission identifiers (linear order).
    public const int MISSION_1 = 1;
    public const int MISSION_2 = 2; // reserved for future expansion (NOT implemented)

    [Header("Mission 1 - Lost Package Config")]
    [Tooltip("Total clues the player must collect before the package appears.")]
    public int totalClues = 3;
    [Tooltip("Reward (Rs) granted when Mission 1 is completed.")]
    public int rewardAmount = 150;

    [Header("Scene References (auto-found if left empty)")]
    [Tooltip("Hidden package object, activated after all clues are collected.")]
    public GameObject packageObject;
    [Tooltip("The green start marker; its interaction is disabled on completion.")]
    public MissionStartPoint startPoint;
    [Tooltip("Objective / progress / popup UI driver.")]
    public MissionUI ui;

    [Header("Navigation (GPS arrow)")]
    [Tooltip("Clue locations in order, used to point the GPS arrow. Assign to match totalClues.")]
    public Transform[] clueLocations;

    // Runtime state
    private readonly Dictionary<int, MissionState> missionStates = new Dictionary<int, MissionState>();
    private int currentMission = 0; // 0 = none active
    private int cluesFound = 0;

    public int CurrentMission => currentMission;
    public int CluesFound => cluesFound;

    private void Awake()
    {
        // Singleton + persistence between scenes.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitialiseMissionStates();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResolveSceneReferences();

        // Mission 1 is available from the start of the game.
        if (GetState(MISSION_1) == MissionState.Available)
            Debug.Log("[Mission] Mission 1 'Lost Package' is now AVAILABLE.");
    }

    /// <summary>Sets the initial linear state: Mission 1 available, Mission 2 locked.</summary>
    private void InitialiseMissionStates()
    {
        if (missionStates.Count > 0) return; // keep progress if manager persisted
        missionStates[MISSION_1] = MissionState.Available;
        missionStates[MISSION_2] = MissionState.Locked; // future mission stays locked
    }

    /// <summary>Re-acquire scene object references after a scene (re)load.</summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolveSceneReferences();
    }

    private void ResolveSceneReferences()
    {
        if (ui == null) ui = FindObjectOfType<MissionUI>();
        if (startPoint == null) startPoint = FindObjectOfType<MissionStartPoint>();
        // packageObject can't be auto-found while inactive via FindObjectOfType,
        // so it is expected to be assigned by the editor automation / inspector.
    }

    // ---------------------------------------------------------------------
    // Public API used by scene objects (start point, clues, package)
    // ---------------------------------------------------------------------

    public MissionState GetState(int missionId)
    {
        return missionStates.TryGetValue(missionId, out var s) ? s : MissionState.Locked;
    }

    /// <summary>True only while Mission 1 is actively in progress.</summary>
    public bool IsMission1Active => currentMission == MISSION_1 && GetState(MISSION_1) == MissionState.Active;

    /// <summary>Clues can only be picked up while Mission 1 is active.</summary>
    public bool CanCollectClues() => IsMission1Active && cluesFound < totalClues;

    /// <summary>Called by the start point when the player presses M inside the marker.</summary>
    public void StartMission1()
    {
        if (GetState(MISSION_1) != MissionState.Available)
        {
            Debug.Log("[Mission] Cannot start Mission 1 (state = " + GetState(MISSION_1) + ").");
            return;
        }

        missionStates[MISSION_1] = MissionState.Active;
        currentMission = MISSION_1;
        cluesFound = 0;

        Debug.Log("[Mission] Mission 1 'Lost Package' STARTED.");

        if (ui != null)
        {
            ui.HideStartPrompt();
            ui.ShowObjective("Find 3 clues to locate the lost package.");
            ui.UpdateProgress(cluesFound, totalClues);
        }

        // Ensure the package starts hidden.
        if (packageObject != null) packageObject.SetActive(false);

        // GPS arrow: point at the first clue.
        PointNavigatorAtCurrentClue();
    }

    /// <summary>Called by a clue when collected.</summary>
    public void CollectClue()
    {
        if (!CanCollectClues()) return;

        cluesFound++;
        Debug.Log("[Mission] Clue collected: " + cluesFound + "/" + totalClues);

        if (ui != null) ui.UpdateProgress(cluesFound, totalClues);

        if (cluesFound >= totalClues)
            ActivatePackage();
        else
            PointNavigatorAtCurrentClue(); // GPS arrow: point at the next clue.
    }

    /// <summary>Reveals the hidden package and updates the objective text.</summary>
    private void ActivatePackage()
    {
        Debug.Log("[Mission] All clues found - Package ACTIVATED.");

        if (packageObject != null)
            packageObject.SetActive(true);
        else
            Debug.LogWarning("[Mission] Package object reference is missing!");

        if (ui != null) ui.ShowObjective("Retrieve the Package");

        // GPS arrow: point at the package.
        if (MissionNavigator.Instance != null && packageObject != null)
            MissionNavigator.Instance.PointTo(packageObject.transform, "Retrieve the Package");
    }

    /// <summary>Called by the package when collected.</summary>
    public void CollectPackage()
    {
        if (!IsMission1Active) return;
        if (cluesFound < totalClues) return; // safety: package not legitimately active

        CompleteMission1();
    }

    private void CompleteMission1()
    {
        missionStates[MISSION_1] = MissionState.Completed;
        currentMission = 0;

        Debug.Log("[Mission] Mission 1 'Lost Package' COMPLETED.");

        GiveReward();
        UnlockNextMission();

        if (ui != null)
        {
            ui.ClearObjective();
            ui.ShowMissionComplete();
        }

        // Stop the start point from offering the mission again.
        if (startPoint != null) startPoint.DisableInteraction();

        // GPS arrow: nothing to point at anymore.
        if (MissionNavigator.Instance != null)
            MissionNavigator.Instance.ClearTarget();
    }

    private void GiveReward()
    {
        if (MoneyManager.instance != null)
        {
            MoneyManager.instance.AddMoney(rewardAmount);
            Debug.Log("[Mission] Reward added: Rs " + rewardAmount);
        }
        else
        {
            Debug.LogWarning("[Mission] MoneyManager not found - reward not granted.");
        }

        if (ui != null) ui.ShowRewardPopup(rewardAmount);
    }

    /// <summary>
    /// Unlocks Mission 2 INTERNALLY only. No Mission 2 content is created here;
    /// this simply flips its stored state so a future build can offer it.
    /// </summary>
    private void UnlockNextMission()
    {
        if (GetState(MISSION_2) == MissionState.Locked)
        {
            missionStates[MISSION_2] = MissionState.Available;
            Debug.Log("[Mission] Mission 2 UNLOCKED (internal state only - not yet implemented).");
        }
    }

    /// <summary>Points the GPS arrow at the next uncollected clue in <see cref="clueLocations"/>, if assigned.</summary>
    private void PointNavigatorAtCurrentClue()
    {
        if (MissionNavigator.Instance == null) return;
        if (clueLocations == null || clueLocations.Length == 0) return;
        if (cluesFound >= clueLocations.Length) return;

        var target = clueLocations[cluesFound];
        if (target != null)
            MissionNavigator.Instance.PointTo(target, "Find Clue " + (cluesFound + 1) + "/" + totalClues);
    }

    // =====================================================================
    // GENERIC, REUSABLE MISSION API (added for Mission 2+).
    // Mission 1 keeps its own bespoke methods above; nothing here changes it.
    // Future missions drive their flow entirely through these calls.
    // =====================================================================

    /// <summary>True while the given mission id is the active one.</summary>
    public bool IsMissionActive(int missionId)
    {
        return currentMission == missionId && GetState(missionId) == MissionState.Active;
    }

    /// <summary>
    /// Generic start: only succeeds if the mission is currently Available.
    /// Returns true if the mission transitioned to Active.
    /// </summary>
    public bool TryStartMission(int missionId, string missionName)
    {
        if (GetState(missionId) != MissionState.Available)
        {
            Debug.Log("[Mission] Cannot start Mission " + missionId + " (state = " + GetState(missionId) + ").");
            return false;
        }

        missionStates[missionId] = MissionState.Active;
        currentMission = missionId;
        Debug.Log("[Mission] Mission " + missionId + " '" + missionName + "' STARTED.");
        return true;
    }

    /// <summary>
    /// Generic completion: marks the mission Completed, grants the reward via the
    /// existing MoneyManager, drives the shared UI, and unlocks the next mission
    /// in linear order (if one is registered and still Locked).
    /// </summary>
    public void CompleteMission(int missionId, int reward, string missionName)
    {
        if (GetState(missionId) != MissionState.Active)
        {
            Debug.Log("[Mission] Cannot complete Mission " + missionId + " (state = " + GetState(missionId) + ").");
            return;
        }

        missionStates[missionId] = MissionState.Completed;
        if (currentMission == missionId) currentMission = 0;

        Debug.Log("[Mission] Mission " + missionId + " '" + missionName + "' COMPLETED.");

        if (MoneyManager.instance != null)
        {
            MoneyManager.instance.AddMoney(reward);
            Debug.Log("[Mission] Reward added: Rs " + reward);
        }
        else
        {
            Debug.LogWarning("[Mission] MoneyManager not found - reward not granted.");
        }

        if (ui != null)
        {
            ui.ClearObjective();
            ui.ShowRewardPopup(reward);
            ui.ShowMissionComplete();
        }

        UnlockMissionAfter(missionId);
    }

    /// <summary>
    /// Generic linear unlock: flips mission (id+1) from Locked to Available, but
    /// ONLY if it has been registered. Unregistered missions are ignored, so no
    /// future mission is created implicitly.
    /// </summary>
    private void UnlockMissionAfter(int missionId)
    {
        int next = missionId + 1;
        if (missionStates.ContainsKey(next) && missionStates[next] == MissionState.Locked)
        {
            missionStates[next] = MissionState.Available;
            Debug.Log("[Mission] Mission " + next + " UNLOCKED.");
        }
    }

}