# Asset Catalog

This file tracks downloaded art assets currently inside the project.

## Folder Rules

- `Assets/Art/`:
  Original source packages, working files, previews, and raw imports.
- `Assets/Resources/Imported/`:
  Runtime-loadable copies used by `RuntimeModelSpawner`.
  Do not move or rename these folders unless code paths are updated.
- `Assets/Resources/Characters/`:
  Runtime character sprites currently loaded in-game.

## 1. Character Assets

### 1.1 Pixel hero source
- Path: `Assets/Art/Characters/PixelHero/`
- Type: 2D character source sheets
- Count: 3 PNG files
- Use:
  Main hero reference sheets
  Manual sprite extraction source

### 1.2 Sagiri extraction work folders
- Path: `Assets/Art/Characters/SagiriExtracted/`
- Type: 2D extracted frames
- Use:
  Per-frame cleanup
  Temporary extraction output

- Path: `Assets/Art/Characters/SagiriGrid/`
- Type: 2D grid slices
- Use:
  Grid-based frame inspection
  Candidate animation cells

### 1.3 Sagiri pipeline working set
- Path: `Assets/Art/Characters/SagiriPipeline/`
- Type: 2D processing pipeline
- Main subfolders:
  - `Incoming/`: newly dropped inputs
  - `Raw/`: raw per-animation sheets
  - `Sources/`: clean base directional frames
  - `Normalized/`: processed animation outputs
  - `Previews/`: review sheets
  - `PreviousRuntime/`: older runtime exports
  - `V2/`, `V3/`: pipeline revisions
- Use:
  Character iteration history
  Sprite normalization work

### 1.4 Runtime hero sprites
- Path: `Assets/Resources/Characters/SagiriRuntime/`
- Type: 2D runtime sprite set
- Count: 23 PNG files
- Use:
  Current in-game hero directions
  Walk / attack / idle frame loading

## 2. 3D Environment Source Packages

### 2.1 Village building kit
- Path: `Assets/Art/Environment/Quaternius/MedievalVillageMegaKit/`
- Type: 3D modular environment package
- Main formats:
  - `FBX`
  - `OBJ`
  - `glTF`
  - textures
- Best use:
  Street walls
  roof pieces
  doors
  windows
  stairs
  village facade modules

### 2.2 Graveyard kit
- Path: `Assets/Art/Environment/Kenney/GraveyardKit/`
- Type: 3D themed environment package
- Best use:
  Fog boundary
  cemetery edge
  ritual markers
  fence lines
  crypts
  braziers

### 2.3 Dungeon kit
- Path: `Assets/Art/Environment/Kenney/ModularDungeonKit/`
- Type: 3D modular dungeon package
- Best use:
  Event room shell
  cellar corridors
  sealed underground spaces
  boss descent route

## 2D Environment Source Packages

### 2D.1 Pixel medieval village mini pack
- Path: `Assets/Art/Environment2D/PixelVillageMiniPack_AI/pixelvault-theme-medieval-village/`
- Type: 2D sprite pack
- Count: 99 PNG files + 1 README
- Status:
  Imported and available
  AI-assisted source naming suggests it should be reviewed before final ship use
- Best use:
  Temporary 2.5D facade cards
  Building silhouette tests
  Street furniture and prop sprites
  Village signage and stand-ins

### 2D.2 Medieval decorations pack
- Path: `Assets/Art/Environment2D/DecorationsMedieval/decoration_medieval/`
- Type: 2D tileset + Tiled map descriptors
- Count:
  2 PNG files
  2 TSX files
  1 TMX file
  1 credits file
- Best use:
  Fences
  signs
  decoration overlays
  tiled dressing references

### 2D.3 Isoverse outdoors free
- Path: `Assets/Art/References/IsoverseMedievalOutdoorsFree/Isoverse medieval outdoors free/`
- Type: reference-only package
- Count:
  1 PNG preview
  1 readme
- Status:
  No usable production sprites in the downloaded zip
  Keep as visual reference only unless a fuller pack is downloaded later
- Best use:
  Angle reference
  silhouette reference
  target art direction reference

## 3. 3D Prop Source Packages

### 3.1 Fantasy props
- Path: `Assets/Art/Props/Quaternius/FantasyPropsMegaKit/`
- Type: 3D prop package
- Best use:
  Crates
  barrels
  tables
  shelves
  candles
  benches
  lanterns
  books
  tools
  carts

## 4. Material Source Packages

### 4.1 Poly Haven wood
- Path: `Assets/Art/Materials/PolyHaven/MedievalWood/`
- Type: material source
- Count: 8 texture files
- Best use:
  Doors
  trim
  wooden house blocks
  barricades

### 4.2 Poly Haven brick
- Path: `Assets/Art/Materials/PolyHaven/MedievalRedBrick/`
- Type: material source
- Count: 8 texture files
- Best use:
  Brick walls
  cellar walls
  ritual platforms
  foundation blocks

## 5. Runtime-Loadable Imported Sets

These folders are the active runtime asset library.

### 5.1 Medieval village runtime set
- Path: `Assets/Resources/Imported/Quaternius/MedievalVillage/`
- Type: runtime 3D building modules
- Count: 176 FBX files
- Use:
  Building facade spawning
  street architecture
  roof and wall runtime assembly

### 5.2 Fantasy props runtime set
- Path: `Assets/Resources/Imported/Quaternius/FantasyProps/`
- Type: runtime 3D props
- Count: 94 FBX files
- Use:
  Street dressing
  room props
  investigation clutter

### 5.3 Graveyard runtime set
- Path: `Assets/Resources/Imported/Kenney/GraveyardKit/`
- Type: runtime 3D atmosphere props
- Count: 91 FBX files
- Use:
  Black fog edge dressing
  cemetery props
  sealed zone atmosphere

### 5.4 Dungeon runtime set
- Path: `Assets/Resources/Imported/Kenney/ModularDungeon/FBX/`
- Type: runtime 3D dungeon modules
- Count: 39 FBX files
- Use:
  Ritual room modules
  cellar and corridor pieces
  underground transition spaces

## 6. Recommended Scene Mapping

### Main
- Primary:
  `Resources/Imported/Quaternius/MedievalVillage`
- Secondary:
  `Resources/Imported/Quaternius/FantasyProps`
  `Resources/Imported/Kenney/GraveyardKit`

### Prologue_EventRoom
- Primary:
  `Resources/Imported/Kenney/ModularDungeon/FBX`
- Secondary:
  `Resources/Imported/Quaternius/FantasyProps`
  `Resources/Imported/Kenney/GraveyardKit`

### Chapter01_RedCreek_Entrance
- Primary:
  `Resources/Imported/Quaternius/MedievalVillage`
- Secondary:
  `Resources/Imported/Quaternius/FantasyProps`

### Chapter01_RedCreek_Core
- Primary:
  `Resources/Imported/Quaternius/MedievalVillage`
- Secondary:
  `Resources/Imported/Quaternius/FantasyProps`

### Chapter01_BossHouse
- Primary:
  `Resources/Imported/Quaternius/MedievalVillage`
- Secondary:
  `Resources/Imported/Kenney/GraveyardKit`
  `Resources/Imported/Quaternius/FantasyProps`

### Chapter01_End
- Primary:
  `Resources/Imported/Kenney/ModularDungeon/FBX`
- Secondary:
  `Resources/Imported/Kenney/GraveyardKit`
  `Resources/Imported/Quaternius/FantasyProps`

## 7. Cleanup Notes

- Keep `Assets/Art/` as source storage.
- Keep `Assets/Resources/Imported/` as runtime storage.
- Do not duplicate new packs into both places unless they must be runtime-loaded.
- If a future pack is 2D-only, prefer:
  - `Assets/Art/Environment2D/<PackName>/`
  - `Assets/Resources/Environment2D/<PackName>/` only if runtime lookup is required.
- Current 2D source packages now live in:
  - `Assets/Art/Environment2D/`
  - `Assets/Art/References/`
