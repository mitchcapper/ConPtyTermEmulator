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
./PublicTerminalCore.dll
./runtimes
./runtimes/win10-x64
./runtimes/win10-x64/native
./runtimes/win10-x64/native/conpty.dll
./runtimes/win10-x64/native/OpenConsole.exe
./TermExample.deps.json
./TermExample.dll
./TermExample.exe
./TermExample.pdb
./TermExample.runtimeconfig.json
```

To accomplish this I did change the nuget package structure to still be able to use a package.

You will need `PublicTerminalCore.dll` and `Microsoft.Terminal.Wpf.dll` from the Terminal build as well.  Place them in `ConPtyTermEmulatorLib/lib` and `ConPtyTermEmulatorLib/runtimes/win10-x64/native` respectively.

You do not need to create a nuget package but can just manually copy all the dlls/exes to the final build location paths above if desired.



## Notes/Issues/Troubleshooting

- This control should not be used for any production situations as they are not meant for release yet: [Productize the WPF, UWP Terminal Controls · Issue #6999 · microsoft/terminal · GitHub](https://github.com/microsoft/terminal/issues/6999)

- There are airspace issues (you cannot put anything above the terminal control)

- If you give an invalid executable for the shell start or if your OpenConsole.exe is not in the right place you will get a crash on start