using System;

namespace MailDaemon.Lib.Report
{
    public class HtmlReport : IReport, IReportStorage
    {
        public string Generate()
        {
            throw new NotImplementedException();
        }

        public void Save(ReportInfo reportInfo)
        {
            throw new NotImplementedException();
        }
    }
}
