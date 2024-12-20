using System.Collections.Generic;
using MiniCover.Core.Model;

namespace MiniCover.Reports.ImpactingFiles
{
    public interface IImpactingFilesReport
    {
        int Execute(InstrumentationResult result, string hitsDirectory);

        IEnumerable<string> GetImpactingFiles(InstrumentationResult result, string hitsDirectory);
    }
}
