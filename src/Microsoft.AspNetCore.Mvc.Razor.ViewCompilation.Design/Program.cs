// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Design.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.ViewCompilation.Design
{
    public class Program
    {
        private readonly static Type ProgramType = typeof(Program);

        public static int Main(string[] args)
        {
#if DEBUG
            DebugHelper.HandleDebugSwitch(ref args);

            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
#endif

            EnsureValidDispatchRecipient(ref args);

            var app = new PrecompilationApplication(ProgramType);
            new PrecompileRunCommand().Configure(app);

            int result = app.Execute(args);

#if DEBUG
            Console.ReadLine();
#endif

            return result;
        }

#if DEBUG
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName assemblyName = new AssemblyName(args.Name);
            string assemblyPath;

            string[] paths = new string[]
            {
                Environment.CurrentDirectory
            };

            foreach (string path in paths)
            {
                assemblyPath = Path.Combine(path, assemblyName.Name + ".dll");
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFile(assemblyPath);

                assemblyPath = Path.Combine(path, assemblyName.Name + ".exe");
                if (File.Exists(assemblyPath))
                    return Assembly.LoadFile(assemblyPath);
            }

            //Console.WriteLine("Could not found " + assemblyName);

            return null;
        }
#endif

        private static void EnsureValidDispatchRecipient(ref string[] args)
        {
            const string DispatcherVersionArgumentName = "--dispatcher-version";

            var dispatcherArgumentIndex = Array.FindIndex(
                args,
                (value) => string.Equals(value, DispatcherVersionArgumentName, StringComparison.OrdinalIgnoreCase));

            if (dispatcherArgumentIndex < 0)
            {
                return;
            }

            var dispatcherArgumentValueIndex = dispatcherArgumentIndex + 1;
            if (dispatcherArgumentValueIndex < args.Length)
            {
                var dispatcherVersion = args[dispatcherArgumentValueIndex];

                var thisAssembly = ProgramType.GetTypeInfo().Assembly;
                var version = thisAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion
                    ?? thisAssembly.GetName().Version.ToString();

                if (string.Equals(dispatcherVersion, version, StringComparison.Ordinal))
                {
                    // Remove dispatcher arguments from
                    var preDispatcherArgument = args.Take(dispatcherArgumentIndex);
                    var postDispatcherArgument = args.Skip(dispatcherArgumentIndex + 2);
                    var newProgramArguments = preDispatcherArgument.Concat(postDispatcherArgument);
                    args = newProgramArguments.ToArray();
                    return;
                }
            }

            // Could not validate the dispatcher version.
            throw new InvalidOperationException("Could not invoke tool");
        }
    }
}
