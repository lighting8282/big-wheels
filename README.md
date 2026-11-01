# Big Wheels

A no-lose monster truck toy for a 3-year-old, made in Unity. There is no text, no timer, no score and no way to fail.

## How to play

- **Tap anywhere** to jump. Tap again in the air to do a front flip. (Desktop: Space or Up Arrow; **F11** toggles fullscreen.)
- The truck drives itself. Drive over a **car** to flatten it. Each flattened car fills one of the **three slots** at the top, and the third sets off confetti.
- **Smash it:** wooden **crates**, a **brick wall**, **bowling pins**, a **glass wall** and a **picket fence** stand in the road, often in pairs. Drive into them and they burst into pieces that bounce along the ground. **Red barrels explode** and set off everything standing near them. Jump high enough and the truck sails over a prop (and crushes it on the way down).
- **Score:** every smash, crushed car, popped balloon, jelly splat and landed flip adds points, with a **combo multiplier** (up to x5, doubled in turbo) while you keep hitting things within three seconds of each other. The score and your **best** are in the panel at the top left, with "+points" floating up from each hit. The best score is saved.
- **Turbo:** every smash and every flattened car lights one of the **four flames** under the slots. The fourth starts about six seconds of turbo: the truck speeds up, trails flames, throws speed lines and smashes things from farther away. There is no way to lose, and nothing ever slows the truck.
- **Ramps and trampolines** launch the truck into a backflip, with balloons to pop in the air.
- **Jelly piles** splat in bright colors and stick to the truck. **Mud puddles** do the same in brown. The **stream** (with ducks) and the **car wash** clean the truck off.
- **Animals** wait by the road and cheer as the truck passes. Tap one to hear it. Farm: cow, sheep, duck. Beach: crab, seagull. Snow: penguin, polar bear, snowman. Night: owl, fox, hedgehog.
- **Night world:** a starry sky with a smiling moon, glowing flowers, drifting fireflies, and the truck turns on its headlight beam.
- **Tap the sun** to change the world. It also changes by itself about every 75 seconds (farm, beach, snow, night).
- **Sticker book:** the first time you discover something (each truck, each animal you tap, each world, and ten things to try), a sticker pops up and flies to the book button in the top-left corner. Tap the book to see all 31 slots. Grey silhouettes are still to find, and tapping an earned one plays its sound. Stickers are saved between sessions.

### Picture buttons

- **Color circles** along the bottom repaint the truck.
- **Right edge, top group:** pick the truck.
  - Monster truck: honks.
  - Digger: its arm swings and dumps dirt on each tap and each crushed car.
  - Fire truck: sirens, and sprays water that pops balloons.
  - Garbage truck: its hopper lid opens and eats the cars it flattens.
- **Right edge, bottom group:** costumes. Party hat, big eyes, flag. Each one toggles on and off.
- The speaker button mutes everything.

### Parent corner

**Tap the gear button** under the sticker book (or hold the speaker button for 2 seconds; a quick tap on the speaker still just mutes). The game pauses and a settings card opens:

- **Volume:** master volume slider.
- **Engine hum:** on or off.
- **World:** Auto (changes by itself and when the sun is tapped), or lock to Farm, Beach, Snow or Night.
- **Break reminder:** off, or after 10, 20 or 30 minutes of play the screen turns into a calm night scene with a sleeping truck. A parent holds the green button for 2 seconds to keep playing.
- **Screen** (desktop builds): windowed or fullscreen (also F11).
- **Enlarge window:** steps through 960x540, 1280x720, 1600x900, 1920x1080 and 2560x1440 (only sizes that fit your desktop) and wraps back to the smallest. The window can also be dragged to any size or maximised. Tapping it while fullscreen leaves fullscreen.
- **Reset stickers and score:** tap twice to confirm. Clears the sticker book and the best score.

Settings are saved between sessions.

## Running it

Open the project in **Unity 6000.4.0f1** (2D URP template, new Input System) and press Play. The scene is intentionally empty: `Game` builds the whole world at startup.

## How it is built

Everything is generated in code, so there are no art or audio files to manage.

| File | What it does |
|---|---|
| `Assets/Scripts/Game.cs` | Game logic, spawning, truck physics, worlds, animals and scene setup |
| `Assets/Scripts/Trucks.cs` | The four truck types: sprites, wheel sizes and anchor points for costumes and special parts |
| `Assets/Scripts/Art.cs`, `Art2.cs` | Software rasteriser that draws every sprite at startup (Art2: animals, new obstacles, beach, snow and night worlds, costumes) |
| `Assets/Scripts/Sfx.cs` | Synthesised sound effects, animal sounds and the engine loop |
| `Assets/Scripts/Hud.cs` | Picture-only UI (counting slots, color swatches, truck and costume buttons, sound, play) |
| `Assets/Scripts/Smash.cs` | Smashable props (crates, bricks, pins, barrels, glass, fence) and their art, flying debris, chain explosions, the four-flame turbo meter and screen shake (a `partial` of `Game`) |
| `Assets/Scripts/Score.cs`, `Display.cs` | Score, combo and the saved best score (a `partial` of `Game`); fullscreen and window-size options for desktop builds |
| `Assets/Scripts/Stickers.cs`, `StickerUi.cs` | The 31 stickers (saved in PlayerPrefs; the bit mask is an int, so keep it at 31 or fewer), their small icons, and the sticker book screen |
| `Assets/Scripts/Settings.cs`, `ParentUi.cs` | Saved parent settings, the hold-to-open button, the parent corner card and the break screen |
| `Assets/Scripts/Editor/BigWheelsEditor.cs` | Project setup and a headless smoke test |

Positions use "logical pixels" (y down, 100 px = 1 Unity unit), matching the original web prototype, so tuning numbers carry over.

### Adding spoken numbers and colors

Unity has no built-in speech. Put recordings in `Assets/Resources/Voice/` named `one`, `two`, `three`, `yay`, and `red`, `orange`, `yellow`, `green`, `blue`, `purple`, `pink`. They play automatically, and the game is silent for any that are missing.

### Headless smoke test

Plays every truck, all four worlds, each new obstacle, the sticker book, the parent corner and break screen (including save-and-reload checks) and a 240-second random session in landscape and portrait, and saves screenshots:

```bash
unity run <project path> -- -executeMethod BigWheels.EditorTools.BigWheelsEditor.Smoke -shotDir <output dir>
```

## Status

Verified through the headless smoke test only. Not yet tried in Play mode, on a touchscreen, or with sound on a real device.

To reset the sticker book, delete the PlayerPrefs keys `bw_stickers` and `bw_seen`.

## License

MIT. See [LICENSE](LICENSE).
