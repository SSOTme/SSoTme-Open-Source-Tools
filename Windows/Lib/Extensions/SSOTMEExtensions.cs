﻿/*****************************
Project:    SSoTme - Open Source Tools (OST)
Created By: EJ Alexandra - 2016
            An Abstract Level, llc
License:    Mozilla Public License 2.0
*****************************/
using HtmlAgilityPack;
using Newtonsoft.Json;
using SassyMQ.Lib.RabbitMQ;
using SSoTme.Lib.XsltHandling;
using SSoTme.OST.Lib.DataClasses;
using SSOTME.TestConApp.Root.TranspileHandlers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SSoTme.OST.Lib.Extensions
{
    public static class SSOTMEExtensions
    {
        public static void FindNS(this object anyObject)
        {
            // do nothing
        }

        public static FileSet ToFileSet(this string fileSetXml)
        {
            XmlSerializer ser = new XmlSerializer(typeof(FileSet));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(fileSetXml));
            return ser.Deserialize(ms) as FileSet;
        }

        public static String ToName(this string nameCandidate)
        {
            string name = nameCandidate.SafeToString();
            name = name.ToCamelString();
            return name;
        }

        public static string GetTranspilersAsJson(string screenName)
        {
            var fs = new FileSet();
            var fsf = fs.FileSetFiles.AddNew();
            fsf.RelativePath = String.Format("{0}-transpilers.json", screenName);
            fsf.AlwaysOverwrite = true;
            var wc = new WebClient();
            var myPublicTranspilersUrl = String.Format("http://www.ssot.me/Public/GetPublicAccounts?screenName={0}", screenName);
            var myTranspilersJson = wc.DownloadString(myPublicTranspilersUrl);
            fsf.FileContents = myTranspilersJson;
            using (var ms = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(FileSet));
                serializer.Serialize(ms, fs);
                ms.Position = 0;
                using (var sr = new StreamReader(ms))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        public static string ToTitle(this string name)
        {
            return name.ToCamelString().TitleFromCamel();
        }

        public static String ToXml(this FileSet fileSet)
        {
            var ser = new XmlSerializer(typeof(FileSet));
            var ms = new MemoryStream();
            ser.Serialize(ms, fileSet);
            ms.Position = 0;
            var fileSetXml = new StreamReader(ms).ReadToEnd();
            return fileSetXml;
        }


        public static void CleanZippedFileSet(this byte[] zippedFileSet)
        {
            zippedFileSet.UnzipToString().CleanFileSet();
        }

        public static void CleanFileSet(this string fileSetXml)
        {

            if (!String.IsNullOrEmpty(fileSetXml) && fileSetXml.Contains("<"))
            {
                fileSetXml = fileSetXml.Substring(fileSetXml.IndexOf("<"));
                fileSetXml = fileSetXml.Replace("FileContents><?xml ", "FileContents>&lt;?xml");
                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.LoadXml(fileSetXml);
                }
                catch { } // Ignore errors cleaning previous files up.  Sometimes this won't work.
                foreach (XmlElement fileSetFileElem in doc.SelectNodes("//FileSetFile"))
                {
                    // Delete the file if it matches the contents
                    XmlNode relPathElem = fileSetFileElem.SelectSingleNode("RelativePath");
                    if (!ReferenceEquals(relPathElem, null))
                    {
                        CleanFileByRelativeName(fileSetFileElem, relPathElem);
                    }
                }

            }
        }

        private static void CleanEmptyFolders()
        {
            CleanEmptyFolders(new DirectoryInfo("."));
        }

        private static void CleanEmptyFolders(DirectoryInfo di)
        {
            if (di.Exists)
            {
                foreach (DirectoryInfo diChildDir in di.GetDirectories())
                {
                    CleanEmptyFolders(diChildDir);
                }
                if (!di.GetDirectories().Any() && !di.GetFiles().Any()) di.Delete();
            }
        }

        private static void CleanFileByRelativeName(XmlElement fileSetFileElem, XmlNode relPathElem)
        {
            var skipElement = fileSetFileElem.SelectSingleNode(".//SkipClean");
            if (!ReferenceEquals(skipElement, null) && String.Equals(skipElement.InnerText, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            else
            {
                FileInfo fiToClean = new FileInfo(GetFullFileName(relPathElem.InnerText, new DirectoryInfo(".")));
                if (fiToClean.Exists)
                {
                    XmlNode fileContentsNode = fileSetFileElem.SelectSingleNode("FileContents");

                    String value = String.Empty;
                    if (!ReferenceEquals(fileContentsNode, null)) value = fileContentsNode.InnerXml;
                    if (File.ReadAllText(fiToClean.FullName) == HttpUtility.HtmlDecode(value))
                    {
                        Console.WriteLine("SSoTme Cleaning {0}", fiToClean.FullName);
                        fiToClean.Delete();
                    }
                }
            }
        }

        private static string GetFullFileName(string relativeFileName, DirectoryInfo rootDI)
        {
            if (rootDI == null) rootDI = new DirectoryInfo(".");
            relativeFileName = relativeFileName.SafeToString().Trim("\r\n\t \\/".ToCharArray());
            FileInfo fi = new FileInfo(Path.Combine(rootDI.FullName, relativeFileName));
            return fi.FullName;
        }

        public static String ConvertXsdToOdxmlFileSetXml(BaseHandler handler, string xsd)
        {
            // Convert file
            // Transpile a thing
            XsltHandler xsltHandler = new XsltHandler(handler);
            String fileName = "Odxml42TranspilerHost.XsltTools.XsdToODXML.xslt";
            var xslt = new StreamReader(handler.GetType().Assembly.GetManifestResourceStream(fileName)).ReadToEnd();
            xslt = xslt.Replace("<RelativePath>DataSchema.odxml</RelativePath>", String.Format("<RelativePath>{0}.odxml</RelativePath>", handler.Payload.CLIInput));
            xsltHandler.LoadXslt(xslt);
            xsltHandler.Xml = xsd;
            xsltHandler.Cook();
            return xsltHandler.FileSetXml;
        }

        public static FileSet ConvertXmlToXsdFileSet(string xml, String inputFileName)
        {
            XDocument srcDoc = XDocument.Parse(xml);

            XmlReader reader = XmlReader.Create(new StringReader(xml));
            XmlSchemaInference inference = new XmlSchemaInference();
            XmlSchemaSet schemaSet = inference.InferSchema(reader);

            // Display the inferred schema.
            var index = 1;
            FileSet fs = new FileSet();
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                MemoryStream sms = new MemoryStream();
                StreamWriter writer = new StreamWriter(sms);
                schema.Write(writer);
                String xsd = Encoding.Default.GetString(sms.GetBuffer(), 0, (int)sms.Length);

                inputFileName = Path.GetFileName(inputFileName);
                if (String.Equals(Path.GetExtension(inputFileName), ".xsd", StringComparison.OrdinalIgnoreCase))
                {
                    inputFileName = inputFileName.Substring(0, inputFileName.Length - 4);
                }

                var extension = ".xsd";
                if (index > 1) extension = index.ToString() + extension;
                index++;

                var dsFileName = String.Format("{0}{1}", inputFileName, extension);
                FileSetFile fsf = new FileSetFile();
                fsf.AlwaysOverwrite = true;
                fsf.RelativePath = dsFileName;
                fsf.FileContents = xsd;
                fs.FileSetFiles.Add(fsf);
            }

            return fs;
        }

        public static String LowerHyphenName(this string name)
        {
            return name.ToTitle().Replace(" ", "-").ToLower();
        }

        public static String StripParamNumber(this string fullParamString)
        {
            fullParamString = fullParamString.SafeToString();
            if (fullParamString.StartsWith("param") &&
                fullParamString.Substring(0, "paramNN=".Length).Contains("="))
            {
                fullParamString = fullParamString.Substring(fullParamString.IndexOf("=") + 1);
            }
            return fullParamString;
        }

        public static String ToSqlSafeString(this object theObject)
        {
            return theObject.SafeToString().Replace("'", "''");
        }
        public static String ToCamelString(this String titleString)
        {
            titleString = titleString.SafeToString().Replace("-", " ");
            var sb = new StringBuilder();
            var prevChar = ' ';
            foreach (char currentChar in titleString)
            {
                if (!Char.IsLetterOrDigit(currentChar))
                {
                    // Do nothing in this case
                }
                else if (prevChar == ' ') sb.Append(currentChar.SafeToString().ToUpper());
                else sb.Append(currentChar);

                prevChar = currentChar;
            }
            return sb.ToString().Replace(" ", "");
        }

        public static String TitleFromCamel(this String camelString)
        {
            camelString = String.Join("", camelString.SafeToString().ToList().Select(feChar => (Char.IsUpper(feChar) ? " " : "") + feChar.SafeToString()));
            return camelString.Trim();
        }

        public static string SHA256(this string password)
        {
            System.Security.Cryptography.SHA256Managed crypt = new System.Security.Cryptography.SHA256Managed();
            System.Text.StringBuilder hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password), 0, Encoding.UTF8.GetByteCount(password));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }




        public static List<XmlElement> FileSetFilesFromFileSetXml(this string fileSetXml)
        {
            var xdoc = new XmlDocument();
            xdoc.LoadXml(fileSetXml);
            return xdoc.DocumentElement
                       .SelectNodes("//FileSetFile")
                       .OfType<XmlElement>()
                       .ToList();
        }


        public static String GetFileContents(this XmlElement fileSetFileElement)
        {
            var fileContentsElement = fileSetFileElement.SelectSingleNode(".//FileContents");
            if (!ReferenceEquals(fileContentsElement, null))
            {
                var fileContents = fileContentsElement.InnerXml;
                return UnwrapCDATA(fileContents);
            }
            else
            {
                var zippedFileContentsElement = fileSetFileElement.SelectSingleNode(".//ZippedTextFileContents");
                if (!ReferenceEquals(zippedFileContentsElement, null))
                {
                    var fileContents = Convert.FromBase64String(zippedFileContentsElement.InnerXml).UnzipToString();
                    return fileContents;
                }
            }

            // If we get here - return an empty string
            return String.Empty;
        }

        public static String ToSingleTextFileFileSetXml(this string inputString, String fileName)
        {
            var xdoc = new XmlDocument();
            var fileSet = xdoc.CreateElement("FileSet");
            xdoc.AppendChild(fileSet);
            var fileSetFiles = xdoc.CreateElement("FileSetFiles");
            fileSet.AppendChild(fileSetFiles);
            var fileSetFile = xdoc.CreateElement("FileSetFile");
            fileSetFiles.AppendChild(fileSetFile);
            var relativePath = xdoc.CreateElement("RelativePath");
            relativePath.InnerText = fileName;
            fileSetFile.AppendChild(relativePath);
            var fileContents = xdoc.CreateElement("ZippedTextFileContents");
            fileContents.InnerXml = Convert.ToBase64String(inputString.Zip());
            fileSetFile.AppendChild(fileContents);
            return xdoc.OuterXml;
        }

        public static string CsvToXml(this String csvText, String fileName)
        {
            String tempFile = String.Format("temp\\{0}", fileName);
            FileInfo fi = new FileInfo(tempFile);
            if (!fi.Directory.Exists) fi.Directory.Create();
            File.WriteAllText(fi.FullName, csvText);
            try
            {

                OleDbConnection conn = new OleDbConnection("Provider=Microsoft.Jet.OleDb.4.0; Data Source = " + Path.GetDirectoryName(fi.FullName) + "; Extended Properties = \"Text;HDR=YES;FMT=Delimited;CharacterSet=65001;\"");

                conn.Open();

                OleDbDataAdapter adapter = new OleDbDataAdapter("SELECT * FROM " + Path.GetFileName(fi.FullName), conn);
                var pluralizer = PluralizationService.CreateService(CultureInfo.CurrentCulture);
                fileName = Path.GetFileNameWithoutExtension(fileName);
                var pluralFileName = pluralizer.Pluralize(fileName);
                var singularFileName = pluralizer.Singularize(fileName);
                DataSet ds = new DataSet(pluralFileName);
                adapter.FillSchema(ds, SchemaType.Mapped);
                var table = ds.Tables[0];
                foreach (DataColumn col in table.Columns)
                {
                    col.DataType = typeof(String);
                    col.MaxLength = 10000;

                }
                adapter.Fill(ds);


                conn.Close();

                table.TableName = singularFileName;

                table.Rows
                    .OfType<DataRow>()
                    .ToList()
                    .ForEach(feRow => CleanRow(feRow));

                String xmlFileName = String.Format("{0}.xml", fi.Name);
                FileStream fs = new FileStream(xmlFileName, FileMode.Create);

                table.WriteXml(fs);
                fs.Close();
                String fileText = File.ReadAllText(xmlFileName);
                string finalXml = fileText.Replace("&lt;![CDATA_START[", "<![CDATA[").Replace("]CDATA_END]&gt;", "]]>");
                // File.WriteAllText(xmlFileName, finalXml);
                return finalXml;
            }
            finally
            {
                File.Delete(tempFile);
            }

        }


        private static void CleanRow(DataRow feRow)
        {
            feRow.Table
                 .Columns
                 .OfType<DataColumn>()
                 .ToList()
                 .ForEach(feCol => CleanCol(feRow, feCol));
        }

        private static void CleanCol(DataRow feRow, DataColumn feCol)
        {
            var value = feRow[feCol].SafeToString();
            if (value.Any(anyChar => !Char.IsLetterOrDigit(anyChar) && !" ?~`!@#$%^&*()-_=+[{]}\\|;:',./?".Contains(anyChar)))
            {
                feRow[feCol] = String.Format("<![CDATA_START[{0}]CDATA_END]>", value);
                feRow.AcceptChanges();
            }
        }


        public static string ConvertJsonToXml(string inputFileName, string inputFileContents, byte[] zippedInputBytes)
        {
            String unzippedContents = zippedInputBytes.UnzipToString();
            if (!String.IsNullOrEmpty(unzippedContents))
            {
                var fileSetFiles = unzippedContents.FileSetFilesFromFileSetXml();
                inputFileContents = fileSetFiles.FirstOrDefault().GetFileContents();
            }

            var inputExtension = Path.GetExtension(inputFileName);
            var inputName = "root";
            if (!String.IsNullOrEmpty(inputFileName))
            {
                inputName = Path.GetFileName(inputFileName).Replace(inputExtension, "");
            }

            if (String.IsNullOrEmpty(inputFileContents)) inputFileContents = "[]";

            String json = String.Format("{{ \"{0}\":{1}}}", inputName, inputFileContents);


            var xml = JsonToXml(json).OuterXml;
            return xml;
        }


        public static String FormatXml(this String Xml)
        {
            String Result = "";

            MemoryStream mStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(mStream, Encoding.Unicode);
            XmlDocument document = new XmlDocument();

            try
            {
                // Load the XmlDocument with the XML.
                document.LoadXml(Xml);

                writer.Formatting = System.Xml.Formatting.Indented;

                // Write the XML into a formatting XmlTextWriter
                document.WriteContentTo(writer);
                writer.Flush();
                mStream.Flush();

                // Have to rewind the MemoryStream in order to read
                // its contents.
                mStream.Position = 0;

                // Read MemoryStream contents into a StreamReader.
                StreamReader sReader = new StreamReader(mStream);

                // Extract the text from the StreamReader.
                String FormattedXML = sReader.ReadToEnd();

                Result = FormattedXML;
            }
            catch (XmlException ex)
            {
                throw ex;
            }

            mStream.Close();
            writer.Close();

            return Result;
        }

        public static string FindClosest(this string fullFileName, string otherFile)
        {
            String dir = Path.GetDirectoryName(fullFileName);
            String combinedPath = Path.Combine(dir, otherFile);
            Uri absoluteUri = new Uri(String.Format("file:///{0}", combinedPath));
            //Console.WriteLine(String.Format("resolving: {0}", absoluteUri.ToString()));
            String fileName = absoluteUri.LocalPath;
            String relativeFileName = fullFileName.RelativeFromFull(fileName);
            bool lookDown = true;
            int count = 0;
            while (!File.Exists(fileName) && (count < 10))
            {
                if (relativeFileName.StartsWith("../") && lookDown) fileName = fullFileName.FullFromRelative(relativeFileName.Substring(3));
                else
                {
                    lookDown = false;
                    fileName = fullFileName.FullFromRelative("../" + relativeFileName);
                }

                relativeFileName = fullFileName.RelativeFromFull(fileName);
                count++;
            }

            if (!File.Exists(fileName)) fileName = absoluteUri.LocalPath;

            return fileName;
        }

        public static event EventHandler FileWritten;
        private static void OnFileWritten(String fileName)
        {
            if (!ReferenceEquals(FileWritten, null)) FileWritten(fileName, EventArgs.Empty);
        }

        private static void WriteAllBytes(string fileName, byte[] fileBytes)
        {
            File.WriteAllBytes(fileName, fileBytes);
            OnFileWritten(fileName);
        }

        public static String UnwrapCDATA(this String cdataText)
        {
            cdataText = cdataText.SafeToString();
            if (cdataText.StartsWith("<![CDATA[") && cdataText.EndsWith("]]>"))
            {
                var startPosition = "<![CDATA[".Length;
                var lengthToExclude = "<![CDATA[]]>".Length;
                cdataText = cdataText.Substring(startPosition, cdataText.Length - lengthToExclude);
            }
            return cdataText;
        }

        private static void WriteAllText(string fileName, string fileContents)
        {
            using (StreamWriter sw = new StreamWriter(File.Open(fileName, FileMode.Create), new UTF8Encoding(false)))
            {
                sw.Write(fileContents.UnwrapCDATA());
            }
            OnFileWritten(fileName);
        }

        public static void SplitFileSetXml(this string fileSetXml, String basePath)
        {
            fileSetXml.SplitFileSetXml(false, basePath);
        }

        public static void SplitFileSetXml(this string fileSetXml, bool overwriteAll, String basePath)
        {
            if (String.IsNullOrEmpty(fileSetXml) || !fileSetXml.Contains("<")) return;

            fileSetXml = fileSetXml.Substring(fileSetXml.IndexOf("<"));
            fileSetXml = fileSetXml.Replace("FileContents><?xml ", "FileContents>&lt;?xml");
            XmlDocument doc = new XmlDocument();
            //doc.PreserveWhitespace = true;
            try
            {
                //if (!newFileContents.StartsWith("<?xml ")) newFileContents = "<?xml version=\"1.0\"?>" + newFileContents;
                int len = fileSetXml.Length;
                fileSetXml = fileSetXml.Trim((char)(65279));
                int len2 = fileSetXml.Length;
                doc.LoadXml(fileSetXml);
                if (doc.DocumentElement.Name == "FileSet")
                {
                    foreach (XmlElement elem in doc.DocumentElement.SelectNodes("//FileSetFile"))
                    {
                        XmlNode contentsNode = elem.SelectSingleNode("FileContents");
                        XmlNode binaryContentsNode = elem.SelectSingleNode("BinaryFileContents");
                        XmlNode zippedBinaryContentsNode = elem.SelectSingleNode("ZippedBinaryFileContents");
                        XmlNode zippedTextContentsNode = elem.SelectSingleNode("ZippedTextFileContents");

                        if (!ReferenceEquals(contentsNode, null))
                        {
                            String contents = contentsNode.InnerXml;
                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                ProcessFileSetFile(String.Format("{0}\\test.xml", basePath), overwriteAll, elem, contents, fileName, basePath);
                            }
                        }
                        else if (!ReferenceEquals(binaryContentsNode, null) ||
                                !ReferenceEquals(zippedBinaryContentsNode, null) ||
                                !ReferenceEquals(zippedTextContentsNode, null))
                        {

                            String contents = String.Empty;
                            if (!ReferenceEquals(binaryContentsNode, null)) contents = binaryContentsNode.InnerXml;
                            else if (!ReferenceEquals(zippedBinaryContentsNode, null)) contents = zippedBinaryContentsNode.InnerXml;
                            else contents = zippedTextContentsNode.InnerXml;

                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                byte[] data = Convert.FromBase64String(contents);


                                var fileInfo = new FileInfo(fileName.InnerText);

                                if (!fileInfo.Directory.Exists) fileInfo.Directory.Create();

                                if (!ReferenceEquals(zippedBinaryContentsNode, null))
                                {
                                    using (StreamWriter sw = new StreamWriter(File.Open(fileName.InnerText, FileMode.Create), new UTF8Encoding(false)))
                                    {
                                        sw.Write(data);
                                    }
                                }
                                else if (!ReferenceEquals(zippedTextContentsNode, null))
                                {

                                    using (StreamWriter sw = new StreamWriter(File.Open(fileName.InnerText, FileMode.Create), new UTF8Encoding(false)))
                                    {
                                        sw.WriteLine(data.UnzipToString());
                                    }
                                }
                                else
                                {
                                    using (StreamWriter sw = new StreamWriter(File.Open(fileName.InnerText, FileMode.Create), new UTF8Encoding(false)))
                                    {
                                        sw.Write(data);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (XmlElement fileName in elem.SelectNodes("RelativePath"))
                            {
                                WriteAllText(fileName.InnerText, "Can't write binary contents.");
                            }

                        }
                    }

                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                // Ignore failures attempting to write extra files
                throw ex;
            }
        }


        private static void ProcessFileSetFile(String relativePathOfXml, bool overwriteAll, XmlElement elem, String contents, XmlElement fileName, String basePath)
        {
            String relativeFileName = fileName.InnerText;
            relativeFileName = FullFromRelative(relativePathOfXml, relativeFileName.Trim("\\/ \r\n".ToCharArray()));
            //lastOutputFiles.Add(fullFileName);
            String dir = Path.GetDirectoryName(relativeFileName);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            bool fileExists = File.Exists(relativeFileName);
            bool writeFile = true;
            XmlNode ooNode = elem.SelectSingleNode("OverwriteMode");
            if (fileExists && (!ReferenceEquals(ooNode, null) && (ooNode.InnerText == "Never")))
            {
                writeFile = false;
                //lastOutputFilesSkipped.Add(fullFileName);
            }

            while (contents.Contains("[$$NEWUUID$$]"))
            {
                contents = String.Format("{0}{1}{2}",
                                    contents.Substring(0, contents.IndexOf("[$$NEWUUID$$]")),
                                    Guid.NewGuid(),
                                    contents.Substring(contents.IndexOf("[$$NEWUUID$$]") + "[$$NEWUUID$$]".Length));
            }
            String decodedContent = HttpUtility.HtmlDecode(contents);
            decodedContent = decodedContent.UnwrapCDATA();

            if (overwriteAll || writeFile)
            {
                if (overwriteAll ||
                    !File.Exists(relativeFileName) ||
                    (File.ReadAllText(relativeFileName) != decodedContent))
                {
                    WriteAllText(relativeFileName, decodedContent);
                }
            }
        }

        public static void SplitFileSetFile(this String fileSetFileName, String basePath)
        {
            String fileContents = File.ReadAllText(fileSetFileName);
            SplitFileSetXml(fileContents, false, basePath);
        }

        public static string FullFromRelative(this string rootFullPath, string relativeFileName)
        {
            relativeFileName = relativeFileName.SafeToString().Trim("\r\n\t \\/".ToCharArray());
            FileInfo fi = new FileInfo(Path.Combine(Path.GetDirectoryName(rootFullPath), relativeFileName));
            return fi.FullName;
        }

        public static string RelativeFromFull(this String rootFullPath, String fullPath)
        {
            if (String.IsNullOrEmpty(fullPath)) return fullPath;
            // Trim the path until it doesn't match
            String dir = Path.GetDirectoryName(rootFullPath).Replace("/", "\\");
            String[] dirParts = dir.Split("\\".ToCharArray());
            String[] fullPathParts = fullPath.Replace("/", "\\").Split("\\".ToCharArray());
            int index = 0;
            while (index < fullPathParts.Length && index < dirParts.Length)
            {
                if (String.Equals(fullPathParts[index], dirParts[index], StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }
                else
                {
                    break;
                }
            }
            String left = String.Join("\\", dirParts.Take(index));
            String[] parts = left.Split("\\/".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            String right = String.Join("\\", fullPathParts.Skip(index));
            int count = dirParts.Length - index;
            for (int i = 0; i < count; i++)
            {
                right = "../" + right;
            }
            return right.Replace("/", "\\");
        }

        public static string GetTextResourceContents(this Assembly assembly, String resourceName)
        {
            var fullResourceName = assembly.GetManifestResourceNames().FirstOrDefault(fod => fod.Contains(resourceName));
            if (String.IsNullOrEmpty(fullResourceName)) throw new Exception(String.Format("Can't find resource '{0}' in '{1}'", resourceName, assembly.FullName));
            else
            {
                var resourceStream = assembly.GetManifestResourceStream(fullResourceName);
                using (var streamReader = new StreamReader(resourceStream))
                {
                    resourceStream.Position = 0;
                    return streamReader.ReadToEnd();
                }
            }
        }

        public static String HtmlToXml(this String htmlText)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlText);
            MemoryStream ms = new MemoryStream();
            doc.OptionOutputAsXml = true;
            var anchors = doc.DocumentNode
                            .SelectNodes("//a");

            if (!ReferenceEquals(anchors, null))
            {
                anchors.Where(whereNode => !ReferenceEquals(whereNode, null))
                        .ToList()
                        .ForEach(feAnchor => feAnchor.CleanSPECDOCLink());
            }

            doc.Save(ms);
            ms.Position = 0;
            var sr = (new StreamReader(ms));
            return sr.ReadToEnd();
        }

        public static String XmlToJson(this XmlDocument doc)
        {
            return JsonConvert.SerializeXmlNode(doc.DocumentElement, Newtonsoft.Json.Formatting.Indented);

        }

        public static XmlDocument JsonToXml(this String json)
        {
            try
            {
                return JsonConvert.DeserializeXmlNode(json);
            }
            catch // (JsonSerializationException jse)
            {
                json = String.Format("{{ root: {0} }}", json);
                return JsonConvert.DeserializeXmlNode(json);
            }

        }

        private static void CleanSPECDOCLink(this HtmlNode node)
        {
            if (!ReferenceEquals(node.Attributes, null))
            {
                var href = node.Attributes["href"];
                if (!ReferenceEquals(href, null))
                {
                    if (!String.IsNullOrEmpty(href.Value))
                    {
                        if (href.Value.Contains("://specdocs/"))
                        {
                            href.Value = href.Value.Substring(href.Value.IndexOf("://specdocs/") + "://specdocs/".Length);
                            var entityIndex = href.Value.ToLower().IndexOf("/entity/");
                            var viewResourceIndex = href.Value.ToLower().IndexOf("/view-resource/");
                            var simpleLinkIndex = href.Value.ToLower().IndexOf("/link/");
                            if (entityIndex >= 0)
                            {
                                var entityName = href.Value.Substring(entityIndex + "/entity/".Length);
                                var candidates = entityName.Split("/&?".ToCharArray());
                                var finalName = candidates.FirstOrDefault(fodCandidate => !String.IsNullOrEmpty(fodCandidate.SafeToString().Trim()));

                                href.Value = String.Format("$SDK_ROOT_URI$Docs/DataModel/Entity_{0}.html", finalName);
                            }
                            else if (viewResourceIndex >= 0)
                            {
                                var resourceName = href.Value.Substring(viewResourceIndex + "/view-resource/".Length);
                                var candidates = resourceName.Split("/&?".ToCharArray());
                                var finalName = candidates.FirstOrDefault(fodCandidate => !String.IsNullOrEmpty(fodCandidate.SafeToString().Trim()));

                                href.Value = String.Format("$SDK_ROOT_URI$Docs/AdditionalResources/Resource_{0}.html", finalName);
                            }
                            else if (simpleLinkIndex >= 0)
                            {
                                href.Value = HttpUtility.UrlDecode(href.Value.Substring(viewResourceIndex + "/link/".Length));
                            }
                            else
                            {
                                href.Value = "ERROR - COULD NOT FIND A MATCHING SPECDOCS// syntax for: '" + href.Value + "'";
                            }

                            if (href.Value.Contains(".html&")) href.Value = href.Value.Substring(0, href.Value.IndexOf(".html&") + ".html".Length);
                        }
                        else if (href.Value.StartsWith("https://www.google.com/url?q="))
                        {
                            var decodedUrl = HttpUtility.UrlDecode(href.Value.Substring("https://www.google.com/url?q=".Length)) + "&amp;";
                            href.Value = decodedUrl.Substring(0, decodedUrl.IndexOf("&amp;"));
                            node.SetAttributeValue("target", "_blank");
                        }
                        else
                        {
                            node.SetAttributeValue("target", "_blank");
                            object o = 1;
                        }
                    }
                }
            }
        }

        public static T GetFirst<T>(this string[] args)
            where T : class
        {
            if (typeof(T) == typeof(String))
            {
                if (ReferenceEquals(args, null)) return String.Empty as T;
                else if (!args.Any()) return String.Empty as T;
                else return args[0] as T;
            }
            else if (typeof(T) == typeof(Uri))
            {
                var uriString = args.GetFirst<String>();
                if (!String.IsNullOrEmpty(uriString))
                {
                    return new Uri(uriString) as T;
                }
            }
            else if (typeof(T) == typeof(FileInfo))
            {
                var fileNameString = args.GetFirst<String>();
                if (!String.IsNullOrEmpty(fileNameString))
                {
                    return new FileInfo(fileNameString) as T;
                }
            }
            else
            {
                var msg = String.Format("Handler not writtent to handle finding first argument of type '{0}'", typeof(T).Name);
                throw new NotImplementedException(msg);
            }

            // If we get here - return nothing
            return default(T);
        }


        public static byte[] Zip(this string str)
        {
            var bytes = new UTF8Encoding(false).GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static bool IsAssignableTo(this Type childClass, Type parentClass)
        {
            return parentClass.IsAssignableFrom(childClass);
        }

        public static String ToFriendlyFullTypeName(this Type type)
        {
            return type.FullName;
        }


        public static Type FindType(this String theTypeName)
        {
            return Type.GetType(theTypeName);
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
        }

        public static PluralizationService Pluralizer { get; private set; }



        public static String Singularize(this String text)
        {
            var wordToSingularize = text.SafeToString().Contains(" ") ? text.Substring(text.LastIndexOf(" ") + 1) : text.SafeToString();
            var singular = Pluralizer.Singularize(wordToSingularize);
            String result = singular;
            if (wordToSingularize.Length != text.Length) result = text.Substring(0, text.Length - wordToSingularize.Length) + singular;
            return result;
        }

        public static String Pluralize(this String text)
        {
            var wordToPluralize = text.SafeToString().Contains(" ") ? text.Substring(text.LastIndexOf(" ") + 1) : text.SafeToString();
            var plural = Pluralizer.Pluralize(wordToPluralize);
            String result = plural;
            if (wordToPluralize.Length != text.Length) result = text.Substring(0, text.Length - wordToPluralize.Length) + plural;
            return result;
        }

        public static String SqlSafeToString(this object theObject)
        {
            return theObject.SafeToString().Replace("'", "''");
        }


        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static Byte[] Unzip(this byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public static string UnzipToString(this byte[] zippedBytes)
        {
            if (ReferenceEquals(zippedBytes, null)) return String.Empty;
            else
            {
                var unzippedBytes = zippedBytes.Unzip();

                return Encoding.UTF8.GetString(unzippedBytes);

            }
        }

        public static String ToTitleCase(this String text)
        {
            if (String.IsNullOrEmpty(text)) return String.Empty;
            else
            {
                text = text.SafeToString();

                if (Char.IsLower(text[0])) text = text.First().ToString().ToUpper() + text.Substring(1);

                return String.Join(String.Empty,
                    text.SafeToString()
                           .Select(c => (Char.IsUpper(c) ? " " + c.ToString() : c.ToString()))
                           .ToArray())
                        .Trim();
            }
        }
    }
}