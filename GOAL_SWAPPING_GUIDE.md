# Goal Swapping Rotation System - Visual Guide

## Overview
The goal swapping system eliminates the need for player teleportation by having 4 goals on the field: 2 original (red/blue) and 2 black goals positioned at the same locations. Goals activate/deactivate dynamically during rotation.

## Goal Placement

```
                    [FIELD LAYOUT]

     Black Goal 2 (inactive)        Black Goal 1 (inactive)
            ║                              ║
            ║                              ║
         ╔═════╗                        ╔═════╗
         ║BLUE ║                        ║ RED ║
         ║GOAL ║                        ║GOAL ║
         ╚═════╝                        ╚═════╝
        (active)                       (active)
            
            
            ⚽ [BALL SPAWN]
            
            
         Players                        Players
         (Blue)                         (Red)
```

## Initial Setup

**Commands:**
```bash
# Stand at red goal position
/set_red
/set_black1   # Same position as red goal

# Stand at blue goal position  
/set_blue
/set_black2   # Same position as blue goal

# Center
/set_center

# Save
/save_goals
```

## Match Flow with Goal Swapping

### Match 1: Blue (GRUB) vs Red (DOORCAMPER)
```
Status: Red Goal [ACTIVE]  |  Blue Goal [ACTIVE]
        Black1 [INACTIVE]  |  Black2 [INACTIVE]

Result: Blue WINS 5-3
```

### Match 2: Blue (GRUB) vs Black (ROAMER at Red position)
```
Goal Swap: Red goal deactivates, Black Goal 1 activates

Status: Black1 [ACTIVE]    |  Blue Goal [ACTIVE]
        Red [INACTIVE]     |  Black2 [INACTIVE]

Teams:  Black at red pos   |  Blue at blue pos
        Red team waits     |  

Result: Black WINS 5-4
```

### Match 3: Black (ROAMER) vs Red (DOORCAMPER reclaims)
```
Goal Swap: Blue goal deactivates, Red goal reactivates
           Black stays at position, Blue goal stays inactive

Status: Black1 [ACTIVE]    |  Red Goal [ACTIVE]
        Blue [INACTIVE]    |  Black2 [INACTIVE]

Teams:  Black at red pos   |  Red reclaimed goal
        Blue team waits    |

Result: Red WINS 5-2
```

### Match 4: Red (DOORCAMPER) vs Blue (GRUB reclaims)
```
Goal Swap: Black1 deactivates, Blue goal reactivates
           Red stays, Black1 becomes inactive

Status: Red Goal [ACTIVE]  |  Blue Goal [ACTIVE]
        Black1 [INACTIVE]  |  Black2 [INACTIVE]

Teams:  Red at red pos     |  Blue reclaimed goal
        Black team waits   |

... cycle continues
```

## Visual: Active vs Inactive Goals

### Active Goal (Full Color)
```
╔═══════════════╗
║               ║
║   RED GOAL    ║  ← Ball can score here
║   [ACTIVE]    ║
║               ║
╚═══════════════╝
```

### Inactive Goal (Faded/Ghosted)
```
┌───────────────┐
│               │
│  BLACK GOAL   │  ← Ball passes through, no score
│  [INACTIVE]   │
│               │
└───────────────┘
```

## Key Benefits

✅ **No Teleportation** - Players stay in position
✅ **Seamless Rotation** - Goals swap instantly
✅ **Visual Clarity** - Active goals are clearly marked
✅ **Fair Competition** - Only active goals count
✅ **Fluid Gameplay** - No interruption from player movement

## Debug Visualization

Use `/goal_debug` to see all goals:
- **Bright Color** = Active goal (registers scoring)
- **Faded Color** = Inactive goal (ignored)
- **All 4 goals** visible simultaneously

## Rotation Logic

```
Winner Team → Stays at their goal (remains active)
Loser Team → Goal deactivates, becomes inactive
Waiting Team → Goal activates at loser's position

Example:
Red loses → Red goal OFF, Black Goal 1 ON (at red position)
Blue loses → Blue goal OFF, Black Goal 2 ON (at blue position)
Black loses → Black goal OFF, Original goal reclaimed (ON)
```

## Commands Quick Reference

```bash
# Setup
/set_red          # Primary red goal
/set_blue         # Primary blue goal
/set_black1       # Black goal at red position
/set_black2       # Black goal at blue position
/save_goals       # Save all 4 goal positions

# Game Control
/start_match      # Begin with red vs blue active
/rotation         # Toggle rotation mode
/goal_debug       # Visualize active/inactive goals

# Match automatically rotates:
# Winner stays → Loser's goal replaced → Waiting team activates
```

## Technical Notes

- Each goal has position (Vector3) and rotation (Quaternion)
- `activeGoals` dictionary tracks state: "red", "blue", "black1", "black2"
- Only goals with `true` state in dictionary register ball entry
- Goal swapping happens in `RotateTeams()` function
- No physics changes needed - detection layer uses boolean flags

## Comparison: Old vs New System

### Old System (v5.2.0)
❌ Players teleported to waiting area
❌ Waiting area position required
❌ Players moved around map
❌ Disrupted gameplay flow

### New System (v5.3.0)
✅ Goals swap at existing positions
✅ No waiting area needed
✅ Players stay in place
✅ Smooth, uninterrupted rotation
