!include 'LogicLib.nsh'
!include "MUI2.nsh"
!include "x64.nsh"
!define APPNAME "Antidote Client"
!define COMPANYNAME "YHware"
!define DESCRIPTION "Antidote Client"
# These three must be integers
!define VERSIONMAJOR 0
!define VERSIONMINOR 2
!define VERSIONBUILD 7
# These will be displayed by the "Click here for support information" link in "Add/Remove Programs"
# It is possible to use "mailto:" links in here to open the email client
# This is the size (in kB) of all the files copied into "Program Files"
!define INSTALLSIZE 36000
!define MUI_ABORTWARNING
!define MUI_FINISHPAGE_RUN "$INSTDIR\Antidote.exe"

;--------------------------------
;Pages

  !insertmacro MUI_PAGE_LICENSE "License.txt"
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH	
  !insertmacro MUI_UNPAGE_CONFIRM
  
;--------------------------------

# set the name of the installer
Name "${COMPANYNAME} - ${APPNAME}"
Outfile "AntidoteClient_${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}.exe"
RequestExecutionLevel admin
InstallDir "$SYSDIR\antidote"
BrandingText "Antidote Client Installer"



Function CreateGUID
  System::Call 'ole32::CoCreateGuid(g .s)'
FunctionEnd


Function CheckAndDownloadDotNet

    DetailPrint 'Checking if .NET 4.7 is installed.'
	# Let's see if the user has the .NET Framework 4.5 installed on their system or not
	# Remember: you need Vista SP2 or 7 SP1.  It is built in to Windows 8, and not needed
	# In case you're wondering, running this code on Windows 8 will correctly return is_equal
	# or is_greater (maybe Microsoft releases .NET 4.5 SP1 for example)
 
	# Set up our Variables
	Var /GLOBAL dotNET47IsThere
	Var /GLOBAL dotNET_CMD_LINE
	Var /GLOBAL EXIT_CODE
 
        # We are reading a version release DWORD that Microsoft says is the documented
        # way to determine if .NET Framework 4.5 is installed
	ReadRegDWORD $dotNET47IsThere HKLM "SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full" "Release"
	IntCmp $dotNET47IsThere 393297 is_equal is_less is_greater
 
	is_equal:
		DetailPrint ".NET 4.7 is installed"
		Goto done_compare_not_needed
	is_greater:
		# Useful if, for example, Microsoft releases .NET 4.5 SP1
		# We want to be able to simply skip install since it's not
		# needed on this system
		DetailPrint ".NET 4.7 or later version of .NET is installed"
		Goto done_compare_not_needed
	is_less:
		DetailPrint ".NET 4.7 is NOT installed"
		Goto done_compare_needed
 
	done_compare_needed:
		#.NET Framework 4.6 install is *NEEDED*
 
		# Microsoft Download Center EXE:
		# Web Bootstrapper: http://go.microsoft.com/fwlink/?LinkId=225704
		# Full Download: http://go.microsoft.com/fwlink/?LinkId=225702
 
		# Setup looks for components\dotNET45Full.exe relative to the install EXE location
		# This allows the installer to be placed on a USB stick (for computers without internet connections)
		# If the .NET Framework 4.5 installer is *NOT* found, Setup will connect to Microsoft's website
		# and download it for you
 
		# Reboot Required with these Exit Codes:
		# 1641 or 3010
 
		# Command Line Switches:
		# /showrmui /passive /norestart
 
		# Silent Command Line Switches:
		# /q /norestart
 
 
		# Let's see if the user is doing a Silent install or not
		IfSilent is_quiet is_not_quiet
 
		is_quiet:
			StrCpy $dotNET_CMD_LINE "/q /norestart"
			Goto LookForLocalFile
		is_not_quiet:
			StrCpy $dotNET_CMD_LINE "/showrmui /passive /norestart"
			Goto LookForLocalFile
 
		LookForLocalFile:
			# Let's see if the user stored the Full Installer
			IfFileExists "$EXEPATH\components\dotNET472Full.exe" do_local_install do_network_install
 
			do_local_install:
				# .NET Framework found on the local disk.  Use this copy
 
				ExecWait '"$EXEPATH\components\dotNet472Full" $dotNET_CMD_LINE' $EXIT_CODE
				Goto is_reboot_requested
 
			# Now, let's Download the .NET
			do_network_install:
 
				Var /GLOBAL dotNetDidDownload
				NSISdl::download "http://go.microsoft.com/fwlink/?linkid=863265" "$TEMP\dotNet472Full.exe" $dotNetDidDownload
 
				StrCmp $dotNetDidDownload success fail
				success:
					ExecWait '"$TEMP\dotNet472Full.exe" $dotNET_CMD_LINE' $EXIT_CODE
					Goto is_reboot_requested
 
				fail:
					MessageBox MB_OK|MB_ICONEXCLAMATION "Unable to download .NET Framework.  ${PRODUCT_NAME} will be installed, but will not function without the Framework!"
					Goto done_dotNET_function
 
				# $EXIT_CODE contains the return codes.  1641 and 3010 means a Reboot has been requested
				is_reboot_requested:
					${If} $EXIT_CODE = 1641
					${OrIf} $EXIT_CODE = 3010
						SetRebootFlag true
					${EndIf}
 
	done_compare_not_needed:
		# Done dotNET Install
		DetailPrint ".NET Requirement satisfied"
		Goto done_dotNET_function
 
	#exit the function
	done_dotNET_function:
 
FunctionEnd



# create a default section.
Section "install"

	Call CheckAndDownloadDotNet

    SetOutPath $INSTDIR
	DetailPrint "Install dir is ... $INSTDIR"
    Call CreateGUID
    Pop $0
    DetailPrint $0
    ClearErrors
    ReadRegStr $1 HKLM "Software\Antidote" "COMPUTER_CODE"
    ${If} ${Errors}
	    DetailPrint 'Computer Code does not exist... making a new one'
    	WriteRegStr HKLM "Software\Antidote" "COMPUTER_CODE" "$0"
   	${EndIf}
	   
    ReadRegStr $1 HKLM "Software\Antidote" "INITIAL_SETUP_COMPLETE"
    ${If} ${Errors}
    	WriteRegStr HKLM "Software\Antidote" "INITIAL_SETUP_COMPLETE" "NO"
   	${EndIf}

    # Write the install directory location
	${If} ${RunningX64}
    	WriteRegStr HKLM "Software\Antidote" "INSTALL_DIR" "C:\Windows\SysWOW64\antidote"
	${Else}
    	WriteRegStr HKLM "Software\Antidote" "INSTALL_DIR" "$INSTDIR"
	${EndIf} 

    # Make antidote start on windows startup
	${If} ${RunningX64}
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote" "C:\Windows\SysWOW64\antidote\Antidote.exe"
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote Shield" "C:\Windows\SysWOW64\antidote\windote.exe"
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote Updater" "C:\Windows\SysWOW64\antidote\AntidoteUpdater.exe"
	${Else}	
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote" "$INSTDIR\Antidote.exe"
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote Shield" "$INSTDIR\windote.exe"
		WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Run" \
					"Antidote Updater" "$INSTDIR\AntidoteUpdater.exe"
	${EndIf} 
    WriteRegStr HKLM "Software\Antidote" \
                 "Version" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
	
    DetailPrint 'Installed registry'
    # create the uninstaller
    WriteUninstaller "$SYSDIR\dictuminitium.exe"
    # WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Antidote" \
                 "DisplayName" "349Control Program"
    # WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Antidote" \
                 "Publisher" "${COMPANYNAME}"
    # WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Antidote" \
                 "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONBUILD}"
	# WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Antidote" \
                 "UninstallString" "$\"$INSTDIR\uninstall_antidote.exe$\""
    # WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
	# WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
    # WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "EstimatedSize" ${INSTALLSIZE}


    File /r "C:\Users\Daniel\source\repos\Antidote\Antidote\bin\Release\*"
	CopyFiles "$INSTDIR\drivers\PhoenixDriver.sys" "$SYSDIR\drivers\"


	SetShellVarContext all
 
SectionEnd


# uninstaller section start
Section "uninstall"

	rmDir /r "$INSTDIR"

	# DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Antidote"
	SimpleSC::StopService $SERVICE_NAME
	SimpleSC::RemoveService $SERVICE_NAME
 
    # first, delete the uninstaller
    Delete "$SYSDIR\dictuminitium.exe"
 
 
# uninstaller section end
SectionEnd
