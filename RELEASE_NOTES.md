# DeathmatchSoccer v5.2.0 - Release Notes

## Overview
Complete overhaul of DeathmatchSoccer plugin with 3-team rotation system, custom skins, and modern UI.

## Major Features

### 🏆 Rotation Mode (NEW)
**Tournament-style competitive gameplay**
- 2 teams compete while 1 team waits
- Winner stays to face the waiting team
- Loser goes to waiting area
- Automatic team rotation after each match
- Match numbering system (#1, #2, #3...)
- Only playing teams can score goals
- Toggle with `/rotation` command

**Game Flow Example:**
```
Match 1: GRUB vs DOORCAMPER (PZG waits) → GRUB wins
Match 2: GRUB vs PZG (DOORCAMPER waits) → PZG wins  
Match 3: PZG vs DOORCAMPER (GRUB waits) → continues...
```

### 👥 3-Team System
**Three unique teams with full branding:**
- **Blue Team:** SHELL-SEA FOOTBALL CLUB (GRUB)
  - Fast & Agile playstyle
  - Blue color theme (#3366FF)
  
- **Red Team:** Loot-pool F.C. (DOORCAMPER)
  - Tactical & Strong playstyle
  - Red color theme (#FF3333)
  
- **Black Team:** Project Zerg-Germain (PZG/ROAMER)
  - Coordinated & Deadly playstyle
  - Gray/White color theme

### 🎨 Custom Skins System
**24 unique skin slots (8 per team):**
- Tshirt (shared by Striker & Goalie)
- Pants (Striker only)
- Metal plate torso (Striker only)
- Metal facemask (shared by Striker & Goalie)
- Thompson weapon (Striker only)
- Heavy plate pants (Goalie only)
- Heavy plate jacket (Goalie only)
- SPAS-12 weapon (Goalie only)

**Requires:** Skins.cs plugin installed

### 🎮 Enhanced Kits

**Striker Kit (100 HP):**
- Tshirt (custom skin)
- Pants (custom skin)
- Metal plate torso (custom skin)
- Metal facemask (custom skin)
- SMG Thompson (custom skin)
- 5x Medical syringe
- 3x Wood barricade cover
- 200x Pistol ammo

**Goalie Kit (200 HP):**
- Tshirt (custom skin)
- Heavy plate pants (custom skin)
- Heavy plate jacket (custom skin)
- Metal facemask (custom skin)
- SPAS-12 shotgun (custom skin)
- 10x Medical syringe
- 64x Shotgun ammo
- 15m leash radius from goal

### 🖥️ Modern UI

**Dynamic Scoreboard:**
- **Rotation Mode:** Shows 2 playing teams with "VS", match number, waiting team
- **3-Way Mode:** Shows all 3 team scores simultaneously
- Team tags and colors
- Real-time score updates

**Team Selection Menu:**
- Interactive UI showing all 3 teams
- Team names, tags, and descriptions
- Current player count per team
- Click to join or use `/join <team>`

**Role Selection:**
- Choose Striker or Goalie
- Team-colored branding
- Displays team name and tag
- Auto-assignment if goalie exists

**Additional UI Elements:**
- Ball range indicator (green/red)
- Goal banner with team colors
- Scrolling ticker messages
- Match number display (rotation mode)

## Commands

### Admin Commands
```bash
# Arena Setup
/set_red          # Set red team goal position
/set_blue         # Set blue team goal position
/set_black        # Set black team goal position
/set_center       # Set ball spawn position
/set_waiting      # Set waiting area for rotation mode

# Game Configuration
/rotation         # Toggle rotation mode ON/OFF
/start_match      # Start the match
/goal_size W H D  # Set goal dimensions
/save_goals       # Save arena configuration
/load_goals       # Reload arena configuration

# Skin Configuration
/setskin <team> <item> <skinId>  # Set team skin
/showskins                       # Display all skin IDs

# Debug
/goal_debug       # Toggle goal zone visualization
/reset_ball       # Respawn the ball
```

### Player Commands
```bash
/join            # Show team selection UI
/join <team>     # Join specific team (blue, red, black)
/teams           # Show team selection UI
```

## Installation

1. Install required plugins:
   - Skins.cs (for custom skins)
   - ImageLibrary (optional, for UI images)

2. Upload DeathmatchSoccer.cs to `oxide/plugins/`

3. Configure arena:
   ```
   /set_red
   /set_blue
   /set_black
   /set_center
   /set_waiting
   /save_goals
   ```

4. Configure team skins (optional):
   ```
   /setskin blue tshirt 123456
   /setskin red weapon 789012
   ... etc
   ```

5. Start match:
   ```
   /start_match
   ```

## Configuration

### Rotation Mode
- **Default:** Enabled
- **Toggle:** `/rotation`
- **ON:** 2 teams play, 1 waits
- **OFF:** 3-way battle mode

### Game Settings
Edit in plugin code:
```csharp
KickForceMultiplier = 3500.0f   // Ball kick strength
MaxKickDistance = 15.0f         // Max kick distance
LeashRadius = 15.0f             // Goalie movement limit
ScoreToWin = 5                  // Goals to win match
```

### Team Rotation Order
Default initial setup:
- Team 1 Playing: Blue (GRUB)
- Team 2 Playing: Red (DOORCAMPER)
- Waiting: Black (PZG)

## Technical Details

### Version History
- **v5.2.0** - Added rotation mode
- **v5.1.0** - 3-team system with custom skins
- **v5.0.0** - Base 2-team system

### Dependencies
- Oxide/uMod framework
- Skins.cs plugin (for custom skins)
- ImageLibrary plugin (optional)

### Data Persistence
- Arena positions saved to `oxide/data/DeathmatchSoccer_Data.json`
- Includes goal positions, rotations, and dimensions
- Loads automatically on server start

### Performance
- Efficient goal checking (0.05s interval)
- HUD updates (0.5s interval)
- Ticker rotation (4.0s interval)

## Known Limitations

1. **Skin Application:** Requires valid skin IDs from Skins.cs database
2. **Waiting Area:** Must be configured for rotation mode teleportation
3. **Goal Detection:** Requires properly positioned goals with correct rotations

## Troubleshooting

**Skins not applying:**
- Verify Skins.cs plugin is loaded
- Check skin IDs with `/showskins`
- Ensure skin IDs exist in Skins database

**Rotation not working:**
- Set waiting area with `/set_waiting`
- Verify rotation mode is ON with `/rotation`
- Check all 3 goals are configured

**Players not teleporting:**
- Confirm waiting area is set
- Check team assignments with team selection UI
- Verify players have joined teams

## Credits
- **Author:** KillaDome
- **Version:** 5.2.0
- **Release Date:** December 6, 2024
- **Plugin Type:** Rust Oxide/uMod

## Support
Refer to documentation files:
- `SETUP_GUIDE.md` - Complete setup instructions
- `UI_REFERENCE.md` - Visual UI layouts
- Plugin header comments - Command reference
