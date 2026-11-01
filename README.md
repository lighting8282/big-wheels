# Big Wheels

A no-lose monster truck toy for a 3-year-old, made in Unity. There is no text, no timer, no score and no way to fail.

## How to play

- **Tap anywhere** to jump. Tap again in the air to do a front flip. (Desktop: Space or Up Arrow.)
- The truck drives itself. Drive over a **car** to flatten it. Each flattened car fills one of the **three slots** at the top, and the third sets off confetti.
- **Ramps and trampolines** launch the truck into a backflip, with balloons to pop in the air.
- **Jelly piles** splat in bright colors and stick to the truck. **Mud puddles** do the same in brown. The **stream** (with ducks) and the **car wash** clean the truck off.
- **Animals** wait by the road and cheer as the truck passes. Tap one to hear it. Farm: cow, sheep, duck. Beach: crab, seagull. Snow: penguin, polar bear, snowman.
- **Tap the sun** to change the world. It also changes by itself about every 75 seconds (farm, beach, snow).

### Picture buttons

- **Color circles** along the bottom repaint the truck.
- **Right edge, top group:** pick the truck.
  - Monster truck: honks.
  - Digger: its arm swings and dumps dirt on each tap and each crushed car.
  - Fire truck: sirens, and sprays water that pops balloons.
  - Garbage truck: its hopper lid opens and eats the cars it flattens.
- **Right edge, bottom group:** costumes. Party hat, big eyes, flag. Each one toggles on and off.
- The speaker button mutes everything.

## Running it

Open the project in **Unity 6000.4.0f1** (2D URP template, new Input System) and press Play. The scene is intentionally empty: `Game` builds the whole world at startup.

## How it is built

Everything is generated in code, so there are no art or audio files to manage.

| File | What it does |
|---|---|
| `Assets/Scripts/Game.cs` | Game logic, spawning, truck physics, worlds, animals and scene setup |
| `Assets/Scripts/Trucks.cs` | The four truck types: sprites, wheel sizes and anchor points for costumes and special parts |
| `Assets/Scripts/Art.cs`, `Art2.cs` | Software rasteriser that draws every sprite at startup (Art2: animals, new obstacles, beach and snow worlds, costumes) |
| `Assets/Scripts/Sfx.cs` | Synthesised sound effects, animal sounds and the engine loop |
| `Assets/Scripts/Hud.cs` | Picture-only UI (counting slots, color swatches, truck and costume buttons, sound, play) |
| `Assets/Scripts/Editor/BigWheelsEditor.cs` | Project setup and a headless smoke test |

Positions use "logical pixels" (y down, 100 px = 1 Unity unit), matching the original web prototype, so tuning numbers carry over.

### Adding spoken numbers and colors

Unity has no built-in speech. Put recordings in `Assets/Resources/Voice/` named `one`, `two`, `three`, `yay`, and `red`, `orange`, `yellow`, `green`, `blue`, `purple`, `pink`. They play automatically, and the game is silent for any that are missing.

### Headless smoke test

Plays every truck, all three worlds, each new obstacle and a 90-second random session in landscape and portrait, and saves screenshots:

```bash
unity run <project path> -- -executeMethod BigWheels.EditorTools.BigWheelsEditor.Smoke -shotDir <output dir>
```

## Status

Verified through the headless smoke test only. Not yet tried in Play mode, on a touchscreen, or with sound on a real device.

## License

MIT. See [LICENSE](LICENSE).
