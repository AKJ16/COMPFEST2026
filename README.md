# 🎮 GridMancer

> An inventory-grid puzzle: drag weapons onto a tiny grid, chain their combos, and beat the enemy before the timer runs out.

<!-- TODO: add docs/banner.png, then uncomment the next line -->
<!-- ![Banner](docs/banner.png) -->

![Engine](https://img.shields.io/badge/Engine-Unity_6-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-purple?logo=csharp)
![Status](https://img.shields.io/badge/Status-Game_Jam-success)
![License](https://img.shields.io/badge/License-MIT-green)

---

## 📖 About

**GridMancer** is a 2D puzzle game created for the **COMPFEST 2026 Game Jam**.

Every stage gives you a small inventory grid and a handful of weapons. Where you place each weapon decides how much damage it deals, because some weapons boost or repeat others. Find the right layout and your combos fire against the enemy before time runs out.

<!-- TODO: add how many hours the jam gave you -->

---

## 🎯 Theme

<!-- TODO: add the official jam theme and one or two sentences on how GridMancer interprets it -->

---

## 🕹️ Gameplay

### Objective

Defeat the enemy in each stage before the timer hits zero. Drag weapons from your inventory onto the grid, build the strongest layout you can, and let the combos do the work.

### Weapons

| Weapon | Type | Size | Effect |
| ------ | ---- | ---- | ------ |
| **Sword** | Attack | 2 wide × 1 tall | 3 damage |
| **Staff** | Attack | 1 wide × 2 tall | 2 damage. Boosted by the books. |
| **Poison Dagger** | Attack | 1 square | 1 damage, plus 1 poison tick on every action. Poison stacks. |
| **Book of Addition** | Modifier | 1 square, 3×3 range | +2 damage to the Staff |
| **Book of Multiplication** | Modifier | 1 square, 3×3 range | ×2 damage to the Staff |
| **Hourglass** | Modifier | 1 wide × 2 tall, 3×4 range | Repeats the attack of weapons it affects |

With both books, the Staff deals (2 × 2) + 2 = **6**, in any order.

### Combos

| Combo | Pair | What it does |
| ----- | ---- | ------------ |
| **Surge** | Book of Addition + Staff | Adds +2 Staff damage |
| **Overload** | Book of Multiplication + Staff | Doubles Staff damage |
| **Echo Strike** | Hourglass + Sword | Replays the Sword's hit |
| **Echo Bolt** | Hourglass + Staff | Replays the Staff's hit |
| **Echo Venom** | Hourglass + Poison Dagger | Replays the dagger's hit and poison |

### 📚 The L.I.G.M.A. Almanac

An in-game book (the **HandBook** button on the HUD) that documents every weapon: stats, size and range diagrams, and combos.

* Combos are a **discovery log**. They show as `??? + ???` until you trigger them in play. Placing items next to each other is not enough, the modifier has to actually affect its target.
* A banner announces each new discovery. If several happen at once, they show one after another.
* While the book is open, the background is blurred and dimmed and the game is paused.
* Discoveries are saved with `PlayerPrefs` and **reset every time the game launches**.

### Features

* 🧩 Drag-and-drop inventory grid puzzle
* ⚔️ Weapons that boost, multiply and repeat each other
* ⏱️ Timer pressure on every stage
* 📚 In-game almanac with discoverable combos
* 🔊 Sound effects (page flips, UI)

---

## 🎮 Controls

| Action | Input |
| ------ | ----- |
| Place a weapon | Drag with Left Mouse onto the grid |
| Open / close the Almanac | HandBook button (or click outside the book to close) |
| Turn Almanac pages | `<` and `>` buttons |
| Pause | Pause button on the HUD |

<!-- TODO: add any keyboard shortcuts, if you have them -->

---

## 📸 Screenshots

<!-- TODO: add screenshots to docs/ and uncomment
![Gameplay 1](docs/gameplay1.png)
![Almanac](docs/almanac.png)
-->

---

## 🚀 Play the Game

<!-- TODO: add your itch.io link or a Releases link -->

---

## 🛠️ Built With

* Unity 6 (Universal Render Pipeline, 2D)
* C#
* TextMeshPro
* Visual Studio Code
* Git & GitHub

---

## 👥 Team

| Name | Role |
| ---- | ---- |
| <!-- TODO --> | <!-- TODO --> |

---

## 📂 Project Structure

```text
Assets/
├── Resources/
│   ├── Audio/        # Almanac sound effects
│   ├── Font/         # Almanac and UI fonts
│   └── Image/        # Almanac emblem
├── Scripts/
│   ├── Handbook/     # L.I.G.M.A. Almanac (HandbookUI, split into partial files)
│   └── Weapons/      # Weapon effects and combo resolution
└── Settings/
```

---

## ⚙️ Getting Started

Clone the repository:

```bash
git clone https://github.com/AKJ16/COMPFEST2026.git
```

Open the project with **Unity 6** (use the version shown in `ProjectSettings/ProjectVersion.txt`), open the **Main Menu** scene, and press **Play**.

---

## 🗺️ Roadmap

- [x] Core grid, weapons and combos
- [x] L.I.G.M.A. Almanac with discoverable combos
- [ ] Tutorial stage before Stage 1, with a dummy enemy and a guide

---

## 🏆 Credits

### Fonts

* **Crimson Text** (SIL Open Font License), used in the Almanac
* **Old Newspaper Types**

### Sound Effects

<!-- TODO: add the source of the page flip sound and any other audio -->

### Art

<!-- TODO: add artist names and asset sources -->

*(Replace with actual credits as required by the licenses.)*

---

## 📜 License

This project is licensed under the **MIT License** unless stated otherwise.

---

## ❤️ Acknowledgements

Thanks to the organizers of **COMPFEST 2026** and everyone who played our game!

If you enjoyed the project, consider giving this repository a ⭐.
