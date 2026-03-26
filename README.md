# NOTE
THIS IS PROBABLY THE WORSE PIECE OF SOURCE CODE U HAVE EVER SEEN.But hey i mean it works
## ONLY WORKS WITH 768x1633 SCREENS. (Dont ask why, commit pull request if u wanna change it)

**Project Overview**
This is a Unity 2D arcade dodger with character selection, score-based difficulty scaling, pickups, and animated transitions between scenes.

**Gameplay**
- Dodge falling obstacles and survive as long as possible.
- Score increases every second while the game is running.
- Heal pickups restore a life (up to the max).
- Game ends when lives reach zero and transitions to the death screen.

**Controls**
- Move left/right using the Input System `Move` action (keyboard arrows/A-D or gamepad stick if mapped).
- In character selection, use arrow keys to switch and `Enter` to confirm.

**Scenes**
- `MainMenu`: Main menu + character selection + music.
- `GameScene`: Core gameplay.
- `DeathScreen`: Game over UI with score reveal and countdown.

**Player Controller**
The player is driven by `playerController` with acceleration/deceleration, horizontal bounds, and sprite swapping for idle/left/right. It also updates a UI head sprite for normal/damage/heal states and applies short invulnerability windows on damage or heal.

**Obstacle System**
`ObstacleSpawner` handles spawning obstacles and heals, plus scaling difficulty over time and by score.
- Spawn rate accelerates over time, capped by `maxSpawnRate`.
- Time phases: Easy (0–30s), Medium (30–60s), Hard (60–90s), Insane (90s+).
- Score tiers unlock harder obstacle sets via thresholds.
- Supports multi-spawn events and wave events.

**Scoring and Lives**
`GameManager` tracks score, lives, and high score. Score increments once per second by a random value between 2 and 7 at game start. Hearts UI is updated when lives change.

**Audio**
`AudioManager` plays a random music clip and one-shot SFX. Volume settings persist via `AudioPrefs`. There are also scene-specific audio scripts for main menu and death screen.

**Parallax**
`parallax` scrolls a material’s texture offset vertically and/or horizontally for background motion.

**Persistence**
Stored via `PlayerPrefs`:
- `HighScore`
- `LastScore`
- `MusicVolume`
- `SfxVolume`
- `selectedCharacter`

**Dependencies**
- Unity Input System (`PlayerInput`, `InputAction`).
- TextMeshPro (`TextMeshProUGUI`).
- Cinemachine (for impulse camera shake).
- DOTween (for menu UI animations).

**Setup Checklist**
- Add `GameManager` to `GameScene` and assign `scoreDisplay`, `highScoreDisplay`, `heartImages`, and optional `loader`.
- Add `ObstacleSpawner` to `GameScene` and assign `obstaclePrefabs` and `healPrefab`.
- Create a Player object with `playerController`, `SpriteRenderer`, `PlayerInput` (with `Move` action), `CinemachineImpulseSource`, and a 2D trigger collider. Assign movement sprites and `playerHead`.
- Tag the Player `Player`.
- Tag obstacles `Obstacle` and give them `ObstacleController` + 2D trigger collider.
- Tag heal pickups `Heal` and give them a 2D trigger collider.
- Add an `AudioManager` object tagged `Audio` with music/sfx `AudioSource`s and clips.
- Ensure scenes named `MainMenu`, `GameScene`, and `DeathScreen` are added to Build Settings.
- In `MainMenu`, assign `MainScript` references and populate the `characters` list with sprites for normal/damage/heal.
- If using transitions, add `TransitionLoader` objects with an `Animator` and start/end clips.

**Notes**
`GameManager` saves score on game over and on app pause/focus changes. The death screen reads `LastScore` and `HighScore` from `PlayerPrefs` to display results.
