$ErrorActionPreference='Stop'
$framework=[Runtime.InteropServices.RuntimeEnvironment]::GetRuntimeDirectory()
$parameters=New-Object System.CodeDom.Compiler.CompilerParameters
$parameters.GenerateExecutable=$true
$parameters.GenerateInMemory=$false
$parameters.OutputAssembly=Join-Path $PSScriptRoot 'Air5-Control.exe'
$parameters.CompilerOptions='/target:winexe /optimize+ /resource:"' + (Join-Path $PSScriptRoot 'src\MainWindow.xaml') + '",Air5UI'
$parameters.CompilerOptions += ' /win32icon:"' + (Join-Path $PSScriptRoot 'src\Air5.ico') + '" /win32manifest:"' + (Join-Path $PSScriptRoot 'src\app.manifest') + '"'
$references=@('System.dll','System.Core.dll','System.Windows.Forms.dll','System.Drawing.dll','System.Xaml.dll','System.Xml.dll', (Join-Path $framework 'WPF\PresentationFramework.dll'),(Join-Path $framework 'WPF\PresentationCore.dll'),(Join-Path $framework 'WPF\WindowsBase.dll'),(Join-Path $framework 'System.Runtime.WindowsRuntime.dll'),"$env:WINDIR\System32\WinMetadata\Windows.Devices.winmd","$env:WINDIR\System32\WinMetadata\Windows.Foundation.winmd","$env:WINDIR\System32\WinMetadata\Windows.Storage.winmd")
foreach($name in @('System.Runtime','System.Runtime.InteropServices.WindowsRuntime','System.Threading.Tasks','System.ObjectModel')) { $references+=[Reflection.Assembly]::Load($name+', Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a').Location }
$parameters.ReferencedAssemblies.AddRange([string[]]$references)
$provider=New-Object Microsoft.CSharp.CSharpCodeProvider
$result=$provider.CompileAssemblyFromFile($parameters,(Join-Path $PSScriptRoot 'src\Air5.cs'))
$result.Errors | ForEach-Object { $_.ToString() }
if($result.Errors.HasErrors){throw 'Derleme basarisiz.'}
'Build OK'


