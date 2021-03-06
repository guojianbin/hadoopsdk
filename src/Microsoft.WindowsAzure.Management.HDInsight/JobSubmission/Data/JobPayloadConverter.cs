﻿namespace Microsoft.WindowsAzure.Management.HDInsight.JobSubmission.Data
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.WindowsAzure.Management.Framework;
    using Microsoft.WindowsAzure.Management.Framework.DynamicXml.Reader;
    using Microsoft.WindowsAzure.Management.Framework.DynamicXml.Writer;

    /// <summary>
    /// Converts job payloads to and from objects as needed by the SDK.
    /// </summary>
    public class JobPayloadConverter
    {
        /// <summary>
        /// Deserializes a job creation result object from a payload string.
        /// </summary>
        /// <param name="payload">
        /// The payload.
        /// </param>
        /// <returns>
        /// A HDInsightJobCreationResults object representing the payload.
        /// </returns>
        public HDInsightJobCreationResults DeserializeJobCreationResults(string payload)
        {
            HDInsightJobCreationResults results = new HDInsightJobCreationResults();
            results.ErrorCode = string.Empty;
            results.HttpStatusCode = HttpStatusCode.Accepted;
            XmlDocumentConverter documentConverter = new XmlDocumentConverter();
            var document = documentConverter.GetXmlDocument(payload);
            DynaXmlNamespaceTable nameTable = new DynaXmlNamespaceTable(document);
            var node = document.SelectSingleNode("//def:PassthroughResponse/def:Data", nameTable.NamespaceManager);
            results.JobId = node.InnerText;
            XmlElement error = (XmlElement)document.SelectSingleNode("//def:PassthroughResponse/def:Error", nameTable.NamespaceManager);
            if (error.IsNotNull())
            {
                var errorId = error.SelectSingleNode("//def:ErrorId", nameTable.NamespaceManager);
                if (errorId.IsNotNull())
                {
                    results.ErrorCode = errorId.InnerText;
                }
                var statusCode = error.SelectSingleNode("//def:StatusCode", nameTable.NamespaceManager);
                if (statusCode.IsNotNull())
                {
                    HttpStatusCode httpStatusCode = HttpStatusCode.Accepted;
                    if (HttpStatusCode.TryParse(statusCode.InnerText, out httpStatusCode))
                    {
                        results.HttpStatusCode = httpStatusCode;
                    }
                }
            }
            return results;
        }

        /// <summary>
        /// Deserailzies a payload into a HDInsightJobList.
        /// </summary>
        /// <param name="payload">
        /// The payload.
        /// </param>
        /// <returns>
        /// An HDInsightJobList representing the payload.
        /// </returns>
        public HDInsightJobList DeserializeJobList(string payload)
        {
            var jobs = new List<string>();
            HDInsightJobList results = new HDInsightJobList();
            XmlDocumentConverter documentConverter = new XmlDocumentConverter();
            var document = documentConverter.GetXmlDocument(payload);
            DynaXmlNamespaceTable nameTable = new DynaXmlNamespaceTable(document);
            var prefix = nameTable.GetPrefixesForNamespace("http://schemas.microsoft.com/2003/10/Serialization/Arrays").SingleOrDefault();
            if (prefix.IsNotNull())
            {
                var query = string.Format(CultureInfo.InvariantCulture, "//def:PassthroughResponse/def:Data/{0}:string", prefix);
                var nodes = document.SelectNodes(query, nameTable.NamespaceManager);
                foreach (XmlNode node in nodes)
                {
                    jobs.Add(node.InnerText);
                }
            }
            results.ErrorCode = string.Empty;
            results.HttpStatusCode = HttpStatusCode.Accepted;
            XmlElement error = (XmlElement)document.SelectSingleNode("//def:PassthroughResponse/def:Error", nameTable.NamespaceManager);
            if (error.IsNotNull())
            {
                var errorId = error.SelectSingleNode("//def:ErrorId", nameTable.NamespaceManager);
                if (errorId.IsNotNull())
                {
                    results.ErrorCode = errorId.InnerText;
                }
                var statusCode = error.SelectSingleNode("//def:StatusCode", nameTable.NamespaceManager);
                if (statusCode.IsNotNull())
                {
                    HttpStatusCode httpStatusCode = HttpStatusCode.Accepted;
                    if (HttpStatusCode.TryParse(statusCode.InnerText, out httpStatusCode))
                    {
                        results.HttpStatusCode = httpStatusCode;
                    }
                }
            }
            results.JobIds.AddRange(jobs);
            return results;
        }

        /// <summary>
        /// Serializes a job creation object into a payload that can be sent to the server by a rest client.
        /// </summary>
        /// <param name="job">
        /// The job creation details to send to the server.
        /// </param>
        /// <returns>
        /// A string that represents the payload.
        /// </returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity",
            Justification = "This is a result of dynaXml and interface flowing, which makes the code easer to maintain not harder. [tgs]")]
        public string SerializeJobCreationDetails(HDInsightJobCreationDetails job)
        {
            if (job.IsNull())
            {
                throw new ArgumentNullException("job");
            }
            if (job.JobName.IsNullOrEmpty())
            {
                throw new ArgumentException("A job name is required when submitting a job.", "job");
            }
            if (job.OutputStorageLocation.IsNullOrEmpty())
            {
                job.OutputStorageLocation = "(ignore)";
            }
            var asHiveJob = job as HDInsightHiveJobCreationDetails;
            var asMapReduceJob = job as HDInsightMapReduceJobCreationDetails;
            var jobType = asHiveJob.IsNotNull() ? "Hive" : "MapReduce";

            dynamic dynaXml = DynaXmlBuilder.Create();
            dynaXml.b
                     .xmlns("http://schemas.datacontract.org/2004/07/Microsoft.ClusterServices.RDFEProvider.ResourceExtensions.JobSubmission.Models")
                     .xmlns.i("http://www.w3.org/2001/XMLSchema-instance")
                     .xmlns.a("http://schemas.microsoft.com/2003/10/Serialization/Arrays")
                     .xmlns.s("http://www.w3.org/2001/XMLSchema")
                     .ClientJobRequest
                     .b
                     .End();

            if (asMapReduceJob.IsNotNull() && asMapReduceJob.ApplicationName.IsNotNullOrEmpty())
            {
                dynaXml.ApplicationName(asMapReduceJob.ApplicationName);
            }
            else
            {
                dynaXml.ApplicationName.End();
            }

            dynaXml.Arguments
                   .b
                     .sp("arguments")
                   .d
                   .End();

            if (asMapReduceJob.IsNotNull() && asMapReduceJob.JarFile.IsNotNullOrEmpty())
            {
                dynaXml.JarFile(asMapReduceJob.JarFile);
            }
            else
            {
                dynaXml.JarFile.End();
            }

            dynaXml.JobName(job.JobName)
                   .JobType(jobType.ToString())
                   .OutputStorageLocation(job.OutputStorageLocation)
                   .Parameters
                   .b
                     .sp("parameters")
                   .d
                   .Query
                   .b
                     .sp("query")
                   .d
                   .Resources
                   .b
                     .sp("resources")
                   .d
                   .End();

            dynaXml.d.d.End();

            if (asHiveJob.IsNotNull() && asHiveJob.Query.IsNotNull())
            {
                dynaXml.rp("query")
                       .text(asHiveJob.Query);
            }
            if (asMapReduceJob.IsNotNull() && asMapReduceJob.Arguments.IsNotNull())
            {
                foreach (var argument in asMapReduceJob.Arguments)
                {
                    dynaXml.rp("arguments").xmlns.a.@string(argument);
                }
            }
            if (job.Parameters.IsNotNull())
            {
                foreach (var parameter in job.Parameters)
                {
                    dynaXml.rp("parameters")
                           .JobRequestParameter
                           .b
                             .Key(parameter.Key)
                             .Value
                             .b
                               .at.xmlns.i.type("s:string")
                               .text(parameter.Value)
                             .d
                           .d
                           .End();
                }
            }
            if (job.Resources.IsNotNull())
            {
                foreach (var resource in job.Resources)
                {
                    dynaXml.rp("resources")
                           .JobRequestParameter
                           .b
                             .Key(resource.Key)
                             .Value
                             .b
                               .at.xmlns.i.type("s:string")
                               .text(resource.Value)
                             .d
                           .d
                           .End();
                }
            }
            return dynaXml.ToString();
        }

        /// <summary>
        /// Desterilizes the job details payload data.
        /// </summary>
        /// <param name="payload">
        /// The payload data returned from a server.
        /// </param>
        /// <param name="jobId">
        /// The jobId for the job requested.
        /// </param>
        /// <returns>
        /// A new HDInsightJob object representing the job.
        /// </returns>
        public HDInsightJob DeserializeJobDetails(string payload, string jobId)
        {
            HDInsightJob retval = new HDInsightJob();
            XmlDocumentConverter documentConverter = new XmlDocumentConverter();
            var document = documentConverter.GetXmlDocument(payload);
            DynaXmlNamespaceTable nameTable = new DynaXmlNamespaceTable(document);
            var query = "//def:PassthroughResponse/def:Data";
            var node = document.SelectSingleNode(query, nameTable.NamespaceManager);
            if (node.IsNotNull())
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    XmlElement element = child as XmlElement;
                    if (element.IsNotNull())
                    {
                        switch (element.LocalName)
                        {
                            case "ErrorOutputPath":
                                retval.ErrorOutputPath = element.InnerText;
                                break;
                            case "ExitCode":
                                var errorCode = element.InnerText;
                                if (errorCode.IsNotNullOrEmpty())
                                {
                                    int outCode;
                                    if (int.TryParse(errorCode, NumberStyles.Integer, CultureInfo.InvariantCulture, out outCode))
                                    {
                                        retval.ExitCode = outCode;
                                    }
                                }
                                break;
                            case "LogicalOutputPath":
                                retval.LogicalOutputPath = element.InnerText;
                                break;
                            case "Name":
                                retval.Name = element.InnerText;
                                break;
                            case "PhysicalOutputPath":
                                retval.PhysicalOutputPath = element.InnerText;
                                break;
                            case "Query":
                                retval.Query = element.InnerText;
                                break;
                            case "StatusCode":
                                retval.StatusCode = element.InnerText;
                                break;
                            case "SubmissionTime":
                                var submissionTime = element.InnerText;
                                if (submissionTime.IsNotNullOrEmpty())
                                {
                                    long timeInTicks;
                                    if (long.TryParse(submissionTime, NumberStyles.Integer, CultureInfo.InvariantCulture, out timeInTicks))
                                    {
                                        retval.SubmissionTime = new DateTime(timeInTicks);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
            retval.ErrorCode = string.Empty;
            retval.HttpStatusCode = HttpStatusCode.Accepted;
            XmlElement error = (XmlElement)document.SelectSingleNode("//def:PassthroughResponse/def:Error", nameTable.NamespaceManager);
            if (error.IsNotNull())
            {
                var errorId = error.SelectSingleNode("//def:ErrorId", nameTable.NamespaceManager);
                if (errorId.IsNotNull())
                {
                    retval.ErrorCode = errorId.InnerText;
                }
                var statusCode = error.SelectSingleNode("//def:StatusCode", nameTable.NamespaceManager);
                if (statusCode.IsNotNull())
                {
                    HttpStatusCode httpStatusCode = HttpStatusCode.Accepted;
                    if (HttpStatusCode.TryParse(statusCode.InnerText, out httpStatusCode))
                    {
                        retval.HttpStatusCode = httpStatusCode;
                    }
                }
            }
            retval.JobId = jobId;
            return retval;
        }
    }
}
