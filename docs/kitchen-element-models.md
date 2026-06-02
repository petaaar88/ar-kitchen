# Kitchen element models

Kitchen element 3D models live in `Assets/Models/`, split into 3 groups, each a subfolder with `.blend` source + exported `.fbx` (FBX imported with useFileScale/useFileUnits). Each model file is named `<code> <Type>.fbx` (e.g. `C3 Stove.fbx`), where the code marks its standardised measurement.

Measurements are **width × height × depth in cm**:

**Storage** (Fridge models, `Assets/Models/Storage`):
- S1 — 60 × 90 × 60
- S2 — 60 × 180 × 60
- S3 — 90 × 180 × 60
- S4 — 120 × 180 × 60

**Washing** (Sink models, `Assets/Models/Washing`):
- W1 — 30 × 90 × 60
- W2 — 60 × 90 × 60
- W3 — 90 × 90 × 60
- W4 — 120 × 90 × 60

**Cooking** (Stove models, `Assets/Models/Cooking`):
- C1 — 30 × 2 × 60  (intentional: thin drop-in cooktop surface, 2 cm tall)
- C2 — 60 × 90 × 60
- C3 — 120 × 180 × 60

Long-term plan: an [external service](external-layout-service.md) will receive the voxel's width/depth/height and return a generated kitchen layout (likely JSON) selecting/placing these models automatically.

Each model has a `KitchenElementDefinition` ScriptableObject (in `Assets/Scripts/Kitchen/Definitions/`, named `<code> <Type>.asset`) carrying group/code/dimensions and a reference to its FBX. `KitchenElementView.Apply()` instantiates that FBX and drops its bounding-box min corner onto the view's bottom/back/left pivot (keeping the FBX's imported axis-correction rotation/scale, plus a 180° yaw so the front faces the room). `KitchenLayoutController` lines them up along the voxel's -X wall with a 270° Y rotation; the catalog (`KitchenCatalogUI`, built by `UISceneSetup`) shows an 11-button grid grouped by group colour. The procedural cube was removed — `KitchenElement.prefab` is now just a container (view + floating label). Regenerate via Tools ▸ AR Kitchen ▸ Create Kitchen Element Prefab / Create Default Kitchen Definitions / Setup UI. Element "mandatory" flags were dropped (don't map to size variants).
