using System.Linq;
using MiniCover.Core.Hits;
using MiniCover.Core.Model;
using MiniCover.Reports.Helpers;

namespace MiniCover.Reports.ImpactingFiles
{
    public sealed class ImpactingFilesReport : IImpactingFilesReport
    {
        private readonly IHitsReader _hitsReader;
        private readonly ISummaryFactory _summaryFactory;

        public ImpactingFilesReport(
            IHitsReader hitsReader,
            ISummaryFactory summaryFactory)
        {
            _hitsReader = hitsReader;
            _summaryFactory = summaryFactory;
        }

        public int Execute(InstrumentationResult result, string hitsDirectory)
        {
            var hitsInfo = _hitsReader.TryReadFromDirectory(hitsDirectory);
            var files = result.GetSourceFiles();
            var impactedFiles = _summaryFactory.GetSummaryGrid(files, hitsInfo, 90)
                .Where(row => row.File && row.Summary.CoveredBranches > 0)
                .SelectMany(row => row.SourceFiles);

            foreach (var file in impactedFiles)
            {
                System.Console.WriteLine(file.Path);
            }

            return 0;
        }
    }
}
