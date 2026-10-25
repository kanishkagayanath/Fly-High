# Circular driving minimap

Added on 2026-10-25 for the PC CityDrive HUD.

- Round map anchored to the top-right of the safe area, 220 by 220 reference UI units with a 20-unit margin.
- A fixed north-up view displays nearby roads, pavements, building footprints and shoreline. A mint arrow stays centred on the car and turns with its heading; an N badge marks north.
- Map radius is 55 world metres. It follows the car's interpolated rendered position, using the same pose source as the smooth driving camera.
- Map geometry is cached at startup from the authored scene; per-frame updates move the map and arrow. No extra camera or RenderTexture is used by the minimap.
- The map and existing status HUD hide together in the menu and return on resume. Noninteractive map graphics do not block input.

Files:
- `Assets/PhoneController/CityMinimap.cs`: minimap layout, scene-map geometry and player tracking.
- `Assets/PhoneController/MinimapGraphic.cs`: vector circles, player arrow and cached map triangles, with the required CanvasRenderer.
- `Assets/PhoneController/PrototypeBootstrap.cs`: creates the minimap only in CityDrive's PC branch and groups the driving HUD for menu visibility.

Validation: live Unity compilation passed. Game-view capture verified the circular display. Runtime checks found zero position/heading error after moving and turning the player, 9,069 cached geometry vertices, correct top-right anchors, circular masking and zero raycast targets. Menu hide/resume checks passed. The initial missing CanvasRenderer setup was corrected; the final live Console contained zero errors. Returned to Edit mode and removed the temporary capture asset from the project.

Existing non-readable imported meshes use a rectangular footprint fallback; the current widened roads use their real curved mesh geometry. Buildings use clear rectangular footprints rather than rendering rooftops.

Preview: `Minimap-preview.png`. The phone controller APK does not need rebuilding. Rebuild the standalone PC game when you want the minimap in its executable.

## Road appearance update - 2026-10-25

The minimap now reuses the scene's Roads texture with the authored mesh UVs and material tiling/offset. Dark asphalt, dashed lane lines, curved markings and junction paint therefore match the driving scene. Flat dark asphalt remains the fallback for untextured roads. Pavements, building footprints, shoreline, circular clipping and player marker retain their existing presentation.

This remains a cached uGUI map: the current city uses 8,817 flat vertices plus 252 textured road vertices (9,069 total), with one shared road-texture layer and no extra camera or RenderTexture. Texture pixels do not need CPU read access. Geometry is built at startup; tracking only moves the content and turns the player arrow.

Validation: Unity live compilation completed without errors. Game-view capture confirmed dark roads and source lane markings. Runtime checks verified textured UVs, circular masking, no raycast targets, zero follow/heading error after a player move/turn, and menu hide/resume. Final current Unity Console: zero errors, zero warnings. No phone APK rebuild is needed; rebuild the PC executable to include this update.
