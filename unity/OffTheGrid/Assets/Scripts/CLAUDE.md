# OFF THE GRID — Unity Presentation & Input Layer

Unity code here is a **view** and **input adapter** for the sim. It owns:
- Rendering (sprites, UI, animations)
- Input handling (touch, keyboard, device rotation)
- Device-specific concerns (screen size, frame rate, battery)

It **never** owns:
- Game logic (decisions, state mutations, RNG)
- Balance constants
- Determinism (all RNG is from the sim)

## The contract with Sim

The sim returns immutable snapshots. Unity reads them and draws.
The sim accepts commands. Unity builds them and enqueues.

```csharp
// Every frame:
var nextState = sim.Step(inputCommandThisFrame, rngDrawsTheSimNeeded);
ui.UpdateFromSnapshot(nextState);  // View never mutates
```

Commands come from:
- Player input (tapped archery, chose a location on the map)
- Environment (time passed, a rival's decision resolved)
- RNG results (weather rolled, hunt succeeded/failed)

**Never** compute outcomes here. Return the action; sim returns the result.

## Code layout

```
Assets/Scripts/
├── CLAUDE.md                           (you are here)
├── Game/                               (input and commands)
│   ├── InputManager.cs                 (touch/keyboard → GameCommand)
│   └── CommandBuilder.cs
├── UI/
│   ├── HudManager.cs                   (top-level render coordinator)
│   ├── Screens/
│   │   ├── CampScreen.cs
│   │   ├── MapScreen.cs
│   │   ├── ArcheryScreen.cs
│   │   └── ...
│   └── Components/
│       ├── HealthBar.cs
│       ├── StatStrip.cs
│       └── ...
└── Boot.cs                             (startup, hands sim to UI)
```

## Connecting to Sim

Import `LastOut.Sim` and `LastOut.Data`. Read `ISimLog` if you need to debug.

Never import `src/` or `tools/` — only the assemblies.

## Do not

- Do not add game logic here (body decay, morale calculation, food metabolism).
- Do not roll RNG (even for animations or particle effects in a determinism-critical section).
- Do not mutate the snapshot the sim gives you. Copy what you need for state tracking.
- Do not call into the sim from the render loop to ask "what if I did X?" — the sim
  is single-threaded and doesn't support queries, only commands-and-step.
