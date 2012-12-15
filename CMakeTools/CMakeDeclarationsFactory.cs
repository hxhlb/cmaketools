﻿/* ****************************************************************************
 * 
 * Copyright (C) 2012 by David Golub.  All rights reserved.
 * 
 * This software is subject to the Microsoft Reciprocal License (Ms-RL).
 * A copy of the license can be found in the License.txt file included
 * in this distribution.
 * 
 * You must not remove this notice, or any other, from this software.
 * 
 * **************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.Package;

namespace CMakeTools
{
    /// <summary>
    /// Factory to create declarations objects for CMake commands.
    /// </summary>
    static class CMakeDeclarationsFactory
    {
        private delegate Declarations FactoryMethod(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters);

        // Map from CMake commands to factory methods to create their corresponding
        // declarations objects for use after an opening parenthesis.
        private static Dictionary<CMakeCommandId, FactoryMethod> _methods =
            new Dictionary<CMakeCommandId, FactoryMethod>()
        {
            { CMakeCommandId.Include,               CreateIncludeDeclarations },
            { CMakeCommandId.FindPackage,           CreatePackageDeclarations },
            { CMakeCommandId.AddSubdirectory,       CreateSubdirectoryDeclarations },
            { CMakeCommandId.EnableLanguage,        CreateLanguageDeclarations },
            { CMakeCommandId.AddDependencies,       CreateTargetDeclarations },
            { CMakeCommandId.TargetLinkLibraries,   CreateTargetDeclarations }
        };

        // Map from CMake commands to factory methods to create their corresponding
        // declarations object for use after whitespace.
        private static Dictionary<CMakeCommandId, FactoryMethod> _wsMethods =
            new Dictionary<CMakeCommandId, FactoryMethod>()
        {
            { CMakeCommandId.AddExecutable, CreateSourceDeclarations },
            { CMakeCommandId.AddLibrary,    CreateSourceDeclarations }
        };

        static CMakeDeclarationsFactory()
        {
            // Display subcommands for all commands that have them.
            IEnumerable<CMakeCommandId> triggers =
                CMakeSubcommandMethods.GetMemberSelectionTriggers();
            foreach (CMakeCommandId id in triggers)
            {
                _methods[id] = CreateSubcommandDeclarations;
            }
        }

        private static Declarations CreateIncludeDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            return new CMakeIncludeDeclarations(req.FileName);
        }

        private static Declarations CreatePackageDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            return new CMakePackageDeclarations(req.FileName);
        }

        private static Declarations CreateSubdirectoryDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            bool requireCMakeLists =
                (CMakePackage.Instance.CMakeOptionPage.ShowSubdirectories ==
                CMakeOptionPage.SubdirectorySetting.CMakeListsOnly);
            return new CMakeSubdirectoryDeclarations(req.FileName, requireCMakeLists);
        }

        private static Declarations CreateLanguageDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            return new CMakeLanguageDeclarations(req.FileName);
        }

        private static Declarations CreateTargetDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            List<string> targets = CMakeParsing.ParseForTargetNames(source.GetLines());

            // Use the icon index for a VC++ project.
            return new SimpleDeclarations(targets, 199);
        }

        private static Declarations CreateSubcommandDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            IEnumerable<string> subcommands = CMakeSubcommandMethods.GetSubcommands(id);
            if (subcommands == null)
            {
                return null;
            }

            // Use the icon index for a keyword.
            return new SimpleDeclarations(subcommands.ToList(), 206);
        }

        private static Declarations CreateSourceDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters)
        {
            return new CMakeSourceDeclarations(req.FileName, priorParameters, id);
        }

        /// <summary>
        /// Create a declarations object.
        /// </summary>
        /// <param name="id">The CMake command for which to create the object.</param>
        /// <param name="req">The parse request for which to create the object.</param>
        /// <param name="source">The CMake source file.</param>
        /// <param name="priorParameters">
        /// List of parameters appearing prior to the parameters that triggered the
        /// parse request, if it was triggered by whitespace, or null otherwise.
        /// </param>
        /// <returns>The newly created declarations object.</returns>
        public static Declarations CreateDeclarations(CMakeCommandId id,
            ParseRequest req, Source source, List<string> priorParameters = null)
        {
            Dictionary<CMakeCommandId, FactoryMethod> map =
                priorParameters == null ? _methods : _wsMethods;
            if (!map.ContainsKey(id))
            {
                return null;
            }
            return map[id](id, req, source, priorParameters);
        }

        /// <summary>
        /// Get all the commands that should trigger member selection.
        /// </summary>
        /// <returns>A collection of commands.</returns>
        public static IEnumerable<CMakeCommandId> GetMemberSelectionTriggers()
        {
            return _methods.Keys;
        }

        /// <summary>
        /// Get all the commands that should trigger member selection on whitespace.
        /// </summary>
        /// <returns>A collection of commands.</returns>
        public static IEnumerable<CMakeCommandId> GetWSMemberSelectionTriggers()
        {
            return _wsMethods.Keys;
        }
    }
}