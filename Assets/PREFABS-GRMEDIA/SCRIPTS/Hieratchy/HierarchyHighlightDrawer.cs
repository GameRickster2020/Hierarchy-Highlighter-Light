using System.Collections.Generic;
using UnityEditor;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEngine;
using UIToolkit = UnityEngine.UIElements;

[InitializeOnLoad]
public static class HierarchyHighlightDrawer
{
    private static readonly List<HierarchyViewItem> BoundItems = new List<HierarchyViewItem>();
    private const float SwatchSize = 12f;
    private const float SwatchRightPadding = 6f;
    private const float LeftBarWidth = 4f;

    static HierarchyHighlightDrawer()
    {
        EditorApplication.hierarchyWindowItemByEntityIdOnGUI -= OnHierarchyGUI;
        EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI;

        HierarchyWindow.BindViewItem -= OnHierarchyViewItem;
        HierarchyWindow.BindViewItem += OnHierarchyViewItem;

        HierarchyWindow.UnbindViewItem -= OnUnbindHierarchyViewItem;
        HierarchyWindow.UnbindViewItem += OnUnbindHierarchyViewItem;

        HierarchyWindow.PopulateContextMenu -= OnPopulateContextMenu;
        HierarchyWindow.PopulateContextMenu += OnPopulateContextMenu;
    }

    private static void OnPopulateContextMenu(
        HierarchyWindow window,
        HierarchyView view,
        HierarchyViewItem item,
        UIToolkit.DropdownMenu menu)
    {
        if (item.Handler is not HierarchyGameObjectHandler gameObjectHandler)
            return;

        GameObject go = gameObjectHandler.GetGameObject(item.Node);
        if (go == null)
            return;

        foreach (HierarchyHighlightPopup.Preset preset in HierarchyHighlightPopup.Presets)
        {
            HierarchyHighlightPopup.Preset selectedPreset = preset;
            menu.AppendAction(
                $"Hierarchy Highlight/{selectedPreset.label}",
                _ => ApplyPresetToObject(go, selectedPreset.color),
                UIToolkit.DropdownMenuAction.Status.Normal);
        }

        menu.AppendSeparator("Hierarchy Highlight/");
        menu.AppendAction(
            "Hierarchy Highlight/Clear Highlight",
            _ => ClearHighlightObject(go),
            UIToolkit.DropdownMenuAction.Status.Normal);
    }

    private static void ApplyPresetToObject(GameObject go, Color color)
    {
        if (!IsInSelection(go))
            Selection.activeGameObject = go;

        ApplyColorToSelection(color);
    }

    private static void ClearHighlightObject(GameObject go)
    {
        if (!IsInSelection(go))
            Selection.activeGameObject = go;

        ClearHighlightSelection();
    }

    private static void OnHierarchyViewItem(
        HierarchyWindow window,
        HierarchyView view,
        HierarchyViewItem item)
    {
        if (item.Handler is not HierarchyGameObjectHandler gameObjectHandler)
            return;

        GameObject go = gameObjectHandler.GetGameObject(item.Node);
        Color rowColor = go != null && go.TryGetComponent(out HierarchyHighlight marker)
            ? marker.color
            : Color.clear;

        if (!BoundItems.Contains(item))
            BoundItems.Add(item);

        item.RowContainer.style.backgroundColor = rowColor;
    }

    private static void OnUnbindHierarchyViewItem(
        HierarchyWindow window,
        HierarchyView view,
        HierarchyViewItem item)
    {
        BoundItems.Remove(item);
    }

    private static void RefreshBoundItems()
    {
        for (int i = BoundItems.Count - 1; i >= 0; i--)
        {
            HierarchyViewItem item = BoundItems[i];
            if (item == null)
            {
                BoundItems.RemoveAt(i);
                continue;
            }

            if (item.Handler is not HierarchyGameObjectHandler gameObjectHandler)
                continue;

            GameObject go = gameObjectHandler.GetGameObject(item.Node);
            Color rowColor = go != null && go.TryGetComponent(out HierarchyHighlight marker)
                ? marker.color
                : Color.clear;

            item.RowContainer.style.backgroundColor = rowColor;
        }
    }

    private static void OnHierarchyGUI(EntityId entityID, Rect selectionRect)
    {
        GameObject go = EditorUtility.EntityIdToObject(entityID) as GameObject;
        if (go == null) return;

        HierarchyHighlight marker = go.GetComponent<HierarchyHighlight>();

        // Draw subtle background + left accent bar if highlighted
        if (marker != null)
        {
            Color rowColor = marker.color;
            Rect fullRect = new Rect(0f, selectionRect.y, selectionRect.xMax + 60f, selectionRect.height - 1f);
            EditorGUI.DrawRect(fullRect, rowColor);

            Rect leftBarRect = new Rect(0f, selectionRect.y, LeftBarWidth, selectionRect.height - 1f);
            EditorGUI.DrawRect(leftBarRect, MakeSolid(rowColor));
        }

        // Small clickable swatch on the right
        Rect swatchRect = new Rect(
            selectionRect.xMax - SwatchSize - SwatchRightPadding,
            selectionRect.y + 2f,
            SwatchSize,
            selectionRect.height - 4f
        );

        DrawSwatch(go, marker, swatchRect);
    }

    private static void DrawSwatch(GameObject go, HierarchyHighlight marker, Rect swatchRect)
    {
        bool isHovered = swatchRect.Contains(Event.current.mousePosition);

        Color borderColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.18f)
            : new Color(0f, 0f, 0f, 0.20f);

        Color emptyColor = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.08f)
            : new Color(0f, 0f, 0f, 0.08f);

        Color fillColor = marker != null ? MakeSolid(marker.color) : emptyColor;

        if (isHovered)
        {
            swatchRect = new Rect(swatchRect.x - 1f, swatchRect.y - 1f, swatchRect.width + 2f, swatchRect.height + 2f);
        }

        EditorGUI.DrawRect(swatchRect, borderColor);

        Rect inner = new Rect(
            swatchRect.x + 1f,
            swatchRect.y + 1f,
            swatchRect.width - 2f,
            swatchRect.height - 2f
        );

        EditorGUI.DrawRect(inner, fillColor);

        if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
        {
            if (!IsInSelection(go))
                Selection.activeGameObject = go;

            Rect screenRect = GUIUtility.GUIToScreenRect(swatchRect);
            PopupWindow.Show(screenRect, new HierarchyHighlightPopup());
            Event.current.Use();
        }
    }

    private static bool IsInSelection(GameObject go)
    {
        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == go)
                return true;
        }
        return false;
    }

    public static void ApplyColorToSelection(Color color)
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            HierarchyHighlight marker = go.GetComponent<HierarchyHighlight>();
            if (marker == null)
                marker = Undo.AddComponent<HierarchyHighlight>(go);

            marker.color = color;
            EditorUtility.SetDirty(marker);
        }

        RefreshBoundItems();
        EditorApplication.RepaintHierarchyWindow();
    }

    public static void ClearHighlightSelection()
    {
        foreach (GameObject go in Selection.gameObjects)
        {
            HierarchyHighlight marker = go.GetComponent<HierarchyHighlight>();
            if (marker != null)
                Undo.DestroyObjectImmediate(marker);
        }

        RefreshBoundItems();
        EditorApplication.RepaintHierarchyWindow();
    }

    private static Color MakeSolid(Color c)
    {
        return new Color(c.r, c.g, c.b, 0.95f);
    }
}

public class HierarchyHighlightPopup : PopupWindowContent
{
    internal struct Preset
    {
        public string label;
        public Color color;

        public Preset(string label, Color color)
        {
            this.label = label;
            this.color = color;
        }
    }

    internal static readonly Preset[] Presets =
    {
        new Preset("WIP",       new Color(1.00f, 0.90f, 0.20f, 0.18f)), // yellow
        new Preset("Edited",    new Color(1.00f, 0.65f, 0.20f, 0.18f)), // orange
        new Preset("Fix",       new Color(1.00f, 0.40f, 0.40f, 0.18f)), // red
        new Preset("Note",      new Color(1.00f, 0.45f, 0.75f, 0.18f)), // pink
        new Preset("Review",    new Color(0.72f, 0.50f, 1.00f, 0.18f)), // purple

        new Preset("Important", new Color(0.35f, 0.65f, 1.00f, 0.18f)), // blue
        new Preset("FX",        new Color(0.30f, 1.00f, 1.00f, 0.18f)), // cyan
        new Preset("Done",      new Color(0.35f, 1.00f, 0.50f, 0.18f)), // green
        new Preset("Gameplay",  new Color(0.75f, 1.00f, 0.30f, 0.18f)), // lime
        new Preset("Ignore",    new Color(0.65f, 0.65f, 0.65f, 0.18f)), // gray
    };

    private const int Columns = 5;
    private const float SwatchSize = 24f;
    private const float Padding = 8f;
    private const float Gap = 6f;
    private const float FooterHeight = 24f;
    private const float ButtonHeight = 22f;

    private string hoveredLabel = "Hover a color";

    public override Vector2 GetWindowSize()
    {
        int rows = Mathf.CeilToInt(Presets.Length / (float)Columns);
        float width = Padding * 2f + Columns * SwatchSize + (Columns - 1) * Gap;
        float height = Padding * 2f
                     + rows * SwatchSize
                     + (rows - 1) * Gap
                     + 8f
                     + FooterHeight
                     + 8f
                     + ButtonHeight;

        return new Vector2(width, height);
    }

    public override void OnGUI(Rect rect)
    {
        hoveredLabel = "Hover a color";

        DrawPalette();

        GUILayout.Space(8);

        Rect footerRect = GUILayoutUtility.GetRect(10f, FooterHeight, GUILayout.ExpandWidth(true));
        DrawFooter(footerRect, hoveredLabel);

        GUILayout.Space(8);

        if (GUILayout.Button("Clear Highlight", GUILayout.Height(ButtonHeight)))
        {
            HierarchyHighlightDrawer.ClearHighlightSelection();
            editorWindow.Close();
        }

        if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.Repaint)
            editorWindow.Repaint();
    }

    private void DrawPalette()
    {
        int index = 0;
        int rows = Mathf.CeilToInt(Presets.Length / (float)Columns);

        GUILayout.Space(Padding);

        for (int row = 0; row < rows; row++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(Padding);

            for (int col = 0; col < Columns; col++)
            {
                if (index < Presets.Length)
                {
                    DrawColorButton(Presets[index]);
                    index++;
                }
                else
                {
                    GUILayout.Space(SwatchSize);
                }

                if (col < Columns - 1)
                    GUILayout.Space(Gap);
            }

            GUILayout.Space(Padding);
            GUILayout.EndHorizontal();

            if (row < rows - 1)
                GUILayout.Space(Gap);
        }
    }

    private void DrawColorButton(Preset preset)
    {
        Rect rect = GUILayoutUtility.GetRect(
            SwatchSize, SwatchSize,
            GUILayout.Width(SwatchSize),
            GUILayout.Height(SwatchSize)
        );

        bool hovered = rect.Contains(Event.current.mousePosition);

        if (hovered)
            hoveredLabel = preset.label;

        Color border = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, hovered ? 0.35f : 0.20f)
            : new Color(0f, 0f, 0f, hovered ? 0.35f : 0.20f);

        EditorGUI.DrawRect(rect, border);

        Rect inner = new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f);
        EditorGUI.DrawRect(inner, new Color(preset.color.r, preset.color.g, preset.color.b, 0.95f));

        if (hovered)
        {
            Rect outline = new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f);

            Handles.BeginGUI();
            Handles.color = new Color(1f, 1f, 1f, 0.35f);
            Handles.DrawAAPolyLine(
                2f,
                new Vector3(outline.x, outline.y),
                new Vector3(outline.xMax, outline.y),
                new Vector3(outline.xMax, outline.yMax),
                new Vector3(outline.x, outline.yMax),
                new Vector3(outline.x, outline.y)
            );
            Handles.EndGUI();
        }

        if (GUI.Button(rect, new GUIContent("", preset.label), GUIStyle.none))
        {
            HierarchyHighlightDrawer.ApplyColorToSelection(preset.color);
            editorWindow.Close();
        }
    }

    private void DrawFooter(Rect rect, string text)
    {
        Color bg = EditorGUIUtility.isProSkin
            ? new Color(1f, 1f, 1f, 0.06f)
            : new Color(0f, 0f, 0f, 0.05f);

        EditorGUI.DrawRect(rect, bg);

        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        EditorGUI.LabelField(rect, text, style);
    }
}

public class HierarchyHighlightWindow : EditorWindow
{
    [MenuItem("Window/Hierarchy Highlights")]
    private static void Open()
    {
        GetWindow<HierarchyHighlightWindow>("Hierarchy Highlights");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Apply a highlight preset to the current Hierarchy selection.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(8f);

        foreach (HierarchyHighlightPopup.Preset preset in HierarchyHighlightPopup.Presets)
        {
            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = new Color(preset.color.r, preset.color.g, preset.color.b, 0.95f);

            if (GUILayout.Button(preset.label, GUILayout.Height(24f)))
                HierarchyHighlightDrawer.ApplyColorToSelection(preset.color);

            GUI.backgroundColor = previousBackground;
        }

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Clear Highlight", GUILayout.Height(24f)))
            HierarchyHighlightDrawer.ClearHighlightSelection();
    }
}
