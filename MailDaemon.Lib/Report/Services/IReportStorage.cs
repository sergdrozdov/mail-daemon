using System;
using System.Collections.Generic;
using System.Text;

namespace MailDaemon.Lib.Report
{
    public interface IReportStorage
    {
        void Save(ReportInfo reportInfo);
    }
}
