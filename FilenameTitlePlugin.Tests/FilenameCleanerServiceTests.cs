using Jellyfin.Plugin.FilenameTitlePlugin;
using Xunit;

namespace FilenameTitlePlugin.Tests;

public class FilenameCleanerServiceTests
{
    private readonly FilenameCleanerService _sut = new();

    [Theory]
    [InlineData("The.Dark.Knight.mkv", "The Dark Knight")]
    [InlineData("Breaking_Bad_S01E01.mp4", "Breaking Bad S01E01")]
    [InlineData("Movie.2024.1080p.BluRay.x264.mkv", "Movie 2024")]
    [InlineData("www.EzTvX.to_The.Dark.Knight.2008.1080p.BluRay.x264.mkv", "The Dark Knight 2008")]
    [InlineData("The.Office.S03E05.720p.WEB-DL.AAC.x264.mp4", "The Office S03E05")]
    [InlineData("Inception.2010.4K.UHD.BluRay.HEVC.TrueHD.Atmos.mkv", "Inception 2010")]
    [InlineData("The.Dark.Knight", "The Dark Knight")]
    [InlineData("Movie.2024.1080p.BluRay.XviD.avi", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DivX.avi", "Movie 2024")]
    [InlineData("Movie.2024.2160p.BluRay.AV1.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.VP9.webm", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.Opus.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.FLAC.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.EAC3.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DDP.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DD.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DD5.1.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DTS-HD.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DTSHD.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DTS-X.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.LPCM.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.PCM.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264.10bit.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264.8bit.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264.Hi10P.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.REMUX.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.HDLight.mkv", "Movie 2024")]
    [InlineData("Movie.1972.1080p.BluRay.REMASTERED.mkv", "Movie 1972")]
    [InlineData("Movie.1972.1080p.BluRay.REMASTER.mkv", "Movie 1972")]
    [InlineData("Movie.2024.1080p.BluRay.RESTORED.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.UNCUT.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.LIMITED.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.IMAX.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.MULTi.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DUAL.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.VOSTFR.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.FRENCH.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.GERMAN.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.NORDiC.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.SUBBED.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.DUBBED.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.LATIN.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.JAPANESE.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-YIFY.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-RARBG.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-NTb.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-FLUX.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-ION10.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-EDITH.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-AMIABLE.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-EVO.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-PSA.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-GALAXY.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-MONKEE.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-DEFLATE.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-FoV.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-WAFFLES.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-SiRiUs.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-ETHEL.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-DUST.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-AIDA.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-ROVERS.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-NOSCREENS.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-SbR.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.x264-NOGRP.mkv", "Movie 2024")]
    [InlineData("torrentleech.org_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
    [InlineData("www.spa-tracker.be_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
    [InlineData("privatehd.to_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
    [InlineData("rargb.to_Movie.2024.1080p.BluRay.mkv", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.iso", "Movie 2024")]
    [InlineData("Movie.2024.1080p.BluRay.img", "Movie 2024")]
    public void Clean_ReturnsExpectedTitle(string input, string expected)
    {
        Assert.Equal(expected, _sut.Clean(input));
    }

    [Fact]
    public void Clean_ExtensionOnly_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Clean(".mkv"));
    }

    [Fact]
    public void Clean_YearInBrackets_UnwrappedNotRemoved()
    {
        Assert.Equal("Interstellar 2014", _sut.Clean("Interstellar.(2014).mkv"));
    }

    [Fact]
    public void Clean_YearInSquareBrackets_UnwrappedNotRemoved()
    {
        Assert.Equal("Dune 2021", _sut.Clean("Dune.[2021].2160p.mkv"));
    }

    [Fact]
    public void Clean_MultipleSpacesCollapsed()
    {
        Assert.Equal("Some Movie", _sut.Clean("Some...Movie.mkv"));
    }

    [Fact]
    public void Clean_RealEnglishWordsInTitle_NotStripped()
    {
        Assert.Equal("The King 2022", _sut.Clean("The.King.2022.1080p.BluRay.x264.mkv"));
    }
}
