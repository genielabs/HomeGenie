; Requirements: Inno Setup (http://www.jrsoftware.org/isdl.php)
; - open this script with Inno Setup
; - compile and run

[Setup]
AppName=HomeGenie
AppVerName=HomeGenie 1.1 beta (r515)
AppPublisher=GenieLabs
AppPublisherURL=http://www.homegenie.it
AppVersion=1.1 beta (r515)
DefaultDirName={pf}\HomeGenie
DefaultGroupName=HomeGenie
Compression=lzma
SolidCompression=yes
; Win2000 or higher
MinVersion=5.0
LicenseFile=..\..\LICENCE.TXT
;InfoAfterFile=..\..\README.TXT



; This installation requires admin priveledges.  This is needed to install
; drivers on windows vista and later.
PrivilegesRequired=admin

; "ArchitecturesInstallIn64BitMode=x64 ia64" requests that the install
; be done in "64-bit mode" on x64 & Itanium, meaning it should use the
; native 64-bit Program Files directory and the 64-bit view of the
; registry. On all other architectures it will install in "32-bit mode".
ArchitecturesInstallIn64BitMode=x64 ia64
;WindowVisible=True
AppCopyright=(c) 2011-2015 G-Labs - info@homegenie.it

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
;Name: "basque"; MessagesFile: "compiler:Languages\Basque.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "catalan"; MessagesFile: "compiler:Languages\Catalan.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "danish"; MessagesFile: "compiler:Languages\Danish.isl"
Name: "dutch"; MessagesFile: "compiler:Languages\Dutch.isl"
Name: "finnish"; MessagesFile: "compiler:Languages\Finnish.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "hebrew"; MessagesFile: "compiler:Languages\Hebrew.isl"
Name: "hungarian"; MessagesFile: "compiler:Languages\Hungarian.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "norwegian"; MessagesFile: "compiler:Languages\Norwegian.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "portuguese"; MessagesFile: "compiler:Languages\Portuguese.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
;Name: "slovak"; MessagesFile: "compiler:Languages\Slovak.isl"
Name: "slovenian"; MessagesFile: "compiler:Languages\Slovenian.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Code]
function IsX64: Boolean;
begin
  Result := Is64BitInstallMode and (ProcessorArchitecture = paX64);
end;

function IsI64: Boolean;
begin
  Result := Is64BitInstallMode and (ProcessorArchitecture = paIA64);
end;

function IsX86: Boolean;
begin
  Result := not IsX64 and not IsI64;
end;

function Is64: Boolean;
begin
  Result := IsX64 or IsI64;
end;


function IsDotNetDetected(version: string; service: cardinal): boolean;
// Indicates whether the specified version and service pack of the .NET Framework is installed.
//
// version -- Specify one of these strings for the required .NET Framework version:
//    'v1.1.4322'     .NET Framework 1.1
//    'v2.0.50727'    .NET Framework 2.0
//    'v3.0'          .NET Framework 3.0
//    'v3.5'          .NET Framework 3.5
//    'v4\Client'     .NET Framework 4.0 Client Profile
//    'v4\Full'       .NET Framework 4.0 Full Installation
//    'v4.5'          .NET Framework 4.5
//
// service -- Specify any non-negative integer for the required service pack level:
//    0               No service packs required
//    1, 2, etc.      Service pack 1, 2, etc. required
var
    key: string;
    install, release, serviceCount: cardinal;
    check45, success: boolean;
begin
    // .NET 4.5 installs as update to .NET 4.0 Full
    if version = 'v4.5' then begin
        version := 'v4\Full';
        check45 := true;
    end else
        check45 := false;

    // installation key group for all .NET versions
    key := 'SOFTWARE\Microsoft\NET Framework Setup\NDP\' + version;

    // .NET 3.0 uses value InstallSuccess in subkey Setup
    if Pos('v3.0', version) = 1 then begin
        success := RegQueryDWordValue(HKLM, key + '\Setup', 'InstallSuccess', install);
    end else begin
        success := RegQueryDWordValue(HKLM, key, 'Install', install);
    end;

    // .NET 4.0/4.5 uses value Servicing instead of SP
    if Pos('v4', version) = 1 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Servicing', serviceCount);
    end else begin
        success := success and RegQueryDWordValue(HKLM, key, 'SP', serviceCount);
    end;

    // .NET 4.5 uses additional value Release
    if check45 then begin
        success := success and RegQueryDWordValue(HKLM, key, 'Release', release);
        success := success and (release >= 378389);
    end;

    result := success and (install = 1) and (serviceCount >= service);
end;



function GetUninstallString: string;
var
  sUnInstPath: string;
  sUnInstallString: String;
begin
  Result := '';
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeGenie_is1'); //Your App GUID/ID
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade: Boolean;
begin
  Result := (GetUninstallString <> '');
end;

function InitializeSetup: Boolean;
var
  V: Integer;
  iResultCode: Integer;
  sUnInstallString: string;
begin
  Result := True; // in case when no previous version is found
  if RegValueExists(HKEY_LOCAL_MACHINE,'Software\Microsoft\Windows\CurrentVersion\Uninstall\HomeGenie_is1', 'UninstallString') then
  begin
//Your App GUID/ID
    V := MsgBox(ExpandConstant('An old version of HomeGenie was detected. Uninstall is required to proceed. Do you want to uninstall it?'), mbInformation, MB_YESNO); //Custom Message if App installed
    if V = IDYES then
    begin
      sUnInstallString := GetUninstallString();
      sUnInstallString :=  RemoveQuotes(sUnInstallString);
      Exec(ExpandConstant(sUnInstallString), '', '', SW_SHOW, ewWaitUntilTerminated, iResultCode);
      Result := True; //if you want to proceed after uninstall
                //Exit; //if you want to quit after uninstall
    end
    else
      Result := False; //when older version present and not uninstalled
  end;
  
  if not IsDotNetDetected('v4.5', 0) then begin
     MsgBox('HomeGenie requires Microsoft .NET Framework 4.5.'#13#13
         'Please use Windows Update to install this version,'#13
         'and then re-run the HomeGenie setup program.', mbInformation, MB_OK);
     Result := false;
  end 
  else
     Result := true;

  //MsgBox('Connect your automation interfaces now, if not already connected.', mbInformation, MB_OK);
end;

[Files]
Source: "C:\Program Files\ISTool\isxdl.dll"; Flags: dontcopy
Source: ".\Drivers\USB_ActiveHome_Interface\*"; DestDir: "{app}\Drivers\LibUsb_MarmitekCM15Pro"; Flags: ignoreversion recursesubdirs
Source: "..\..\HomeGenie\bin\Debug\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs
Source: "..\..\HomeGenie\bin\Debug\HomeGenie.exe"; DestDir: "{app}"; Flags: ignoreversion replacesameversion
Source: "..\..\HomeGenie\bin\Debug\README.TXT"; DestDir: "{app}"; Flags: ignoreversion replacesameversion

[InstallDelete]
Type: files; Name: "{app}\SQLite.Interop.dll";

[Tasks]
;Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; 
;Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; 

[Icons]
Name: "{group}\HomeGenie 1.1 beta (r515)"; Filename: "{app}\HomeGenieManager.exe"
Name: "{group}\Uninstall HomeGenie 1.1 beta (r515)"; Filename: "{uninstallexe}"
Name: "{commondesktop}\HomeGenie"; Filename: "{app}\HomeGenieManager.exe"

[Run]
;Filename: "rundll32"; Parameters: "libusb0.dll,usb_touch_inf_file_np_rundll {win}\inf\input.inf"

; Install drivers and service, then start it
;Filename: "rundll32"; Parameters: "libusb0.dll,usb_install_driver_np_rundll {app}\Drivers\LibUsb_MarmitekCM15Pro\X10_USB_ActiveHome_(ACPI-compliant).inf"; StatusMsg: "Installing driver (this may take a few seconds) ..."
Filename: "{app}\Drivers\LibUsb_MarmitekCM15Pro\InstallDriver.exe"; WorkingDir: "{app}"; Flags: hidewizard; Description: "X10 driver install"; StatusMsg: "Installing X10 driver"
Filename: "{app}\HomeGenieService.exe"; Parameters: "--install"; WorkingDir: "{app}"; Flags: waituntilterminated runhidden; StatusMsg: "Registering HomeGenie Windows Service..."
Filename: "net.exe"; Parameters: "start HomeGenieService"; Flags: waituntilterminated runhidden; Description: "Starting HomeGenie service"; StatusMsg: "Starting HomeGenie Service..."
Filename: "{app}\HomeGenieManager.exe"; WorkingDir: "{app}"; Flags: nowait shellexec runminimized

[UninstallRun]
Filename: net.exe; Parameters: "stop HomeGenieService"; StatusMsg: "Stopping HomeGenie Service..."; Flags: waituntilterminated runhidden
Filename: "{app}\HomeGenieService.exe"; Parameters: "--uninstall"; StatusMsg: "Unregistering HomeGenie Service..."; WorkingDir: "{app}"; Flags: waituntilterminated runhidden

