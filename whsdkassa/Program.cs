using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Configuration;
using System.Collections;
using System.Collections.Specialized;

using System.Diagnostics;
using System.IO;

namespace whsdkassa
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.OutputEncoding = Encoding.GetEncoding(1251);
            
            //setting up log folder
            String appPath = AppDomain.CurrentDomain.BaseDirectory;
            foreach (TraceListener t in Trace.Listeners) {
                if (t is Essential.Diagnostics.RollingFileTraceListener) {
                    Essential.Diagnostics.RollingFileTraceListener e_t = (Essential.Diagnostics.RollingFileTraceListener)t;
                    String targetDirName = Path.GetDirectoryName(e_t.FilePathTemplate);
                    if (!Directory.Exists(targetDirName)) {
                        Directory.CreateDirectory(targetDirName);
                    }
                }
            }

            Trace.TraceInformation("Starting...");

            try
            {
                NameValueCollection prevedSection = (NameValueCollection)ConfigurationManager.GetSection("preved");
                NameValueCollection whsdSection = (NameValueCollection)ConfigurationManager.GetSection("whsd");
                //
                DataCollector dc = new DataCollector(prevedSection);
                DbInserter dbi = new DbInserter(whsdSection);
                dc.proceedUndoneData(dbi);
                dbi.disconnect();
                Trace.TraceInformation("...Task finished");
            } catch (Exception e) {
                Trace.TraceError("Critical error happened, exception '" + e.GetType() + "' thrown, trace: \r\n" + e.StackTrace);
            }
        }
    }
}
