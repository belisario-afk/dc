using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    /*
     * DeathmatchSoccer - 3-Team Soccer Battle Plugin
     * 
     * FEATURES:
     * - 3-Team System: Blue (SHELL-SEA/GRUB), Red (Loot-pool/DOORCAMPER), Black (PZG/ROAMER)
     * - Custom Skins: Each team can have unique skins via Skins.cs plugin
     * - Modern UI: Team selection menu, 3-team scoreboard, role selection
     * - 2 Roles: Striker (100HP, Thompson) and Goalie (200HP, SPAS-12)
     * 
     * ADMIN COMMANDS:
     * /set_red, /set_blue, /set_black - Set goal positions
     * /set_center - Set ball spawn position
     * /save_goals, /load_goals - Persist arena data
     * /start_match - Begin the match
     * /setskin <team> <item> <skinId> - Configure team skins
     * /showskins - Display all skin configurations
     * /goal_debug - Toggle goal zone visualization
     * 
     * PLAYER COMMANDS:
     * /join [team] - Join a team (shows UI if no team specified)
     * /teams - Show team selection UI
     */
    [Info("DeathmatchSoccer", "KillaDome", "5.1.0")]
    [Description("3-Team Deathmatch Soccer with Custom Skins and Modern UI")]
    public class DeathmatchSoccer : RustPlugin
    {
        // ==========================================
        // 1. CONFIGURATION
        // ==========================================
        private string middlewareUrl = "http://165.22.174.250/chat"; 
        private string licenseKey = "2c573abe-3172-4fc9-b834-ba4c70fb1eb8";

        // SETTINGS
        private float KickForceMultiplier = 3500.0f; 
        private float MaxKickDistance = 15.0f; 
        private float LeashRadius = 15.0f;     
        private int ScoreToWin = 5;
        
        // GOAL BOX (Overwritten by LoadData)
        private float GoalWidth = 8.0f;
        private float GoalHeight = 4.0f;
        private float GoalDepth = 6.0f; 

        // IMAGES
        private string ImgScoreboardBg = "https://i.imgur.com/6Xq6x9q.png"; 
        private string ImgGoalBanner = "https://i.imgur.com/Jb9y1Xm.png";   

        [PluginReference] Plugin ImageLibrary;
        [PluginReference] Plugin Skins;

        // CUSTOM SKIN IDS (Configure these for each team)
        private Dictionary<string, TeamSkins> teamSkins = new Dictionary<string, TeamSkins>
        {
            { "blue", new TeamSkins { 
                TshirtSkin = 0,      // Set custom skin ID for blue team tshirt
                PantsSkin = 0,       // Set custom skin ID for blue team pants
                TorsoSkin = 0,       // Set custom skin ID for blue team metal.plate.torso
                FacemaskSkin = 0,    // Set custom skin ID for blue team metal.facemask
                WeaponSkin = 0,      // Set custom skin ID for blue team thompson
                GoaliePantsSkin = 0, // Set custom skin ID for blue goalie heavy.plate.pants
                GoalieJacketSkin = 0,// Set custom skin ID for blue goalie heavy.plate.jacket
                GoalieWeaponSkin = 0 // Set custom skin ID for blue goalie spas12
            }},
            { "red", new TeamSkins { 
                TshirtSkin = 0,      
                PantsSkin = 0,       
                TorsoSkin = 0,       
                FacemaskSkin = 0,    
                WeaponSkin = 0,      
                GoaliePantsSkin = 0, 
                GoalieJacketSkin = 0,
                GoalieWeaponSkin = 0 
            }},
            { "black", new TeamSkins { 
                TshirtSkin = 0,      
                PantsSkin = 0,       
                TorsoSkin = 0,       
                FacemaskSkin = 0,    
                WeaponSkin = 0,      
                GoaliePantsSkin = 0, 
                GoalieJacketSkin = 0,
                GoalieWeaponSkin = 0 
            }}
        };
        
        private class TeamSkins
        {
            public ulong TshirtSkin { get; set; }
            public ulong PantsSkin { get; set; }
            public ulong TorsoSkin { get; set; }
            public ulong FacemaskSkin { get; set; }
            public ulong WeaponSkin { get; set; }
            public ulong GoaliePantsSkin { get; set; }
            public ulong GoalieJacketSkin { get; set; }
            public ulong GoalieWeaponSkin { get; set; }
        }

        // STATE
        private BaseEntity activeBall;
        private BasePlayer lastKicker; 
        private Vector3 redGoalPos, blueGoalPos, blackGoalPos, centerPos;
        private Quaternion redGoalRot, blueGoalRot, blackGoalRot;
        
        private int scoreRed = 0;
        private int scoreBlue = 0;
        private int scoreBlack = 0;
        private bool gameActive = false; 
        private bool matchStarted = false; 
        private bool debugActive = false;
        
        // TEAM CONFIGURATIONS
        private Dictionary<string, TeamConfig> teamConfigs = new Dictionary<string, TeamConfig>
        {
            { "blue", new TeamConfig { Name = "SHELL-SEA FOOTBALL CLUB", Tag = "GRUB", Color = "0.2 0.4 1", HexColor = "#3366FF" } },
            { "red", new TeamConfig { Name = "Loot-pool F.C.", Tag = "DOORCAMPER", Color = "1 0.2 0.2", HexColor = "#FF3333" } },
            { "black", new TeamConfig { Name = "Project Zerg-Germain", Tag = "ROAMER", Color = "0.2 0.2 0.2", HexColor = "#333333" } }
        };
        
        private Timer gameTimer, tickerTimer, hudTimer, debugTimer;
        private Dictionary<ulong, bool> ballRangeState = new Dictionary<ulong, bool>();
        
        // TICKER
        private List<string> tickerMessages = new List<string> { "3-TEAM DEATHMATCH SOCCER", "SHOOT BALL TO SCORE", "KILL ENEMIES", "FIRST TO 5 WINS", "BLUE vs RED vs BLACK" };
        private int tickerIndex = 0;

        // TEAMS
        private List<ulong> redTeam = new List<ulong>();
        private List<ulong> blueTeam = new List<ulong>();
        private List<ulong> blackTeam = new List<ulong>();
        private Dictionary<ulong, string> playerRoles = new Dictionary<ulong, string>();
        
        // TEAM CONFIG CLASS
        private class TeamConfig
        {
            public string Name { get; set; }
            public string Tag { get; set; }
            public string Color { get; set; }
            public string HexColor { get; set; }
        }

        // DATA FILE
        private const string DataFileName = "DeathmatchSoccer_Data";

        // ==========================================
        // 2. DATA PERSISTENCE (SAVING/LOADING)
        // ==========================================
        private class ArenaData
        {
            public float Rx, Ry, Rz; // Red Pos
            public float Bx, By, Bz; // Blue Pos
            public float Blx, Bly, Blz; // Black Pos
            public float Cx, Cy, Cz; // Center Pos
            public float Rqx, Rqy, Rqz, Rqw; // Red Rot
            public float Bqx, Bqy, Bqz, Bqw; // Blue Rot
            public float Blqx, Blqy, Blqz, Blqw; // Black Rot
            public float Gw, Gh, Gd; // Dimensions
        }

        private void SaveArenaData()
        {
            var data = new ArenaData
            {
                Rx = redGoalPos.x, Ry = redGoalPos.y, Rz = redGoalPos.z,
                Bx = blueGoalPos.x, By = blueGoalPos.y, Bz = blueGoalPos.z,
                Blx = blackGoalPos.x, Bly = blackGoalPos.y, Blz = blackGoalPos.z,
                Cx = centerPos.x, Cy = centerPos.y, Cz = centerPos.z,
                Rqx = redGoalRot.x, Rqy = redGoalRot.y, Rqz = redGoalRot.z, Rqw = redGoalRot.w,
                Bqx = blueGoalRot.x, Bqy = blueGoalRot.y, Bqz = blueGoalRot.z, Bqw = blueGoalRot.w,
                Blqx = blackGoalRot.x, Blqy = blackGoalRot.y, Blqz = blackGoalRot.z, Blqw = blackGoalRot.w,
                Gw = GoalWidth, Gh = GoalHeight, Gd = GoalDepth
            };
            Interface.Oxide.DataFileSystem.WriteObject(DataFileName, data);
        }

        private void LoadArenaData()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(DataFileName))
            {
                var data = Interface.Oxide.DataFileSystem.ReadObject<ArenaData>(DataFileName);
                if (data != null)
                {
                    redGoalPos = new Vector3(data.Rx, data.Ry, data.Rz);
                    blueGoalPos = new Vector3(data.Bx, data.By, data.Bz);
                    blackGoalPos = new Vector3(data.Blx, data.Bly, data.Blz);
                    centerPos = new Vector3(data.Cx, data.Cy, data.Cz);
                    redGoalRot = new Quaternion(data.Rqx, data.Rqy, data.Rqz, data.Rqw);
                    blueGoalRot = new Quaternion(data.Bqx, data.Bqy, data.Bqz, data.Bqw);
                    blackGoalRot = new Quaternion(data.Blqx, data.Blqy, data.Blqz, data.Blqw);
                    if (data.Gw > 0) { GoalWidth = data.Gw; GoalHeight = data.Gh; GoalDepth = data.Gd; }
                    Puts("Arena Data Loaded.");
                }
            }
        }

        // ==========================================
        // 3. LIFECYCLE
        // ==========================================
        void OnServerInitialized()
        {
            LoadArenaData(); // Load saved goals
            
            if (ImageLibrary != null)
            {
                ImageLibrary.Call("AddImage", ImgScoreboardBg, "Soccer_Bar_BG");
                ImageLibrary.Call("AddImage", ImgGoalBanner, "Soccer_Goal_Banner");
            }
        }

        void Unload()
        {
            if (activeBall != null && !activeBall.IsDestroyed) activeBall.Kill();
            if (gameTimer != null) gameTimer.Destroy();
            if (tickerTimer != null) tickerTimer.Destroy();
            if (hudTimer != null) hudTimer.Destroy();
            if (debugTimer != null) debugTimer.Destroy();
            
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "SoccerScoreboard");
                CuiHelper.DestroyUi(player, "SoccerTicker");
                CuiHelper.DestroyUi(player, "GoalBanner");
                CuiHelper.DestroyUi(player, "BallRangeHUD");
                CuiHelper.DestroyUi(player, "RoleSelectUI");
                CuiHelper.DestroyUi(player, "TeamSelectUI");
                CuiHelper.DestroyUi(player, "LeashHUD");
            }
        }

        // ==========================================
        // 4. ADMIN COMMANDS
        // ==========================================
        [ChatCommand("save_goals")]
        private void CmdSaveGoals(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            SaveArenaData();
            SendReply(player, "Arena Data Saved! Positions will persist.");
        }

        [ChatCommand("load_goals")]
        private void CmdLoadGoals(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            LoadArenaData();
            SendReply(player, "Arena Data Reloaded.");
            if (redGoalPos != Vector3.zero) DrawGoal(player, redGoalPos, redGoalRot, Color.red, 5f);
        }

        [ChatCommand("start_match")]
        private void CmdStartMatch(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (centerPos == Vector3.zero) { SendReply(player, "Error: Set Center first!"); return; }
            
            scoreRed = 0; scoreBlue = 0; scoreBlack = 0;
            gameActive = true; matchStarted = true;
            
            SpawnBall();
            RefreshScoreboardAll();
            StartTicker();
            
            if (gameTimer != null) gameTimer.Destroy();
            gameTimer = timer.Repeat(0.05f, 0, CheckGoals);
            
            if (hudTimer != null) hudTimer.Destroy();
            hudTimer = timer.Repeat(0.5f, 0, HudLoop);

            PrintToChat("MATCH STARTED! 3 Teams Battle!");
            CallMiddleware("EVENT: MATCH_START. Score 0-0-0. 3-Team Battle.");
        }

        [ChatCommand("goal_size")]
        private void CmdSetSize(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length < 3) { SendReply(player, $"Current: {GoalWidth}x{GoalHeight}x{GoalDepth}"); return; }
            if (float.TryParse(args[0], out float w) && float.TryParse(args[1], out float h) && float.TryParse(args[2], out float d))
            {
                GoalWidth = w; GoalHeight = h; GoalDepth = d;
                SendReply(player, $"Goal Updated: {w}x{h}x{d}. Use /save_goals to keep.");
            }
        }

        [ChatCommand("set_red")] private void CmdSetRed(BasePlayer p, string c, string[] a) { if(p.IsAdmin){ redGoalPos=p.transform.position; redGoalRot=p.transform.rotation; SendReply(p, "Red Set."); DrawGoal(p, redGoalPos, redGoalRot, Color.red, 5f); }}
        [ChatCommand("set_blue")] private void CmdSetBlue(BasePlayer p, string c, string[] a) { if(p.IsAdmin){ blueGoalPos=p.transform.position; blueGoalRot=p.transform.rotation; SendReply(p, "Blue Set."); DrawGoal(p, blueGoalPos, blueGoalRot, Color.blue, 5f); }}
        [ChatCommand("set_black")] private void CmdSetBlack(BasePlayer p, string c, string[] a) { if(p.IsAdmin){ blackGoalPos=p.transform.position; blackGoalRot=p.transform.rotation; SendReply(p, "Black Set."); DrawGoal(p, blackGoalPos, blackGoalRot, Color.black, 5f); }}
        [ChatCommand("set_center")] private void CmdSetCenter(BasePlayer p, string c, string[] a) { if(p.IsAdmin){ centerPos=p.transform.position; SendReply(p, "Center Set."); }}
        [ChatCommand("reset_ball")] private void CmdResetBall(BasePlayer p, string c, string[] a) { if(p.IsAdmin){ SpawnBall(); SendReply(p, "Ball Reset."); }}
        
        [ChatCommand("goal_debug")]
        private void CmdToggleDebug(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            debugActive = !debugActive;
            if (debugTimer != null) debugTimer.Destroy();
            
            if (debugActive)
            {
                SendReply(player, "Debug ON.");
                debugTimer = timer.Repeat(1.0f, 0, () => {
                    if (redGoalPos != Vector3.zero) DrawGoal(player, redGoalPos, redGoalRot, Color.red, 1.0f);
                    if (blueGoalPos != Vector3.zero) DrawGoal(player, blueGoalPos, blueGoalRot, Color.blue, 1.0f);
                    if (blackGoalPos != Vector3.zero) DrawGoal(player, blackGoalPos, blackGoalRot, Color.black, 1.0f);
                });
            }
            else SendReply(player, "Debug OFF.");
        }

        [ChatCommand("setskin")]
        private void CmdSetSkin(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            if (args.Length < 3)
            {
                SendReply(player, "Usage: /setskin <team> <item> <skinId>");
                SendReply(player, "Teams: blue, red, black");
                SendReply(player, "Items: tshirt, pants, torso, facemask, weapon, goaliepants, goaliejacket, goalieweapon");
                SendReply(player, "Example: /setskin blue tshirt 123456789");
                return;
            }
            
            string team = args[0].ToLower();
            string item = args[1].ToLower();
            if (!ulong.TryParse(args[2], out ulong skinId))
            {
                SendReply(player, "Invalid skin ID. Must be a number.");
                return;
            }
            
            if (!teamSkins.ContainsKey(team))
            {
                SendReply(player, "Invalid team. Use: blue, red, or black");
                return;
            }
            
            var skins = teamSkins[team];
            switch (item)
            {
                case "tshirt": skins.TshirtSkin = skinId; break;
                case "pants": skins.PantsSkin = skinId; break;
                case "torso": skins.TorsoSkin = skinId; break;
                case "facemask": skins.FacemaskSkin = skinId; break;
                case "weapon": skins.WeaponSkin = skinId; break;
                case "goaliepants": skins.GoaliePantsSkin = skinId; break;
                case "goaliejacket": skins.GoalieJacketSkin = skinId; break;
                case "goalieweapon": skins.GoalieWeaponSkin = skinId; break;
                default:
                    SendReply(player, "Invalid item name.");
                    return;
            }
            
            SendReply(player, $"Set {team} {item} skin to {skinId}");
        }

        [ChatCommand("showskins")]
        private void CmdShowSkins(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            SendReply(player, "=== TEAM SKIN IDs ===");
            foreach (var kvp in teamSkins)
            {
                var team = kvp.Key;
                var skins = kvp.Value;
                SendReply(player, $"--- {team.ToUpper()} ---");
                SendReply(player, $"Tshirt: {skins.TshirtSkin}");
                SendReply(player, $"Pants: {skins.PantsSkin}");
                SendReply(player, $"Torso: {skins.TorsoSkin}");
                SendReply(player, $"Facemask: {skins.FacemaskSkin}");
                SendReply(player, $"Thompson: {skins.WeaponSkin}");
                SendReply(player, $"Goalie Pants: {skins.GoaliePantsSkin}");
                SendReply(player, $"Goalie Jacket: {skins.GoalieJacketSkin}");
                SendReply(player, $"Goalie SPAS-12: {skins.GoalieWeaponSkin}");
            }
        }

        // ==========================================
        // 5. JOINING & TEAMS
        // ==========================================
        [ChatCommand("teams")]
        private void CmdTeams(BasePlayer player, string command, string[] args)
        {
            ShowTeamSelectUI(player);
        }

        [ChatCommand("join")]
        private void CmdJoin(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0) { 
                ShowTeamSelectUI(player);
                return; 
            }
            string team = args[0].ToLower();

            redTeam.Remove(player.userID);
            blueTeam.Remove(player.userID);
            blackTeam.Remove(player.userID);
            playerRoles.Remove(player.userID);
            ballRangeState.Remove(player.userID);
            
            CuiHelper.DestroyUi(player, "BallRangeHUD"); 
            CuiHelper.DestroyUi(player, "LeashHUD");
            CuiHelper.DestroyUi(player, "TeamSelectUI");

            if (team == "red") { redTeam.Add(player.userID); CheckRole(player, "red"); }
            else if (team == "blue") { blueTeam.Add(player.userID); CheckRole(player, "blue"); }
            else if (team == "black") { blackTeam.Add(player.userID); CheckRole(player, "black"); }
            else SendReply(player, "Invalid team. Use: blue, red, or black");
        }

        [ConsoleCommand("select_team")]
        private void CmdSelectTeam(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;
            CuiHelper.DestroyUi(player, "TeamSelectUI");
            
            string team = arg.Args[0].ToLower();
            redTeam.Remove(player.userID);
            blueTeam.Remove(player.userID);
            blackTeam.Remove(player.userID);
            playerRoles.Remove(player.userID);
            ballRangeState.Remove(player.userID);
            
            CuiHelper.DestroyUi(player, "BallRangeHUD"); 
            CuiHelper.DestroyUi(player, "LeashHUD");

            if (team == "red") { redTeam.Add(player.userID); CheckRole(player, "red"); }
            else if (team == "blue") { blueTeam.Add(player.userID); CheckRole(player, "blue"); }
            else if (team == "black") { blackTeam.Add(player.userID); CheckRole(player, "black"); }
        }

        private void CheckRole(BasePlayer player, string team)
        {
            int goalies = 0;
            List<ulong> list = (team == "red") ? redTeam : (team == "blue") ? blueTeam : blackTeam;
            foreach(ulong id in list) if(playerRoles.ContainsKey(id) && playerRoles[id] == "Goalie") goalies++;

            if(goalies == 0) ShowRoleUI(player, team);
            else AssignRole(player, "Striker");
        }

        [ConsoleCommand("select_role")]
        private void CmdSelectRole(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null || arg.Args == null || arg.Args.Length < 1) return;
            CuiHelper.DestroyUi(player, "RoleSelectUI");
            AssignRole(player, arg.Args[0]);
        }

        private void AssignRole(BasePlayer player, string role)
        {
            playerRoles[player.userID] = role;
            if (centerPos != Vector3.zero) {
                Vector3 goalPos = redTeam.Contains(player.userID) ? redGoalPos : 
                                 blueTeam.Contains(player.userID) ? blueGoalPos : blackGoalPos;
                Quaternion goalRot = redTeam.Contains(player.userID) ? redGoalRot : 
                                    blueTeam.Contains(player.userID) ? blueGoalRot : blackGoalRot;
                Vector3 spawn = goalPos + (goalRot * Vector3.forward * 5f);
                player.Teleport(spawn);
            }
            GiveKit(player, role);
            UpdateScoreUI(player);
        }

        // ==========================================
        // 6. KITS & HUD LOOPS
        // ==========================================
        private void GiveKit(BasePlayer player, string role)
        {
            player.inventory.Strip();
            
            // Determine which team the player is on
            string team = redTeam.Contains(player.userID) ? "red" : 
                         blueTeam.Contains(player.userID) ? "blue" : "black";
            TeamSkins skins = teamSkins[team];
            
            if (role == "Striker") 
            {
                // Striker Kit (All positions except Goalie)
                GiveItemWithSkin(player, "tshirt", 1, skins.TshirtSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "pants", 1, skins.PantsSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "metal.plate.torso", 1, skins.TorsoSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "metal.facemask", 1, skins.FacemaskSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "smg.thompson", 1, skins.WeaponSkin, player.inventory.containerBelt);
                player.inventory.GiveItem(ItemManager.CreateByName("syringe.medical", 5), player.inventory.containerMain);
                player.inventory.GiveItem(ItemManager.CreateByName("barricade.wood.cover", 3), player.inventory.containerMain);
                player.inventory.GiveItem(ItemManager.CreateByName("ammo.pistol", 200), player.inventory.containerMain);
                player.SetMaxHealth(100); 
                player.health = 100;
            } 
            else // Goalie
            {
                GiveItemWithSkin(player, "tshirt", 1, skins.TshirtSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "heavy.plate.pants", 1, skins.GoaliePantsSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "heavy.plate.jacket", 1, skins.GoalieJacketSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "metal.facemask", 1, skins.FacemaskSkin, player.inventory.containerWear);
                GiveItemWithSkin(player, "shotgun.spas12", 1, skins.GoalieWeaponSkin, player.inventory.containerBelt);
                player.inventory.GiveItem(ItemManager.CreateByName("syringe.medical", 10), player.inventory.containerMain);
                player.inventory.GiveItem(ItemManager.CreateByName("ammo.shotgun", 64), player.inventory.containerMain);
                player.SetMaxHealth(200); 
                player.health = 200;
            }
        }
        
        private void GiveItemWithSkin(BasePlayer player, string itemName, int amount, ulong skinId, ItemContainer container)
        {
            Item item = ItemManager.CreateByName(itemName, amount, skinId);
            if (item != null)
            {
                player.inventory.GiveItem(item, container);
            }
        }

        private void HudLoop()
        {
            if (!matchStarted) return;

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (!playerRoles.ContainsKey(player.userID)) continue;
                string role = playerRoles[player.userID];
                bool isRed = redTeam.Contains(player.userID);
                bool isBlue = blueTeam.Contains(player.userID);
                bool isBlack = blackTeam.Contains(player.userID);

                // BALL RANGE HUD
                if (activeBall != null)
                {
                    float distBall = Vector3.Distance(player.transform.position, activeBall.transform.position);
                    bool inRange = distBall <= MaxKickDistance;

                    if (!ballRangeState.ContainsKey(player.userID) || ballRangeState[player.userID] != inRange)
                    {
                        ballRangeState[player.userID] = inRange;
                        DrawBallRangeUI(player, inRange);
                    }
                }

                // GOALIE LEASH
                if (role == "Goalie")
                {
                    Vector3 home = isRed ? redGoalPos : isBlue ? blueGoalPos : blackGoalPos;
                    if (home != Vector3.zero && Vector3.Distance(player.transform.position, home) > LeashRadius)
                    {
                        player.ShowToast(GameTip.Styles.Red_Normal, "RETURN TO GOAL!");
                        if (Vector3.Distance(player.transform.position, home) > LeashRadius + 5f)
                        {
                            HitInfo h = new HitInfo(); h.damageTypes.Add(global::Rust.DamageType.Radiation, 5f);
                            player.Hurt(h);
                        }
                    }
                }
            }
        }

        // ==========================================
        // 7. UI DRAWING
        // ==========================================
        private string GetImg(string name) { return (ImageLibrary != null) ? (string)ImageLibrary.Call("GetImage", name) : ""; }

        private void RefreshScoreboardAll() { foreach (var player in BasePlayer.activePlayerList) UpdateScoreUI(player); }

        private void UpdateScoreUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "SoccerScoreboard");
            if (!matchStarted) return;

            var container = new CuiElementContainer();
            string imgId = GetImg("Soccer_Bar_BG");
            
            var panel = new CuiPanel { Image = { Color = "0 0 0 0.8" }, RectTransform = { AnchorMin = "0.25 0.90", AnchorMax = "0.75 0.98" }, CursorEnabled = false };
            if (!string.IsNullOrEmpty(imgId))
                container.Add(new CuiElement { Name = "SoccerScoreboard", Parent = "Overlay", Components = { new CuiRawImageComponent { Png = imgId }, new CuiRectTransformComponent { AnchorMin = "0.25 0.90", AnchorMax = "0.75 0.98" } } });
            else container.Add(panel, "Overlay", "SoccerScoreboard");

            // Three team display with team names
            var blueConfig = teamConfigs["blue"];
            var redConfig = teamConfigs["red"];
            var blackConfig = teamConfigs["black"];
            
            // Blue Team (Left)
            container.Add(new CuiLabel { Text = { Text = blueConfig.Tag, FontSize = 10, Align = TextAnchor.UpperCenter, Color = blueConfig.Color + " 0.8" }, RectTransform = { AnchorMin = "0.05 0.6", AnchorMax = "0.28 0.95" } }, "SoccerScoreboard");
            container.Add(new CuiLabel { Text = { Text = scoreBlue.ToString(), FontSize = 28, Align = TextAnchor.MiddleCenter, Color = blueConfig.Color + " 1", Font = "robotocondensed-bold.ttf" }, RectTransform = { AnchorMin = "0.05 0.1", AnchorMax = "0.28 0.7" } }, "SoccerScoreboard");
            
            // Red Team (Middle)
            container.Add(new CuiLabel { Text = { Text = redConfig.Tag, FontSize = 10, Align = TextAnchor.UpperCenter, Color = redConfig.Color + " 0.8" }, RectTransform = { AnchorMin = "0.36 0.6", AnchorMax = "0.64 0.95" } }, "SoccerScoreboard");
            container.Add(new CuiLabel { Text = { Text = scoreRed.ToString(), FontSize = 28, Align = TextAnchor.MiddleCenter, Color = redConfig.Color + " 1", Font = "robotocondensed-bold.ttf" }, RectTransform = { AnchorMin = "0.36 0.1", AnchorMax = "0.64 0.7" } }, "SoccerScoreboard");
            
            // Black Team (Right)
            container.Add(new CuiLabel { Text = { Text = blackConfig.Tag, FontSize = 10, Align = TextAnchor.UpperCenter, Color = "0.8 0.8 0.8 0.8" }, RectTransform = { AnchorMin = "0.72 0.6", AnchorMax = "0.95 0.95" } }, "SoccerScoreboard");
            container.Add(new CuiLabel { Text = { Text = scoreBlack.ToString(), FontSize = 28, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1", Font = "robotocondensed-bold.ttf" }, RectTransform = { AnchorMin = "0.72 0.1", AnchorMax = "0.95 0.7" } }, "SoccerScoreboard");

            CuiHelper.AddUi(player, container);
        }

        private void DrawBallRangeUI(BasePlayer player, bool inRange)
        {
            CuiHelper.DestroyUi(player, "BallRangeHUD");
            var container = new CuiElementContainer();
            string color = inRange ? "0.2 0.8 0.2 0.8" : "0.8 0.2 0.2 0.8"; 
            string text = inRange ? "IN KICK RANGE" : "TOO FAR FROM BALL";
            container.Add(new CuiPanel { Image = { Color = color }, RectTransform = { AnchorMin = "0.4 0.12", AnchorMax = "0.6 0.15" }, CursorEnabled = false }, "Overlay", "BallRangeHUD");
            container.Add(new CuiLabel { Text = { Text = text, FontSize = 12, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, "BallRangeHUD");
            CuiHelper.AddUi(player, container);
        }

        private void ShowGoalBanner(string team)
        {
            string col = (team == "RED") ? "1 0.2 0.2" : (team == "BLUE") ? "0.2 0.4 1" : "0.8 0.8 0.8";
            string teamTag = (team == "RED") ? teamConfigs["red"].Tag : (team == "BLUE") ? teamConfigs["blue"].Tag : teamConfigs["black"].Tag;
            foreach (var p in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(p, "GoalBanner");
                var c = new CuiElementContainer();
                c.Add(new CuiPanel { Image = { Color = $"{col} 0.3", FadeIn = 0.1f }, RectTransform = { AnchorMin = "0 0.4", AnchorMax = "1 0.6" } }, "Overlay", "GoalBanner");
                c.Add(new CuiLabel { Text = { Text = $"{teamTag} SCORES!", FontSize = 50, Align = TextAnchor.MiddleCenter, Font = "permanentmarker.ttf", FadeIn=0.2f }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, "GoalBanner");
                CuiHelper.AddUi(p, c);
                timer.Once(3f, () => CuiHelper.DestroyUi(p, "GoalBanner"));
            }
        }

        private void ShowRoleUI(BasePlayer player, string team)
        {
            CuiHelper.DestroyUi(player, "RoleSelectUI");
            var c = new CuiElementContainer();
            var config = teamConfigs[team];
            string p = c.Add(new CuiPanel { Image = { Color = "0 0 0 0.9" }, RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" }, CursorEnabled = true }, "Overlay", "RoleSelectUI");
            c.Add(new CuiLabel { Text = { Text = $"CHOOSE ROLE - {config.Name}", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = config.Color + " 1" }, RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 1" } }, p);
            c.Add(new CuiLabel { Text = { Text = $"({config.Tag})", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.6" }, RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.8" } }, p);
            c.Add(new CuiButton { Button = { Command = "select_role Striker", Color = "0.2 0.6 0.2 1" }, Text = { Text = "STRIKER", FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.1 0.2", AnchorMax = "0.45 0.6" } }, p);
            c.Add(new CuiButton { Button = { Command = "select_role Goalie", Color = "0.8 0.4 0.1 1" }, Text = { Text = "GOALIE", FontSize = 16, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.55 0.2", AnchorMax = "0.9 0.6" } }, p);
            CuiHelper.AddUi(player, c);
        }

        private void ShowTeamSelectUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "TeamSelectUI");
            var c = new CuiElementContainer();
            string panel = c.Add(new CuiPanel { Image = { Color = "0 0 0 0.95" }, RectTransform = { AnchorMin = "0.25 0.25", AnchorMax = "0.75 0.75" }, CursorEnabled = true }, "Overlay", "TeamSelectUI");
            
            // Title
            c.Add(new CuiLabel { Text = { Text = "SELECT YOUR TEAM", FontSize = 24, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf" }, RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 0.98" } }, panel);
            
            // Blue Team Button
            var blueConfig = teamConfigs["blue"];
            string blueBtn = c.Add(new CuiButton { Button = { Command = "select_team blue", Color = blueConfig.Color + " 0.8" }, Text = { Text = "", FontSize = 1 }, RectTransform = { AnchorMin = "0.05 0.55", AnchorMax = "0.32 0.78" } }, panel);
            c.Add(new CuiLabel { Text = { Text = blueConfig.Name, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.35" } }, blueBtn);
            c.Add(new CuiLabel { Text = { Text = $"[{blueConfig.Tag}]", FontSize = 18, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = "0.95 0.7" } }, blueBtn);
            c.Add(new CuiLabel { Text = { Text = $"{blueTeam.Count} Players", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7" }, RectTransform = { AnchorMin = "0.05 0.75", AnchorMax = "0.95 0.95" } }, blueBtn);
            
            // Red Team Button
            var redConfig = teamConfigs["red"];
            string redBtn = c.Add(new CuiButton { Button = { Command = "select_team red", Color = redConfig.Color + " 0.8" }, Text = { Text = "", FontSize = 1 }, RectTransform = { AnchorMin = "0.36 0.55", AnchorMax = "0.64 0.78" } }, panel);
            c.Add(new CuiLabel { Text = { Text = redConfig.Name, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.35" } }, redBtn);
            c.Add(new CuiLabel { Text = { Text = $"[{redConfig.Tag}]", FontSize = 18, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = "0.95 0.7" } }, redBtn);
            c.Add(new CuiLabel { Text = { Text = $"{redTeam.Count} Players", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7" }, RectTransform = { AnchorMin = "0.05 0.75", AnchorMax = "0.95 0.95" } }, redBtn);
            
            // Black Team Button
            var blackConfig = teamConfigs["black"];
            string blackBtn = c.Add(new CuiButton { Button = { Command = "select_team black", Color = "0.3 0.3 0.3 0.8" }, Text = { Text = "", FontSize = 1 }, RectTransform = { AnchorMin = "0.68 0.55", AnchorMax = "0.95 0.78" } }, panel);
            c.Add(new CuiLabel { Text = { Text = blackConfig.Name, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.05", AnchorMax = "0.95 0.35" } }, blackBtn);
            c.Add(new CuiLabel { Text = { Text = $"[{blackConfig.Tag}]", FontSize = 18, Align = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", Color = "1 1 1 1" }, RectTransform = { AnchorMin = "0.05 0.4", AnchorMax = "0.95 0.7" } }, blackBtn);
            c.Add(new CuiLabel { Text = { Text = $"{blackTeam.Count} Players", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.7" }, RectTransform = { AnchorMin = "0.05 0.75", AnchorMax = "0.95 0.95" } }, blackBtn);
            
            // Team descriptions
            c.Add(new CuiLabel { Text = { Text = "Fast & Agile", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" }, RectTransform = { AnchorMin = "0.05 0.45", AnchorMax = "0.32 0.53" } }, panel);
            c.Add(new CuiLabel { Text = { Text = "Tactical & Strong", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" }, RectTransform = { AnchorMin = "0.36 0.45", AnchorMax = "0.64 0.53" } }, panel);
            c.Add(new CuiLabel { Text = { Text = "Coordinated & Deadly", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 1" }, RectTransform = { AnchorMin = "0.68 0.45", AnchorMax = "0.95 0.53" } }, panel);
            
            // Instructions
            c.Add(new CuiLabel { Text = { Text = "Click a team to join the battle!", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.8" }, RectTransform = { AnchorMin = "0 0.15", AnchorMax = "1 0.25" } }, panel);
            c.Add(new CuiLabel { Text = { Text = "You can also use: /join blue, /join red, /join black", FontSize = 11, Align = TextAnchor.MiddleCenter, Color = "1 1 1 0.5" }, RectTransform = { AnchorMin = "0 0.08", AnchorMax = "1 0.15" } }, panel);
            
            CuiHelper.AddUi(player, c);
        }

        private void StartTicker()
        {
            if (tickerTimer != null) tickerTimer.Destroy();
            tickerTimer = timer.Repeat(4.0f, 0, () => {
                tickerIndex++; if (tickerIndex >= tickerMessages.Count) tickerIndex = 0;
                string msg = tickerMessages[tickerIndex];
                foreach(var p in BasePlayer.activePlayerList) UpdateTickerUI(p, msg);
            });
        }

        private void UpdateTickerUI(BasePlayer player, string msg)
        {
            CuiHelper.DestroyUi(player, "SoccerTicker");
            if (!matchStarted) return;
            var c = new CuiElementContainer();
            c.Add(new CuiPanel { Image = { Color = "0 0 0 0.6" }, RectTransform = { AnchorMin = "0.35 0.87", AnchorMax = "0.65 0.90" } }, "Overlay", "SoccerTicker");
            c.Add(new CuiLabel { Text = { Text = msg, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 0 1" }, RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" } }, "SoccerTicker");
            CuiHelper.AddUi(player, c);
        }

        // ==========================================
        // 8. PHYSICS & GAME LOGIC
        // ==========================================
        void OnPlayerRespawn(BasePlayer player)
        {
            if (matchStarted && (redTeam.Contains(player.userID) || blueTeam.Contains(player.userID) || blackTeam.Contains(player.userID)))
            {
                NextTick(() => {
                    if (!playerRoles.ContainsKey(player.userID)) playerRoles[player.userID] = "Striker";
                    string role = playerRoles[player.userID];
                    Vector3 goalPos = redTeam.Contains(player.userID) ? redGoalPos : 
                                     blueTeam.Contains(player.userID) ? blueGoalPos : blackGoalPos;
                    Quaternion goalRot = redTeam.Contains(player.userID) ? redGoalRot : 
                                        blueTeam.Contains(player.userID) ? blueGoalRot : blackGoalRot;
                    if (goalPos != Vector3.zero) player.Teleport(goalPos + (goalRot * Vector3.forward * 5f));
                    player.metabolism.radiation_poison.value = 0;
                    player.health = player.MaxHealth();
                    if (player.IsSleeping()) player.EndSleeping();
                    GiveKit(player, role);
                });
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (activeBall != null && entity == activeBall)
            {
                info.damageTypes.ScaleAll(0); 
                if (info.Initiator is BasePlayer p)
                {
                    if (Vector3.Distance(p.transform.position, entity.transform.position) > MaxKickDistance) return;
                    lastKicker = p; 
                    Vector3 dir = (entity.transform.position - p.transform.position).normalized; dir.y += 0.2f; 
                    entity.GetComponent<Rigidbody>()?.AddForce(dir * KickForceMultiplier, ForceMode.Impulse);
                    Effect.server.Run("assets/bundled/prefabs/fx/impacts/blunt/metal/metal_impact.prefab", entity.transform.position);
                }
            }
        }

        private void SpawnBall()
        {
            if (activeBall != null && !activeBall.IsDestroyed) activeBall.Kill();
            string prefab = "assets/content/vehicles/ball/ball.entity.prefab";
            activeBall = GameManager.server.CreateEntity(prefab, centerPos + new Vector3(0, 2, 0));
            if (activeBall == null) { prefab = "assets/prefabs/misc/soccerball/soccerball.prefab"; activeBall = GameManager.server.CreateEntity(prefab, centerPos + new Vector3(0, 1, 0)); }
            if (activeBall == null) return;
            activeBall.Spawn();
            
            Rigidbody rb = activeBall.GetComponent<Rigidbody>();
            if (rb != null) 
            { 
                rb.mass = 200.0f; 
                rb.drag = 1.5f; 
                rb.angularDrag = 1.0f; 
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; 
                rb.WakeUp(); 
            }
        }

        private void CheckGoals()
        {
            if (!gameActive || activeBall == null) return;
            
            // Determine which goal was scored in and award point to the kicking team
            string scoringTeam = null;
            
            if (IsInside(activeBall.transform.position, blueGoalPos, blueGoalRot))
            {
                // Ball went into blue's goal - determine who kicked it
                if (lastKicker != null)
                {
                    if (redTeam.Contains(lastKicker.userID)) scoringTeam = "RED";
                    else if (blackTeam.Contains(lastKicker.userID)) scoringTeam = "BLACK";
                }
            }
            else if (IsInside(activeBall.transform.position, redGoalPos, redGoalRot))
            {
                // Ball went into red's goal - determine who kicked it
                if (lastKicker != null)
                {
                    if (blueTeam.Contains(lastKicker.userID)) scoringTeam = "BLUE";
                    else if (blackTeam.Contains(lastKicker.userID)) scoringTeam = "BLACK";
                }
            }
            else if (IsInside(activeBall.transform.position, blackGoalPos, blackGoalRot))
            {
                // Ball went into black's goal - determine who kicked it
                if (lastKicker != null)
                {
                    if (redTeam.Contains(lastKicker.userID)) scoringTeam = "RED";
                    else if (blueTeam.Contains(lastKicker.userID)) scoringTeam = "BLUE";
                }
            }
            
            if (scoringTeam != null) HandleGoal(scoringTeam);
        }

        private bool IsInside(Vector3 b, Vector3 g, Quaternion r)
        {
            Vector3 l = Quaternion.Inverse(r) * (b - g);
            return Mathf.Abs(l.x) < GoalWidth/2 && Mathf.Abs(l.y) < GoalHeight/2 && Mathf.Abs(l.z) < GoalDepth/2;
        }

        private void HandleGoal(string team)
        {
            gameActive = false;
            if (team == "RED") scoreRed++; 
            else if (team == "BLUE") scoreBlue++; 
            else if (team == "BLACK") scoreBlack++;
            
            Effect.server.Run("assets/prefabs/tools/c4/effects/c4_explosion.prefab", activeBall.transform.position);
            RefreshScoreboardAll(); ShowGoalBanner(team);
            string mvp = (lastKicker != null) ? lastKicker.displayName : "None";
            tickerMessages.Add($"GOAL: {team} ({mvp})");
            CallMiddleware($"EVENT: GOAL. {team} Scores. MVP: {mvp}. Score: R{scoreRed}-B{scoreBlue}-Bl{scoreBlack}");
            if (scoreRed >= ScoreToWin || scoreBlue >= ScoreToWin || scoreBlack >= ScoreToWin) EndMatch(team);
            else timer.Once(5f, () => { SpawnBall(); gameActive = true; });
        }

        private void EndMatch(string winner)
        {
            PrintToChat($"GAME OVER! {winner} WINS!");
            CallMiddleware($"EVENT: MATCH_END. Winner: {winner}");
            if (activeBall != null) activeBall.Kill();
            gameActive = false; matchStarted = false;
            timer.Once(5f, () => { foreach(var p in BasePlayer.activePlayerList) { CuiHelper.DestroyUi(p, "SoccerScoreboard"); CuiHelper.DestroyUi(p, "SoccerTicker"); CuiHelper.DestroyUi(p, "BallRangeHUD"); CuiHelper.DestroyUi(p, "LeashHUD"); } });
        }

        private void DrawGoal(BasePlayer player, Vector3 c, Quaternion r, Color col, float dur)
        {
            float hw=GoalWidth/2, hh=GoalHeight/2, hd=GoalDepth/2;
            Vector3[] p = new Vector3[8];
            p[0]=c+r*new Vector3(-hw,-hh,-hd); p[1]=c+r*new Vector3(hw,-hh,-hd); p[2]=c+r*new Vector3(hw,-hh,hd); p[3]=c+r*new Vector3(-hw,-hh,hd);
            p[4]=c+r*new Vector3(-hw,hh,-hd); p[5]=c+r*new Vector3(hw,hh,-hd); p[6]=c+r*new Vector3(hw,hh,hd); p[7]=c+r*new Vector3(-hw,hh,hd);
            player.SendConsoleCommand("ddraw.line", dur, col, p[0], p[1]); player.SendConsoleCommand("ddraw.line", dur, col, p[1], p[2]); player.SendConsoleCommand("ddraw.line", dur, col, p[2], p[3]); player.SendConsoleCommand("ddraw.line", dur, col, p[3], p[0]);
            player.SendConsoleCommand("ddraw.line", dur, col, p[4], p[5]); player.SendConsoleCommand("ddraw.line", dur, col, p[5], p[6]); player.SendConsoleCommand("ddraw.line", dur, col, p[6], p[7]); player.SendConsoleCommand("ddraw.line", dur, col, p[7], p[4]);
            player.SendConsoleCommand("ddraw.line", dur, col, p[0], p[4]); player.SendConsoleCommand("ddraw.line", dur, col, p[1], p[5]); player.SendConsoleCommand("ddraw.line", dur, col, p[2], p[6]); player.SendConsoleCommand("ddraw.line", dur, col, p[3], p[7]);
            player.SendConsoleCommand("ddraw.text", dur, col, c + new Vector3(0, hh + 2f, 0), "GOAL ZONE");
        }

        private void CallMiddleware(string text)
        {
            var msg = new List<object> { new { role = "system", content = "Sports Caster AI" }, new { role = "user", content = text } };
            // ADDED: Mode = soccer to trigger correct prompt on server
            var data = new { license = licenseKey, server_ip = ConVar.Server.ip, messages = msg, mode = "soccer", user_input = text };
            
            Puts($"[AI DEBUG] Sending to: {middlewareUrl}");

            webrequest.Enqueue(middlewareUrl, JsonConvert.SerializeObject(data), (c, r) => {
                if (c == 200) {
                    try {
                        var res = JsonConvert.DeserializeObject<OpenAIResponse>(r);
                        var clean = res.choices[0].message.content.Replace("```json","").Replace("```","").Trim();
                        int s=clean.IndexOf('{'), e=clean.LastIndexOf('}');
                        if(s>=0 && e>s) PrintToChat($"<color=#00ffff>[COMMENTATOR]</color>: {JsonConvert.DeserializeObject<AnnouncerResponse>(clean.Substring(s,e-s+1)).message_to_player}");
                    } catch (Exception ex) { Puts($"[AI ERROR] Parse failed: {ex.Message}"); }
                } else { Puts($"[AI ERROR] Code: {c} | {r}"); }
            }, this, RequestMethod.POST, new Dictionary<string, string> { { "Content-Type", "application/json" } });
        }

        public class OpenAIResponse { public Choice[] choices { get; set; } }
        public class Choice { public Message message { get; set; } }
        public class Message { public string content { get; set; } }
        public class AnnouncerResponse { public string message_to_player { get; set; } }
    }
}