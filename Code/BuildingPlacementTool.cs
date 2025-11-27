using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace RulerBox
{
    public class BuildingPlacementTool : MonoBehaviour
    {
        public static BuildingPlacementTool Instance;
        private string pendingBuildingId;
        private Kingdom targetKingdom;
        private InvestmentsList.InvestmentDefinition pendingInvestment;
        void Awake()
        {
            Instance = this;
        }
        // Start placing a building for the given kingdom and investment
        public void StartPlacement(Kingdom k, InvestmentsList.InvestmentDefinition investment)
        {
            if (k == null || investment == null) return;
            targetKingdom = k;
            pendingInvestment = investment;
            pendingBuildingId = investment.GetBuildingAssetId(k);
            if (string.IsNullOrEmpty(pendingBuildingId))
            {
                WorldTip.showNow("Cannot determine building type for this race!", true, "top", 2f);
                CancelPlacement();
                return;
            }
            WorldTip.showNow($"Click territory to place: {investment.Name}", false, "top", 3f);
        }
        // Cancel the current building placement
        public void CancelPlacement()
        {
            pendingBuildingId = null;
            targetKingdom = null;
            pendingInvestment = null;
        }
        public bool IsPlacing => !string.IsNullOrEmpty(pendingBuildingId);
        // Update is called once per frame
        void Update()
        {
            if (!IsPlacing) return;
            if (Input.GetMouseButtonDown(1))
            {
                CancelPlacement();
                WorldTip.showNow("Placement Cancelled", false, "top", 1f);
                return;
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject()) return;
                TryPlaceBuilding();
            }
        }
        // Attempt to place the building at the mouse position
        private void TryPlaceBuilding()
        {
            WorldTile tile = World.world.getMouseTilePos();
            if (tile == null) return;
            if (tile.Type.ocean || tile.Type.liquid)
            {
                WorldTip.showNow("Must place on land", true, "top", 1f);
                return;
            }
            if (tile.zone == null || tile.zone.city == null || tile.zone.city.kingdom != targetKingdom)
            {
                WorldTip.showNow("Must place within kingdom territory", true, "top", 1f);
                return;
            }
            City city = tile.zone.city;
            if (!CheckAffordability(city, pendingInvestment))
            {
                WorldTip.showNow($"{city.data.name} needs more resources!", true, "top", 2f);
                return; 
            }
            DeductCosts(city, pendingInvestment);
            BuildingAsset pAsset = AssetManager.buildings.get(pendingBuildingId);
            if (pAsset != null)
            {
                World.world.buildings.addBuilding(pAsset.id, tile);
                if (AssetManager.powers.get("spawn_puff") != null)
                {
                    EffectsLibrary.spawnAt("fx_spawn", tile.posV3, 1f);
                }
            }
            CancelPlacement();
            InvestmentsWindow.Refresh(targetKingdom);
            TopPanelUI.Refresh();
        }
        // Check if the city can afford the investment
        public static bool CheckAffordability(City city, InvestmentsList.InvestmentDefinition def)
        {
            if (city == null || def == null || def.Costs == null) return false;
            // 1. Check Material Resources
            foreach (var cost in def.Costs)
            {
                if (city.getResourcesAmount(cost.ResourceId) < cost.Amount) return false;
            }
            // 2. Check Hidden Treasury Cost (RulerBox Economy)
            if (def.TreasuryPctCost > 0f && city.kingdom != null)
            {
                var d = KingdomMetricsSystem.Get(city.kingdom);
                if (d != null && d.Treasury > 0)
                {
                    long tax = (long)(d.Treasury * def.TreasuryPctCost);
                    if (tax > d.Treasury) return false;
                }
            }
            return true;
        }
        // Deduct the costs of the investment from the city
        private void DeductCosts(City city, InvestmentsList.InvestmentDefinition def)
        {
            // Deduct Material Resources
            foreach (var cost in def.Costs)
            {
                int remaining = cost.Amount;
                if (city.storages != null) 
                {
                    foreach(Building storage in city.storages)
                    {
                        if (remaining <= 0) break;
                        if (!storage.isUsable() || storage.resources == null) continue;

                        int has = storage.resources.get(cost.ResourceId);
                        if (has > 0)
                        {
                            int take = Mathf.Min(has, remaining);
                            storage.resources.change(cost.ResourceId, -take);
                            remaining -= take;
                        }
                    }
                }
            }
            // Deduct Hidden Treasury Cost (RulerBox Economy)
            if (def.TreasuryPctCost > 0f && city.kingdom != null)
            {
                var d = KingdomMetricsSystem.Get(city.kingdom);
                if (d != null && d.Treasury > 0)
                {
                    long tax = (long)(d.Treasury * def.TreasuryPctCost);
                    if (tax > 0)
                    {
                        d.Treasury -= tax;
                        // WorldTip.showNow($"Investment Cost: -{tax} Gold", ...);
                    }
                }
            }
        }
    }
}