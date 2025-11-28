using System;
using System.Collections.Generic;
using UnityEngine;

namespace RulerBox
{
    public static class EventsList
    {
        //========================================================================================
        // TO FIX AFTER EVERYTHING IS DONE
        //========================================================================================

        // Helper to get KingdomMetricsSystem data
        private static KingdomMetricsSystem.Data GetData(Kingdom k)
        {
            return KingdomMetricsSystem.Get(k);
        }

        // Event Definitions
        public class EventDef  
        {
            public string Id;
            public string Title;
            public string Text;
            public string ImagePath; // Optional image
            public Func<Kingdom, bool> Trigger;
            public List<EventOption> Options = new List<EventOption>();
        }
        
        // Event Option Definition
        public class EventOption
        {
            public string Text;
            public string Tooltip;
            public Action<Kingdom> Action;
        }
        
        public static readonly List<EventDef> Definitions = new List<EventDef>();
        
        // Static constructor to initialize events
        static EventsList()
        {
            // ==========================================
            // ECONOMY EVENTS
            // ==========================================

            // 1. Stock Market Boom
            Definitions.Add(new EventDef
            {
                Id = "econ_boom",
                Title = "Economic Boom",
                Text = "The country's economy has been performing better than usual. This is a testament to our stable conditions and strong economic policies.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Trigger if high stability and positive balance
                    return d.Stability > 70f && d.Balance > 100 && UnityEngine.Random.value < 0.02f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Reap the benefits",
                        Tooltip = "<color=#7CFC00>Gain 5,000 Gold</color>\n<color=#7CFC00>Stability +5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 5000;
                            d.Stability += 5f;
                            // Add a temporary stability boost effect
                            d.ActiveEffects.Add(new TimedEffect(60f, 0.5f)); // +0.5 stab/sec for 60s
                        }
                    }
                }
            });
            // 2. Hyperinflation (Crisis)
            Definitions.Add(new EventDef
            {
                Id = "econ_hyperinflation",
                Title = "Hyperinflation",
                Text = "Due to recent monetary decisions and spiraling debt, the value of our currency is plummeting! Our economy is crippling.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Trigger if deep in debt
                    return d.Treasury < -2000 && UnityEngine.Random.value < 0.05f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Perhaps that was a bad idea...",
                        Tooltip = "<color=#FF5A5A>Lose 200% Treasury</color>\n<color=#FF5A5A>Stability -20</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            long debt = Math.Abs(d.Treasury);
                            d.Treasury -= (long)(debt * 2f); 
                            //d.Treasury = -5000; // Reset to a flat debt
                            d.Stability -= 20f;
                            d.ActiveEffects.Add(new TimedEffect(120f, -0.2f)); // Negative drift
                        }
                    },
                    new EventOption
                    {
                        Text = "Print more money (Monetary Policy)",
                        Tooltip = "<color=#7CFC00>Gain 10,000 Gold</color>\n<color=#FF5A5A>Corruption +10%</color>\n<color=#FF5A5A>Stability -5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 10000;
                            d.CorruptionLevel += 0.10f;
                            d.Stability -= 5f;
                        }
                    }
                }
            });
            // 3. IMF Intervention
            Definitions.Add(new EventDef
            {
                Id = "econ_imf",
                Title = "IMF Intervention",
                Text = "Our treasury is in ruins. The International Monetary Fund offers to assist, but demands strict austerity policies in return.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    return d.Treasury < -5000 && UnityEngine.Random.value < 0.1f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Accept Offer",
                        Tooltip = "<color=#7CFC00>Treasury reset to 0</color>\n<color=#FF5A5A>Stability drift -0.5/sec (Depression)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury = 0;
                            d.ActiveEffects.Add(new TimedEffect(120f, -0.5f)); // Economic Depression effect
                        }
                    },
                    new EventOption
                    {
                        Text = "Refuse Offer",
                        Tooltip = "We will not let foreigners dictate us! (No Effect)",
                        Action = k => { /* Do nothing */ }
                    }
                }
            });
            
            // ==========================================
            // UNREST & STRIKES
            // ==========================================
            
            // 4. Strikers Make Demands
            Definitions.Add(new EventDef
            {
                Id = "unrest_strikers",
                Title = "Strikers Make Demands",
                Text = "Workers are dissatisfied with conditions and threaten a nationwide strike unless demands are met.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Trigger if Stability is low or Unemployment high
                    return d.Stability < 45f && UnityEngine.Random.value < 0.03f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Accept Demands",
                        Tooltip = "<color=#FF5A5A>Cost 1,000 Gold</color>\n<color=#7CFC00>Stability +2</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 1000;
                            d.Stability += 2f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Refuse Demands",
                        Tooltip = "<color=#FF5A5A>Workers Strike!</color>\n<color=#FF5A5A>Stability -10</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 10f;
                            d.TaxRateLocal *= 0.8f;
                            d.ActiveEffects.Add(new TimedEffect(60f, -0.2f));
                        }
                    },
                    new EventOption
                    {
                        Text = "Repress the workers",
                        Tooltip = "<color=#FF5A5A>Stability -3</color>\n<color=#FF5A5A>War Exhaustion +0.5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 3f;
                            d.WarExhaustion += 0.5f;
                            d.ManpowerCurrent = (long)(d.ManpowerCurrent * 0.95f);
                        }
                    }
                }
            });
            
            // 5. Mass Demonstrations
            Definitions.Add(new EventDef
            {
                Id = "unrest_demonstrations",
                Title = "Mass Demonstrations",
                Text = "The general population is threatening mass demonstrations unless the government resigns.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    return d.Stability < 30f && UnityEngine.Random.value < 0.05f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Concede",
                        Tooltip = "<color=#7CFC00>Stability +5</color>\n<color=#7CFC00>Corruption -10%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability += 5f;
                            d.CorruptionLevel = Mathf.Max(0, d.CorruptionLevel - 0.1f);
                        }
                    },
                    new EventOption
                    {
                        Text = "Crackdown",
                        Tooltip = "<color=#FF5A5A>Stability -8</color>\n<color=#FF5A5A>War Exhaustion +1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 8f;
                            d.WarExhaustion += 1f;
                        }
                    }
                }
            });
            
            // ==========================================
            // WAR & MILITARY
            // ==========================================
            
            // 6. Widespread Mutinies
            Definitions.Add(new EventDef
            {
                Id = "mil_mutiny",
                Title = "Widespread Mutinies",
                Text = "Soldiers are refusing orders due to low pay and high exhaustion.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    return d.WarExhaustion > 20f && UnityEngine.Random.value < 0.04f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Increase Wages",
                        Tooltip = "<color=#FF5A5A>Cost 2,000 Gold</color>\n<color=#7CFC00>Mutiny ends</color>",
                        Action = k => GetData(k).Treasury -= 2000
                    },
                    new EventOption
                    {
                        Text = "Set an example",
                        Tooltip = "<color=#FF5A5A>Execute deserters</color>\n<color=#FF5A5A>Stability -5</color>\n<color=#FF5A5A>War Exhaustion +1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 5f;
                            d.WarExhaustion += 1f;
                            d.Soldiers = (int)(d.Soldiers * 0.9f); // Lose 10% soldiers
                        }
                    }
                }
            });
            
            // 7. The Last Stand (Desperation Bonus)
            Definitions.Add(new EventDef
            {
                Id = "mil_last_stand",
                Title = "The Last Stand",
                Text = "All is not lost! Our population rallies in one final act of defiance.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Trigger if actively in war (implied by WE) and low manpower
                    return d.WarExhaustion > 50f && d.ManpowerCurrent < 100 && UnityEngine.Random.value < 0.1f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "We shall never surrender!",
                        Tooltip = "<color=#7CFC00>Gain 500 Manpower</color>\n<color=#7CFC00>Stability +5</color>\n<color=#7CFC00>War Exhaustion -1.5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ManpowerCurrent += 500;
                            d.Stability += 5f;
                            d.WarExhaustion = Mathf.Max(0, d.WarExhaustion - 1.5f);
                        }
                    }
                }
            });
            
            // ==========================================
            // INSURGENCY
            // ==========================================
            
            // 8. Spread of Insurgency
            Definitions.Add(new EventDef
            {
                Id = "insurgency_start",
                Title = "Spread of Insurgency",
                Text = "Dissatisfied citizens are taking up arms. It is not a civil war yet, but it is dangerous.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Very low stability trigger
                    return d.Stability < 20f && d.CorruptionLevel > 0.3f && UnityEngine.Random.value < 0.05f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Target practice for the military!",
                        Tooltip = "<color=#FF5A5A>Stability -5</color>\n<color=#FF5A5A>Active Conflict starts</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 5f;
                            d.ActiveEffects.Add(new TimedEffect(300f, -0.1f)); // Long term drain
                        }
                    }
                }
            });
            
            // 9. Capital Bombing (Insurgency Event)
            Definitions.Add(new EventDef
            {
                Id = "insurgency_bombing",
                Title = "Capital Bombing",
                Text = "Insurgents have orchestrated a bombing in the capital!",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Only happens if stability is already critical
                    return d.Stability < 25f && UnityEngine.Random.value < 0.03f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Mourn the victims",
                        Tooltip = "<color=#FF5A5A>Stability -3</color>",
                        Action = k => GetData(k).Stability -= 3f
                    },
                    new EventOption
                    {
                        Text = "Vow Revenge",
                        Tooltip = "<color=#FF5A5A>Stability -1.5</color>\n<color=#FF5A5A>War Exhaustion +1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 1.5f;
                            d.WarExhaustion += 1f;
                        }
                    }
                }
            });
            
            // ==========================================
            // CORRUPTION & SCANDALS
            // ==========================================
            
            // 10. Public Scandal
            Definitions.Add(new EventDef
            {
                Id = "cor_scandal",
                Title = "Public Scandal",
                Text = "A politician has been accused of a grievous crime.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    return d.CorruptionLevel > 0.2f && UnityEngine.Random.value < 0.02f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Coverup",
                        Tooltip = "<color=#FF5A5A>Cost 500 Gold</color>\n<color=#FF5A5A>Corruption +5%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 500;
                            d.CorruptionLevel += 0.05f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Do nothing",
                        Tooltip = "<color=#FF5A5A>Stability -2.5</color>",
                        Action = k => GetData(k).Stability -= 2.5f
                    }
                }
            });
            
            // 11. Corporate Dealing
            Definitions.Add(new EventDef
            {
                Id = "cor_dealing",
                Title = "Corporate Dealing",
                Text = "A national corporation offers a 'generous donation' in exchange for incentives.",
                Trigger = k => UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Accept",
                        Tooltip = "<color=#7CFC00>Gain 3,000 Gold</color>\n<color=#FF5A5A>Corruption +10%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 3000;
                            d.CorruptionLevel += 0.10f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Reject",
                        Tooltip = "No effect.",
                        Action = k => { }
                    }
                }
            });
            
            // 12. A New Researcher
            Definitions.Add(new EventDef
            {
                Id = "util_researcher",
                Title = "A New Researcher",
                Text = "A brilliant researcher is seeking employment. He is expensive but skilled.",
                Trigger = k => UnityEngine.Random.value < 0.01f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Hire Them",
                        Tooltip = "<color=#FF5A5A>Cost 800 Gold</color>\n<color=#7CFC00>Gain 300 Research Power</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 800;
                        }
                    },
                    new EventOption
                    {
                        Text = "Too expensive",
                        Tooltip = "No effect.",
                        Action = k => { }
                    }
                }
            });
        }
        
        // Method to get a random valid event for a kingdom
        public static EventDef GetRandomEvent(Kingdom k)
        {
            var valid = new List<EventDef>();
            foreach (var def in Definitions)
            {
                if (def.Trigger != null && def.Trigger(k))
                {
                    valid.Add(def);
                }
            }

            if (valid.Count > 0)
            {
                return valid[UnityEngine.Random.Range(0, valid.Count)];
            }
            return null;
        }
    }
}