using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("PblTestWeb")]
[assembly: AssemblyDescription("PBL Web Testing Host")]
#if DEBUG
[assembly: AssemblyConfiguration("DEBUG")]
#elif RELEASE
[assembly: AssemblyConfiguration("RELEASE")]
#endif
[assembly: AssemblyCompany("Kevin Kennedy")]
[assembly: AssemblyProduct("Stateless Captcha")]
[assembly: AssemblyCopyright("Copyright © 2015")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: CLSCompliant(true)]
