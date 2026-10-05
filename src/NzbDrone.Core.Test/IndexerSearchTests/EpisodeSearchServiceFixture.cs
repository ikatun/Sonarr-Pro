using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class EpisodeSearchServiceFixture : CoreTest<EpisodeSearchService>
    {
        private const int SeriesId = 7;
        private List<Episode> _episodes;
        private List<string> _searches;

        [SetUp]
        public void Setup()
        {
            // Nathan for You after being added: two regular seasons plus a special, all missing.
            _episodes = new List<Episode>
            {
                Episode(1, 1), Episode(2, 1), Episode(3, 2), Episode(4, 2), Episode(5, 0)
            };
            _searches = new List<string>();

            Mocker.GetMock<IQueueService>().Setup(s => s.GetQueue()).Returns(new List<Queue.Queue>());
            Mocker.GetMock<IEpisodeService>().Setup(s => s.GetEpisodeBySeries(SeriesId)).Returns(() => _episodes);
            Mocker.GetMock<IEpisodeService>()
                .Setup(s => s.GetEpisodes(It.IsAny<IEnumerable<int>>()))
                .Returns<IEnumerable<int>>(ids => _episodes.Where(e => ids.Contains(e.Id)).ToList());

            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.CompleteSeriesSearch(SeriesId, It.IsAny<bool>(), false))
                .Callback(() => _searches.Add("complete"))
                .ReturnsAsync(new List<DownloadDecision>());
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.SeasonSearch(SeriesId, It.IsAny<int>(), It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), false))
                .Callback<int, int, List<Episode>, bool, bool, bool>((id, season, episodes, monitored, user, interactive) => _searches.Add("season " + season))
                .ReturnsAsync(new List<DownloadDecision>());
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.EpisodeSearch(It.IsAny<Episode>(), It.IsAny<bool>(), false))
                .Callback<Episode, bool, bool>((episode, user, interactive) => _searches.Add("episode " + episode.Id))
                .ReturnsAsync(new List<DownloadDecision>());
            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>()));
        }

        private static Episode Episode(int id, int season)
        {
            return new Episode { Id = id, SeriesId = SeriesId, SeasonNumber = season, Monitored = true, AirDateUtc = DateTime.UtcNow.AddDays(-30) };
        }

        [Test]
        public void should_try_complete_series_first_then_regular_seasons_then_specials()
        {
            Subject.Execute(new MissingEpisodeSearchCommand(SeriesId) { Monitored = true });

            _searches.Should().Equal("complete", "season 1", "season 2", "episode 5");
        }

        [Test]
        public void should_not_search_seasons_covered_by_a_grabbed_complete_series_pack()
        {
            var pack = new DownloadDecision(new RemoteEpisode { Episodes = _episodes.Where(e => e.SeasonNumber > 0).ToList() });
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.CompleteSeriesSearch(SeriesId, It.IsAny<bool>(), false))
                .Callback(() => _searches.Add("complete"))
                .ReturnsAsync(new List<DownloadDecision> { pack });
            Mocker.GetMock<IProcessDownloadDecisions>()
                .Setup(s => s.ProcessDecisions(It.Is<List<DownloadDecision>>(d => d.Contains(pack))))
                .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision> { pack }, new List<DownloadDecision>(), new List<DownloadDecision>()));

            Subject.Execute(new MissingEpisodeSearchCommand(SeriesId) { Monitored = true });

            _searches.Should().Equal("complete", "episode 5");
        }

        [Test]
        public void should_skip_episodes_imported_while_the_search_was_running()
        {
            // A manually grabbed pack imports season 2 while season 1 is being searched.
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.SeasonSearch(SeriesId, 1, It.IsAny<List<Episode>>(), It.IsAny<bool>(), It.IsAny<bool>(), false))
                .Callback(() =>
                {
                    _searches.Add("season 1");
                    _episodes = _episodes.Select(e => e.SeasonNumber == 2 ? new Episode { Id = e.Id, SeriesId = SeriesId, SeasonNumber = 2, Monitored = true, AirDateUtc = e.AirDateUtc, EpisodeFileId = 99 } : e).ToList();
                })
                .ReturnsAsync(new List<DownloadDecision>());

            Subject.Execute(new MissingEpisodeSearchCommand(SeriesId) { Monitored = true });

            _searches.Should().Equal("complete", "season 1", "episode 5");
        }
    }
}
