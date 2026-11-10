; Inno Setup script. Build with:  iscc /DAppVersion=1.0.0 installer\TrackpadCompanion.iss
; Expects the published app in ..\publish (dotnet publish ... -o publish).

#ifndef AppVersion
  #define AppVersion "0.0.0-dev"
#endif

[Setup]
AppId={{B7C1E5A2-6D1F-4C57-9B1A-3E2A5D0C7A41}
AppName=Trackpad Companion
AppVersion={#AppVersion}
AppPublisher=Trackpad Companion contributors
DefaultDirName={autopf}\Trackpad Companion
DefaultGroupName=Trackpad Companion
OutputDir=..\dist
OutputBaseFilename=TrackpadCompanion-Setup-{#AppVersion}
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
WizardStyle=modern
UninstallDisplayIcon={app}\TrackpadCompanion.exe
CloseApplications=yes

[Tasks]
Name: "driver"; Description: "Download and install the Magic Trackpad driver (Microsoft-signed, needs internet)"; GroupDescription: "Driver:"; Check: not DriverPresent
Name: "startup"; Description: "Start Trackpad Companion in the tray when I sign in"; GroupDescription: "Options:"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "install-driver.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Trackpad Companion"; Filename: "{app}\TrackpadCompanion.exe"
Name: "{group}\Reinstall Magic Trackpad driver"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install-driver.ps1"""

[Run]
Filename: "schtasks.exe"; Parameters: "/Create /TN ""Trackpad Companion"" /TR ""\""{app}\TrackpadCompanion.exe\"" --tray"" /SC ONLOGON /RL HIGHEST /F"; Flags: runhidden; Tasks: startup
Filename: "{app}\TrackpadCompanion.exe"; Description: "Start Trackpad Companion"; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: "schtasks.exe"; Parameters: "/Delete /TN ""Trackpad Companion"" /F"; Flags: runhidden; RunOnceId: "DeleteStartupTask"

[Code]
// The driver service exists once the driver has been installed on a device.
function DriverPresent: Boolean;
begin
  Result := RegKeyExists(HKLM64, 'SYSTEM\CurrentControlSet\Services\AmtPtpHidFilter');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if (CurStep = ssPostInstall) and WizardIsTaskSelected('driver') then
  begin
    WizardForm.StatusLabel.Caption := 'Downloading and installing the Magic Trackpad driver...';
    if (not Exec('powershell.exe',
         '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\install-driver.ps1') + '"',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode)) or (ResultCode <> 0) then
      MsgBox('Trackpad Companion was installed, but the driver could not be installed automatically.' + #13#10 + #13#10 +
             'Details: %TEMP%\TrackpadCompanion-driver.log' + #13#10 +
             'You can retry from the Start menu ("Reinstall Magic Trackpad driver") or follow the manual steps in the README.',
             mbInformation, MB_OK);
  end;
end;
