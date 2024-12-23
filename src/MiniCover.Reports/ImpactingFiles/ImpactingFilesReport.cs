using System.Collections.Generic;
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
        
        public IEnumerable<string> GetImpactingFiles(InstrumentationResult result, string hitsDirectory)
        {
            var hitsInfo = _hitsReader.TryReadFromDirectory(hitsDirectory);
            var files = result.GetSourceFiles();
            var impactedFiles = _summaryFactory.GetSummaryGrid(files, hitsInfo, 90)
                .Where(row => row.File && row.Summary.CoveredBranches > 0)
                .SelectMany(row => row.SourceFiles);

            return impactedFiles.Select(file => file.Path);
        }

        public int Execute(InstrumentationResult result, string hitsDirectory)
        {
            var impactingFiles = GetImpactingFiles(result, hitsDirectory);

            foreach (var file in impactingFiles)
            {
                System.Console.WriteLine(file);
            }

            return 0;
        }
    }
}
