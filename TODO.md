# Todo

Planned work, ideas, and features. Each line:
`- [ ] [Priority] Short description *(YYYY-MM-DD)*`

Priority is one of `High`, `Medium`, `Low`. Managed by the `/todo` and `/plan` skills.

<!-- OPEN -->
- [ ] [High] Voxel placement: Scale mode — 6 face drag handles + Width/Depth/Height slider panel, both driving VoxelController.Resize *(2026-04-21)*
- [ ] [High] Voxel placement: Placement mode — finger drag raycasts against AR plane and calls VoxelController.MoveTo *(2026-04-21)*
- [ ] [High] Voxel placement: Rotation mode — on-screen rotate-left/right buttons spin voxel around Y axis in 15° increments *(2026-04-21)*
<!-- /OPEN -->

## Done

<!-- DONE -->
- [x] [High] Voxel placement: Voxel prefab — 1×1×1m cube, wireframe outline + semi-transparent URP material, pivot at bottom-center *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: VoxelController — MonoBehaviour owning dimensions, position, rotation; exposes Resize, MoveTo, Rotate methods *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: AR tap-to-place — raycast hit on plane spawns voxel once; subsequent taps ignored outside Edit > Placement *(2026-04-21, done 2026-04-22)*
- [x] [High] Voxel placement: Edit/Done UI — Done locks voxel; Edit reveals Scale/Placement/Rotation sub-buttons; state machine drives active mode *(2026-04-21, done 2026-04-22)*
<!-- /DONE -->
