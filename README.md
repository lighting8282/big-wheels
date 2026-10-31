# Big Wheels

A no-lose monster truck toy for a 3-year-old, made in Unity. There is no text, no timer, no score and no way to fail.

## How to play

- **Tap anywhere** to jump. Tap again in the air to do a front flip. (Desktop: Space or Up Arrow.)
- The truck drives itself. Drive over a **car** to flatten it. Each flattened car fills one of the five slots at the top, and the fifth sets off confetti.
- Drive up a **ramp** for a big backflip, then pop the **balloons** in the air.
- Splash through **mud puddles**, then drive through the **car wash** to clean up.
- Tap a **color circle** at the bottom to repaint the truck.
- The speaker button in the corner mutes everything.

## Running it

Open the project in **Unity 6000.4.0f1** (2D URP template, new Input System) and press Play. The scene is intentionally empty: `Game` builds the whole world at startup.

## How it is built

Everything is generated in code, so there are no art or audio files to manage.

| File | What it does |
|---|---|
| `Assets/Scripts/Game.cs` | Game logic, spawning, truck physics and scene setup |
| `Assets/Scripts/Art.cs` | Software rasteriser that draws every sprite at startup |
| `Assets/Scripts/Sfx.cs` | Synthesised sound effects and the engine loop |
| `Assets/Scripts/Hud.cs` | Picture-only UI (counting slots, color swatches, sound, play) |
| `Assets/Scripts/Editor/BigWheelsEditor.cs` | Project setup and a headless smoke test |

Positions use "logical pixels" (y down, 100 px = 1 Unity unit), matching the original web prototype, so tuning numbers carry over.

### Adding spoken numbers and colors

Unity has no built-in speech. Put recordings in `Assets/Resources/Voice/` named `one` to `five`, `yay`, and `red`, `orange`, `yellow`, `green`, `blue`, `purple`, `pink`. They play automatically, and the game is silent for any that are missing.

### Headless smoke test

Plays 40 simulated seconds in landscape and portrait and saves screenshots:

```bash
unity run <project path> -- -executeMethod BigWheels.EditorTools.BigWheelsEditor.Smoke -shotDir <output dir>
```

## Status

Verified through the headless smoke test only. Not yet tried in Play mode, on a touchscreen, or with sound on a real device.

## License

MIT. See [LICENSE](LICENSE).
