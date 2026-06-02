# External layout service

The core idea of AR Kitchen: once the user finalizes the voxel's dimensions, the app calls an **external service** with the voxel's **width, depth, and height**. The service returns a generated kitchen **layout** (probably a JSON object) describing which elements go where.

The app then realizes that layout by placing the corresponding 3D models from [kitchen element models](kitchen-element-models.md) — the same `KitchenElementDefinition` / `KitchenElementView` / `KitchenLayoutController` pipeline that currently handles manual placement.

Status (as of 2026-06-02): not built yet. Current code only supports **manual** placement — the user taps catalog buttons to add elements one at a time. The service-driven auto-layout is the next major step. When implemented, the response format (element codes like S1/W2/C3, positions, ordering) needs to map onto the existing definitions and the wall-aligned layout logic.
