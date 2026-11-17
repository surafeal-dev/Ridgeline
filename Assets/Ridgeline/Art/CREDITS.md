# Art and audio credits

All third-party art and audio below is **CC0 1.0 (public domain)**; the font section at the end is the one exception. CC0 assets are free for commercial use, and no attribution is required. They are credited here anyway.

| What | Files | Source | Author | License |
|---|---|---|---|---|
| Jeep body and tyre | `Vehicle/jeep_body.png`, `Vehicle/jeep_tire.png` | [Free Monster Truck Sprite](https://opengameart.org/content/monstertruck) | OpenGameArt user (listed CC0) | CC0 |
| Driver (head, hurt face, body, arm, hand, leg) | `Driver/*` | [Toon Characters 1](https://opengameart.org/content/toon-characters-1), "Male person" | Kenney (kenney.nl) | CC0 |
| Terrain tiles, coins, gem, flag, sign, rock, bush, grass, HUD coin | `Terrain/*`, `Pickups/coin_*`, `Pickups/gem_blue.png`, `Decor/flag.png`, `Decor/sign.png`, `Decor/small_*`, `UI/hud_coin.png` | [New Platformer Pack](https://opengameart.org/content/new-platformer-pack) | Kenney | CC0 |
| Far background, trees, bushes | `Background/meadow_far.png`, `Decor/tree*.png`, `Decor/bush*.png` | [Background Elements Redux](https://opengameart.org/content/background-elements-redux) | Kenney | CC0 |
| UI icons | `UI/icon_*.png` | [Game Icons](https://opengameart.org/content/game-icons) | Kenney | CC0 |
| Coin, fuel, bonus, crash, bump sounds | `../Audio/coin.ogg`, `fuel.ogg`, `bonus.ogg`, `crash.ogg`, `bump.ogg` | [New Platformer Pack](https://opengameart.org/content/new-platformer-pack) | Kenney | CC0 |
| Click sound | `../Audio/click.ogg` | [Interface Sounds](https://opengameart.org/content/interface-sounds) | Kenney | CC0 |

## Changes and in-house art

- The Jeep's cab roof and side window were removed to make an open-top jeep, and the windshield was made see-through. `Ridgeline > Build Art Set` rebuilds this from the untouched original in `Art/Source~`.
- The driver is drawn as a deliberately oversized bobblehead (Toon Characters head, scaled up).
- `Pickups/fuel_can.png` is original. It is drawn in code by `ArtSetBuilder.DrawFuelCan`, because no CC0 fuel can was found.
- The engine sound is synthesised at runtime by `AudioService`.

Kenney's licence text is in `LICENSE-Kenney-CC0.txt`.

## Font

The font is **Lilita One** (`../Fonts/LilitaOne-Regular.ttf`) by Juan Montoreano, from [Google Fonts](https://github.com/google/fonts/tree/main/ofl/lilitaone). It is licensed under the **SIL Open Font License 1.1**, which allows it to be embedded in commercial games. The licence text must ship with the font; it is in `../Fonts/LilitaOne-OFL.txt`.
