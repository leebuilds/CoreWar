using UnityEngine;

/// <summary>
/// Shared sizing for the decks collection screen.
/// </summary>
public static class DecksLayout
{
    public static readonly Vector2 WindowSize = new Vector2(1560f, 880f);

    public const float RowHeight = 200f;
    public const float RowSpacing = 16f;
    public const float ColumnSpacing = 10f;
    public const float HorizontalPadding = 8f;

    /// <summary>
    /// Usable width inside the scroll content (window body minus chrome and padding).
    /// </summary>
    public static float ContentRowWidth =>
        WindowSize.x
        - 2f * (MenuUiFactory.WindowBorderWidth + MenuUiFactory.ContentPadding)
        - 2f * HorizontalPadding;

    /// <summary>
    /// Left column: roughly one third of the row.
    /// </summary>
    public static float SpecialtyWidth => ContentRowWidth / 3f;

    /// <summary>
    /// Each tier card: remaining two thirds split evenly across three cards.
    /// </summary>
    public static float CardWidth =>
        (ContentRowWidth - SpecialtyWidth - (3f * ColumnSpacing)) / 3f;

    public static Vector2 CardSize => new Vector2(CardWidth, RowHeight);

    public static Vector2 SpecialtySize => new Vector2(SpecialtyWidth, RowHeight);
}
