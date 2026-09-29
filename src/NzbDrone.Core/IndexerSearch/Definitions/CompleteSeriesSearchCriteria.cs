using System.Linq;

namespace NzbDrone.Core.IndexerSearch.Definitions
{
    public class CompleteSeriesSearchCriteria : SearchCriteriaBase
    {
        public string[] QueryTitles => CleanSceneTitles.Select(t => t.Replace('+', ' ')).ToArray();

        public override string ToString()
        {
            return $"[{Series.Title}] Complete series";
        }
    }
}
