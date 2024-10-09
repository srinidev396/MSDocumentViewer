// *************************************************************
// Copyright (c) 1991-2022 LEAD Technologies, Inc.              
// All Rights Reserved.                                         
// *************************************************************
using System.Reflection;

[assembly: AssemblyTitle("DocumentService")]
[assembly: AssemblyDescription("LEADTOOLS DocumentService")]
[assembly: AssemblyCompany("LEAD Technologies, Inc.")]
#if !FOR_STD
[assembly: AssemblyProduct("LEADTOOLS (r) for .NET Framework")]
#else
[assembly: AssemblyProduct("LEADTOOLS (r) for .NET Core")]
#endif
[assembly: AssemblyCopyright("Copyright (c) 1991-2022 LEAD Technologies, Inc.")]
[assembly: AssemblyTrademark("LEADTOOLS (r) is a trademark of LEAD Technologies, Inc.")]

[assembly: AssemblyFileVersion("1.5.33.1")]

#if LTV22_CONFIG
[assembly: AssemblyVersion("22.0.0.0")]
[assembly: AssemblyInformationalVersion("22.0.0.0")]
#elif LTV21_CONFIG
[assembly: AssemblyVersion("21.0.0.0")]
[assembly: AssemblyInformationalVersion("21.0.0.0")]
#else
#error LEADTOOLS configuration is not set
#endif
