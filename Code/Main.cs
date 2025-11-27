using System;
using NCMS;
using UnityEngine;
using HarmonyLib;
using NCMS.Utils;

namespace RulerBox
{
    [ModEntry]
    class Main : MonoBehaviour
    {
        public static Main instance;
        public static Kingdom selectedKingdom;
        public static string ModPath;
        public static Harmony harmony;

        void Start()
        {
            harmony = new Harmony("RulerBox.Mod");
            harmony.PatchAll();   
        }

        void Awake()
        {
            instance = this;
            Config.show_console_on_error = false;
            ModPath = AppDomain.CurrentDomain.BaseDirectory;

            HubUI.Initialize();
            EventsUI.Initialize();
            TopPanelUI.Initialize();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.K)) SelectKingdom();

            if (Main.selectedKingdom != null)
            {
                if (!Main.selectedKingdom.isAlive() || Main.selectedKingdom.data == null)
                {
                    ClearSelection();
                    return;
                }

                ArmySystem.Tick(Time.unscaledDeltaTime);

                if (World.world.isPaused()) return;
                
                float dt = World.world.delta_time; 
                if (dt <= 0f) return;

                KingdomMetricsSystem.Tick(dt);
                HubUI.Refresh();
                TopPanelUI.Refresh();
                EventsSystem.Tick(dt);
                TradeManager.Tick(dt);
                DiplomacyWindow.Refresh(Main.selectedKingdom);
            }
        }

        private void SelectKingdom()
        {
            var pTile = World.world.getMouseTilePos();
            Kingdom kingdom = null;

            if (pTile != null && !World.world.isBusyWithUI())
            {
                var city = pTile.zone?.city;
                kingdom = city?.kingdom;
            }

            if (kingdom == null)
            {
                WorldTip.showNow("No kingdom found under cursor.", false, "top", 2f, "#ff0000ff");
                return;
            }

            selectedKingdom = kingdom;
            SelectedMetas.selected_kingdom = kingdom;
            kingdom.addTrait("tax_rate_tribute_low"); 

            HubUI.SetVisibility(true);
            EventsUI.SetVisible(true);

            WorldTip.showNow($"<b>RulerBox</b>: Focused <b>{kingdom.data.name}</b>", false, "top", 2f, "#9EE07A");
        }

        public void ClearSelection()
        {
            selectedKingdom = null;
            SelectedMetas.selected_kingdom = null;
            HubUI.SetVisibility(false);
            EventsUI.SetVisible(false);
            TopPanelUI.Hide();
            EventsUI.HidePopup(); 
            WorldTip.showNow("Kingdom destroyed or lost.", false, "top", 3f, "#FF5A5A");
        }
    }
}