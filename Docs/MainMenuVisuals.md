# Illustrated main menu

The desktop main menu uses the supplied artwork in `Assets/UI`: MainMenu.jpg is the city/car/title backdrop, Group1.png is the cyan Play/Resume frame, Document.png the purple Options frame, and Group.png the orange Exit frame. The menu keeps the existing gameplay state and controls.

Play is the largest action, with a secondary caption. Buttons have smooth hover, keyboard-focus and pressed scaling, plus brightness transitions. Main and Options panels ease in over 0.28 seconds using unscaled time, so presentation continues while gameplay is paused. A feathered left gradient improves text readability without hiding the car or baked title. The full illustration fits inside the available safe area; ultrawide and 4:3 screens use a dark surround rather than cropping the title/car.

Options has matching framed controls, a volume meter, fullscreen choice and city lighting choice. The phone card updates between Connect your phone and Ready to drive after the existing Wi-Fi receiver connects or times out. Its instructions point to Find PC in the current controller app. Networking continues during menu pause.

## Authoring and ownership

`Assets/PhoneController/Resources/FlyHighMenu.asset` contains persistent references to the original background and imported button sprites. Replace those references to retheme the menu. No JPEG pixels were changed or files moved. UI import settings disable mipmaps, clamp texture edges, preserve non-power-of-two proportions, and use high-quality compression; the background retains its 2752×1536 resolution. Button imports use single full-rect sprites with nine-slice borders. Source GUIDs are preserved.

`DesktopMainMenu` builds the desktop view and continues to own pause/resume, Options and Exit. `MenuButtonMotion` animates only the button visual child. `MenuGradient` is a small cached uGUI mesh. No extra camera, runtime texture processing, material clones, packages, scene edits or phone UI changes are needed. The existing bootstrap passes its receiver to the menu for connection status.

## Validation — 2026-10-25

- Live Unity compilation passed. Separate Windows and Android conditional source compilation passed, with existing unused opposite-platform bootstrap-field warnings only.
- Actual menu button invocations passed Play, Resume label, Options, volume decrease/meter, city-lighting mode cycle and Back; pause hid the driving HUD, and resume restored it. Exactly one EventSystem remained, with keyboard focus restored to Play/Resume. Test volume/mode preferences were restored.
- Unscaled hover/press tests while timeScale was zero measured scales 1.035 and 0.975, with stable button-root dimensions. Live UDP input changed the phone card to Ready to drive, and timeout restored Connect your phone.
- Layout checks at 1280×720, 2560×1080 and 1024×768 retained the entire artwork aspect ratio and nonzero text areas. Main menu and Options were inspected in Game-view captures.
- The actual Exit button stopped Editor Play mode. Afterward CityDrive was not dirty, its Android build target was unchanged, timeScale was restored to 1, temporary layout objects were gone and the controller UDP port was free. Final current Console had zero errors; an existing Account API warning was unrelated. A fresh menu session was captured with the initial Play label, then returned to Edit mode.

Use Unity Play mode to see the updated menu. Rebuild the PC game before distributing it. The existing phone APK is compatible; no Android rebuild is needed. No standalone PC executable or new APK was built for this visual update.


## Updated imported PNG buttons — 2026-10-29

FlyHighMenu now references Group1.png (cyan Play), Document.png (purple Options), and Group.png (orange Exit). Their transparent full-rect sprites retain the supplied artwork and GUIDs, with mipmaps off, NPOT proportions retained and borders configured for nine-slice scaling. The user had removed the older JPEG button files; those files were not restored or deleted by this change.

The three actual menu controls rendered the assigned sprites. Play/Resume, Options, Back, one EventSystem and zero-drift traffic pause passed. The actual orange Exit button stopped Play. Preview: the Codex chat outputs/Main-menu-new-buttons.png. No phone APK update is required.

## Main-menu vehicle shop — 2026-10-29

The home menu now has Play, Shop, Options and Exit, reusing the imported button frames. Shop displays six actual vehicle previews with saved balance, purchase prices, owned/selected states and Buy/Select actions. It hides the home phone card while open. Back and Escape return home. The city remains paused while shopping; hover/focus presentation continues in unscaled time. See VehicleShop.md for catalog configuration and persistence.

## In-game pause — 2026-11-01

The top-left PAUSE button and Escape now open an in-game menu with Resume, Settings (master sound control) and Home. It uses the same imported cyan/purple/orange button frames as the main menu. Home returns to the menu while keeping the current drive/mission; the existing Play/Resume button continues it. The rebuilt phone controller mirrors pause actions and sound settings. See PauseMenu.md for installation and validation.

## Marked content cleanup — 2026-11-01

Removed the home FREE ROAM/Your city captions, shared keyboard/help footer and exploration slogan, plus the Options Display row. The four home buttons now sit slightly higher with consistent spacing. Options retains sound, city lighting and Back, reflowed into the available space. The saved startup fullscreen preference remains intact. The shop's default persistence sentence is removed; purchase/select/error feedback is still shown when an action needs it.
