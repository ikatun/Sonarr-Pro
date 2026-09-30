using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.TrackedDownloads;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Queue;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.IndexerSearchTests
{
    [TestFixture]
    public class SeriesSearchServiceFixture : CoreTest<SeriesSearchService>
    {
        private Series _series;

        [SetUp]
        public void Setup()
        {
            _series = new Series
                      {
                          Id = 1,
                          Title = "Title",
                          Seasons = new List<Season>(),
                          QualityProfile = new LazyLoaded<QualityProfile>(Builder<QualityProfile>.CreateNew().With(q => q.UpgradeAllowed = true).Build())
                      };

            Mocker.GetMock<IQueueService>().Setup(s => s.GetQueue()).Returns(new List<Queue.Queue>());
            Mocker.GetMock<IEpisodeService>().Setup(s => s.GetEpisodeBySeries(It.IsAny<int>())).Returns(new List<Episode>());

            Mocker.GetMock<ISeriesService>()
                  .Setup(s => s.GetSeries(It.IsAny<int>()))
                  .Returns(_series);

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), It.IsAny<bool>(), true, true, false))
                  .ReturnsAsync(new List<DownloadDecision>());

            Mocker.GetMock<IProcessDownloadDecisions>()
                  .Setup(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                  .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>()));
        }

        [Test]
        public void should_only_include_monitored_seasons()
        {
            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 0, Monitored = false },
                                  new Season { SeasonNumber = 1, Monitored = true }
                              };

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false), Times.Exactly(_series.Seasons.Count(s => s.Monitored)));
        }

        [Test]
        public void should_only_search_missing_if_profile_does_not_allow_upgrades()
        {
            _series.Seasons = new List<Season>
            {
                new Season { SeasonNumber = 0, Monitored = false },
                new Season { SeasonNumber = 1, Monitored = true }
            };

            _series.QualityProfile.Value.UpgradeAllowed = false;

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>()
                .Verify(v => v.SeasonSearch(_series.Id, It.IsAny<int>(), true, true, true, false), Times.Exactly(_series.Seasons.Count(s => s.Monitored)));
        }

        [Test]
        public void should_start_with_lower_seasons_first()
        {
            var seasonOrder = new List<int>();

            _series.Seasons = new List<Season>
                              {
                                  new Season { SeasonNumber = 3, Monitored = true },
                                  new Season { SeasonNumber = 1, Monitored = true },
                                  new Season { SeasonNumber = 2, Monitored = true }
                              };

            Mocker.GetMock<ISearchForReleases>()
                  .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false))
                  .ReturnsAsync(new List<DownloadDecision>())
                  .Callback<int, int, bool, bool, bool, bool>((seriesId, seasonNumber, missingOnly, monitoredOnly, userInvokedSearch, interactiveSearch) => seasonOrder.Add(seasonNumber));

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            seasonOrder.First().Should().Be(_series.Seasons.OrderBy(s => s.SeasonNumber).First().SeasonNumber);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void should_carry_successful_pack_coverage_across_seasons(bool grabbed)
        {
            _series.Seasons = Enumerable.Range(1, 3).Select(n => new Season { SeasonNumber = n, Monitored = true }).ToList();
            var episodes = Enumerable.Range(1, 3).Select(n => new Episode { Id = n, SeriesId = _series.Id, SeasonNumber = n, Monitored = true }).ToList();
            Mocker.GetMock<IEpisodeService>().Setup(s => s.GetEpisodeBySeries(_series.Id)).Returns(episodes);
            var pack = new DownloadDecision(new RemoteEpisode { Series = _series, Episodes = episodes.Take(2).ToList() });
            var searches = new List<int>();
            Mocker.GetMock<ISearchForReleases>()
                .Setup(s => s.SeasonSearch(_series.Id, It.IsAny<int>(), false, true, true, false))
                .Callback<int, int, bool, bool, bool, bool>((id, season, missing, monitored, user, interactive) => searches.Add(season))
                .ReturnsAsync(new List<DownloadDecision>());
            Mocker.GetMock<IProcessDownloadDecisions>()
                .SetupSequence(s => s.ProcessDecisions(It.IsAny<List<DownloadDecision>>()))
                .ReturnsAsync(new ProcessedDecisions(grabbed ? new List<DownloadDecision> { pack } : new List<DownloadDecision>(), new List<DownloadDecision> { pack }, new List<DownloadDecision>()))
                .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>()))
                .ReturnsAsync(new ProcessedDecisions(new List<DownloadDecision>(), new List<DownloadDecision>(), new List<DownloadDecision>()));

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            searches.Should().Equal(grabbed ? new[] { 1, 3 } : new[] { 1, 2, 3 });
        }

        [TestCase(false, TrackedDownloadState.Downloading, 0)]
        [TestCase(true, TrackedDownloadState.Downloading, 1)]
        [TestCase(false, TrackedDownloadState.FailedPending, 1)]
        [TestCase(false, TrackedDownloadState.Imported, 1)]
        public void should_skip_only_fully_covered_seasons(bool partial, TrackedDownloadState state, int expectedSearches)
        {
            _series.Seasons = new List<Season> { new Season { SeasonNumber = 2, Monitored = true } };
            var episodes = new List<Episode>
            {
                new Episode { Id = 1, SeasonNumber = 1, Monitored = true },
                new Episode { Id = 2, SeasonNumber = 2, Monitored = true, EpisodeFileId = state == TrackedDownloadState.Imported ? 42 : 0 },
                new Episode { Id = 3, SeasonNumber = 2, Monitored = true, EpisodeFileId = state == TrackedDownloadState.Imported ? 43 : 0 }
            };
            Mocker.GetMock<IEpisodeService>().Setup(s => s.GetEpisodeBySeries(_series.Id)).Returns(episodes);
            Mocker.GetMock<IQueueService>().Setup(s => s.GetQueue()).Returns(new List<Queue.Queue>
            {
                new Queue.Queue
                {
                    TrackedDownloadState = state,
                    RemoteEpisode = new RemoteEpisode
                    {
                        Series = _series,
                        Episodes = partial ? episodes.Take(2).ToList() : episodes,
                        ParsedEpisodeInfo = new ParsedEpisodeInfo { FullSeason = true }
                    }
                }
            });

            Subject.Execute(new SeriesSearchCommand { SeriesId = _series.Id, Trigger = CommandTrigger.Manual });

            Mocker.GetMock<ISearchForReleases>().Verify(s => s.SeasonSearch(_series.Id, 2, false, true, true, false), Times.Exactly(expectedSearches));
        }
    }
}
