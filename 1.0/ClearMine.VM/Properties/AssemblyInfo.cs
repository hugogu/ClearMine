﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;

[assembly: AssemblyTitle("ClearMine.VM")]
[assembly: AssemblyDescription("Contains ViewModels in the whole application.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Fadefy")]
[assembly: AssemblyProduct("ClearMine.VM")]
[assembly: AssemblyCopyright("Copyright © 2012 Fadefy")]
[assembly: AssemblyTrademark("Fadefy")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]
[assembly: Guid("0a320a58-78bd-4f1e-946a-96cb4f5b27ed")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: InternalsVisibleTo("ClearMine")]
[assembly: InternalsVisibleTo("ClearMine.Themes")]
[assembly: XmlnsDefinition("http://schemas.microsoft.com/winfx/2006/xaml/presentation", "ClearMine.VM.Behaviors")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1811", 
    Justification = "They property in the vm will be accessed by the xaml where FxCop cannot recogize.")]
[assembly: SuppressMessage("Microsoft.Performance", "CA1822",
    Justification = "A static property requires the xaml knows the VM's name which will create a tight relationship between them.")]