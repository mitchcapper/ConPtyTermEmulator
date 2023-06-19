# WPF Terminal Console Control Lib/Sample

Simple example library/control/test app for the Windows Terminal WPF Control and the new ConPTY Library.

![](TermExample/Screenshot.png)

## Usage

Using the control is as simple as adding the control to your xaml ie:

`<term:BasicTerminalControl StartupCommandLine="pwsh.exe" />`

there are other options you can customize but that will get you a fully working terminal with mouse/keyboard support including ANSI formatting.

## Building This Library/Demo

Make sure your output looks like:

```
./ConPtyTermEmulatorLib.dll
./ConPtyTermEmulatorLib.pdb
./Microsoft.Terminal.Wpf.dll
./Microsoft.Terminal.Wpf.pdb
./Microsoft.Terminal.Wpf.xml
./runtimes/win-x64/native/conpty.dll
./runtimes/win-x64/native/PublicTerminalCore.dll
./runtimes/win-x64/native/OpenConsole.exe
./TermExample.deps.json
./TermExample.dll
./TermExample.exe
./TermExample.runtimeconfig.json
```

Microsoft now has a CI nuget package for Microsoft.Terminal.Wpf so the EmulatorLib will pull that in by default.  For ConPTY (including OpenConsole.exe) there is no package built automatically yet.  To avoid having to build it locally yourself you can download one of the latest drop artifacts from the azure CI runs.  Go to: https://dev.azure.com/ms/terminal/_build?definitionId=136 click on one of the successful runs under the stages for the "Build x64" it should say X jobs completed and under that show a link to the artifacts ("ie 3 artifacts").  Download the "drop" artifact and copy the files from drop\Release\x64\test\ of: ConPty.dll and OpenConsole.dll to ConPtyTermEmulatorLib/runtimes/win-x64/native.

Adding the dlls direct to the ConPtyTermEmulatorLib means it will produce a complete nuget package with no manual work for any projects you include it in. Any project you use it in should end up with the right files in the places above.



## Notes/Issues/Troubleshooting

- This control should not be used for any production situations as they are not meant for release yet: [Productize the WPF, UWP Terminal Controls · Issue #6999 · microsoft/terminal · GitHub](https://github.com/microsoft/terminal/issues/6999)

- There are airspace issues (you cannot put anything above the terminal control)

- If you give an invalid executable for the shell start or if your OpenConsole.exe is not in the right place you will get a crash on start