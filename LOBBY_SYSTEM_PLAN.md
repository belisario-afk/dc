# Lobby System - Implementation Plan

## Overview
Add a physical lobby system where players walk into spheres to join teams, with UI prompts for team selection.

## Feature Requirements

### 1. Physical Join Spheres
- **3 Team Spheres** - One sphere for each team (Blue, Red, Black)
- **Sphere Placement** - Admin command to position spheres
- **Visual Indicators** - Team-colored spheres with labels
- **Collision Detection** - Trigger when player enters sphere radius
- **Persistence** - Save sphere positions with arena data

### 2. Join Trigger System
- **OnEntityEnter Detection** - Monitor when players enter sphere zones
- **Cooldown** - Prevent spam triggering (3-5 second cooldown per player)
- **Permission Check** - Optionally restrict who can join
- **Lobby State** - Only active during pre-match lobby phase

### 3. Team Selection UI
- **Auto-Popup** - Team selection UI appears when entering sphere
- **Existing UI Reuse** - Use current `ShowTeamSelectUI()` function
- **Confirmation** - Player clicks team button or walks away to cancel
- **Feedback** - Visual/audio confirmation of team join

### 4. Lobby State Management
- **Pre-Match Lobby** - State before match starts
- **In-Match State** - Spheres disabled during active matches
- **Post-Match Lobby** - Spheres reactivate after match ends
- **Player Management** - Track which players are in lobby vs in-game

### 5. Match Start System
- **Manual Start** - Admin command `/start_match` (existing)
- **Auto Start** - Optional: Auto-start when teams have minimum players
- **Ready System** - Optional: Players must ready-up before start
- **Countdown** - 10-second countdown before match begins

## Technical Implementation

### Phase 1: Sphere Creation & Placement

**New Variables:**
```csharp
private Vector3 blueSpherePos = Vector3.zero;
private Vector3 redSpherePos = Vector3.zero;
private Vector3 blackSpherePos = Vector3.zero;
private float sphereRadius = 3.0f; // Trigger radius
private bool lobbyActive = false;
private Dictionary<ulong, float> lastJoinAttempt = new Dictionary<ulong, float>();
```

**Admin Commands:**
```bash
/set_blue_sphere   # Set blue team join sphere position
/set_red_sphere    # Set red team join sphere position
/set_black_sphere  # Set black team join sphere position
/lobby on/off      # Toggle lobby mode
```

**Sphere Visual:**
```csharp
// Create visual sphere entity
// - Use sphere entity or marker
// - Apply team color
// - Add floating text label
// - Emit particle effects
```

### Phase 2: Collision Detection

**Approach 1: Sphere Collision (Recommended)**
```csharp
// Create actual sphere entities with colliders
private List<BaseEntity> joinSpheres = new List<BaseEntity>();

void CreateJoinSphere(Vector3 position, string team)
{
    // Spawn sphere entity
    // Set collision layer
    // Store reference
}

// Hook: OnEntityEnter
void OnEntityEnter(TriggerBase trigger, BaseEntity entity)
{
    // Check if entity is player
    // Check if trigger is join sphere
    // Determine which team sphere
    // Show team selection UI
}
```

**Approach 2: Distance Monitoring (Alternative)**
```csharp
// Check player distance in timer loop
private Timer lobbyTimer;

void LobbyLoop()
{
    if (!lobbyActive) return;
    
    foreach (var player in BasePlayer.activePlayerList)
    {
        // Check distance to each sphere
        // Trigger UI if within radius
    }
}
```

### Phase 3: UI Integration

**Modify ShowTeamSelectUI:**
```csharp
private void ShowTeamSelectUI(BasePlayer player, string triggeredBy = "command")
{
    // Add parameter to track trigger source
    // Show same UI
    // Add "Walk away to cancel" text if triggered by sphere
}
```

**Sphere Entry Handler:**
```csharp
private void OnSphereEnter(BasePlayer player, string team)
{
    // Check cooldown
    if (lastJoinAttempt.ContainsKey(player.userID))
    {
        if (Time.time - lastJoinAttempt[player.userID] < 3f)
            return;
    }
    
    // Update cooldown
    lastJoinAttempt[player.userID] = Time.time;
    
    // Show UI
    ShowTeamSelectUI(player, $"sphere_{team}");
    
    // Optional: Auto-join if player already decided
    // Optional: Show specific team UI only
}
```

### Phase 4: Lobby State Management

**States:**
```csharp
private enum GameState
{
    Lobby,      // Pre-match, spheres active
    Starting,   // Countdown phase
    InProgress, // Match active
    Ended       // Post-match, before next lobby
}

private GameState currentState = GameState.Lobby;
```

**State Transitions:**
```csharp
private void SetGameState(GameState newState)
{
    currentState = newState;
    
    switch (newState)
    {
        case GameState.Lobby:
            ActivateJoinSpheres();
            lobbyActive = true;
            break;
            
        case GameState.Starting:
            // Countdown timer
            break;
            
        case GameState.InProgress:
            DeactivateJoinSpheres();
            lobbyActive = false;
            break;
            
        case GameState.Ended:
            // Show match results
            timer.Once(10f, () => SetGameState(GameState.Lobby));
            break;
    }
}
```

### Phase 5: Match Start Enhancements

**Enhanced Start Command:**
```csharp
[ChatCommand("start_match")]
private void CmdStartMatch(BasePlayer player, string command, string[] args)
{
    if (!player.IsAdmin) return;
    
    // Check if in lobby state
    if (currentState != GameState.Lobby)
    {
        SendReply(player, "Match already in progress!");
        return;
    }
    
    // Check team counts
    if (redTeam.Count == 0 || blueTeam.Count == 0)
    {
        SendReply(player, "Need players on at least 2 teams!");
        return;
    }
    
    // Start countdown
    SetGameState(GameState.Starting);
    StartCountdown();
}
```

**Countdown System:**
```csharp
private void StartCountdown()
{
    int countdown = 10;
    
    Timer countdownTimer = timer.Repeat(1f, countdown, () => 
    {
        if (countdown > 0)
        {
            PrintToChat($"Match starting in {countdown}...");
            countdown--;
        }
        else
        {
            BeginMatch();
        }
    });
}

private void BeginMatch()
{
    SetGameState(GameState.InProgress);
    
    // Existing match start logic
    scoreRed = 0; scoreBlue = 0; scoreBlack = 0;
    matchNumber = 1;
    
    if (rotationMode)
    {
        team1Playing = "blue";
        team2Playing = "red";
        waitingTeam = "black";
        
        activeGoals["red"] = true;
        activeGoals["blue"] = true;
        activeGoals["black1"] = false;
        activeGoals["black2"] = false;
    }
    
    gameActive = true;
    matchStarted = true;
    
    SpawnBall();
    RefreshScoreboardAll();
    StartTicker();
    
    // Start game loops
    if (gameTimer != null) gameTimer.Destroy();
    gameTimer = timer.Repeat(0.05f, 0, CheckGoals);
    
    if (hudTimer != null) hudTimer.Destroy();
    hudTimer = timer.Repeat(0.5f, 0, HudLoop);
}
```

### Phase 6: Optional Features

**Auto-Start:**
```csharp
private bool autoStart = false;
private int minPlayersPerTeam = 2;

private void CheckAutoStart()
{
    if (!autoStart || currentState != GameState.Lobby) return;
    
    int teamsReady = 0;
    if (redTeam.Count >= minPlayersPerTeam) teamsReady++;
    if (blueTeam.Count >= minPlayersPerTeam) teamsReady++;
    if (blackTeam.Count >= minPlayersPerTeam) teamsReady++;
    
    if (teamsReady >= 2)
    {
        PrintToChat("Teams ready! Auto-starting match...");
        SetGameState(GameState.Starting);
        StartCountdown();
    }
}
```

**Ready System:**
```csharp
private Dictionary<ulong, bool> playerReady = new Dictionary<ulong, bool>();

[ChatCommand("ready")]
private void CmdReady(BasePlayer player, string command, string[] args)
{
    if (currentState != GameState.Lobby) return;
    
    playerReady[player.userID] = true;
    PrintToChat($"{player.displayName} is ready!");
    
    CheckAllReady();
}

private void CheckAllReady()
{
    // Check if all players in teams are ready
    // Auto-start if everyone ready
}
```

## Data Persistence

**Update ArenaData:**
```csharp
private class ArenaData
{
    // Existing fields...
    public float BlueSx, BlueSy, BlueSz;  // Blue sphere pos
    public float RedSx, RedSy, RedSz;    // Red sphere pos
    public float BlackSx, BlackSy, BlackSz; // Black sphere pos
    public float SphereRadius;
}
```

## Visual Design

### Sphere Appearance
```
┌─────────────────┐
│                 │
│   ◯ BLUE TEAM   │  ← Glowing blue sphere
│   [GRUB]        │     with floating text
│                 │
│   3 Players     │
│                 │
└─────────────────┘
```

### Particle Effects
- Constant particle emission
- Team-colored particles
- Pulsing glow effect
- Trail effect when player enters

### UI Enhancement
```
When player enters sphere:

┌──────────────────────────────────┐
│  You entered BLUE TEAM sphere    │
│                                  │
│  Click JOIN to enter team        │
│  Walk away to cancel             │
└──────────────────────────────────┘
```

## Implementation Order

### Sprint 1: Basic Spheres (Foundation)
1. Add sphere position variables
2. Create `/set_*_sphere` commands
3. Add sphere markers (visual indicators)
4. Save sphere positions to data

### Sprint 2: Collision Detection
1. Implement distance monitoring system
2. Add cooldown mechanism
3. Trigger UI on sphere entry
4. Test with multiple players

### Sprint 3: Lobby State
1. Add GameState enum
2. Implement state transitions
3. Enable/disable spheres based on state
4. Update match start to use states

### Sprint 4: Polish & Testing
1. Add particle effects
2. Improve visual feedback
3. Add countdown system
4. Test full match cycle

### Sprint 5: Optional Features
1. Auto-start system
2. Ready-up system
3. Team balance checks
4. Minimum player requirements

## Commands Summary

**New Admin Commands:**
```bash
/set_blue_sphere   # Position blue join sphere
/set_red_sphere    # Position red join sphere
/set_black_sphere  # Position black join sphere
/lobby on          # Enable lobby mode
/lobby off         # Disable lobby mode
/lobby_debug       # Show sphere positions
/auto_start on/off # Toggle auto-start
```

**Enhanced Commands:**
```bash
/start_match       # Start match (with countdown)
/end_match         # End current match, return to lobby
```

**Player Commands:**
```bash
/ready             # Mark as ready (optional feature)
/unready           # Remove ready status
```

## Benefits

✅ **Physical Interaction** - More immersive than command-based joining
✅ **Visual Clarity** - Clear team selection areas
✅ **Intuitive** - New players understand immediately
✅ **Flexible** - Can still use `/join` commands
✅ **State Management** - Clear lobby vs match phases
✅ **Professional** - Matches traditional game lobbies

## Technical Considerations

**Performance:**
- Distance checks every 0.5s (not every frame)
- Cooldown prevents spam
- Efficient collision detection

**Compatibility:**
- Works alongside existing `/join` commands
- Backward compatible
- Optional feature (can be disabled)

**Scalability:**
- Easy to add more sphere types
- Can add spawn point spheres
- Extensible for future features

## Next Steps

1. ✅ Fix rotation bug (COMPLETED)
2. 📋 Review and approve this plan
3. 🔨 Implement Sprint 1 (Basic Spheres)
4. 🧪 Test and iterate
5. 🚀 Deploy remaining sprints

---

**Version:** 5.4.0 (Planned)
**Feature:** Lobby System with Join Spheres
**Status:** Planning Phase - Awaiting Approval
