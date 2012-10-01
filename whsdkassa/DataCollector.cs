using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

using System.Diagnostics;

namespace whsdkassa
{
    class DataCollector
    {
        private String _appPath;
        private String _prevedPath;
        private String _oldFilesPath;

        private Dictionary<String, String> _iniSettings = new Dictionary<String, String>();

        public DataCollector(NameValueCollection settings) {
            this._appPath = AppDomain.CurrentDomain.BaseDirectory;
            this._prevedPath = settings["installPath"].TrimEnd('\\');
            this._oldFilesPath = settings["oldFilesPath"].TrimEnd('\\');
            if (!Path.IsPathRooted(this._oldFilesPath)) {
                this._oldFilesPath = this._prevedPath + Path.DirectorySeparatorChar + this._oldFilesPath;
                if (!Directory.Exists(this._oldFilesPath)) {
                    Directory.CreateDirectory(this._oldFilesPath);
                }
            }
            this.initIniSettings();
        }

        public void proceedUndoneData(DbInserter dbi) {
            IEnumerable<String> undoneFiles = this.getUndoneFiles();
            int c = undoneFiles.Count();
            if (c == 0) {
                Trace.TraceInformation("Nothing to process");
                return;
            } else {
                Trace.TraceInformation("Have to process " + c + " new file(s)");
            }
            foreach (String filePath in undoneFiles) {
                //parsing file
                Hashtable info = this.parseOneFile(filePath);
                if (info == null) {
                    Trace.TraceInformation("The file '" + filePath + "' was broken, skipping it");
                    continue;
                }
                //inserting info to database
                try
                {
                    dbi.insertPrevedData(info);
                } catch (System.Data.Common.DbException e) {
                    Trace.TraceError("Database quering exception '" + e.GetType() + "' for file '" + filePath + "' thrown, trace:\r\n" + e.StackTrace + "\r\n skipping file");
                    continue;
                }
                //setting file as done
                this.makeFileDone(filePath);
            }
        }

        private Hashtable parseOneFile(String filePath) {
            //for floating point values
            CultureInfo ci = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            //reading first line
            try
            {
                Hashtable mainData = new Hashtable();
                StreamReader sr = new StreamReader(filePath);
                String firstLine = sr.ReadLine();
                String[] mainDataArr = firstLine.Split(';');
                if (mainDataArr.Length != 6)
                {
                    String mess = "File '" + filePath + "' has wrong first line";
                    Trace.TraceError(mess);
                    throw new WHSDKassaException(mess);
                }
                Regex dtRegex = new Regex(@"^\d{14}$");
                MatchCollection dMatches = dtRegex.Matches(mainDataArr[2]);
                if (dMatches.Count == 0) {
                    throw new WHSDKassaException("String " + mainDataArr[2] + " is not date");
                }
                mainData.Add("operatorId", int.Parse(mainDataArr[1]));
                mainData.Add("dateTime", mainDataArr[2]);
                mainData.Add("clientCode", mainDataArr[3]);
                mainData.Add("envelope", int.Parse(mainDataArr[4]));
                mainData.Add("amount", double.Parse(mainDataArr[5], NumberStyles.Any, ci));
                //reading all lines except 1
                List<Hashtable> coinsData = new List<Hashtable>();
                //strings loop
                while (!sr.EndOfStream)
                {
                    String line = sr.ReadLine();
                    if (line.Length == 0) {
                        continue;
                    }
                    String[] coinsLineArr = line.Split(';');
                    if (coinsLineArr.Length != 4)
                    {
                        throw new WHSDKassaException("The line '" + line + "' in file " + filePath + " has wrong format");
                    }
                    Hashtable coinsLine = new Hashtable();
                    try
                    {
                        coinsLine.Add("coinFlag", int.Parse(coinsLineArr[0]));
                        coinsLine.Add("fNominal", double.Parse(coinsLineArr[1], NumberStyles.Any, ci));
                        coinsLine.Add("itemsCount", int.Parse(coinsLineArr[2]));
                        coinsLine.Add("totalSum", double.Parse(coinsLineArr[3], NumberStyles.Any, ci));
                        coinsData.Add(coinsLine);
                    }
                    catch (FormatException e)
                    {
                        throw new WHSDKassaException("The line '" + line + "' in file " + filePath + " has wrong format of one of arguments(numbers)");
                    }
                    catch (ArgumentNullException) {
                        throw new WHSDKassaException("The line '" + line + "' in file " + filePath + " has not enought parameters");
                    }
                }
                sr.Close();
                //returning result
                Hashtable result = new Hashtable();
                result.Add("mainData", mainData);
                result.Add("coinsData", coinsData);
                result.Add("filePath", filePath);
                return result;
            } 
            catch (WHSDKassaException e) {
                Trace.TraceError("Exception while parsing Preved file, Message: \r\n" + e.Message);
                return null;
            }
            catch (IOException e) {
                Trace.TraceError("IO Exception for file with path '" + filePath + "' thrown. Exception class is: " + e.GetType());
                return null;
            }
        }

        private void initIniSettings()
        {
            this.readIniSettings();
            this.formExportPath();            
        }

        private void readIniSettings() {
            //reading settings from preved .ini file
            String iniPath = this._prevedPath + "\\PreVedWSD.ini";
            try {
                using (StreamReader sr = new StreamReader(iniPath, Encoding.GetEncoding(1251)))
                {
                    while (!sr.EndOfStream)
                    {
                        String s = sr.ReadLine();
                        String[] strs = Regex.Split(s, @"\s+=\s+");
                        if ((strs.Length != 1) && (strs.Length != 2))
                        {
                            throw new Exception("wrong ini file format");
                        }
                        this._iniSettings.Add(strs[0], (strs.Length > 1) ? strs[1] : "");
                    }
                }
            } catch (FileNotFoundException e) {
                Trace.TraceError("Ini file with path '" + iniPath + "' cannot be opened");
                throw e;
            }
        }

        private void formExportPath() { 
            //formatting of ExportDir
            if (!this._iniSettings.ContainsKey("ExportDir")) {
                this._iniSettings.Add("ExportDir", "");
            }
            String exportPath = this._iniSettings["ExportDir"];
            if (exportPath == "") {
                exportPath = this._prevedPath;
            } else if (!Path.IsPathRooted(exportPath)) {
                exportPath = this._prevedPath + Path.DirectorySeparatorChar + exportPath;
            }
            this._iniSettings["ExportDir"] = exportPath.TrimEnd('\\');
            Trace.TraceInformation("Export directory set to " + this._iniSettings["ExportDir"]);
        }

        private IEnumerable<String> getUndoneFiles()
        {
            try {
                String[] allFiles = Directory.GetFiles(this._iniSettings["ExportDir"]);
                List<String> fileList = new List<String>(allFiles);
                int i = 0;
                fileList.RemoveAll(delegate(String filePath)
                {
                    String fileName = Path.GetFileName(filePath);
                    Regex rgx = new Regex(@"\d?_\d+_\d+\.dat$", RegexOptions.IgnoreCase);
                    MatchCollection matches = rgx.Matches(fileName);
                    return matches.Count == 0;
                });
                return fileList;
            } catch (DirectoryNotFoundException e) {
                Trace.TraceError("Directory with exported files does not exist by path " + this._iniSettings["ExportDir"]);
                throw e;
            }
        }

        private void makeFileDone(String srcPath) {
            String destPath = Path.Combine(this._oldFilesPath, Path.GetFileName(srcPath));
            File.Move(srcPath, destPath);
        }
    }
}
