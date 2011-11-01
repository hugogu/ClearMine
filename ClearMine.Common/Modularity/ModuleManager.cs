﻿namespace ClearMine.Common.Modularity
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// 
    /// </summary>
    public class ModuleManager
    {
        private static IList<IModule> loadedModules = new List<IModule>();

        /// <summary>
        /// 
        /// </summary>
        public static void LoadModules()
        {
            var dlls = Directory.EnumerateFiles(".", "*.dll", SearchOption.AllDirectories).Concat(Directory.EnumerateFiles(".", "*.exe", SearchOption.AllDirectories));
            foreach(var dll in dlls)
            {
                var assembly = Assembly.LoadFile(Path.GetFullPath(dll));
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetInterface(typeof(IModule).FullName) != null
                        && loadedModules.All(m => m.GetType() != type))
                    {
                        var module = Activator.CreateInstance(type) as IModule;
                        module.InitializeModule();
                        loadedModules.Add(module);
                    }
                }
            }
        }
    }
}