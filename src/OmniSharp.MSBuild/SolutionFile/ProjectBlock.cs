// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OmniSharp.MSBuild.Solution
{
    public class ProjectBlock
    {
        private Guid _projectTypeGuid;
        private Guid _projectGuid;
        private readonly string _projectName;
        private readonly string _projectPath;
        private readonly IEnumerable<SectionBlock> _projectSections;

        public ProjectBlock(Guid projectTypeGuid, string projectName, string projectPath, Guid projectGuid, IEnumerable<SectionBlock> projectSections)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                //throw new ArgumentException(string.Format(WorkspacesResources.StringIsNullOrEmpty, "projectName"));
                throw new ArgumentException();
            }

            if (string.IsNullOrEmpty(projectPath))
            {
                //throw new ArgumentException(string.Format(WorkspacesResources.StringIsNullOrEmpty, "projectPath"));
                throw new ArgumentException();
            }

            _projectTypeGuid = projectTypeGuid;
            _projectName = projectName;
            _projectPath = projectPath;
            _projectGuid = projectGuid;
            _projectSections = projectSections.ToList();
        }

        public Guid ProjectTypeGuid => _projectTypeGuid;

        public string ProjectName => _projectName;

        public string ProjectPath => _projectPath;

        public Guid ProjectGuid => _projectGuid;

        public IEnumerable<SectionBlock> ProjectSections => _projectSections;

        internal string GetText()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"", ProjectTypeGuid.ToString("B").ToUpper(), ProjectName, ProjectPath, ProjectGuid.ToString("B").ToUpper());
            builder.AppendLine();

            foreach (var block in _projectSections)
            {
                builder.Append(block.GetText(indent: 1));
            }

            builder.AppendLine("EndProject");

            return builder.ToString();
        }

        internal static ProjectBlock Parse(TextReader reader)
        {
            var startLine = reader.ReadLine().TrimStart(null);
            var scanner = new LineScanner(startLine);

            if (scanner.ReadUpToAndEat("(\"") != "Project")
            {
                //throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, "Project"));
                throw new Exception();
            }

            var projectTypeGuid = Guid.Parse(scanner.ReadUpToAndEat("\")"));

            // Read chars upto next quote, must contain "=" with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != "=")
            {
                //throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile);
                throw new Exception();
            }

            var projectName = scanner.ReadUpToAndEat("\"");

            // Read chars upto next quote, must contain "," with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != ",")
            {
                //throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile2);
                throw new Exception();
            }

            var projectPath = scanner.ReadUpToAndEat("\"");

            // Read chars upto next quote, must contain "," with optional leading/trailing whitespaces.
            if (scanner.ReadUpToAndEat("\"").Trim() != ",")
            {
                //throw new Exception(WorkspacesResources.InvalidProjectBlockInSolutionFile3);
                throw new Exception();
            }

            var projectGuid = Guid.Parse(scanner.ReadUpToAndEat("\""));

            var projectSections = new List<SectionBlock>();

            while (char.IsWhiteSpace((char)reader.Peek()))
            {
                projectSections.Add(SectionBlock.Parse(reader));
            }

            // Expect to see "EndProject" but be tolerant with missing tags as in Dev12. 
            // Instead, we may see either P' for "Project" or 'G' for "Global", which will be handled next.
            if (reader.Peek() != 'P' && reader.Peek() != 'G')
            {
                if (reader.ReadLine() != "EndProject")
                {
                    //throw new Exception(string.Format(WorkspacesResources.InvalidProjectBlockInSolutionFile4, "EndProject"));
                    throw new Exception();
                }
            }

            return new ProjectBlock(projectTypeGuid, projectName, projectPath, projectGuid, projectSections);
        }
    }
}