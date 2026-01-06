# Open76 AI Coding Guidelines

## Project Overview
Open76 is a Unity engine reimplementation of Activision's Interstate '76 (1997), parsing original game assets and formats to recreate gameplay in modern Unity. Focuses on level rendering, car physics, and mission scripting via stack-based FSM.

## Architecture
- **Core Systems**: Singleton managers (CacheManager, VirtualFilesystem, LevelLoader) handle asset loading and world setup.
- **Asset Pipeline**: Custom parsers in `Assets/Scripts/System/Fileparsers/` reverse-engineer I76 formats (.geo meshes, .sdf scenes, .vcf cars, .msn missions).
- **Mission Logic**: FSMRunner executes stack machines with bytecode opcodes (PUSH, ACTION, JMP) for game logic.
- **File Access**: VirtualFilesystem prioritizes loose files (MISSIONS/, ADDON/) over compressed ZFS archive.

## Key Patterns
- Use singletons for global state (e.g., `CacheManager.Instance.ImportMesh()`).
- Cache parsed assets in dictionaries (meshes, materials, SDFs) to avoid redundant parsing.
- Handle palette-based textures: load .act palettes, apply to .vqm/.map textures.
- Instantiate prefabs from Resources/ (e.g., `Resources.Load<GameObject>("Prefabs/CarPrefab")`).
- FSM actions in FSMActionDelegator: implement entity-specific behaviors (cars, buildings).

## Conventions
- File parsers: Static `ReadX()` methods (e.g., `GeoParser.ReadGeoMesh(filename)`).
- Naming: PascalCase for classes/methods, camelCase for locals.
- Error handling: Debug.LogWarning for missing assets, exceptions for format errors.
- Coordinates: I76 uses custom units; convert via `/100.0f` for rotations, `*640` for terrain patches.

## Workflows
- **Level Loading**: Parse .msn via MsnMissionParser, create terrain patches, place objects from .sdf/.vcf.
- **Asset Import**: CacheManager.ImportGeo() for meshes, ImportSdf() for scenes, ImportVcf() for cars.
- **Debugging**: Use FSMRunner gizmos for path visualization; check VirtualFilesystem.FileExists() for asset presence.
- **Building**: Standard Unity build process; no custom scripts.

## Examples
- Load mesh: `GeoMeshCacheEntry entry = CacheManager.Instance.ImportMesh("car.geo", vtf, textureGroup);`
- Place object: `GameObject obj = CacheManager.Instance.ImportSdf("building.sdf", parent, pos, rot, canWreck, out sdf, out wrecked);`
- FSM action: Implement in FSMActionDelegator.DoAction() switch case for new actions.

Reference: [CacheManager.cs](C:/Users/Rob/Documents/Unity/Open76/Assets/Scripts/System/CacheManager.cs), [VirtualFilesystem.cs](C:/Users/Rob/Documents/Unity/Open76Assets/Scripts/System/VirtualFilesystem.cs), [LevelLoader.cs](C:/Users/Rob/Documents/Unity/Open76/Assets/Scripts/System/LevelLoader.cs)