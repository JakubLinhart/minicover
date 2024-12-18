using System.Threading.Tasks;
using MiniCover.CommandLine.Options;
using MiniCover.Reports.ImpactingFiles;

namespace MiniCover.CommandLine.Commands;

public sealed class ImpactingFilesReportCommand : ICommand
{
    private readonly ICoverageLoadedFileOption _coverageLoadedFileOption;
    private readonly HitsDirectoryOption _hitsDirectoryOption;
    private readonly IImpactingFilesReport _impactingFilesReport;

    public ImpactingFilesReportCommand(
        IWorkingDirectoryOption workingDirectoryOption,
        IImpactingFilesReport impactingFilesReport,
        ICoverageLoadedFileOption coverageLoadedFileOption,
        HitsDirectoryOption hitsDirectoryOption)
    {
        _impactingFilesReport = impactingFilesReport;
        _coverageLoadedFileOption = coverageLoadedFileOption;
        _hitsDirectoryOption = hitsDirectoryOption;
        Options = new IOption[] { workingDirectoryOption, coverageLoadedFileOption, hitsDirectoryOption };
    }

    public string CommandName => "impactingFilesReport";
    public string CommandDescription => "Outputs impacted files";
    public IOption[] Options { get; }

    public Task<int> Execute()
    {
        var result = _impactingFilesReport.Execute(_coverageLoadedFileOption.Result, _hitsDirectoryOption.DirectoryInfo.FullName);

        return Task.FromResult(result);
    }
}
