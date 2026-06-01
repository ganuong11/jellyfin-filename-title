using System.Text.RegularExpressions;

namespace Jellyfin.Plugin.FilenameTitlePlugin;

public class FilenameCleanerService
{
    private static readonly string[] QualityTags =
    [
        "2160p", "1080p", "1080i", "720p", "480p", "4K", "UHD",
        "BluRay", "BDRip", "BDRemux", "BRRip",
        "WEB-DL", "WEBRip", "WEBDL", "WEB",
        "HDTV", "DVDRip", "DVDScr", "DVD",
        "x264", "x265", "H264", "H265", "HEVC", "AVC",
        "XviD", "DivX", "AV1", "VP9",
        "10bit", "8bit", "Hi10P", "Hi10", "Hi444",
        "DTSHD", "DTS-HD", "DTSX", "DTS-X",
        "AAC", "AC3", "DTS", "MP3", "TrueHD", "Atmos",
        "Opus", "FLAC", "EAC3", "DDP", "DD", "DD5.1", "DD7.1", "LPCM", "PCM",
        "HDR", "HDR10", "SDR", "DV", "DoVi",
        "PROPER", "REPACK", "EXTENDED", "THEATRICAL", "UNRATED",
        "REMUX", "HDLight", "REMASTERED", "REMASTER", "RESTORED", "UNCUT", "LIMITED", "IMAX",
        "COMPLETE", "INTERNAL",
        "MULTi", "MULTiLANG", "DUAL", "DUALAUDIO", "SUBBED", "DUBBED", "DUB", "VOSTFR",
        "FRENCH", "GERMAN", "ITALIAN", "SPANISH", "LATIN", "JAPANESE", "KOREAN", "CHINESE",
        "RUSSIAN", "HINDI", "TAMIL", "TELUGU", "ARABIC", "PORTUGUESE", "BRAZILIAN",
        "POLISH", "TURKISH", "DUTCH", "NORDiC", "NORDIC",
        "YIFY", "YTS", "RARBG", "PSA", "NTb", "FLUX", "ION10", "EDITH", "AMIABLE",
        "DEFLATE", "GALAXY", "NOGRP", "MONKEE", "FoV", "WAFFLES", "MIRCrew", "SiRiUs",
        "Rets", "PublicHD", "EVO", "ETHEL", "DUST", "AIDA", "SHORTBRE", "GNOME", "GHOUL",
        "ROVERS", "SbR", "VETO", "NOSCREENS", "FLAR"
    ];

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".mkv", ".mp4", ".avi", ".mov", ".wmv", ".flv", ".m4v", ".ts", ".m2ts", ".webm", ".mpg", ".mpeg", ".iso", ".img" };

    private static readonly Regex SiteNameRegex = new(
        @"(?:www\.)?(?:[\w-]+\.)*[\w-]+\.(to|com|net|org|io|tv|me|ws|nu|de|nl|es|it|fr|pl|ru|se|no|dk|fi|is|cz|sk|hu|ro|bg|gr|tr|ua|jp|kr|cn|hk|tw|in|id|th|my|ph|vn|mx|br|ar|cl|co|pe|ve|uy|be)(?=[_.\s]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Strips surrounding brackets from years like (2024) or [2024] but keeps the year itself
    private static readonly Regex BracketedYearRegex = new(
        @"[\(\[]+((19|20)\d{2})[\)\]]+",
        RegexOptions.Compiled);

    private static readonly Regex ExtraSpacesRegex = new(
        @"\s{2,}",
        RegexOptions.Compiled);

    // Strips leading and trailing whitespace + punctuation (hyphen, period, underscore, slash) left over after tag stripping
    private static readonly Regex LeadingTrailingPunctRegex = new(
        @"^[\s\-._/]+|[\s\-._/]+$",
        RegexOptions.Compiled);

    public string Clean(string filename)
    {
        // Step 1: Strip extension only for known media formats; bare words like "Knight" must not be lost
        var ext = Path.GetExtension(filename);
        var name = VideoExtensions.Contains(ext)
            ? Path.GetFileNameWithoutExtension(filename)
            : Path.GetFileName(filename);

        // Step 2: Remove site name tokens (e.g. www.eztvx.to)
        name = SiteNameRegex.Replace(name, " ");

        // Step 3: Remove quality/codec tags
        foreach (var tag in QualityTags)
        {
            name = Regex.Replace(name, $@"\b{Regex.Escape(tag)}\b", " ", RegexOptions.IgnoreCase);
        }

        // Step 4: Replace dots and underscores with spaces
        name = name.Replace('.', ' ').Replace('_', ' ');

        // Step 5: Strip brackets around years — (2024) → 2024, [2024] → 2024
        name = BracketedYearRegex.Replace(name, "$1");

        // Step 6: Collapse whitespace
        name = ExtraSpacesRegex.Replace(name, " ");
        name = LeadingTrailingPunctRegex.Replace(name, string.Empty);

        return name;
    }
}
