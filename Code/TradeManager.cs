using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RulerBox
{
    public static class TradeManager
    {
        public class TradeContract
        {
            public int Id;
            public Kingdom Source;
            public Kingdom Target;
            public string ResourceId;
            public int AmountPerTick;
            public int CostPerTick;
            public float Timer;
            public bool IsSelling; // True if Player is Source (Selling), False if Player is Target (Buying)
        }

        private static List<TradeContract> activeContracts = new List<TradeContract>();
        private static int nextId = 1;
        private const float TRADE_INTERVAL = 120f; // 2 minutes

        public static void Tick(float dt)
        {
            if (World.world.isPaused()) return;

            for (int i = activeContracts.Count - 1; i >= 0; i--)
            {
                var c = activeContracts[i];
                
                if (!CanTrade(c.Source, c.Target, out string reason))
                {
                    CancelContract(c, "Relations worsened: " + reason);
                    continue;
                }

                // Validate Kingdoms
                if (c.Source == null || !c.Source.isAlive() || c.Target == null || !c.Target.isAlive())
                {
                    CancelContract(c, "Kingdom destroyed");
                    continue;
                }

                c.Timer += dt;
                if (c.Timer >= TRADE_INTERVAL)
                {
                    c.Timer = 0f;
                    ExecuteContract(c);
                }
            }
        }

        // --- NEW PUBLIC API FOR METRICS ---
        public static long GetTradeIncome(Kingdom k)
        {
            long total = 0;
            foreach (var c in activeContracts)
            {
                if (c.Source == k) // Selling -> Income
                {
                    // Income per tick scaled to "Yearly" projection for the UI?
                    // Metrics usually show yearly rates. 
                    // Trade happens every 120s (2 mins). A year is 60s.
                    // So trade happens every 2 years.
                    // Yearly Income = Cost / 2
                    total += c.CostPerTick / 2;
                }
            }
            return total;
        }

        public static long GetTradeExpenses(Kingdom k)
        {
            long total = 0;
            foreach (var c in activeContracts)
            {
                if (c.Target == k) // Buying -> Expense
                {
                    total += c.CostPerTick / 2;
                }
            }
            return total;
        }
        // ----------------------------------

        public static void AddContract(Kingdom source, Kingdom target, string resource, int amount, int cost, bool isSelling)
        {
            activeContracts.Add(new TradeContract
            {
                Id = nextId++,
                Source = source,
                Target = target,
                ResourceId = resource,
                AmountPerTick = amount,
                CostPerTick = cost,
                Timer = 0f, // Starts fresh
                IsSelling = isSelling
            });
            
            WorldTip.showNow($"Trade contract established: {amount} {resource} every 2m.", false, "top", 2f, "#9EE07A");
        }

        public static void CancelAllTrades(Kingdom player, Kingdom other, string resourceId)
        {
            int removed = activeContracts.RemoveAll(c => 
                c.ResourceId == resourceId && 
                ((c.Source == player && c.Target == other) || (c.Target == player && c.Source == other))
            );

            if (removed > 0)
            {
                WorldTip.showNow($"Cancelled {removed} active trades with {other.data.name}.", false, "top", 2f, "#FF5A5A");
            }
        }

        public static void ExecuteContract(TradeContract c)
        {
            var buyer = c.Target;
            var seller = c.Source;

            var buyerData = KingdomMetricsSystem.Get(buyer);
            var sellerData = KingdomMetricsSystem.Get(seller);

            // 1. Check Money
            if (buyerData.Treasury < c.CostPerTick)
            {
                CancelContract(c, $"{buyer.data.name} cannot afford the payment.");
                return;
            }

            // 2. Check Resource
            int available = GetKingdomResourceCount(seller, c.ResourceId);
            if (available < c.AmountPerTick)
            {
                CancelContract(c, $"{seller.data.name} does not have enough {c.ResourceId}.");
                return;
            }

            // 3. Execute Transfer
            buyerData.Treasury -= c.CostPerTick;
            sellerData.Treasury += c.CostPerTick;

            TransferResource(seller, buyer, c.ResourceId, c.AmountPerTick);

            // Visual Feedback
            if (c.Source == Main.selectedKingdom) 
                WorldTip.showNow($"Trade: Sold {c.AmountPerTick} {c.ResourceId} (+{c.CostPerTick}g)", false, "top", 2f, "#9EE07A");
            else if (c.Target == Main.selectedKingdom)
                WorldTip.showNow($"Trade: Bought {c.AmountPerTick} {c.ResourceId} (-{c.CostPerTick}g)", false, "top", 2f, "#9EE07A");
        }

        public static bool ExecuteOneTimeTrade(Kingdom source, Kingdom target, string resource, int amount, int cost)
        {
            var buyerData = KingdomMetricsSystem.Get(target);
            var sellerData = KingdomMetricsSystem.Get(source);

            if (buyerData.Treasury < cost)
            {
                WorldTip.showNow("Trade Failed: Buyer cannot afford it.", false, "top", 2f, "#FF5A5A");
                return false;
            }

            int available = GetKingdomResourceCount(source, resource);
            if (available < amount)
            {
                WorldTip.showNow("Trade Failed: Seller missing resources.", false, "top", 2f, "#FF5A5A");
                return false;
            }

            buyerData.Treasury -= cost;
            sellerData.Treasury += cost;
            TransferResource(source, target, resource, amount);

            return true;
        }

        private static void CancelContract(TradeContract c, string reason)
        {
            activeContracts.Remove(c);
            
            // Alert Player if involved
            if (c.Source == Main.selectedKingdom || c.Target == Main.selectedKingdom)
            {
                Kingdom other = (c.Source == Main.selectedKingdom) ? c.Target : c.Source;
                EventsSystem.TriggerTradeCancelled(other, c.ResourceId, reason);
            }
        }

        // --- Helpers ---

        public static int CalculatePrice(string resourceId, int amount)
        {
            int totalWorld = 0;
            if (World.world.kingdoms != null)
            {
                foreach(var k in World.world.kingdoms.list)
                {
                    if(k.isAlive()) totalWorld += GetKingdomResourceCount(k, resourceId);
                }
            }

            // Base Prices
            float basePrice = 1f;
            switch(resourceId)
            {
                case "wood": case "stone": basePrice = 1f; break;
                case "wheat": case "berries": basePrice = 2f; break;
                case "bread": case "meat": case "fish": basePrice = 3f; break;
                case "gold": case "mithril": case "adamantine": basePrice = 8f; break;
                default: basePrice = 4f; break;
            }

            // Scarcity Multiplier: The less there is, the higher the price.
            float scarcity = 1f + (5000f / (totalWorld + 500f)); 
            
            float finalPricePerUnit = basePrice * scarcity;
            return Mathf.CeilToInt(finalPricePerUnit * amount);
        }

        private static int GetKingdomResourceCount(Kingdom k, string id)
        {
            int total = 0;
            if (k.cities != null)
            {
                foreach(var city in k.cities)
                {
                    if (city.isAlive()) total += city.getResourcesAmount(id);
                }
            }
            return total;
        }

        public static bool CanTrade(Kingdom k1, Kingdom k2, out string reason)
        {
            reason = "";
            if (k1 == null || k2 == null) return false;
            
            // Check War Status
            if (k1.isEnemy(k2))
            {
                reason = "At War";
                return false;
            }

            // Check Opinion Score
            var opinion = World.world.diplomacy.getOpinion(k1, k2);
            if (opinion != null && opinion.total < -10)
            {
                reason = "Poor Relations";
                return false;
            }

            return true;
        }

        private static void TransferResource(Kingdom from, Kingdom to, string id, int amount)
        {
            // Take from 'from' cities iteratively
            int remaining = amount;
            if (from.cities != null)
            {
                foreach(var city in from.cities)
                {
                    if(remaining <= 0) break;
                    
                    int has = city.getResourcesAmount(id);
                    int take = Math.Min(has, remaining);
                    
                    if(take > 0)
                    {
                        city.takeResource(id, take);
                        remaining -= take;
                    }
                }
            }

            // Give to 'to' capital or distribute
            if (to.capital != null && to.capital.isAlive())
            {
                to.capital.addResourcesToRandomStockpile(id, amount); 
            }
            else if (to.cities != null && to.cities.Count > 0)
            {
                to.cities[0].addResourcesToRandomStockpile(id, amount);
            }
        }
    }
}