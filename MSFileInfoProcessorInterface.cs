using MSFileInfoScannerInterfaces;
using PRISM;

// Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
// Started in 2005
//

namespace MSFileInfoScanner
{
    public abstract class iMSFileInfoProcessor : clsEventNotifier
    {
        public enum ProcessingOptions
        {
            CreateTICAndBPI = 0,
            ComputeOverallQualityScores = 1,
            CreateDatasetInfoFile = 2,
            CreateLCMS2DPlots = 3,
            CopyFileLocalOnReadError = 4,
            UpdateDatasetStatsTextFile = 5,
            CreateScanStatsFile = 6,
            CheckCentroidingStatus = 7,
            PlotWithPython = 8
        }

        public abstract bool ProcessDataFile(string strDataFilePath, clsDatasetFileInfo datasetFileInfo);
        public abstract bool CreateOutputFiles(string strInputFileName, string strOutputFolderPath);
        public abstract string GetDatasetInfoXML();
        public abstract string GetDatasetNameViaPath(string strDataFilePath);
        public abstract clsLCMSDataPlotterOptions LCMS2DPlotOptions { get; set; }
        public abstract int LCMS2DOverviewPlotDivisor { get; set; }
        public abstract string DatasetStatsTextFileName { get; set; }
        public abstract int DatasetID { get; set; }
        public abstract int ScanStart { get; set; }
        public abstract int ScanEnd { get; set; }
        public abstract bool ShowDebugInfo { get; set; }
        public abstract bool GetOption(ProcessingOptions eOption);
        public abstract void SetOption(ProcessingOptions eOption, bool blnValue);

    }
}

