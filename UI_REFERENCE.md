# DeathmatchSoccer - UI Layout Reference

## Main UI Elements

### 1. Scoreboard (Top Center)
```
┌────────────────────────────────────────────────────────────┐
│                      SCOREBOARD                             │
│  ┌────────────┐   ┌────────────┐   ┌────────────┐         │
│  │    GRUB    │   │ DOORCAMPER │   │     PZG    │         │
│  │     5      │   │     3      │   │     2      │         │
│  │   (Blue)   │   │    (Red)   │   │  (Black)   │         │
│  └────────────┘   └────────────┘   └────────────┘         │
└────────────────────────────────────────────────────────────┘
```

### 2. Team Selection UI (Center Screen)
```
┌─────────────────────────────────────────────────────────────┐
│              SELECT YOUR TEAM                                │
│                                                              │
│  ┌────────────┐    ┌────────────┐    ┌────────────┐       │
│  │            │    │            │    │            │       │
│  │  SHELL-SEA │    │ Loot-pool  │    │  Project   │       │
│  │  FOOTBALL  │    │    F.C.    │    │   Zerg-    │       │
│  │    CLUB    │    │            │    │  Germain   │       │
│  │            │    │            │    │            │       │
│  │   [GRUB]   │    │[DOORCAMPER]│    │   [PZG]    │       │
│  │            │    │            │    │            │       │
│  │ 3 Players  │    │ 2 Players  │    │ 4 Players  │       │
│  │            │    │            │    │            │       │
│  └────────────┘    └────────────┘    └────────────┘       │
│                                                              │
│  Fast & Agile   Tactical & Strong  Coordinated & Deadly    │
│                                                              │
│         Click a team to join the battle!                    │
│   You can also use: /join blue, /join red, /join black     │
└─────────────────────────────────────────────────────────────┘
```

### 3. Role Selection UI (Center Screen)
```
┌──────────────────────────────────────┐
│  CHOOSE ROLE - SHELL-SEA FOOTBALL    │
│              CLUB                     │
│             (GRUB)                    │
│                                       │
│  ┌──────────────┐  ┌──────────────┐ │
│  │              │  │              │ │
│  │   STRIKER    │  │   GOALIE     │ │
│  │              │  │              │ │
│  │   100 HP     │  │   200 HP     │ │
│  │   Thompson   │  │   SPAS-12    │ │
│  │              │  │              │ │
│  └──────────────┘  └──────────────┘ │
└──────────────────────────────────────┘
```

### 4. Ball Range Indicator (Bottom Center)
```
┌─────────────────────────┐
│   IN KICK RANGE  ✓      │  (Green when in range)
└─────────────────────────┘

┌─────────────────────────┐
│  TOO FAR FROM BALL  ✗   │  (Red when out of range)
└─────────────────────────┘
```

### 5. Goal Banner (Full Width)
```
═════════════════════════════════════════════════════════
║                                                         ║
║              GRUB SCORES!                              ║
║                                                         ║
═════════════════════════════════════════════════════════
(Appears for 3 seconds when goal is scored)
```

### 6. Ticker Messages (Top, Below Scoreboard)
```
┌───────────────────────────────────────┐
│  3-TEAM DEATHMATCH SOCCER             │
└───────────────────────────────────────┘

Rotates between:
- "3-TEAM DEATHMATCH SOCCER"
- "SHOOT BALL TO SCORE"
- "KILL ENEMIES"
- "FIRST TO 5 WINS"
- "BLUE vs RED vs BLACK"
- "GOAL: RED (PlayerName)" (when goals happen)
```

## Kit Loadouts

### Striker Kit (All Positions)
```
┌────────────────────────────────┐
│ Armor:                          │
│  • tshirt (team skin)           │
│  • pants (team skin)            │
│  • metal.plate.torso (team skin)│
│  • metal.facemask (team skin)   │
│                                 │
│ Weapons:                        │
│  • smg.thompson (team skin)     │
│  • 200x ammo.pistol             │
│                                 │
│ Items:                          │
│  • 5x syringe.medical           │
│  • 3x barricade.wood.cover      │
│                                 │
│ Health: 100 HP                  │
└────────────────────────────────┘
```

### Goalie Kit
```
┌────────────────────────────────┐
│ Armor:                          │
│  • tshirt (team skin)           │
│  • heavy.plate.pants (team skin)│
│  • heavy.plate.jacket(team skin)│
│  • metal.facemask (team skin)   │
│                                 │
│ Weapons:                        │
│  • shotgun.spas12 (team skin)   │
│  • 64x ammo.shotgun             │
│                                 │
│ Items:                          │
│  • 10x syringe.medical          │
│                                 │
│ Health: 200 HP                  │
│ Leash: 15m from goal            │
└────────────────────────────────┘
```

## Color Scheme

### Team Colors
- **Blue Team (GRUB)**: RGB(0.2, 0.4, 1.0) - #3366FF
- **Red Team (DOORCAMPER)**: RGB(1.0, 0.2, 0.2) - #FF3333
- **Black Team (ROAMER)**: RGB(0.8, 0.8, 0.8) - Gray/White text on dark background

### UI Colors
- Background panels: Black with 80-95% opacity
- Success indicators: Green (0.2, 0.8, 0.2)
- Warning/Error: Red (0.8, 0.2, 0.2)
- Team-specific elements use team colors

## Command Reference Quick Guide

### Admin Setup Commands
```
/set_red         → Position red goal
/set_blue        → Position blue goal
/set_black       → Position black goal
/set_center      → Position ball spawn
/save_goals      → Save configuration
/start_match     → Begin game
/setskin         → Configure team skins
/showskins       → View skin IDs
```

### Player Commands
```
/join            → Show team menu
/join <team>     → Join specific team
/teams           → Show team menu
```

### Debug Commands
```
/goal_debug      → Toggle goal visualization
/reset_ball      → Respawn ball
/load_goals      → Reload config
/goal_size       → Adjust goal dimensions
```

## Skin Configuration Slots (8 per team)

Each team has these customizable items:
1. **tshirt** - Used by both Striker and Goalie
2. **pants** - Striker only
3. **torso** - Striker metal.plate.torso
4. **facemask** - Used by both Striker and Goalie
5. **weapon** - Striker Thompson
6. **goaliepants** - Goalie heavy.plate.pants
7. **goaliejacket** - Goalie heavy.plate.jacket
8. **goalieweapon** - Goalie SPAS-12

Total: 24 unique skin slots (8 items × 3 teams)
