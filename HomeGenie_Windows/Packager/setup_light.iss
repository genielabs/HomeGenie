; Requirements: Inno Setup (http://www.jrsoftware.org/isdl.php)
; - open this script with Inno Setup
; - compile and run

[Setup]
AppName=HomeGenie
AppVerName=HomeGenie 1.2.527
AppPublisher=GenieLabs
AppPublisherURL=http://www.homegenie.it
AppVersion=1.1.527
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
AppCopyright=(c) 2011-2018 G-Labs - info@homegenie.it

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
  
end;

[Files]
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
Name: "{group}\HomeGenie 1.1.527"; Filename: "{app}\HomeGenieManager.exe"
Name: "{group}\Uninstall HomeGenie 1.1.527"; Filename: "{uninstallexe}"
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

