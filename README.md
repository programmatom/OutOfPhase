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
