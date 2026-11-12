namespace D2ApiCreator.Application.DTOs;

/// <summary>Rodzaje formantów (content controls) rozpoznawane w szablonie DOCX.</summary>
public static class ContentControlKinds
{
    /// <summary>Fragment wariantowy — pokazywany lub usuwany zależnie od warunku (worker „Stan” / „Usuń obszar”).</summary>
    public const string Variant = "variant";

    /// <summary>Fragment powielany dla każdego elementu listy z modelu danych (sekcja powtarzana Worda albo konwencja nazwy).</summary>
    public const string Repeat = "repeat";

    /// <summary>Fragment będący listą numerowaną lub punktowaną (akapity z numeracją).</summary>
    public const string List = "list";

    /// <summary>Tabela (formant obejmuje tabelę albo jest formantem wiersza / komórki).</summary>
    public const string Table = "table";

    /// <summary>Typ wartości w schemacie danych dla danego rodzaju: wariant → <c>boolean</c>, reszta → <c>array</c>.</summary>
    public static string SchemaType(string kind) => kind == Variant ? "boolean" : "array";
}

/// <summary>Poziom osadzenia formantu w dokumencie.</summary>
public static class ContentControlLevels
{
    public const string Block = "block";
    public const string Inline = "inline";
    public const string Row = "row";
    public const string Cell = "cell";
}

/// <summary>
/// Szczegóły jednego formantu tekstowego znalezionego w szablonie — uzupełnienie słownika
/// <see cref="DocumentPlaceholderDto.ContentControls"/> (który zostaje dla zgodności wstecz).
/// </summary>
public class ContentControlDetailDto
{
    /// <summary>Identyfikator: Tag → Alias → ID (jak klucz w <c>ContentControls</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary><see cref="ContentControlKinds"/>: <c>variant</c> | <c>repeat</c> | <c>list</c> | <c>table</c>.</summary>
    public string Kind { get; set; } = ContentControlKinds.Variant;

    /// <summary>Dla list: <c>bullet</c> (od kropki) albo <c>numbered</c>; w pozostałych przypadkach null.</summary>
    public string? ListType { get; set; }

    /// <summary><c>MainDocument</c> | <c>Header</c> | <c>Footer</c>.</summary>
    public string Location { get; set; } = "MainDocument";

    /// <summary><see cref="ContentControlLevels"/>: <c>block</c> | <c>inline</c> | <c>row</c> | <c>cell</c>.</summary>
    public string Level { get; set; } = ContentControlLevels.Block;

    /// <summary>Początek tekstu formantu (do 80 znaków) — żeby projektant rozpoznał fragment.</summary>
    public string? Preview { get; set; }

    public int ParagraphCount { get; set; }
    public bool ContainsTable { get; set; }
    public bool ContainsList { get; set; }

    /// <summary>Sekcja powtarzana Worda (w15:repeatingSection) albo nazwa w konwencji powtarzania (<c>[]</c>, <c>repeat:</c>, <c>lista:</c>).</summary>
    public bool IsRepeating { get; set; }

    /// <summary>Nazwa formantu nadrzędnego, gdy ten jest zagnieżdżony (np. wariant wewnątrz sekcji powtarzanej).</summary>
    public string? Parent { get; set; }

    /// <summary>Typ workera proponowany dla tego rodzaju (nazwy jak w <c>worker_profiles.key</c>).</summary>
    public string SuggestedWorkerType { get; set; } = "Stan";

    /// <summary>Alias (tytuł) formantu z Worda, jeśli inny niż identyfikator.</summary>
    public string? Alias { get; set; }

    // --- Szablony XLSX (null dla DOCX) ---------------------------------------------------------

    /// <summary>Nazwa arkusza, w którym jest wiersz powielany.</summary>
    public string? Sheet { get; set; }

    /// <summary>Zakres wiersza powielanego, np. <c>A5:C5</c>.</summary>
    public string? Range { get; set; }

    /// <summary>Teksty z wiersza nagłówka nad wierszem powielanym (wg kolumn), jeśli jest.</summary>
    public List<string>? HeaderCells { get; set; }

    /// <summary>Kolumny wiersza powielanego: komórka → znacznik → właściwość elementu listy.</summary>
    public List<TemplateColumnDto>? Columns { get; set; }
}

/// <summary>Kolumna wiersza powielanego w arkuszu.</summary>
public class TemplateColumnDto
{
    /// <summary>Adres komórki w szablonie, np. <c>B5</c>.</summary>
    public string Cell { get; set; } = string.Empty;

    /// <summary>Znacznik w komórce, np. <c>&lt;%pozycje_kwota%&gt;</c>.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>
    /// Właściwość elementu listy, na którą mapuje się komórka (np. <c>kwota</c> dla listy obiektów);
    /// <c>null</c>, gdy wiersz ma jedną kolumnę i lista jest listą wartości prostych.
    /// </summary>
    public string? Property { get; set; }

    /// <summary>Tekst nagłówka nad kolumną, jeśli jest.</summary>
    public string? Header { get; set; }
}

/// <summary>Położenie luźnego znacznika w arkuszu (XLSX) — uzupełnienie listy <c>Placeholders</c>.</summary>
public class PlaceholderDetailDto
{
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Etykieta: tekst z najbliższej komórki po lewej (albo nad), np. „Koszt”.</summary>
    public string? Label { get; set; }

    public string? Sheet { get; set; }

    public string? Cell { get; set; }
}
