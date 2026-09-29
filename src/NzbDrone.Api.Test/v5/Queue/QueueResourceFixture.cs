using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Tv;
using Sonarr.Api.V5.Queue;

namespace NzbDrone.Api.Test.v5.Queue
{
    [TestFixture]
    public class QueueResourceFixture
    {
        [Test]
        public void should_map_actual_episode_seasons_and_existing_files_without_episode_subresources()
        {
            var model = new NzbDrone.Core.Queue.Queue
            {
                SeasonNumber = 1,
                Episodes = new List<Episode>
                {
                    new Episode { Id = 21, SeasonNumber = 2 },
                    new Episode { Id = 11, SeasonNumber = 1, EpisodeFileId = 50 },
                    new Episode { Id = 51, SeasonNumber = 5, EpisodeFileId = 51 }
                }
            };

            var resource = model.ToResource(false, false);

            resource.SeasonNumbers.Should().Equal(1, 2, 5);
            resource.EpisodeIds.Should().BeEquivalentTo(new[] { 11, 21, 51 });
            resource.EpisodeIdsBySeason[1].Should().Equal(11);
            resource.EpisodeIdsBySeason[2].Should().Equal(21);
            resource.EpisodeIdsBySeason[5].Should().Equal(51);
            resource.EpisodeIdsWithFiles.Should().BeEquivalentTo(new[] { 11, 51 });
            resource.EpisodesWithFilesCount.Should().Be(2);
            resource.Episodes.Should().BeNull();
        }

        [Test]
        public void should_count_repeated_episode_associations_once()
        {
            var episode = new Episode { Id = 7, SeasonNumber = 0, EpisodeFileId = 70 };
            var model = new NzbDrone.Core.Queue.Queue { Episodes = new List<Episode> { episode, episode } };

            var resource = model.ToResource(false, false);

            resource.EpisodeIds.Should().Equal(7);
            resource.SeasonNumbers.Should().Equal(0);
            resource.EpisodeIdsBySeason[0].Should().Equal(7);
            resource.EpisodeIdsWithFiles.Should().Equal(7);
            resource.EpisodesWithFilesCount.Should().Be(1);
        }

        [Test]
        public void unmatched_queue_item_should_retain_known_season_without_inventing_episodes()
        {
            var resource = new NzbDrone.Core.Queue.Queue { SeasonNumber = 4 }.ToResource(false, false);

            resource.SeasonNumbers.Should().Equal(4);
            resource.EpisodeIds.Should().BeEmpty();
            resource.EpisodeIdsBySeason.Should().BeEmpty();
            resource.EpisodeIdsWithFiles.Should().BeEmpty();
        }
    }
}
