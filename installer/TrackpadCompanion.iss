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

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "fr"; MessagesFile: "compiler:Languages\French.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"
Name: "es"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "ja"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "zhhans"; MessagesFile: "languages\ChineseSimplified.isl"
Name: "pl"; MessagesFile: "compiler:Languages\Polish.isl"
Name: "ru"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "af"; MessagesFile: "languages\Afrikaans.isl"

[CustomMessages]
en.DriverTask=Download and install the Magic Trackpad driver (Microsoft-signed, needs internet)
en.DriverGroup=Driver:
en.StartupTask=Start Trackpad Companion in the tray when I sign in
en.OptionsGroup=Options:
en.DriverStatus=Downloading and installing the Magic Trackpad driver...
en.DriverFail=Trackpad Companion was installed, but the driver could not be installed automatically.%n%nYou can retry from the Start menu or follow the manual steps in the README.
en.ReinstallDriver=Reinstall Magic Trackpad driver
en.StartApp=Start Trackpad Companion
fr.DriverTask=Télécharger et installer le pilote du Magic Trackpad (signé par Microsoft, connexion Internet requise)
fr.DriverGroup=Pilote :
fr.StartupTask=Lancer Trackpad Companion dans la zone de notification à l’ouverture de session
fr.OptionsGroup=Options :
fr.DriverStatus=Téléchargement et installation du pilote Magic Trackpad...
fr.DriverFail=Trackpad Companion a été installé, mais le pilote n’a pas pu l’être automatiquement.%n%nVous pouvez réessayer depuis le menu Démarrer (« Reinstall Magic Trackpad driver ») ou suivre les étapes manuelles du README.
fr.ReinstallDriver=Réinstaller le pilote Magic Trackpad
fr.StartApp=Lancer Trackpad Companion
de.DriverTask=Magic-Trackpad-Treiber herunterladen und installieren (von Microsoft signiert, Internetverbindung nötig)
de.DriverGroup=Treiber:
de.StartupTask=Trackpad Companion bei der Anmeldung im Tray starten
de.OptionsGroup=Optionen:
de.DriverStatus=Magic-Trackpad-Treiber wird heruntergeladen und installiert...
de.DriverFail=Trackpad Companion wurde installiert, aber der Treiber konnte nicht automatisch installiert werden.%n%nSie können es über das Startmenü erneut versuchen oder die manuellen Schritte in der README befolgen.
de.ReinstallDriver=Magic-Trackpad-Treiber neu installieren
de.StartApp=Trackpad Companion starten
es.DriverTask=Descargar e instalar el controlador del Magic Trackpad (firmado por Microsoft, requiere Internet)
es.DriverGroup=Controlador:
es.StartupTask=Iniciar Trackpad Companion en la bandeja al iniciar sesión
es.OptionsGroup=Opciones:
es.DriverStatus=Descargando e instalando el controlador del Magic Trackpad...
es.DriverFail=Trackpad Companion se instaló, pero no se pudo instalar el controlador automáticamente.%n%nPuedes reintentarlo desde el menú Inicio o seguir los pasos manuales del README.
es.ReinstallDriver=Reinstalar el controlador del Magic Trackpad
es.StartApp=Iniciar Trackpad Companion
ja.DriverTask=Magic Trackpad ドライバーをダウンロードしてインストールする（Microsoft 署名済み、インターネット接続が必要）
ja.DriverGroup=ドライバー:
ja.StartupTask=サインイン時に Trackpad Companion をトレイで起動する
ja.OptionsGroup=オプション:
ja.DriverStatus=Magic Trackpad ドライバーをダウンロードしてインストールしています...
ja.DriverFail=Trackpad Companion はインストールされましたが、ドライバーを自動でインストールできませんでした。%n%nスタートメニューから再試行するか、README の手動手順に従ってください。
ja.ReinstallDriver=Magic Trackpad ドライバーを再インストール
ja.StartApp=Trackpad Companion を起動
zhhans.DriverTask=下载并安装 Magic Trackpad 驱动程序（Microsoft 签名，需要联网）
zhhans.DriverGroup=驱动程序：
zhhans.StartupTask=登录时在托盘中启动 Trackpad Companion
zhhans.OptionsGroup=选项：
zhhans.DriverStatus=正在下载并安装 Magic Trackpad 驱动程序...
zhhans.DriverFail=Trackpad Companion 已安装，但无法自动安装驱动程序。%n%n您可以从“开始”菜单重试，或按 README 中的手动步骤操作。
zhhans.ReinstallDriver=重新安装 Magic Trackpad 驱动程序
zhhans.StartApp=启动 Trackpad Companion
pl.DriverTask=Pobierz i zainstaluj sterownik Magic Trackpad (podpisany przez Microsoft, wymaga internetu)
pl.DriverGroup=Sterownik:
pl.StartupTask=Uruchamiaj Trackpad Companion w zasobniku po zalogowaniu
pl.OptionsGroup=Opcje:
pl.DriverStatus=Pobieranie i instalowanie sterownika Magic Trackpad...
pl.DriverFail=Trackpad Companion został zainstalowany, ale nie udało się automatycznie zainstalować sterownika.%n%nMożesz spróbować ponownie z menu Start lub wykonać kroki ręczne z pliku README.
pl.ReinstallDriver=Zainstaluj ponownie sterownik Magic Trackpad
pl.StartApp=Uruchom Trackpad Companion
ru.DriverTask=Скачать и установить драйвер Magic Trackpad (подписан Microsoft, нужен интернет)
ru.DriverGroup=Драйвер:
ru.StartupTask=Запускать Trackpad Companion в трее при входе в систему
ru.OptionsGroup=Параметры:
ru.DriverStatus=Загрузка и установка драйвера Magic Trackpad...
ru.DriverFail=Trackpad Companion установлен, но драйвер не удалось установить автоматически.%n%nПовторите попытку из меню «Пуск» или выполните шаги из README вручную.
ru.ReinstallDriver=Переустановить драйвер Magic Trackpad
ru.StartApp=Запустить Trackpad Companion
ptbr.DriverTask=Baixar e instalar o driver do Magic Trackpad (assinado pela Microsoft, requer internet)
ptbr.DriverGroup=Driver:
ptbr.StartupTask=Iniciar o Trackpad Companion na bandeja ao entrar no Windows
ptbr.OptionsGroup=Opções:
ptbr.DriverStatus=Baixando e instalando o driver do Magic Trackpad...
ptbr.DriverFail=O Trackpad Companion foi instalado, mas não foi possível instalar o driver automaticamente.%n%nVocê pode tentar novamente pelo menu Iniciar ou seguir os passos manuais do README.
ptbr.ReinstallDriver=Reinstalar o driver do Magic Trackpad
ptbr.StartApp=Iniciar o Trackpad Companion
af.DriverTask=Laai die Magic Trackpad-drywer af en installeer dit (deur Microsoft onderteken, internet nodig)
af.DriverGroup=Drywer:
af.StartupTask=Begin Trackpad Companion in die skinkbord wanneer ek aanmeld
af.OptionsGroup=Opsies:
af.DriverStatus=Laai tans die Magic Trackpad-drywer af en installeer dit...
af.DriverFail=Trackpad Companion is geïnstalleer, maar die drywer kon nie outomaties geïnstalleer word nie.%n%nJy kan weer probeer vanaf die Start-kieslys of die handmatige stappe in die README volg.
af.ReinstallDriver=Installeer Magic Trackpad-drywer weer
af.StartApp=Begin Trackpad Companion

[Tasks]
Name: "driver"; Description: "{cm:DriverTask}"; GroupDescription: "{cm:DriverGroup}"; Check: not DriverPresent
Name: "startup"; Description: "{cm:StartupTask}"; GroupDescription: "{cm:OptionsGroup}"

[Files]
Source: "..\publish\*"; DestDir: "{app}"; Flags: recursesubdirs createallsubdirs ignoreversion
Source: "install-driver.ps1"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Trackpad Companion"; Filename: "{app}\TrackpadCompanion.exe"
Name: "{group}\{cm:ReinstallDriver}"; Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install-driver.ps1"""

[Run]
Filename: "schtasks.exe"; Parameters: "/Create /TN ""Trackpad Companion"" /TR ""\""{app}\TrackpadCompanion.exe\"" --tray"" /SC ONLOGON /RL HIGHEST /F"; Flags: runhidden; Tasks: startup
Filename: "{app}\TrackpadCompanion.exe"; Description: "{cm:StartApp}"; Flags: nowait postinstall skipifsilent

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
    WizardForm.StatusLabel.Caption := CustomMessage('DriverStatus');
    if (not Exec('powershell.exe',
         '-NoProfile -ExecutionPolicy Bypass -File "' + ExpandConstant('{app}\install-driver.ps1') + '"',
         '', SW_HIDE, ewWaitUntilTerminated, ResultCode)) or (ResultCode <> 0) then
      MsgBox(CustomMessage('DriverFail') + #13#10#13#10 + GetEnv('TEMP') + '\TrackpadCompanion-driver.log', mbInformation, MB_OK);
  end;
end;
