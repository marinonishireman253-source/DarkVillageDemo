# DarkVillage UI Asset Brief for Gemini Pro

## Project Context

- Engine: Unity 6
- Camera language: `cutaway / dollhouse / single-room framing`
- Genre feel: narrative exploration with indoor horror pressure
- Visual priority: keep the center of the room readable; UI should live on edges and lower screen bands
- Mood keywords:
  - old timber house
  - brass and soot
  - cold interior haze
  - doorframe pressure
  - folk horror
  - restrained gothic

## Important Design Rules

- Do not design bright fantasy RPG UI.
- Do not make it look like sci-fi, cyberpunk, or mobile gacha UI.
- Do not use neon glow, large gem buttons, or ornate royal fantasy gold.
- The UI must feel handmade, worn, and slightly oppressive.
- The center playfield must stay visually open.
- Panels should feel like aged lacquered wood, smoked metal, dark linen, tarnished brass, and candlelit varnish.

## Asset Set Needed

Design a cohesive UI kit for a 2.5D indoor horror exploration game. Deliver transparent PNG assets.

Need these assets:

1. Dialogue panel frame
- Wide bottom dialogue frame
- Designed for 9-slice use
- Dark charcoal wood + tarnished brass edge
- Slight grime and age marks
- Optional subtle corner motifs inspired by beams / brackets

2. Choice panel frame
- Smaller vertical panel for response options
- Same family as dialogue panel
- Slightly lighter interior so text remains readable

3. Portrait frame
- Small square portrait holder
- More ornamental than the dialogue box
- Should feel like a framed cabinet photo or devotional plaque

4. Quest panel frame
- Compact top-corner objective panel
- Minimal but not bland
- Same family, lighter ornament density

5. Combat info panel frame
- Compact top-left frame
- Slight ember or dried-blood accent
- Still elegant and restrained

6. Interaction prompt chip
- Small bottom prompt plate
- Includes a separate keycap badge style

7. Modal frame
- Large centered completion / story panel
- Heavier trim and stronger ceremonial mood

8. Decorative overlays
- Thin brass dividers
- Corner brackets
- Small header tabs
- Dust / soot / smoke edge overlays
- Optional faint cloth or paper grain overlay

9. Buttons
- Primary button
- Secondary button
- Hover / selected / pressed states
- Same material family

10. Marker chip
- Small floating target label
- Clear at distance, still grounded in the same art direction

## Material Language

- Base: dark soot-black wood, blue-black lacquer, deep slate
- Metal: muted brass, not shiny gold
- Accent colors:
  - brass
  - moss green
  - ember red
  - bone / wax white for text areas only
- Surface treatment:
  - light scratches
  - worn edges
  - smoke staining
  - subtle candlelit warm highlights

## Composition Guidance

- Avoid heavy ornament across the entire border.
- Ornament should cluster near corners, tabs, and section dividers.
- Panels should feel weighty and quiet, not flashy.
- Think “room pressure” and “old house relic”, not “heroic inventory screen”.

## Technical Delivery Specs

- PNG with transparent background
- Clean 9-slice-safe borders
- 2x resolution preferred
- Keep center fill areas spacious for dynamic text
- No baked text in the images
- No baked icons unless provided as separate elements

Recommended export sizes:

- Dialogue frame: `1600 x 420`
- Choice frame: `900 x 520`
- Portrait frame: `360 x 360`
- Quest frame: `700 x 240`
- Combat frame: `700 x 260`
- Interaction prompt: `780 x 180`
- Modal frame: `1200 x 760`
- Buttons: `480 x 120`
- Marker chip: `220 x 80`

## Prompt to Paste into Gemini Pro

Design a full UI kit for a Unity game with a `cutaway / dollhouse` indoor horror exploration camera. The game shows one room at a time from outside the building, so the UI must stay mostly on screen edges and leave the center readable. Art direction should feel like an old timber house, soot-dark wood, tarnished brass, candle haze, and restrained folk-horror gothic. Avoid bright fantasy RPG styling, mobile game gloss, sci-fi, neon, and decorative overload.

Create transparent PNG concepts for:

- a wide bottom dialogue frame
- a smaller response-choice frame
- a square portrait frame
- a compact quest panel
- a compact combat panel with a faint ember accent
- a small interaction prompt chip with key badge
- a larger modal completion frame
- primary and secondary buttons
- thin divider ornaments
- corner bracket ornaments
- a small floating target marker chip

The kit must feel cohesive, readable, aged, tactile, and slightly oppressive. Use dark charcoal, deep slate, muted brass, moss green, ember red, and wax-white highlights. Surfaces should suggest worn lacquered wood, smoked metal, and faint grime. Borders must be 9-slice friendly. Do not include text labels inside the art. Keep centers open for dynamic Unity text.

## Integration Notes

When these assets are ready, import them as UI sprites and wire them into:

- `Assets/Scripts/UI/Dialogue/DialogueCanvasView.cs`
- `Assets/Scripts/UI/HUD/HudCanvasView.cs`
- `Assets/Scripts/UI/Modal/ModalCanvasView.cs`
- `Assets/Scripts/UI/Core/UiTheme.cs`
