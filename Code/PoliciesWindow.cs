using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

// --- DATA STRUCTURES ---

/// <summary>
/// Defines the static data for a policy loaded from text.
/// </summary>
public class PolicyDef
{
    public string Id; // Internal ID derived from name (e.g., "vassalage")
    public string Name; // Display Name (e.g., "Vassalage")
    public string Description; 
    public int Cost;
    // You would add a Sprite field here if you have specific icons for each
    // public Sprite Icon; 
}

// --- POLICY MANAGER ---

/// <summary>
/// Manages loading policy data and tracking which policies are active.
/// </summary>
public static class PolicyManager
{
    // The raw text content from aaa.txt
    private const string RawPolicyData = @"Vassalage
Allows the recruitment of vassals to manage distant lands, increasing tax efficiency but slightly raising autonomy.
100

Mercenary Contracts
Enables hiring mercenary companies for immediate military support at a high gold cost.
250

Royal Guard
Establish an elite guard unit for the monarch, increasing stability and defense but at a high upkeep cost.
500

Feudal Obligations
Standardizes military service requirements from lords, increasing manpower but slightly lowering stability.
150

Spy Network
Develops a kingdom-wide spy network to uncover plots and increase diplomatic visibility.
300

Naval Dominance
Focuses resources on building a powerful navy to control trade routes and coastal regions.
400

Fortification Effort
Mandates the construction of defensive structures in border provinces, increasing defensiveness but costing gold.
200

Diplomatic Corps
Establishes a permanent corps of diplomats to improve relations with neighboring kingdoms.
150

Legal Reform
Standardizes laws across the kingdom, increasing stability and tax income but costing significant gold to implement.
350

Cultural Assimilation
Promotes the dominant culture in newly conquered lands, speeding up integration but increasing unrest.
250

Religious Inquisition
Enforces religious unity, increasing stability and piety but significantly raising unrest in diverse regions.
400

Trade Guilds
Encourages the formation of trade guilds, boosting trade income and production but reducing royal control over the economy.
200
";

    public static List<PolicyDef> AllPolicies = new List<PolicyDef>();
    // HashSet for quick lookups of active policy IDs
    public static HashSet<string> ActivePolicyIds = new HashSet<string>();

    public static bool IsLoaded { get; private set; }

    /// <summary>
    /// Parses the raw text data into objects.
    /// </summary>
    public static void LoadPolicies()
    {
        if (IsLoaded) return;

        AllPolicies.Clear();
        // Split by newlines, removing empty entries to handle trailing lines
        string[] lines = RawPolicyData.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        // Iterate through lines in blocks of 3
        for (int i = 0; i < lines.Length; i += 3)
        {
            // Ensure we have 3 lines to read
            if (i + 2 >= lines.Length) break; 

            PolicyDef def = new PolicyDef();
            def.Name = lines[i].Trim();
            // Create a simple ID by removing spaces and lowercasing
            def.Id = def.Name.Replace(" ", "").ToLower(); 
            def.Description = lines[i + 1].Trim();
            
            // TryParse handles potential non-number inputs safely
            int.TryParse(lines[i + 2].Trim(), out def.Cost);

            AllPolicies.Add(def);
        }

        IsLoaded = true;
        Debug.Log($"[Policies] Loaded {AllPolicies.Count} policies.");
    }

    /// <summary>
    /// Attempts to activate a policy. Returns true if successful (affordable).
    /// </summary>
    public static bool TryActivatePolicy(PolicyDef def, Kingdom kingdom)
    {
        if (ActivePolicyIds.Contains(def.Id))
        {
            Debug.Log($"[Policies] {def.Name} is already active.");
            return false;
        }

        // Assuming you have access to the Kingdom's data object here
        // Replace 'KingdomMetricsSystem.GetKingdomData(kingdom)' with your actual data access method
        var data = KingdomMetricsSystem.GetKingdomData(kingdom); 
        
        if (data.Treasury >= def.Cost)
        {
            // Deduct Cost
            data.Treasury -= def.Cost;
            
            // Activate
            ActivePolicyIds.Add(def.Id);
            Debug.Log($"[Policies] Activated {def.Name} for {def.Cost} gold.");
            
            // TODO: Apply the actual game effects of the policy here
            ApplyPolicyEffects(def, kingdom);
            return true;
        }
        else
        {
            Debug.Log($"[Policies] Cannot afford {def.Name}. Treasury: {data.Treasury}, Cost: {def.Cost}");
            return false;
        }
    }

    private static void ApplyPolicyEffects(PolicyDef def, Kingdom kingdom)
    {
        // Add logic here to apply bonuses based on def.Id
        // e.g., if (def.Id == "vassalage") { data.TaxEfficiency += 0.1f; }
    }
}

// --- THE WINDOW UI ---

public class PoliciesWindow : MonoBehaviour
{
    // Assign these in the inspector or find them dynamically
    public Transform ContentGridContainer; // The GridLayoutGroup object holding the buttons
    public GameObject PolicyItemPrefab; // The prefab matching the image design
    
    // Standard generic window setup
    private static PoliciesWindow _instance;
    public static PoliciesWindow Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PoliciesWindow>(true);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        _instance = this;
        // Ensure data is loaded before the window first opens
        PolicyManager.LoadPolicies();
    }

    private void OnEnable()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        // 1. Clear existing items
        foreach (Transform child in ContentGridContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Get current kingdom (assuming the player's kingdom is focused)
        Kingdom playerKingdom = GetPlayerKingdom();

        // 3. Populate grid
        foreach (PolicyDef def in PolicyManager.AllPolicies)
        {
            CreatePolicyItemButton(def, playerKingdom);
        }
    }

    private void CreatePolicyItemButton(PolicyDef def, Kingdom kingdom)
    {
        GameObject go = Instantiate(PolicyItemPrefab, ContentGridContainer);

        // --- Find UI Components within the Prefab ---
        // Important: Adjust these path strings if your prefab hierarchy is different.
        // Based on the image: Icon on left, Name top right, Cost bottom right.
        
        // The main button component
        Button button = go.GetComponent<Button>(); 
        // The background image of the button (to change color if active)
        Image bgImage = go.GetComponent<Image>(); 

        Text nameText = go.transform.Find("NameText").GetComponent<Text>();
        // Assuming cost is in a container like "CostPanel" -> "CostText"
        Text costText = go.transform.Find("CostPanel/CostText").GetComponent<Text>(); 
        // The main policy icon
        Image policyIcon = go.transform.Find("Icon").GetComponent<Image>(); 

        // --- Set Data ---
        nameText.text = def.Name;
        costText.text = def.Cost.ToString();
        
        // Placeholder for icon setup.
        // If you have specific sprites, load them here based on def.Id
        // policyIcon.sprite = Resources.Load<Sprite>("Icons/Policies/" + def.Id); 

        // --- Set State (Active/Inactive) ---
        bool isActive = PolicyManager.ActivePolicyIds.Contains(def.Id);
        bool canAfford = false;

        if (kingdom != null)
        {
             // Replace with actual data access
            var data = KingdomMetricsSystem.GetKingdomData(kingdom);
            canAfford = data.Treasury >= def.Cost;
        }

        if (isActive)
        {
            // Visual cue for active policies (e.g., Green tint)
            bgImage.color = new Color(0.7f, 1f, 0.7f); 
            costText.text = "Active";
            button.interactable = false; // Cannot buy again
        }
        else
        {
            // Normal color
            bgImage.color = Color.white;
            // If they can't afford it, grey out the button or text
            costText.color = canAfford ? Color.yellow : Color.red; 
            button.interactable = canAfford;
        }

        // --- Set Interactions ---

        // Click Handler
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            if (!isActive && kingdom != null)
            {
                bool success = PolicyManager.TryActivatePolicy(def, kingdom);
                if (success)
                {
                    // Refresh the whole window to update states and treasury views
                    RefreshView(); 
                    // Optionally play a sound
                    // Sfx.Play("buy_sound");
                }
            }
        });

        // Tooltip (Standard WorldBox/NCMS tooltip setup)
        // Adjust based on the specific tooltip system your mod uses.
        string tooltipContent = $"<b>{def.Name}</b>\n\n{def.Description}\n\n<color=yellow>Cost: {def.Cost} Gold</color>";
        // Tooltip.Add(go, tooltipContent); // Placeholder call
    }

    // Helper to get the player's kingdom focusing on. Adjust as needed for your mod.
    private Kingdom GetPlayerKingdom()
    {
        // Example: if using standard NCMS utils or getting the kingdom the camera is over
        // return Config.selectedKingdom;
        return null; // Placeholder
    }

    // Standard window toggle support
    public void Toggle()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}