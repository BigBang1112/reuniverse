using Microsoft.AspNetCore.Components;

namespace Reuniverse.Razor.Tests;

public class ReuMetadataTagsTests : BunitContext
{
    private IRenderedComponent<ReuMetadataTags> RenderTags(Action<ComponentParameterCollectionBuilder<ReuMetadataTags>>? configure = null)
    {
        return Render<ReuMetadataTags>(parameters =>
        {
            parameters.Add(x => x.Title, "Page title");
            configure?.Invoke(parameters);
        });
    }

    [Test]
    public async Task Render_WithTitle_WritesOpenGraphTitle()
    {
        // Act
        var cut = RenderTags();

        // Assert
        await Assert.That(cut.Find("meta[property='og:title']").GetAttribute("content")).IsEqualTo("Page title");
    }

    [Test]
    public async Task Render_WithDescription_WritesDescriptionTags()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.Description, "About the page"));

        // Assert
        await Assert.That(cut.Find("meta[name='description']").GetAttribute("content")).IsEqualTo("About the page");
        await Assert.That(cut.Find("meta[property='og:description']").GetAttribute("content")).IsEqualTo("About the page");
    }

    [Test]
    public async Task Render_WithoutDescription_LeavesDescriptionOut()
    {
        // Act
        var cut = RenderTags();

        // Assert
        await Assert.That(cut.FindAll("meta[name='description']").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithoutUrl_UsesCurrentUri()
    {
        // Act
        var cut = RenderTags();

        // Assert
        await Assert.That(cut.Find("meta[property='og:url']").GetAttribute("content")).IsEqualTo("http://localhost/");
    }

    [Test]
    public async Task Render_WithUrl_UsesUrlForCanonical()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.Url, "https://example.com/page"));

        // Assert
        await Assert.That(cut.Find("link[rel='canonical']").GetAttribute("href")).IsEqualTo("https://example.com/page");
    }

    [Test]
    public async Task Render_WithCanonicalUrl_PrefersCanonicalUrl()
    {
        // Act
        var cut = RenderTags(parameters => parameters
            .Add(x => x.Url, "https://example.com/page")
            .Add(x => x.CanonicalUrl, "https://example.com/canonical"));

        // Assert
        await Assert.That(cut.Find("link[rel='canonical']").GetAttribute("href")).IsEqualTo("https://example.com/canonical");
    }

    [Test]
    public async Task Render_WithCanonicalOff_LeavesCanonicalOut()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.IncludeCanonical, false));

        // Assert
        await Assert.That(cut.FindAll("link[rel='canonical']").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithImagePath_ResolvesAbsoluteUrl()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.ImagePath, "images/preview.png"));

        // Assert
        await Assert.That(cut.Find("meta[property='og:image']").GetAttribute("content")).IsEqualTo("http://localhost/images/preview.png");
    }

    [Test]
    public async Task Render_WithImageUrlAndPath_PrefersImageUrl()
    {
        // Act
        var cut = RenderTags(parameters => parameters
            .Add(x => x.ImageUrl, "https://cdn.example.com/preview.png")
            .Add(x => x.ImagePath, "images/preview.png"));

        // Assert
        await Assert.That(cut.Find("meta[property='og:image']").GetAttribute("content")).IsEqualTo("https://cdn.example.com/preview.png");
    }

    [Test]
    public async Task Render_WithoutImage_LeavesImageOut()
    {
        // Act
        var cut = RenderTags();

        // Assert
        await Assert.That(cut.FindAll("meta[property='og:image']").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithTwitterOff_LeavesTwitterOut()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.IncludeTwitter, false));

        // Assert
        await Assert.That(cut.FindAll("meta[name='twitter:card']").Count).IsEqualTo(0);
    }

    [Test]
    public async Task Render_WithSiteNameAndThemeColor_WritesTags()
    {
        // Act
        var cut = RenderTags(parameters => parameters.Add(x => x.SiteName, "Site").Add(x => x.ThemeColor, "#000"));

        // Assert
        await Assert.That(cut.Find("meta[property='og:site_name']").GetAttribute("content")).IsEqualTo("Site");
        await Assert.That(cut.Find("meta[name='theme-color']").GetAttribute("content")).IsEqualTo("#000");
    }
}
