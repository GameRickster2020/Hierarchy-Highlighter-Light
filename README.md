# Hierarchy Highlighter Light

A lightweight Unity 6.6 editor tool for color-coding GameObjects in the Hierarchy.

## Installation

Copy these files into the same relative locations in your Unity project:

- `Assets/PREFABS-GRMEDIA/SCRIPTS/Hieratchy/HierarchyHighlight.cs`
- `Assets/PREFABS-GRMEDIA/SCRIPTS/Hieratchy/HierarchyHighlight.cs.meta`
- `Assets/PREFABS-GRMEDIA/SCRIPTS/Hieratchy/HierarchyHighlightDrawer.cs`
- `Assets/PREFABS-GRMEDIA/SCRIPTS/Hieratchy/HierarchyHighlightDrawer.cs.meta`

The `.meta` files must be copied with the scripts so existing scene and prefab component references remain valid.

This tool targets Unity 6.6 and requires the built-in Hierarchy package in `Packages/manifest.json`:

```json
"com.unity.modules.hierarchy": "1.0.0"
```

Allow Unity to recompile and finish importing the scripts.

## Applying a highlight

1. Select one or more GameObjects in the Hierarchy.
2. Open `Window > Hierarchy Highlights`.
3. Click a colored preset such as `WIP`, `Edited`, `Fix`, or `Done`.

The selected objects receive a `HierarchyHighlight` component if needed, and their existing serialized color is preserved unless you choose a new preset.

You can also right-click a GameObject in the Unity 6.6 Hierarchy and choose:

`Hierarchy Highlight > [preset]`

Use `Hierarchy Highlight > Clear Highlight` to remove the component from the selected object(s).

## Dedicated preset tab

Open `Window > Hierarchy Highlights`, then dock or drag the window tab beside the Hierarchy window. This keeps the preset controls separate from Unity's `Open Prefab in Context` control.

## Notes

- The tool supports Unity 6.6's new UI Toolkit Hierarchy and the legacy Hierarchy callback.
- Runtime behavior is unchanged; `HierarchyHighlight` only stores a color for editor display.
- Do not rename the `HierarchyHighlight` class or replace its `.meta` file when updating an existing project.
