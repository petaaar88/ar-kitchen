# Todo

Planned work, ideas, and features. Each line:
`- [ ] [Priority] Short description *(YYYY-MM-DD)*`

Priority is one of `High`, `Medium`, `Low`. Managed by the `/todo` and `/plan` skills.

<!-- OPEN -->
- [ ] [High] Kitchen elements: KitchenElementDefinition ScriptableObject (displayName, sizeMeters, color, isMandatory, isFiller) + 4 assets for Fridge/Stove/Sink/Counter *(2026-06-01)*
- [ ] [High] Kitchen elements: KitchenElementView prefab — colored cuboid + floating TMP label, scaled from definition at runtime *(2026-06-01)*
- [ ] [High] Kitchen elements: KitchenLayoutController on voxel — ordered list of instances along local +X from -X/-Z corner; TryAdd / RemoveLast / UsedLength / RemainingLength / DepthFits; recompute on Resize *(2026-06-01)*
- [ ] [High] Kitchen elements: extend VoxelEditMode with FillKitchen, wire VoxelStateManager.SetMode to toggle the catalog UI without breaking existing modes *(2026-06-01)*
- [ ] [High] Kitchen elements: bottom catalog bar UI with 4 tap-to-add buttons (color swatch + name + width) calling KitchenLayoutController.TryAdd *(2026-06-01)*
- [ ] [High] Kitchen elements: live remaining-length TMP readout above catalog ("X.X m free") driven by KitchenLayoutController.RemainingLength *(2026-06-01)*
- [ ] [High] Kitchen elements: catalog button disabled-state when element won't fit remaining length or voxel depth too shallow *(2026-06-01)*
- [ ] [Medium] Kitchen elements: reject + warn on depth-too-shallow — TryAdd returns reason enum, UI shows transient toast ("Voxel too shallow for Fridge — needs 65 cm depth") *(2026-06-01)*
- [ ] [High] Kitchen elements: single "Remove last" button calling KitchenLayoutController.RemoveLast; disabled when list is empty *(2026-06-01)*
- [ ] [Medium] Kitchen elements: live mandatory-element banner ("Missing: Fridge, Sink") that auto-hides when all isMandatory definitions are placed at least once *(2026-06-01)*
- [ ] [High] Kitchen elements: KitchenUI integration — add "Fill" button alongside Edit/Done that enters FillKitchen mode; catalog + banner only show in this mode *(2026-06-01)*
<!-- /OPEN -->

## Done

<!-- DONE -->
- [x] [High] Voxel placement: Voxel prefab — 1×1×1m cube, wireframe outline + semi-transparent URP material, pivot at bottom-center *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: VoxelController — MonoBehaviour owning dimensions, position, rotation; exposes Resize, MoveTo, Rotate methods *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: AR tap-to-place — raycast hit on plane spawns voxel once; subsequent taps ignored outside Edit > Placement *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: Edit/Done UI — Done locks voxel; Edit reveals Scale/Placement/Rotation sub-buttons; state machine drives active mode *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: Rotation mode — on-screen rotate-left/right buttons spin voxel around Y axis in 15° increments *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: Placement mode — finger drag raycasts against AR plane and calls VoxelController.MoveTo *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: Scale mode — Width/Depth/Height slider panel driving VoxelController.Resize *(2026-04-21, done 2026-04-22)*
<!-- /DONE -->
