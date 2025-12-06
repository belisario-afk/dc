# DeathmatchSoccer Setup Guide

## Overview
DeathmatchSoccer is a 3-team competitive soccer plugin for Rust with custom team skins, modern UI, and two distinct player roles.

## Requirements
- **Skins.cs** plugin (for custom team skins)
- **ImageLibrary** plugin (optional, for UI images)

## Teams Configuration

### Team 1: Blue - SHELL-SEA FOOTBALL CLUB
- Tag: **GRUB**
- Color: Blue (#3366FF)
- Style: Fast & Agile

### Team 2: Red - Loot-pool F.C.
- Tag: **DOORCAMPER**
- Color: Red (#FF3333)
- Style: Tactical & Strong

### Team 3: Black - Project Zerg-Germain
- Tag: **ROAMER** (PZG)
- Color: Black/Gray (#333333)
- Style: Coordinated & Deadly

## Player Roles

### Striker (All Positions)
**Health:** 100 HP

**Equipment:**
- tshirt (custom skin per team)
- pants (custom skin per team)
- metal.plate.torso (custom skin per team)
- metal.facemask (custom skin per team)
- smg.thompson (custom skin per team)
- 5x syringe.medical
- 3x barricade.wood.cover
- 200x ammo.pistol

### Goalie
**Health:** 200 HP

**Equipment:**
- tshirt (custom skin per team)
- heavy.plate.pants (custom skin per team)
- heavy.plate.jacket (custom skin per team)
- metal.facemask (custom skin per team)
- shotgun.spas12 (custom skin per team)
- 10x syringe.medical
- 64x ammo.shotgun

## Initial Setup

### Step 1: Set Arena Positions
As admin, position yourself where each goal should be and run:

```
/set_red      # Set red team goal position
/set_blue     # Set blue team goal position
/set_black    # Set black team goal position
/set_center   # Set ball spawn position
```

### Step 2: Configure Goal Size (Optional)
```
/goal_size <width> <height> <depth>
# Example: /goal_size 8 4 6
```

### Step 3: Save Arena Configuration
```
/save_goals   # Saves all positions and goal dimensions
```

### Step 4: Configure Team Skins
Set custom skin IDs for each team using:

```
/setskin <team> <item> <skinId>
```

**Teams:** blue, red, black

**Items:**
- `tshirt` - Striker & Goalie shirt
- `pants` - Striker pants
- `torso` - Striker metal.plate.torso
- `facemask` - Striker & Goalie facemask
- `weapon` - Striker Thompson
- `goaliepants` - Goalie heavy.plate.pants
- `goaliejacket` - Goalie heavy.plate.jacket
- `goalieweapon` - Goalie SPAS-12

**Example Configuration:**
```
# Blue Team (GRUB)
/setskin blue tshirt 123456
/setskin blue pants 234567
/setskin blue torso 345678
/setskin blue facemask 456789
/setskin blue weapon 567890
/setskin blue goaliepants 678901
/setskin blue goaliejacket 789012
/setskin blue goalieweapon 890123

# Red Team (DOORCAMPER)
/setskin red tshirt 111111
/setskin red pants 222222
# ... etc

# Black Team (ROAMER/PZG)
/setskin black tshirt 333333
/setskin black pants 444444
# ... etc
```

### Step 5: View Current Skin Configuration
```
/showskins    # Displays all configured skin IDs
```

### Step 6: Start Match
```
/start_match  # Begins the 3-team battle
```

## Player Commands

### Joining Teams
```
/join         # Shows team selection UI
/join blue    # Join Blue team (SHELL-SEA/GRUB)
/join red     # Join Red team (Loot-pool/DOORCAMPER)
/join black   # Join Black team (PZG/ROAMER)
/teams        # Shows team selection UI
```

## Game Mechanics

### Scoring
- First team to **5 goals** wins
- Score by shooting the ball into opponent goals
- Any goal scores for the team that kicked it (2 opponents per team)

### Roles
- **First player** on each team gets to choose: Striker or Goalie
- **Subsequent players** automatically become Strikers
- Only 1 Goalie allowed per team

### Goalie Restrictions
- Goalies have a **leash radius** (default 15m from their goal)
- Going too far triggers warnings and radiation damage
- Designed to keep goalies defending their goal

### Ball Mechanics
- **Kick distance:** 15m maximum
- **Kick force:** 3500 units
- Ball UI indicator shows if you're in kick range (green/red)

## Debug Commands

```
/goal_debug   # Toggle goal zone visualization (shows boxes)
/reset_ball   # Respawn the ball at center
/load_goals   # Reload saved arena data
```

## UI Elements

### Scoreboard (Top Center)
- Shows all 3 team scores
- Displays team tags (GRUB, DOORCAMPER, ROAMER)
- Color-coded per team

### Team Selection UI
- Shows all 3 teams with names and tags
- Displays current player count per team
- Team descriptions
- Click to join or use `/join <team>`

### Role Selection UI
- Appears when joining a team (if goalie slot available)
- Shows team name and tag with team colors
- Choose between Striker or Goalie

### Ball Range Indicator
- Green: "IN KICK RANGE"
- Red: "TOO FAR FROM BALL"
- Updates in real-time

### Goal Banner
- Large banner when any team scores
- Shows scoring team tag
- Displays for 3 seconds

### Ticker
- Scrolling messages at top
- Shows match events and tips
- Updates every 4 seconds

## Troubleshooting

### Skins Not Applying
1. Ensure **Skins.cs** plugin is loaded
2. Verify skin IDs are correct using `/showskins`
3. Check that skin IDs exist in your Skins database
4. Rejoin team to reapply skins

### Arena Not Loading
1. Make sure you've run `/save_goals` after setting positions
2. Use `/load_goals` to manually reload
3. Check data file: `oxide/data/DeathmatchSoccer_Data.json`

### Players Can't Join
1. Verify match has started with `/start_match`
2. Check that center position is set (`/set_center`)
3. Ensure goals are configured for all 3 teams

## Advanced Configuration

### Modifying Game Settings
Edit these values in the plugin code:

```csharp
private float KickForceMultiplier = 3500.0f;  // Ball kick strength
private float MaxKickDistance = 15.0f;        // Max distance to kick
private float LeashRadius = 15.0f;            // Goalie movement limit
private int ScoreToWin = 5;                   // Goals needed to win
```

## Notes

- Skin IDs default to 0 (no custom skin) until configured
- Each team can have completely unique visual appearance
- More kit options can be added in future updates
- AI commentator integration available (if middleware configured)
- Match data persists across server restarts

## Credits
- Plugin: KillaDome
- Version: 5.1.0
- Updated: 2025-12-06
