using Dashboard_v2.Application.Common.Interfaces;
using Dashboard_v2.Application.Publications;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Dashboard_v2.Application.UnitTests.Publications;

public class PublicationServiceCrossRefTests
{
    [Test]
    public async Task SearchCrossRefCandidatesAsyncReturnsEnrichedDoiResultWhenFound()
    {
        var crossRefClient = new Mock<ICrossRefClient>();
        crossRefClient
            .Setup(x => x.GetWorkByDoiAsync("10.1000/test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublicationCrossRefDto
            {
                Doi = "10.1000/test",
                Title = "A Paper",
                Type = "journal-article",
                ContainerTitle = "Journal",
                Authors = ["Ada Lovelace"],
            });

        var service = CreateService(crossRefClient);

        var result = await service.SearchCrossRefCandidatesAsync("10.1000/test", "ignored");

        result.Count.ShouldBe(1);
        result[0].SuggestedPublicationType.ShouldBe(0);
        var publicationData = result[0].PublicationData;
        publicationData.ShouldNotBeNull();
        publicationData.ShouldContain("Container: Journal");
        publicationData.ShouldContain("Authors: Ada Lovelace");
        crossRefClient.Verify(x => x.SearchWorksByTitleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SearchCrossRefCandidatesAsyncFallsBackToTitleWhenDoiHasNoMatch()
    {
        var crossRefClient = new Mock<ICrossRefClient>();
        crossRefClient
            .Setup(x => x.GetWorkByDoiAsync("10.1000/missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PublicationCrossRefDto?)null);
        crossRefClient
            .Setup(x => x.SearchWorksByTitleAsync("fallback title", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new PublicationCrossRefDto
                {
                    Title = "Fallback Result",
                    Type = "book",
                }
            ]);

        var service = CreateService(crossRefClient);

        var result = await service.SearchCrossRefCandidatesAsync("10.1000/missing", "fallback title");

        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("Fallback Result");
        result[0].SuggestedPublicationType.ShouldBe(1);
        crossRefClient.Verify(x => x.SearchWorksByTitleAsync("fallback title", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PublicationService CreateService(Mock<ICrossRefClient> crossRefClient)
    {
        return new PublicationService(
            new Mock<IApplicationDbContext>().Object,
            new Mock<IUser>().Object,
            crossRefClient.Object,
            new Mock<IOpenAireClient>().Object,
            new Mock<IAuthorResolutionService>().Object,
            new Mock<IAuthorCleanupService>().Object);
    }
}
