using WinCleaner.Core;

namespace WinCleaner.Core.Tests;

public class SectionTests
{
    [Fact]
    public void Sidebar_has_six_sections_in_order()
    {
        Assert.Equal(
            ["overview", "system", "dev", "large", "process", "fan"],
            Section.All.Select(s => s.Id));
    }
}
