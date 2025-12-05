using System;
using System.Collections.Generic;
using UnityEngine;

namespace RulerBox
{
    public static class EventsList
    {
        
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
                            d.ActiveEffects.Add(new TimedEffect("econ_boom", 60f, 0f, 10f)); // +10 Stability Target for 60s
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
                    return d.Treasury < -50 && UnityEngine.Random.value < 0.05f;
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
                            d.ActiveEffects.Add(new TimedEffect("econ_hyperinflation", 120f, 0f, -10f)); // -10 Stability Target
                        }
                    },
                    new EventOption
                    {
                        Text = "Print more money (Monetary Policy)",
                        Tooltip = "<color=#7CFC00>Gain 3,000 Gold</color>\n<color=#FF5A5A>Corruption +10%</color>\n<color=#FF5A5A>Stability -5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 3000;
                            d.CorruptionFromEvents += 0.10f; // +10%
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
                            d.ActiveEffects.Add(new TimedEffect("econ_imf", 120f, 0f, -20f)); // Economic Depression effect (-20 Target)
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
                            d.ActiveEffects.Add(new TimedEffect("unrest_strikers", 60f, 0f, -10f));
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
                            d.CorruptionFromEvents -= 0.10f; // -10%
                            if (d.CorruptionFromEvents < 0) d.CorruptionFromEvents = 0;
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
            // HEALTH & PLAGUE
            // ==========================================

            // 8. The Black Death (Plague)
            Definitions.Add(new EventDef
            {
                Id = "health_plague",
                Title = "The Plague Spreads",
                Text = "A deadly pestilence is sweeping through our cities! The people are dying in the streets.",
                Trigger = k => 
                {
                    var d = GetData(k);
                    // Trigger if Plague Risk is high enough
                    return d.PlagueRisk > 50f && UnityEngine.Random.value < 0.05f;
                },
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Enforce Quarantine",
                        Tooltip = "<color=#FF5A5A>Cost 500 Gold</color>\n<color=#7CFC00>Plague Risk -30</color>\n<color=#7CFC00>Stability +5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            if (d.Treasury >= 500)
                            {
                                d.Treasury -= 500;
                                d.PlagueRiskAccumulator -= 30f; // Reduce risk
                                d.Stability += 5f;
                            }
                            else
                            {
                                // Failed quarantine due to lack of funds
                                d.Stability -= 5f;
                                d.Population = (long)(d.Population * 0.9f);
                            }
                        }
                    },
                    new EventOption
                    {
                        Text = "Let it burn out",
                        Tooltip = "<color=#FF5A5A>Population -15%</color>\n<color=#FF5A5A>Stability -10</color>\n<color=#7CFC00>Plague Risk Reset</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Population = (long)(d.Population * 0.85f);
                            d.Stability -= 10f;
                            d.PlagueRiskAccumulator = -20f; // Reset risk significantly
                        }
                    },
                    new EventOption
                    {
                        Text = "Pray for salvation",
                        Tooltip = "<color=#7CFC00>Culture Knowledge +100</color>\n<color=#FF5A5A>Population -5%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Population = (long)(d.Population * 0.95f);
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
                            d.ActiveEffects.Add(new TimedEffect("insurgency_start", 300f, -0.1f)); // Long term drain
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
                            d.CorruptionFromEvents += 0.05f; // +5%
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
                        Tooltip = "<color=#7CFC00>Gain 1,500 Gold</color>\n<color=#FF5A5A>Corruption +10%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 1500;
                            d.CorruptionFromEvents += 0.10f; // +10%
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
            // ==========================================
            // NEW EVENTS (User Requested)
            // ==========================================

            // 13. Monopoly Formation
            Definitions.Add(new EventDef
            {
                Id = "econ_monopoly",
                Title = "Monopoly Formation",
                Text = "A powerful corporation has monopolized a key industry, driving up prices but increasing efficiency.",
                Trigger = k => GetData(k).CorruptionLevel > 0.15f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Allow it",
                        Tooltip = "<color=#7CFC00>Gain 2,000 Gold</color>\n<color=#FF5A5A>Corruption +10%</color>\n<color=#FF5A5A>Stability -5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 2000;
                            d.CorruptionFromEvents += 0.10f; // +10%
                            d.Stability -= 5f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Break it up",
                        Tooltip = "<color=#FF5A5A>Cost 1,000 Gold</color>\n<color=#7CFC00>Stability +5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 1000;
                            d.Stability += 5f;
                        }
                    }
                }
            });

            // 14. National Monument
            Definitions.Add(new EventDef
            {
                Id = "cult_monument",
                Title = "National Monument",
                Text = "Architects propose a grand monument to celebrate our nation's glory.",
                Trigger = k => GetData(k).Treasury > 5000 && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Build it!",
                        Tooltip = "<color=#FF5A5A>Cost 3,000 Gold</color>\n<color=#7CFC00>Stability +10 (Permanent Bonus)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 3000;
                            d.ActiveEffects.Add(new TimedEffect("national_monument", 99999f, 0f)); // Permanent modifier
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

            // 15. Military Parade
            Definitions.Add(new EventDef
            {
                Id = "mil_parade",
                Title = "Military Parade",
                Text = "Generals suggest a military parade to boost morale and show strength.",
                Trigger = k => GetData(k).Soldiers > 100 && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Authorize Parade",
                        Tooltip = "<color=#FF5A5A>Cost 500 Gold</color>\n<color=#7CFC00>Stability +5</color>\n<color=#7CFC00>War Exhaustion -2</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 500;
                            d.Stability += 5f;
                            d.WarExhaustion = Mathf.Max(0, d.WarExhaustion - 2f);
                        }
                    },
                    new EventOption
                    {
                        Text = "Unnecessary",
                        Tooltip = "No effect.",
                        Action = k => { }
                    }
                }
            });

            // 16. Anti-War Protests
            Definitions.Add(new EventDef
            {
                Id = "unrest_antiwar",
                Title = "Anti-War Protests",
                Text = "Citizens are protesting against the ongoing conflict.",
                Trigger = k => GetData(k).WarExhaustion > 15f && UnityEngine.Random.value < 0.05f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Suppress Protests",
                        Tooltip = "<color=#FF5A5A>Stability -5</color>\n<color=#FF5A5A>War Exhaustion +1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 5f;
                            d.WarExhaustion += 1f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Listen to them",
                        Tooltip = "<color=#FF5A5A>Stability -2</color>\n<color=#7CFC00>War Exhaustion -1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 2f;
                            d.WarExhaustion = Mathf.Max(0, d.WarExhaustion - 1f);
                        }
                    }
                }
            });

            // 17. Deserters
            Definitions.Add(new EventDef
            {
                Id = "mil_deserters",
                Title = "Mass Desertions",
                Text = "Reports indicate that soldiers are deserting their posts in large numbers.",
                Trigger = k => GetData(k).WarExhaustion > 30f && UnityEngine.Random.value < 0.04f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Disgraceful!",
                        Tooltip = "<color=#FF5A5A>Lose 10% Manpower</color>\n<color=#FF5A5A>Manpower Cap -20% (120s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ManpowerCurrent = (long)(d.ManpowerCurrent * 0.9f);
                            d.ActiveEffects.Add(new TimedEffect("chronic_desertions", 120f, -0.1f));
                        }
                    }
                }
            });

            // 18. Skills Shortage
            Definitions.Add(new EventDef
            {
                Id = "econ_shortage",
                Title = "Skills Shortage",
                Text = "The war has drained the workforce of skilled labor, impacting industry and research.",
                Trigger = k => GetData(k).WarExhaustion > 40f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "A tragedy",
                        Tooltip = "<color=#FF5A5A>Factory Output -20% (180s)</color>\n<color=#FF5A5A>Research -20% (180s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ActiveEffects.Add(new TimedEffect("skills_shortage", 180f, 0f));
                        }
                    }
                }
            });

            // 19. Blackmail Threat
            Definitions.Add(new EventDef
            {
                Id = "cor_blackmail",
                Title = "Blackmail Threat",
                Text = "A high-ranking official is being blackmailed by a criminal organization.",
                Trigger = k => GetData(k).CorruptionLevel > 0.2f && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Pay them off",
                        Tooltip = "<color=#FF5A5A>Cost 1,500 Gold</color>\n<color=#FF5A5A>Corruption +5%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 1500;
                            d.CorruptionFromEvents += 0.05f; // +5%
                        }
                    },
                    new EventOption
                    {
                        Text = "Refuse",
                        Tooltip = "<color=#FF5A5A>Stability -10</color>\n<color=#FF5A5A>Scandal exposed</color>",
                        Action = k => GetData(k).Stability -= 10f
                    }
                }
            });

            // 20. Substandard Weapons
            Definitions.Add(new EventDef
            {
                Id = "mil_bad_weapons",
                Title = "Substandard Weapons",
                Text = "It has been revealed that a military supplier provided defective equipment.",
                Trigger = k => GetData(k).CorruptionLevel > 0.25f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Investigate",
                        Tooltip = "<color=#FF5A5A>Cost 500 Gold</color>\n<color=#7CFC00>Corruption -5%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 500;
                            d.CorruptionFromEvents -= 0.05f; // -5%
                            if (d.CorruptionFromEvents < 0) d.CorruptionFromEvents = 0;
                        }
                    },
                    new EventOption
                    {
                        Text = "Ignore it",
                        Tooltip = "<color=#FF5A5A>Army Attack -15% (120s)</color>\n<color=#FF5A5A>Stability -5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 5f;
                            d.ActiveEffects.Add(new TimedEffect("substandard_weapons", 120f, 0f));
                        }
                    }
                }
            });

            // 21. Exclusive Military Contracts
            Definitions.Add(new EventDef
            {
                Id = "cor_contracts",
                Title = "Exclusive Contracts",
                Text = "Defense contractors offer a large bribe for exclusive rights.",
                Trigger = k => UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Accept Bribe",
                        Tooltip = "<color=#7CFC00>Gain 5,000 Gold</color>\n<color=#FF5A5A>Corruption +15%</color>\n<color=#FF5A5A>Upkeep -10% (120s)</color>\n<color=#7CFC00>Factory Output +5% (120s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury += 5000;
                            d.CorruptionFromEvents += 0.15f; // +15%
                            d.ActiveEffects.Add(new TimedEffect("powerful_mic", 120f, 0f));
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

            // 22. Power Station Sabotaged
            Definitions.Add(new EventDef
            {
                Id = "ins_sabotage",
                Title = "Power Station Sabotaged",
                Text = "Insurgents have sabotaged a major power station, crippling industry.",
                Trigger = k => GetData(k).Stability < 30f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Repair it",
                        Tooltip = "<color=#FF5A5A>Cost 1,000 Gold</color>",
                        Action = k => GetData(k).Treasury -= 1000
                    },
                    new EventOption
                    {
                        Text = "Leave it",
                        Tooltip = "<color=#FF5A5A>Factory Output -40% (120s)</color>",
                        Action = k => GetData(k).ActiveEffects.Add(new TimedEffect("rolling_blackouts", 120f, -0.1f))
                    }
                }
            });

            // 23. Key Insurgent Leader Killed
            Definitions.Add(new EventDef
            {
                Id = "ins_leader_killed",
                Title = "Insurgent Leader Killed",
                Text = "Special forces have eliminated a key insurgent leader.",
                Trigger = k => GetData(k).Stability < 40f && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Excellent news!",
                        Tooltip = "<color=#7CFC00>Stability +10</color>\n<color=#7CFC00>War Exhaustion -2</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability += 10f;
                            d.WarExhaustion = Mathf.Max(0, d.WarExhaustion - 2f);
                        }
                    }
                }
            });

            // 24. Popular War Support
            Definitions.Add(new EventDef
            {
                Id = "mil_war_support",
                Title = "Popular War Support",
                Text = "The population is rallying behind the war effort!",
                Trigger = k => GetData(k).WarExhaustion > 10f && GetData(k).Stability > 60f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Mobilize them!",
                        Tooltip = "<color=#7CFC00>Manpower Cap +20% (120s)</color>\n<color=#7CFC00>WE Gain -20% (120s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ManpowerMaxMultiplier *= 1.2f;
                            d.WarExhaustionGainMultiplier *= 0.8f;
                            d.ActiveEffects.Add(new TimedEffect("popular_war_support", 120f, 0f));
                        }
                    },
                    new EventOption
                    {
                        Text = "Ignore",
                        Tooltip = "No effect.",
                        Action = k => { }
                    }
                }
            });

            // ==========================================
            // NEW EVENTS BATCH 2 (Golden Age, etc.)
            // ==========================================

            // 25. Golden Age
            Definitions.Add(new EventDef
            {
                Id = "cult_golden_age",
                Title = "Golden Age",
                Text = "Arts, sciences, and commerce are flourishing! Our nation is the envy of the world.",
                Trigger = k => GetData(k).Stability > 80f && GetData(k).Treasury > 2000 && UnityEngine.Random.value < 0.01f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "It is our destiny!",
                        Tooltip = "<color=#7CFC00>Culture +150</color>\n<color=#7CFC00>Research +20% (300s)</color>\n<color=#7CFC00>Tax +10% (300s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ActiveEffects.Add(new TimedEffect("golden_age", 300f, 0f));
                        }
                    }
                }
            });

            // 26. Famine
            Definitions.Add(new EventDef
            {
                Id = "disaster_famine",
                Title = "Great Famine",
                Text = "Crops have failed and our stockpiles are running low. The people are starving.",
                Trigger = k => GetData(k).Population > 100 && UnityEngine.Random.value < 0.015f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Import Food",
                        Tooltip = "<color=#FF5A5A>Cost 1,000 Gold</color>\n<color=#7CFC00>Mitigate Famine</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            if (d.Treasury >= 1000)
                            {
                                d.Treasury -= 1000;
                            }
                            else
                            {
                                 WorldTip.showNow("Not enough gold! Thousands starve.", false, "top", 3f, "#FF5A5A");
                                 d.Population = (long)(d.Population * 0.9f);
                                 d.Stability -= 10f;
                            }
                        }
                    },
                    new EventOption
                    {
                        Text = "Let them eat cake",
                        Tooltip = "<color=#FF5A5A>Population -10%</color>\n<color=#FF5A5A>Stability -15</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Population = (long)(d.Population * 0.9f);
                            d.Stability -= 15f;
                        }
                    }
                }
            });

            // 27. Diplomatic Incident
            Definitions.Add(new EventDef
            {
                Id = "dip_incident",
                Title = "Diplomatic Incident",
                Text = "One of our ambassadors has insulted a foreign dignitary.",
                Trigger = k => UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Apologize formally",
                        Tooltip = "<color=#FF5A5A>Cost 200 Gold</color>\n<color=#FF5A5A>PP Gain -10% (120s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 200;
                            d.ActiveEffects.Add(new TimedEffect("humiliated_diplomat", 120f, 0f));
                        }
                    },
                    new EventOption
                    {
                        Text = "Stand by our man",
                        Tooltip = "<color=#FF5A5A>War Exhaustion +3</color>\n<color=#7CFC00>Stability +1</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.WarExhaustion += 3f;
                            d.Stability += 1f;
                        }
                    }
                }
            });

            // 28. Mineral Discovery
            Definitions.Add(new EventDef
            {
                Id = "econ_mineral",
                Title = "Rich Vein Discovered",
                Text = "Prospectors have found a massive deposit of precious metals!",
                Trigger = k => UnityEngine.Random.value < 0.01f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Develop it",
                        Tooltip = "<color=#FF5A5A>Cost 500 Gold</color>\n<color=#7CFC00>Resource Output +25% (Permanent)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 500;
                            d.ResourceOutputModifier += 0.25f; // Permanent boost (simplified)
                        }
                    },
                    new EventOption
                    {
                        Text = "Quick Cash",
                        Tooltip = "<color=#7CFC00>Gain 2,000 Gold</color>",
                        Action = k => GetData(k).Treasury += 2000
                    }
                }
            });

            // 29. General's Coup
            Definitions.Add(new EventDef
            {
                Id = "mil_coup",
                Title = "General's Ambition",
                Text = "A popular general is gathering support to overthrow the government!",
                Trigger = k => GetData(k).Stability < 25f && GetData(k).Soldiers > 50 && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Purge the Military",
                        Tooltip = "<color=#FF5A5A>Lose 50% Manpower</color>\n<color=#FF5A5A>Stability -10</color>\n<color=#7CFC00>Prevent Coup</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.ManpowerCurrent /= 2;
                            d.Stability -= 10f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Offer Power Sharing",
                        Tooltip = "<color=#7CFC00>Stability +10</color>\n<color=#FF5A5A>Corruption +20%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability += 10f;
                            d.CorruptionFromEvents += 0.20f;
                        }
                    }
                }
            });

            // 30. Political Scandal
            Definitions.Add(new EventDef
            {
                Id = "pol_scandal_major",
                Title = "Political Scandal",
                Text = "A major scandal involving the royal family has erupted!",
                Trigger = k => UnityEngine.Random.value < 0.015f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Cover it up",
                        Tooltip = "<color=#FF5A5A>Cost 150 Gold</color>\n<color=#FF5A5A>Corruption +15%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 150;
                            d.CorruptionFromEvents += 0.15f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Face the public",
                        Tooltip = "<color=#FF5A5A>Stability -20</color>",
                        Action = k => GetData(k).Stability -= 20f
                    }
                }
            });

            // 31. Grand Festival
            Definitions.Add(new EventDef
            {
                Id = "cult_grand_festival",
                Title = "Grand Festival",
                Text = "The people wish to hold a grand festival to celebrate our culture.",
                Trigger = k => GetData(k).Treasury > 300 && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Fund it",
                        Tooltip = "<color=#FF5A5A>Cost 200 Gold</color>\n<color=#7CFC00>Stability +15</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 200;
                            d.Stability += 15f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Refuse",
                        Tooltip = "<color=#FF5A5A>Stability -5</color>",
                        Action = k => GetData(k).Stability -= 5f
                    }
                }
            });

            // 32. Local Warlord
            Definitions.Add(new EventDef
            {
                Id = "mil_warlord",
                Title = "Local Warlord Rises",
                Text = "With central authority weak, a local warlord has seized control of a province in the chaos!",
                Trigger = k => GetData(k).Stability < 30f && UnityEngine.Random.value < 0.02f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Crush him!",
                        Tooltip = "<color=#FF5A5A>Stability -5</color>\n<color=#FF5A5A>War Exhaustion +5</color>\n<color=#7CFC00>Show of Force</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 5f;
                            d.WarExhaustion += 5f;
                        }
                    },
                    new EventOption
                    {
                        Text = "Bribe him",
                        Tooltip = "<color=#FF5A5A>Cost 250 Gold</color>\n<color=#7CFC00>Stability +5</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Treasury -= 250;
                            d.Stability += 5f;
                        }
                    }
                }
            });

            // 33. Plague of Madness
            Definitions.Add(new EventDef
            {
                Id = "disaster_madness",
                Title = "Plague of Madness",
                Text = "A strange affliction is driving people mad! Chaos reigns in the streets.",
                Trigger = k => UnityEngine.Random.value < 0.005f, // Rare
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Purge the afflicted",
                        Tooltip = "<color=#FF5A5A>Stability -25</color>\n<color=#FF5A5A>Population -5%</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability -= 25f;
                            d.Population = (long)(d.Population * 0.95f);
                        }
                    },
                    new EventOption
                    {
                        Text = "Do nothing",
                        Tooltip = "<color=#FF5A5A>Stability -40</color>",
                        Action = k => GetData(k).Stability -= 40f
                    }
                }
            });

            // 34. Heir Born
            Definitions.Add(new EventDef
            {
                Id = "cult_heir",
                Title = "Royal Heir Born",
                Text = "A healthy heir has been born to the royal family! The nation rejoices.",
                Trigger = k => UnityEngine.Random.value < 0.01f, // Rare
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Celebrate!",
                        Tooltip = "<color=#7CFC00>Stability +20</color>\n<color=#FF5A5A>Cost 50 Gold</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            d.Stability += 20f;
                            d.Treasury -= 50;
                        }
                    }
                }
            });

            // 35. Mercenary Contract
            Definitions.Add(new EventDef
            {
                Id = "mil_mercenaries",
                Title = "Mercenary Company",
                Text = "A famous mercenary company offers their services for a reasonable price.",
                Trigger = k => GetData(k).WarExhaustion > 5f && UnityEngine.Random.value < 0.03f,
                Options = new List<EventOption>
                {
                    new EventOption
                    {
                        Text = "Hire them",
                        Tooltip = "<color=#FF5A5A>Cost 200 Gold</color>\n<color=#7CFC00>Manpower +100</color>\n<color=#7CFC00>Army Attack +10% (180s)</color>",
                        Action = k => 
                        {
                            var d = GetData(k);
                            if (d.Treasury >= 200)
                            {
                                d.Treasury -= 200;
                                d.ManpowerCurrent += 100;
                                d.ActiveEffects.Add(new TimedEffect("mercs_hired", 180f, 0f));
                            }
                        }
                    },
                    new EventOption
                    {
                        Text = "Dismiss",
                        Tooltip = "No effect",
                        Action = k => { }
                    }
                }
            });
        }

        public static EventDef GetRandomEvent(Kingdom k)
        {
            var validEvents = new List<EventDef>();
            foreach (var def in Definitions)
            {
                if (def.Trigger(k))
                {
                    validEvents.Add(def);
                }
            }
            
            if (validEvents.Count == 0) return null;
            return validEvents[UnityEngine.Random.Range(0, validEvents.Count)];
        }
    }
}