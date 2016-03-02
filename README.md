Out Of Phase II - Music Synthesis & Composition Software
===================

Description
-------------
Out of Phase is an open source music synthesis and sequencing program oriented towards creating electronica and electronic dance music, built for .NET platform using Windows Forms (probably only works on Windows at this time).

Official Web Site
-------------
http://outofphasemusic.com/

Project Layout
-------------
Source is in the top level directory **OutOfPhase**. Within that, the build targets are located in **OutOfPhase\OutOfPhase\bin**. There is an **x86** subdirectory for 32-bit builds and **64** subdirectory for 64-bit builds. Under each of those there is a **Debug** or **Release** subdirectory. So, for example, the 64-bit debug executable is in **OutOfPhase\OutOfPhase\bin\x64\Debug\OutOfPhase.exe**.

There is a debugging visualization tool in **OutOfPhaseTraceScheduleAnalyzer**.

The original Macintosh reference source code is in **Original Macintosh Version - Source.zip**.

Building
-------------
The project can be built with Visual Studio 2015 Community Edition. In order to build, you must also be enlisted in **TextEditor** as a sibling of **OutOfPhase**, which provides text-related controls that the build is dependent on.

The project targets .NET Framework 4.6.1. It is possible that it runs on earlier frameworks, but not supported. In any case, the performance advantage of SIMD (SSE/AVX) support is available only on .NET Framework 4.6.1. All development has been done on Windows 10, although theoretically it should run on systems as far back as Windows 7.

The project also requires the Visual Studio 2015 Redistributable when DirectWrite text rendering is enabled (the default). That can be downloaded from http://www.microsoft.com/en-us/download/details.aspx?id=48145. Alternatively, DirectWrite can be disabled via the application settings file. It's a little tricky because the file won't exist if the application hasn't ever run successfully. If this is the case, you can create the file **%APPDATA%\OutOfPhase\Settings.xml** containing the text **&lt;settings&gt;&lt;EnableDirectWrite&gt;False&lt;/EnableDirectWrite&gt;&lt;/settings&gt;**.

