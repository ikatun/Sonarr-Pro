using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.TvTests
{
    [TestFixture]
    public class SeriesScannedHandlerFixture : CoreTest<SeriesScannedHandler>
    {
        private Series GivenAddedSeries(MonitorTypes monitor)
        {
            // An Overseerr "all seasons" request: TMDb's season list, Specials included and monitored.
            return new Series
            {
                Id = 1,
                Title = "Nathan for You",
                Seasons = new List<Season>
                {
                    new Season { SeasonNumber = 0, Monitored = true },
                    new Season { SeasonNumber = 1, Monitored = true },
                    new Season { SeasonNumber = 2, Monitored = true }
                },
                AddOptions = new AddSeriesOptions { Monitor = monitor, SearchForMissingEpisodes = true }
            };
        }

        [Test]
        public void should_not_monitor_specials_when_added_without_a_monitor_choice()
        {
            var series = GivenAddedSeries(MonitorTypes.Unknown);
            List<Season> applied = null;
            Mocker.GetMock<IEpisodeMonitoredService>()
                .Setup(s => s.SetEpisodeMonitoredStatus(series, It.IsAny<MonitoringOptions>()))
                .Callback<Series, MonitoringOptions>((s, o) => applied = s.Seasons.Select(x => new Season { SeasonNumber = x.SeasonNumber, Monitored = x.Monitored }).ToList());

            Subject.Handle(new SeriesScannedEvent(series, new List<string>()));

            applied.Single(s => s.SeasonNumber == 0).Monitored.Should().BeFalse();
            applied.Where(s => s.SeasonNumber > 0).Should().OnlyContain(s => s.Monitored);
        }

        [TestCase(MonitorTypes.All)]
        [TestCase(MonitorTypes.Future)]
        public void should_keep_specials_as_chosen_with_an_explicit_monitor_choice(MonitorTypes monitor)
        {
            var series = GivenAddedSeries(monitor);

            Subject.Handle(new SeriesScannedEvent(series, new List<string>()));

            series.Seasons.Single(s => s.SeasonNumber == 0).Monitored.Should().BeTrue();
        }
    }
}
