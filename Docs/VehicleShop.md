# Vehicle shop

Open the main menu and choose **SHOP**. The starter Sunset Sport is already owned. Complete coin missions to earn coins, then use **BUY** on an affordable vehicle. A purchase deducts its price once and selects it immediately. Owned vehicles use **SELECT** without another charge. **DRIVING** identifies the selected vehicle. Back or Escape returns to the menu, and Play/Resume drives the selection.

| Vehicle | Model | Price in earned coins |
|---|---|---:|
| Sunset Sport | Current Car_13 | Free starter |
| City Hatch | Car_06 | 100 |
| Coastal Classic | Car_19 | 180 |
| Gold Coupe | Car_16 | 300 |
| Town Cruiser | Van | 250 |
| Neon Concept | Futuristic_Car_1 | 450 |

All six share the current arcade handling and collision footprint. This shop sells vehicle appearances. Previews show the actual imported prefabs. The UI displays your current balance, owned/selected state and how many more coins are needed for an unaffordable vehicle.

## Currency and saving

VehicleGarage is the single owner of currency, owned IDs and selected ID. Its versioned PlayerPrefs snapshot is `FlyHigh.Garage.v1`, containing balance, owned IDs and selection together. Existing `FlyHigh.Coins` is migrated on first use and kept as a compatibility mirror. CoinMissionSystem reads this same balance, credits pickups/bonuses through the garage and publishes it to the phone/HUD. Shopping cannot be undone by an old mission balance being saved afterward.

Purchases validate the catalog entry, ownership and available balance before changing state. Duplicate purchases, locked selections and unknown IDs do not spend coins. Purchases and selections save immediately; mission credits save on completion, pause or shutdown. Balances clamp at int.MaxValue. Unknown owned IDs are retained for future catalog entries. Corrupt/unsupported snapshots are preserved and purchases are disabled rather than silently replacing that record.

Ownership/selection persists across Play sessions and application restarts. Starting a new game session uses the selected prefab; an in-session selection replaces only the visual model. The Rigidbody, collision guard, player position/heading, mission/minimap references and camera target stay the same. Models are centred in car-local space and scaled to fit the existing 1.8 m width / 3.6 m length envelope. Wheel animation is rebound, and old vehicle-light fittings/materials are released before new ones attach. This also works after the player has moved and turned.

## Authoring

- `Resources/FlyHighVehicleShop.asset`: six stable vehicle IDs, names, prices, prefab references and preview sprites. Edit prices here before Play. Keep IDs stable to preserve saved ownership.
- `Editor/VehicleShopBuilder.cs`: **Fly High > City > Prepare vehicle shop** regenerates the six default entries and thumbnails. Running it resets entries to the authored defaults above.
- `Shop/*.png`: six editor-rendered 512x256 previews, imported as sprites. No runtime preview camera or RenderTexture is used.
- `VehicleShopPanel`: six cards, Buy/Select states, balance and Back, using the imported menu frames.
- `DesktopMainMenu`: four-button home layout (Play, Shop, Options, Exit), shop navigation and balance/selection label. Gameplay stays paused in the shop. The phone connection card is hidden while shopping.
- `PrototypeBootstrap`: initialises the garage before the player model and passes the same instance to missions/menu.

No scene, package, physics settings or phone protocol changes are needed. The current controller APK is compatible. Rebuild a standalone PC game before distributing this feature.

## Validation

Live Editor compilation, separate Windows/Android conditional runtime compilation, save/purchase checks, mission reward integration and restart persistence were exercised. Validation artifacts are in the Codex chat outputs/Vehicle-shop-*.json; preview is outputs/Vehicle-shop-preview.png. Test funds and purchases are restored afterward, preserving the user's original 79 coins and original garage record. No standalone PC build or physical Android test was performed.

Final checks passed 17 save cases and 30 purchase/selection cases. A completed mission after purchases added 27 pickups + 25 bonus without losing the shopping balance. All six models retained four wheels and two headlight spots; the selected model survived a fresh session, with one player visual/light component. A real local controller-packet replay drove the selected car 23.18 m, reached 27.83 m/s and settled to zero after release. Layouts at 1280x720, 2560x1080 and 1024x768 retained the artwork and nonzero text areas. The final player preferences were restored to their original record and 79 coins; all test currency and purchases were removed. Current Console errors: zero. Known Editor port retention was cleared through native project reopen and explicit live receiver close before final Stop.
