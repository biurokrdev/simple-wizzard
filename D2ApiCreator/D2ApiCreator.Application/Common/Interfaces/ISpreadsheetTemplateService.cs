using D2ApiCreator.Application.DTOs;

namespace D2ApiCreator.Application.Common.Interfaces;

/// <summary>Wynik analizy szablonu XLSX — składany do tego samego <see cref="DocumentPlaceholderDto"/> co DOCX.</summary>
public class SpreadsheetTemplateAnalysis
{
    /// <summary>Luźne znaczniki <c>&lt;%…%&gt;</c> (komórka z etykietą obok) — jak placeholdery w DOCX („Podstaw wartość”).</summary>
    public List<string> Placeholders { get; set; } = new();

    /// <summary>Położenie i etykieta każdego luźnego znacznika (arkusz, komórka, tekst w komórce obok).</summary>
    public List<PlaceholderDetailDto> PlaceholderDetails { get; set; } = new();

    /// <summary>Wiersze powielane (same znaczniki w wierszu) z opcjonalnym nagłówkiem — jako formanty rodzaju <c>table</c>.</summary>
    public List<ContentControlDetailDto> Tables { get; set; } = new();
}

/// <summary>
/// Analiza szablonu arkusza (XLSX): luźne znaczniki jako „wstaw wartość”, wiersz ze stałym tekstem nad wierszem
/// znaczników jako nagłówek tabeli, wiersz z samymi znacznikami jako element powielany (kolumny → właściwości elementu).
/// </summary>
public interface ISpreadsheetTemplateService
{
    Task<SpreadsheetTemplateAnalysis> AnalyzeAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
}
