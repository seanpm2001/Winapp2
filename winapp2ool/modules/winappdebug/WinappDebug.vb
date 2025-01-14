﻿'    Copyright (C) 2018-2022 Hazel Ward
' 
'    This file is a part of Winapp2ool
' 
'    Winapp2ool is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    Winapp2ool is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with Winapp2ool.  If not, see <http://www.gnu.org/licenses/>.
Option Strict On
Imports System.Globalization
Imports System.Text.RegularExpressions
''' <summary> Observes, reports, and attempts to repair errors in winapp2.ini </summary>
Public Module WinappDebug
    ''' <summary> The winapp2.ini file that will be linted </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property winappDebugFile1 As New iniFile(Environment.CurrentDirectory, "winapp2.ini", mExist:=True)
    ''' <summary> The save path for the linted file. Overwrites the input file by default </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property winappDebugFile3 As New iniFile(Environment.CurrentDirectory, "winapp2.ini", "winapp2-debugged.ini")
    ''' <summary> Indicates that some but not all repairs will run </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property RepairSomeErrsFound As Boolean = False
    ''' <summary> Indicates that the scan settings have been modified from their defaults <br /> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property ScanSettingsChanged As Boolean = False
    ''' <summary> Indicates that the module settings have been modified from their defaults <br/> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property ModuleSettingsChanged As Boolean = False
    ''' <summary> Indicates that the any changes made by the linter should be saved back to disk <br/> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property SaveChanges As Boolean = False
    ''' <summary> Indicates that the linter should attempt to repair errors it finds <br/> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property RepairErrsFound As Boolean = True
    ''' <summary> The number of errors found during the lint </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property ErrorsFound As Integer = 0
    ''' <summary> The list of all entry names found during the lint, used to check for duplicates </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property allEntryNames As New strList
    ''' <summary> The winapp2ool logslice from the most recent Lint run </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property MostRecentLintLog As String = ""
    ''' <summary> The current rules for scans and repairs </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property Rules As New List(Of lintRule) From {
        New lintRule(True, True, "Casing", "improper CamelCasing", "fixing improper CamelCasing"),
        New lintRule(True, True, "Alphabetization", "improper alphabetization", "fixing improper alphabetization"),
        New lintRule(True, True, "Improper Numbering", "improper key numbering", "fixing improper key numbering"),
        New lintRule(True, True, "Parameters", "improper parameterization on FileKeys", "fixing improper parameterization on FileKeys"),
        New lintRule(True, True, "Flags", "improper FileKey/ExcludeKey flag formatting", "fixing improper FileKey/ExcludeKey flag formatting"),
        New lintRule(True, True, "Slashes", "improper use of slashes (\)", "fixing improper use of slashes (\)"),
        New lintRule(True, True, "Defaults", "Default=True", "enforcing no default key"),
        New lintRule(True, True, "Duplicates", "duplicate key values", "removing keys with duplicated values"),
        New lintRule(True, True, "Unneeded Numbering", "use of numbers where there should not be", "removing numbers used where they shouldn't be"),
        New lintRule(True, True, "Multiples", "multiples of key types that should only occur once in an entry", "removing unneeded multiples of key types that should occur only once"),
        New lintRule(True, True, "Invalid Values", "invalid key values", "fixing certain types of invalid key values"),
        New lintRule(True, True, "Syntax Errors", "some entries whose configuration will not run in CCleaner", "attempting to fix certain types of syntax errors"),
        New lintRule(True, True, "Path Validity", "invalid filesystem or registry locations", "attempting to repair some basic invalid parameters in paths"),
        New lintRule(True, True, "Semicolons", "improper use of semicolons (;)", "fixing some improper uses of semicolons(;)"),
        New lintRule(False, False, "Optimizations", "situations where keys can be merged (experimental)", "automatic merging of keys (experimental)"),
        New lintRule(False, False, "Potentially Duplicate Keys", "duplicated keys between multiple entries", "repair not yet supported")
    }
    ''' <summary> Controls scan/repairs for CamelCasing issues <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintCasing As lintRule = Rules(0)
    ''' <summary> Controls scan/repairs for alphabetization issues <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintAlpha As lintRule = Rules(1)
    ''' <summary> Controls scan/repairs for incorrectly numbered keys <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintWrongNums As lintRule = Rules(2)
    ''' <summary> Controls scan/repairs for parameters inside of FileKeys <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintParams As lintRule = Rules(3)
    ''' <summary> Controls scan/repairs for flags in ExcludeKeys and FileKeys <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintFlags As lintRule = Rules(4)
    ''' <summary> Controls scan/repairs for improper slash usage <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintSlashes As lintRule = Rules(5)
    ''' <summary> Controls scan/repairs for missing or True Default values <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintDefaults As lintRule = Rules(6)
    ''' <summary> Controls scan/repairs for duplicate values <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintDupes As lintRule = Rules(7)
    ''' <summary> Controls scan/repairs for keys with numbers they shouldn't have <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintExtraNums As lintRule = Rules(8)
    ''' <summary> Controls scan/repairs for keys which should only occur once <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintMulti As lintRule = Rules(9)
    ''' <summary> Controls scan/repairs for keys with invlaid values <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintInvalid As lintRule = Rules(10)
    ''' <summary> Controls scan/repairs for winapp2.ini syntax errors <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintSyntax As lintRule = Rules(11)
    ''' <summary> Controls scan/repairs for invalid file or regsitry paths <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintPathValidity As lintRule = Rules(12)
    ''' <summary> Controls scan/repairs for improper use of semicolons <br /> Default: <c> True </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintSemis As lintRule = Rules(13)
    ''' <summary> Controls scan/repairs for keys that can be merged into eachother (FileKeys only currently) <br /> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Property lintOpti As lintRule = Rules(14)
    ''' <summary> Controls scan/repairs for keys that may possibly exist in more than one entry <br /> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property lintMutliDupe As lintRule = Rules(15)

    ''' <summary> Regex to detect long form registry paths </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property longReg As New Regex("HKEY_(C(URRENT_(USER$|CONFIG$)|LASSES_ROOT$)|LOCAL_MACHINE$|USERS$)")
    ''' <summary> Regex to detect short form registry paths </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property shortReg As New Regex("HK(C(C$|R$|U$)|LM$|U$)")
    ''' <summary> Regex to detect valid LangSecRef numbers </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property secRefNums As New Regex("30(0([1-6])|2([1-9])|3([0-8]))")
    ''' <summary> Regex to detect valid drive letter parameters </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property driveLtrs As New Regex("[a-zA-z]:")
    ''' <summary> Regex to detect potential %EnvironmentVariables% </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property envVarRegex As New Regex("%[A-Za-z0-9]*%")
    ''' <summary> Indicates that Default keys should have their values auited instead of being considered invalid for existing <br /> Default: <c> False </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property overrideDefaultVal As Boolean = False
    ''' <summary> The expected value for Default keys when auditing their values <br/> Default: <c> Faalse </c> </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Property expectedDefaultValue As Boolean = False

    ''' <summary> Handles the commandline args for <c> WinappDebug </c> <br />
    ''' WinappDebug commandline args: <br />
    ''' <c> -c </c> enable saving of changes made by the linter
    ''' </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Sub handleCmdLine()
        initDefaultSettings()
        invertSettingAndRemoveArg(SaveChanges, "-c")
        getFileAndDirParams(winappDebugFile1, New iniFile, winappDebugFile3)
        ' Ensure that when we're establishing the ability of the unit tester to set the lint stage that we don't then also run the linter
        If Not cmdargs.Contains("UNIT_TESTING_HALT") Then initDebug()
    End Sub

    ''' <summary> Restore the default state of all of the module's parameters, undoing any changes the user may have made to them </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub initDefaultSettings()
        winappDebugFile1.resetParams()
        winappDebugFile3.resetParams()
        ModuleSettingsChanged = False
        RepairErrsFound = True
        SaveChanges = False
        overrideDefaultVal = False
        expectedDefaultValue = False
        resetScanSettings()
        restoreDefaultSettings(NameOf(WinappDebug), AddressOf createLintSettingsSection)
    End Sub

    ''' <summary> Loads the WinappDebug settings from disk and loads them into memory, overriding the default settings </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Sub getSeralizedLintSettings()
        If Not readSettingsFromDisk Then Return
        For Each kvp In settingsDict(NameOf(WinappDebug))
            Dim lints As New List(Of String) From {"Casing", "Alphabetization", "Improper Numbering", "Parameters", "Flags", "Slashes", "Defaults", "Duplicates", "Unneeded Numbering",
                "Multiples", "Invalid Values", "Syntax Errors", "Path Validity", "Semicolons", "Optimizations", "Potential Duplicate Keys Between Entries"}
            Select Case kvp.Key
                Case NameOf(winappDebugFile1) & "_Dir"
                    winappDebugFile1.Dir = kvp.Value
                Case NameOf(winappDebugFile1) & "_Name"
                    winappDebugFile1.Name = kvp.Value
                Case NameOf(winappDebugFile3) & "_Dir"
                    winappDebugFile3.Dir = kvp.Value
                Case NameOf(winappDebugFile3) & "_Name"
                    winappDebugFile3.Name = kvp.Value
                Case NameOf(RepairSomeErrsFound)
                    RepairSomeErrsFound = CBool(kvp.Value)
                Case NameOf(ScanSettingsChanged)
                    ScanSettingsChanged = CBool(kvp.Value)
                Case NameOf(ModuleSettingsChanged)
                    ModuleSettingsChanged = CBool(kvp.Value)
                Case NameOf(SaveChanges)
                    SaveChanges = CBool(kvp.Value)
                Case NameOf(RepairErrsFound)
                    RepairErrsFound = CBool(kvp.Value)
                Case NameOf(overrideDefaultVal)
                    overrideDefaultVal = CBool(kvp.Value)
                Case NameOf(expectedDefaultValue)
                    expectedDefaultValue = CBool(kvp.Value)
                Case Else
                    Dim lintType = kvp.Key.Replace("_Scan", "")
                    lintType = lintType.Replace("_Repair", "")
                    Dim ind = lints.IndexOf(lintType)
                    ' Don't crash if we rename a setting, just silently fail use the new setting with its default value. The lingering will be removed if 
                    ' entry in winapp2ool.ini will be removed if the user resets the module settings 
                    Try
                        If kvp.Key.Contains("_Scan") Then Rules(ind).ShouldScan = CBool(kvp.Value)
                        If kvp.Key.Contains("_Repair") Then Rules(ind).ShouldRepair = CBool(kvp.Value)
                    Catch ex As ArgumentOutOfRangeException
                        gLog($"{kvp.Key} doesn't seem to be an actual setting, perhaps it is misnamed or the setting name has changed. This value will be ignored")
                    End Try
            End Select
        Next
    End Sub

    '''<summary> Adds the current (typically default) state of the module's settings into the disk-writable settings representation </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Sub createLintSettingsSection()
        Dim lints As New List(Of String) From {"Casing", "Alphabetization", "Improper Numbering", "Parameters", "Flags", "Slashes", "Defaults", "Duplicates", "Unneeded Numbering",
                "Multiples", "Invalid Values", "Syntax Errors", "Path Validity", "Semicolons", "Optimizations", "Potential Duplicate Keys Between Entries"}
        Dim settingsKeys As New List(Of String) From {
            NameOf(RepairSomeErrsFound), tsInvariant(RepairSomeErrsFound), NameOf(ScanSettingsChanged), tsInvariant(ScanSettingsChanged), NameOf(ModuleSettingsChanged), tsInvariant(ModuleSettingsChanged),
            NameOf(SaveChanges), tsInvariant(SaveChanges), NameOf(RepairErrsFound), tsInvariant(RepairErrsFound), NameOf(overrideDefaultVal), tsInvariant(overrideDefaultVal),
            NameOf(expectedDefaultValue), tsInvariant(expectedDefaultValue)}
        For i = 0 To lints.Count - 1
            settingsKeys.Add($"{lints(i)}_Scan")
            settingsKeys.Add(tsInvariant(Rules(i).ShouldScan))
            settingsKeys.Add($"{lints(i)}_Repair")
            settingsKeys.Add(tsInvariant(Rules(i).ShouldRepair))
        Next
        settingsKeys.AddRange({NameOf(winappDebugFile1), winappDebugFile1.Name, winappDebugFile1.Dir, NameOf(winappDebugFile3), winappDebugFile3.Name, winappDebugFile3.Dir})
        createModuleSettingsSection(NameOf(WinappDebug), settingsKeys, 39, 2)
    End Sub

    ''' <summary> Displays the <c> WinappDebug </c> menu to the user </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-16
    Public Sub printMenu()
        printMenuTop({"Scan winapp2.ini for style and syntax errors, and attempt to repair them where possible."})
        print(1, "Run (Default)", "Run the debugger")
        print(1, "File Chooser (winapp2.ini)", "Choose a different file name or path for winapp2.ini", leadingBlank:=True, trailingBlank:=True)
        print(5, "Toggle Saving", "saving the file after correcting errors", enStrCond:=SaveChanges)
        print(1, "File Chooser (save)", "Save a copy of changes made to a new file instead of overwriting winapp2.ini", SaveChanges, trailingBlank:=True)
        print(1, "Toggle Scan Settings", "Enable or disable individual scan and correction routines", leadingBlank:=Not SaveChanges, trailingBlank:=True)
        print(5, "Toggle Default Value Audit", "enforcing a specific value for Default keys", enStrCond:=overrideDefaultVal, trailingBlank:=Not overrideDefaultVal)
        print(1, "Toggle Expected Default", $"Currently enforcing that Default keys have a value of: {expectedDefaultValue}", trailingBlank:=True, cond:=overrideDefaultVal)
        print(0, $"Current winapp2.ini:  {replDir(winappDebugFile1.Path)}", closeMenu:=Not SaveChanges And Not ModuleSettingsChanged And MostRecentLintLog.Length = 0)
        print(0, $"Current save target:  {replDir(winappDebugFile3.Path)}", cond:=SaveChanges, closeMenu:=Not ModuleSettingsChanged And MostRecentLintLog.Length = 0)
        print(2, NameOf(WinappDebug), cond:=ModuleSettingsChanged, closeMenu:=MostRecentLintLog.Length = 0)
        print(1, "Log Viewer", "Show the most recent lint results", cond:=Not MostRecentLintLog.Length = 0, closeMenu:=True, leadingBlank:=True)
    End Sub

    ''' <summary> Handles the user's input from the menu </summary>
    ''' <param name="input"> The String containing the user's input </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Sub handleUserInput(input As String)
        If input Is Nothing Then argIsNull(NameOf(input)) : Return
        Dim saveOrOverride = SaveChanges Or overrideDefaultVal
        Dim saveXorOverride = SaveChanges Xor overrideDefaultVal
        Dim saveAndOverride = SaveChanges And overrideDefaultVal
        Select Case True
            Case input = "0"
                exitModule()
            Case input = "1" Or input.Length = 0
                initDebug()
            Case input = "2"
                changeFileParams(winappDebugFile1, ModuleSettingsChanged, NameOf(WinappDebug), NameOf(winappDebugFile1), NameOf(ModuleSettingsChanged))
            Case input = "3"
                toggleSettingParam(SaveChanges, "Saving", ModuleSettingsChanged, NameOf(WinappDebug), NameOf(SaveChanges), NameOf(ModuleSettingsChanged))
            Case input = "4" And SaveChanges
                changeFileParams(winappDebugFile3, ModuleSettingsChanged, NameOf(WinappDebug), NameOf(winappDebugFile3), NameOf(ModuleSettingsChanged))
            Case (input = "4" And Not SaveChanges) Or (input = "5" And SaveChanges)
                initModule("Scan Settings", AddressOf advSettings.printMenu, AddressOf advSettings.handleUserInput)
                Console.WindowHeight = 30
            Case (input = "5" And Not SaveChanges) Or (input = "6" And SaveChanges)
                toggleSettingParam(overrideDefaultVal, "Default Value Overriding", ModuleSettingsChanged, NameOf(WinappDebug), NameOf(overrideDefaultVal), NameOf(ModuleSettingsChanged))
            Case ModuleSettingsChanged And ((input = "6" And Not saveOrOverride) Or (input = "7" And saveXorOverride) Or (input = "8" And saveAndOverride))
                resetModuleSettings("WinappDebug", AddressOf initDefaultSettings)
            Case Not MostRecentLintLog.Length = 0 And (input = "6" And Not ModuleSettingsChanged) Or
                                        ModuleSettingsChanged And ((input = "7" And (Not saveOrOverride) Or
                                        (input = "8" And saveXorOverride) Or (input = "9" And saveAndOverride)))
                printSlice(MostRecentLintLog)
            Case overrideDefaultVal And (input = "6" And Not SaveChanges) Or (input = "7" And SaveChanges)
                toggleSettingParam(expectedDefaultValue, "Expected Default Value", ModuleSettingsChanged, NameOf(WinappDebug), NameOf(expectedDefaultValue), NameOf(ModuleSettingsChanged))
            Case Else
                setHeaderText(invInpStr, True)
        End Select
    End Sub

    ''' <summary> Validates winapp2.ini, then sets up the output window before sending it off to the linter.
    ''' After linting, reports the results of the lint to the user </summary>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub initDebug()
        ' Abort the scan if winapp2.ini is empty or not found
        If Not enforceFileHasContent(winappDebugFile1) Then Return
        Dim wa2 As New winapp2file(winappDebugFile1)
        clrConsole()
        print(3, "Beginning analysis of winapp2.ini", trailr:=True)
        gLog("Beginning lint", leadr:=True, ascend:=True)
        MostRecentLintLog = ""
        debug(wa2)
        gLog(descend:=True)
        gLog("Lint complete")
        setHeaderText("Lint complete")
        print(4, "Completed analysis of winapp2.ini", conjoin:=True)
        print(0, $"{ErrorsFound} possible errors were detected.")
        print(0, $"Number of entries {winappDebugFile1.Sections.Count}", trailingBlank:=True)
        rewriteChanges(wa2)
        print(0, anyKeyStr, closeMenu:=True)
        crk()
    End Sub

    ''' <summary> Sends the entries in a winapp2.ini format <c> iniFile </c> into specific format and syntax checking routines </summary>
    ''' <param name="fileToBeDebugged"> A <c> winapp2file </c> to be linted </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Sub debug(ByRef fileToBeDebugged As winapp2file)
        ' Abort the scan if we somehow get a null reference winapp2.ini 
        If fileToBeDebugged Is Nothing Then argIsNull(NameOf(fileToBeDebugged)) : Return
        ErrorsFound = 0
        allEntryNames = New strList
        gLog(ascend:=True)
        For Each entryList In fileToBeDebugged.Winapp2entries
            If entryList.Count = 0 Then Continue For
            entryList.ForEach(Sub(entry) processEntry(entry))
        Next
        fileToBeDebugged.rebuildToIniFiles()
        alphabetizeEntries(fileToBeDebugged)
    End Sub

    ''' <summary> Validates the basic structure of a <c> winapp2entry </c> and sends off its individual keys for more specific analysis </summary>
    ''' <param name="entry"> A <c> winapp2entry </c> to be audited for syntax errors </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2022-01-11
    Private Sub processEntry(ByRef entry As winapp2entry)
        gLog($"Processing entry {entry.Name}", buffr:=True)
        Dim hasFileExcludes = False
        Dim hasRegExcludes = False
        ' Check for duplicate names that are differently cased 
        fullNameErr(allEntryNames.chkDupes(entry.Name), entry, "Duplicate entry name detected")
        ' Check that the entry is named properly 
        fullNameErr(Not entry.Name.EndsWith(" *", StringComparison.InvariantCulture), entry, "All entries must end in ' *'")
        ' Confirm the validity of keys and remove any broken ones before continuing
        validateKeys(entry)
        ' Process the entry's keylists in winapp2.ini order (ignore the last list because it has only errors)
        For Each lst In entry.KeyListList
            If lst.KeyType = "Error" Then Continue For
            Select Case lst.KeyType
                Case "DetectFile"
                    processKeyList(lst, AddressOf pDetectFile)
                Case "FileKey"
                    processKeyList(lst, AddressOf pFileKey)
                Case Else
                    processKeyList(lst, AddressOf voidDelegate, hasFileExcludes, hasRegExcludes)
            End Select
        Next
        ' Make sure we only have LangSecRef if we have LangSecRef at all
        fullNameErr(entry.LangSecRef.KeyCount <> 0 And entry.SectionKey.KeyCount <> 0 And lintSyntax.ShouldScan, entry, "Section key found alongside LangSecRef key, but only one should be present")
        ' Make sure we have a LangSecRef or a Section 
        fullNameErr(entry.LangSecRef.KeyCount = 0 And entry.SectionKey.KeyCount = 0 And lintSyntax.ShouldScan, entry, "Entry has no valid classifier key (LangSecRef, Section)")
        ' Make sure we have at least 1 valid detect key and at least one valid cleaning key
        fullNameErr(entry.DetectOS.KeyCount + entry.Detects.KeyCount + entry.SpecialDetect.KeyCount + entry.DetectFiles.KeyCount = 0, entry, "Entry has no valid detection keys (Detect, DetectFile, DetectOS, SpecialDetect)")
        fullNameErr(entry.FileKeys.KeyCount + entry.RegKeys.KeyCount = 0 And lintSyntax.ShouldScan, entry, "Entry has no valid FileKeys or RegKeys")
        ' If we don't have FileKeys or RegKeys, we shouldn't have ExcludeKeys.
        fullNameErr(entry.ExcludeKeys.KeyCount > 0 And entry.FileKeys.KeyCount + entry.RegKeys.KeyCount = 0, entry, "Entry has ExcludeKeys but no valid FileKeys or RegKeys")
        ' Make sure that if we have excludes, we also have corresponding file/reg keys
        fullNameErr(entry.FileKeys.KeyCount = 0 And hasFileExcludes, entry, "ExcludeKeys targeting filesystem locations found without any corresponding FileKeys")
        fullNameErr(entry.RegKeys.KeyCount = 0 And hasRegExcludes, entry, "ExcludeKeys targeting registry locations found without any corresponding RegKeys")
        ' Make sure that if we have a Default key and, it holds the desired value value
        If overrideDefaultVal Then
            Dim expected = tsInvariant(expectedDefaultValue)
            If entry.DefaultKey.KeyCount > 0 Then
                Dim key = entry.DefaultKey.Keys(0)
                fullKeyErr(key, "Incorrect value for Default Key found", Not key.Value = expected, lintDefaults.fixFormat, key.Value, expected)
            Else
                fullNameErr(True, entry, "No Default Key found")
                entry.DefaultKey.add(New iniKey($"Default={expected}"))
            End If
        End If
        fullNameErr(entry.DefaultKey.KeyCount > 0 And lintDefaults.ShouldScan And Not overrideDefaultVal, entry, "Entry has a Default key where there should be none")
        If lintDefaults.fixFormat And entry.DefaultKey.KeyCount > 0 And Not overrideDefaultVal Then entry.DefaultKey.Keys.Clear()
        resetMasterKeyLists()
        gLog($"Finished processing {entry.Name}", buffr:=True)
    End Sub

    ''' <summary> Checks the basic structure of all <c>iniKeys </c> in a <c> winapp2entry </c>,
    ''' attempts to repair some keys, and removes any that are too problematic to continue with </summary>
    ''' <param name="entry"> A <c> winapp2entry </c> whose <c> iniKeys </c> will be audited for basic syntax correctness </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub validateKeys(ByRef entry As winapp2entry)
        For Each lst In entry.KeyListList
            Dim brokenKeys As New keyList
            lst.Keys.ForEach(Sub(key) brokenKeys.add(key, Not cValidity(key)))
            lst.remove(brokenKeys.Keys)
            entry.ErrorKeys.remove(brokenKeys.Keys)
        Next
        ' Attempt to assign keys that had errors to their intended lists
        Dim toRemove As New keyList
        For Each key In entry.ErrorKeys.Keys
            For Each lst In entry.KeyListList
                If lst.KeyType = "Error" Then Continue For
                lst.add(key, key.typeIs(lst.KeyType))
                toRemove.add(key, key.typeIs(lst.KeyType))
            Next
        Next
        ' Remove any repaired keys from the keylist containing only errors 
        entry.ErrorKeys.remove(toRemove.Keys)
    End Sub

    ''' <summary> Alphabetizes all the entries in a winapp2.ini file and observes any that were out of place </summary>
    ''' <param name="winapp"> The <c> winapp2file </c> whose entries will be alphabetized </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub alphabetizeEntries(ByRef winapp As winapp2file)
        For Each innerFile In winapp.EntrySections
            Dim unsortedEntryList = innerFile.namesToStrList
            Dim sortedEntryList = sortEntryNames(innerFile)
            If lintAlpha.ShouldScan Then findOutOfPlace(unsortedEntryList, sortedEntryList, "Entry", innerFile.getLineNumsFromSections)
            If lintAlpha.fixFormat Then innerFile.sortSections(sortedEntryList)
        Next
    End Sub

    ''' <summary> Writes any changes made during the lint back to disk, correcting any errors that were found and repaired </summary>
    ''' <param name="winapp2file"> The <c> winapp2file </c> that was linted </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub rewriteChanges(ByRef winapp2file As winapp2file)
        If SaveChanges Then
            print(0, "Saving changes, do not close winapp2ool or data loss may occur...", leadingBlank:=True)
            winappDebugFile3.overwriteToFile(winapp2file.winapp2string)
            print(0, "Finished saving changes.", trailingBlank:=True)
        End If
    End Sub

    ''' <summary> Assess a list and its sorted state to observe changes in neighboring strings, such as the changes 
    ''' made while sorting the strings alphabetically </summary>
    ''' <param name="someList"> An unsorted list of strings (iniKey values or iniSection names) </param>
    ''' <param name="sortedList"> The sorted state of <c> <paramref name="someList"/> </c> </param>
    ''' <param name="findType"> The type of neighbor checking <br/> <br/> When checking iniKeys (as opposed to entries), <paramref name="findType"/> contains a <c> keyType </c> </param>
    ''' <param name="LineCountList"> The line numbers associated with the lines in <c> <paramref name="someList"/> </c> </param>
    ''' <param name="oopBool"> Indicates that there are out of place entries in the list <br/>  Optional, Default: <c> False </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub findOutOfPlace(ByRef someList As strList, ByRef sortedList As strList, ByVal findType As String, ByRef LineCountList As List(Of Integer), Optional ByRef oopBool As Boolean = False)
        ' Only try to find out of place keys when there's more than one
        If someList.Count > 1 Then
            Dim misplacedEntries As New strList
            ' Learn the neighbors of each string in each respective list
            Dim initialNeighbors = someList.getNeighborList
            Dim sortedNeighbors = sortedList.getNeighborList
            ' Make sure at least one of the neighbors of each string are the same in both the sorted and unsorted state, otherwise the string has moved 
            For i = 0 To someList.Count - 1
                Dim sind = sortedList.Items.IndexOf(someList.Items(i))
                misplacedEntries.add(someList.Items(i), Not (initialNeighbors(i).Key = sortedNeighbors(sind).Key And initialNeighbors(i).Value = sortedNeighbors(sind).Value))
            Next
            ' Report any misplaced entries back to the user
            For Each entry In misplacedEntries.Items
                Dim recInd = someList.indexOf(entry)
                Dim sortInd = sortedList.indexOf(entry)
                Dim curLine = LineCountList(recInd)
                Dim sortLine = LineCountList(sortInd)
                If (recInd = sortInd Or curLine = sortLine) Then Continue For
                entry = If(findType = "Entry", entry, $"{findType & recInd + 1}={entry}")
                If Not oopBool Then oopBool = True
                customErr(LineCountList(recInd), $"{findType} alphabetization", {$"{entry} appears to be out of place", $"Current line: {curLine}", $"Expected line: {sortLine}"})
            Next
        End If
    End Sub

    ''' <summary> Hands off each <c> iniKey </c> in a winapp2.ini format <c> keyList </c> to be audited for correctness </summary>
    ''' <param name="kl"> A <c> keyList </c> of a particular <c> keyType </c> to be audited </param>
    ''' <param name="processKey"> The <c> function </c> that audits the keys of the <c> KeyType </c> provided in <c> <paramref name="kl"/> </c> <br/> 
    ''' <c> VoidDelegate </c> if no further operations are needed outside of the basic formatting checks </param>
    ''' <param name="hasF"> Indicates that the ExcludeKeys contain file system locations <br/> Optional, Default: <c> False </c> </param>
    ''' <param name="hasR"> Indicates that the ExcludeKeys contain registry locations <br/> Optional, Default: <c> False </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub processKeyList(ByRef kl As keyList, processKey As Func(Of iniKey, iniKey), Optional ByRef hasF As Boolean = False, Optional ByRef hasR As Boolean = False)
        ' Don't process empty keyLists 
        If kl.KeyCount = 0 Then Return
        gLog($"Processing {kl.KeyType}s", ascend:=True, buffr:=True)
        Dim curNum = 1
        Dim curStrings As New strList
        Dim dupes As New keyList
        Dim kt = kl.KeyType
        For Each key In kl.Keys
            Select Case kt
                Case "ExcludeKey"
                    cFormat(key, curNum, curStrings, dupes)
                    pExcludeKey(key, hasF, hasR)
                Case "Detect", "DetectFile"
                    If key.typeIs("Detect") Then chkPathFormatValidity(key, True)
                    cFormat(key, curNum, curStrings, dupes, kl.KeyCount = 1)
                Case "RegKey"
                    chkPathFormatValidity(key, True)
                    cFormat(key, curNum, curStrings, dupes)
                    If lintMutliDupe.ShouldScan Then cDuplicateKeysBetweenEntries(key)
                Case "Warning", "DetectOS", "SpecialDetect", "LangSecRef", "Section", "Default"
                    ' No keys of these types should occur more than once per entry
                    If curNum > 1 And lintMulti.ShouldScan Then
                        fullKeyErr(key, $"Multiple {key.KeyType} detected.")
                        dupes.add(key, lintMulti.fixFormat)
                    End If
                    cFormat(key, curNum, curStrings, dupes, True)
                    ' Scan for invalid values in LangSecRef and SpecialDetect
                    If key.typeIs("SpecialDetect") Then chkCasing(key, {"DET_CHROME", "DET_MOZILLA", "DET_THUNDERBIRD", "DET_OPERA"}, key.Value, False)
                    fullKeyErr(key, "LangSecRef holds an invalid value.", lintInvalid.ShouldScan And key.typeIs("LangSecRef") And Not secRefNums.IsMatch(key.Value))
                Case Else
                    cFormat(key, curNum, curStrings, dupes)
            End Select
            ' Any further changes to the key are handled by the given function
            key = processKey(key)
        Next
        ' Remove any duplicates and sort the keys
        kl.remove(dupes.Keys)
        sortKeys(kl, dupes.KeyCount > 0)
        ' Run optimization checks on FileKey lists only 
        If kl.typeIs("FileKey") And lintOpti.ShouldScan Then cOptimization(kl)
        gLog(descend:=True)
    End Sub

    ''' <summary> This function does nothing by design, used when a method or function expects to be passed a function 
    ''' who modifies and iniKey on a KeyType where we don't want to modify the keys </summary>
    ''' <param name="key"> An <c> iniKey </c> to do nothing with </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Function voidDelegate(key As iniKey) As iniKey
        Return key
    End Function

    ''' <summary> Does some basic formatting checks that apply to all winapp2.ini format <c> iniKeys </c> </summary>
    ''' <param name="key"> An <c> iniKey </c> whose format will be audited </param>
    ''' <param name="keyNumber"> The current expected key number for numbered keys </param>
    ''' <param name="keyValues"> The current list of observed <c> iniKey </c> values </param>
    ''' <param name="dupeList"> A tracking list of <c> iniKeys </c> with duplicate values </param>
    ''' <param name="noNumbers"> Indicates that the current set of keys should not be numbered </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub cFormat(ByRef key As iniKey, ByRef keyNumber As Integer, ByRef keyValues As strList, ByRef dupeList As keyList, Optional noNumbers As Boolean = False)
        ' Check for duplicates
        If keyValues.contains(key.Value, True) Then
            Dim dupeKeyStr = $"{key.KeyType}{If(Not noNumbers, (keyValues.Items.IndexOf(key.Value) + 1).ToString(Globalization.CultureInfo.InvariantCulture), "")}={key.Value}"
            If lintDupes.ShouldScan Then customErr(key.LineNumber, "Duplicate key value found", {$"Key:            {key.toString}", $"Duplicates:     {dupeKeyStr}"})
            dupeList.add(key, lintDupes.fixFormat)
        Else
            keyValues.add(key.Value)
        End If
        ' Check for both types of numbering errors (incorrect and unneeded) 
        Dim hasNumberingError = If(noNumbers, Not key.nameIs(key.KeyType), Not key.nameIs(key.KeyType & keyNumber))
        Dim numberingErrStr = If(noNumbers, "Detected unnecessary numbering.", $"{key.KeyType} entry is incorrectly numbered.")
        Dim fixedStr = If(noNumbers, key.KeyType, key.KeyType & keyNumber)
        gLog($"Input mismatch error in {key.toString}", hasNumberingError, indent:=True)
        inputMismatchErr(key.LineNumber, numberingErrStr, key.Name, fixedStr, If(noNumbers, lintExtraNums.ShouldScan, lintWrongNums.ShouldScan) And hasNumberingError)
        fixStr(If(noNumbers, lintExtraNums.fixFormat, lintWrongNums.fixFormat) And hasNumberingError, key.Name, fixedStr)
        ' Scan for and fix any use of incorrect slashes (except in Warning keys) or trailing semicolons
        fullKeyErr(key, "Forward slash (/) detected in lieu of backslash (\).", Not (key.typeIs("Warning") Or key.typeIs("RegKey")) And lintSlashes.ShouldScan And key.vHas("/"),
                                                                                                        lintSlashes.fixFormat, key.Value, key.Value.Replace("/", "\"))
        fullKeyErr(key, "Trailing semicolon (;).", key.toString.Last = CChar(";") And lintSemis.ShouldScan, lintSemis.fixFormat, key.Value, key.Value.TrimEnd(CChar(";")))
        ' Do some formatting checks for environment variables if needed
        If {"FileKey", "ExcludeKey", "DetectFile"}.Contains(key.KeyType) Then cEnVar(key)
        keyNumber += 1
    End Sub

    ''' <summary> Validates the formatting of any %EnvironmentVariables% in a given <c> iniKey </c> </summary>
    ''' <param name="key">The <c> iniKey </c> whose data will be audited for environment variable correctness </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub cEnVar(key As iniKey)
        ' Valid Environmental Variables for winapp2.ini
        Dim enVars = {"AllUsersProfile", "AppData", "CommonAppData", "CommonProgramFiles",
        "Documents", "HomeDrive", "LocalAppData", "LocalLowAppData", "Music", "Pictures", "ProgramData", "ProgramFiles", "Public",
        "RootDir", "SystemDrive", "SystemRoot", "Temp", "Tmp", "UserName", "UserProfile", "Video", "WinDir"}
        fullKeyErr(key, "%EnvironmentVariables% must be surrounded on both sides by a single '%' character.", key.vHas("%") And envVarRegex.Matches(key.Value).Count = 0)
        For Each m As Match In envVarRegex.Matches(key.Value)
            Dim strippedText = m.ToString.Trim(CChar("%"))
            chkCasing(key, enVars, strippedText, False)
        Next
        ' Environment variables should be trailed by a backslash 
        fullKeyErr(key, "Missing backslash (\) after %EnvironmentVariable%.", lintSlashes.ShouldScan And key.vHas("%") And Not key.vHasAny({"%|", "%\"}))
    End Sub

    ''' <summary> Attempts to insert missing equal signs (=) into <c> iniKeys </c> <br/> <br/> Returns <c> True </c> if the repair is 
    '''  successful, <c> False </c> otherwise </summary>
    ''' <param name="key"> A misformatted <c> iniKey </c> to attempt to repair </param>
    ''' <param name="cmds"> An array containing valid winapp2.ini <c> keyTypes </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Function fixMissingEquals(ByRef key As iniKey, cmds As String()) As Boolean
        gLog("Attempting missing equals repair", ascend:=True)
        For Each cmd In cmds
            If key.Name.ToUpperInvariant.Contains(cmd.ToUpperInvariant) Then
                Select Case cmd
                ' We don't expect numbers in these keys
                    Case "Default", "DetectOS", "Section", "LangSecRef", "Section", "SpecialDetect"
                        key.Value = key.Name.Replace(cmd, "")
                        key.Name = cmd
                        key.KeyType = cmd
                    Case Else
                        Dim newName = cmd
                        Dim withNums = key.Name.Replace(cmd, "")
                        For Each c As Char In withNums.ToCharArray
                            If Char.IsNumber(c) Then newName += c : Else Exit For
                        Next
                        key.Value = key.Name.Replace(newName, "")
                        key.Name = newName
                        key.KeyType = cmd
                End Select
                gLog($"Repair complete. Result: {key.toString}", indent:=True, descend:=True)
                ' Don't allow valueless keys in winapp2.ini 
                If key.Value.Length = 0 Then gLog("Repair failed, key will be removed.", descend:=True) : Return False
                Return True
            End If
        Next
        ' Return false if no valid command is found
        gLog("Repair failed, key will be removed.", descend:=True)
        Return False
    End Function

    ''' <summary> Does basic syntax and formatting audits that apply across all keys, returns <c> False </c> 
    ''' if a key is malformed or if a null argument is given </summary>
    ''' <param name="key"> A <c> iniKey </c> whose basic syntactic validity will be assessed </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Function cValidity(key As iniKey) As Boolean
        If key Is Nothing Then argIsNull(NameOf(key)) : Return False
        Dim validCmds = {"Default", "DetectOS", "DetectFile", "Detect", "ExcludeKey",
                        "FileKey", "LangSecRef", "RegKey", "Section", "SpecialDetect", "Warning"}
        ' Attempt to fix the case where keys are missing an equal sign to delineate name and value 
        If key.typeIs("DeleteMe") Then
            gLog($"Broken Key Found: {key.Name}", indent:=True, ascend:=True)
            ' If we didn't find a fixable situation, delete the key
            Dim fixedMsngEq = fixMissingEquals(key, validCmds)
            If Not fixedMsngEq Then customErr(key.LineNumber, $"{key.Name} is missing a '=' or was not provided with a value. It will be deleted.", Array.Empty(Of String)()) : Return False
            fullKeyErr(key, "Missing '=' detected and repaired in key.", fixedMsngEq)
        End If
        ' Remove any instances of double backlashes because we don't expect them 
        If key.vHas("\\", True) Then
            fullKeyErr(key, "Extraneous backslashes (\\) detected", lintSlashes.ShouldScan)
            While (key.Value.Contains("\\") And lintSlashes.fixFormat)
                key.Value = key.Value.Replace("\\", "\")
            End While
        End If
        ' Check for leading or trailing whitespace, do this always as spaces in the name interfere with proper keyType identification
        If key.Name.StartsWith(" ", StringComparison.InvariantCulture) Or key.Name.EndsWith(" ", StringComparison.InvariantCulture) Or
            key.Value.StartsWith(" ", StringComparison.InvariantCulture) Or key.Value.EndsWith(" ", StringComparison.InvariantCulture) Then
            fullKeyErr(key, "Detected unwanted whitespace in iniKey", True)
            fixStr(True, key.Value, key.Value.Trim)
            fixStr(True, key.Name, key.Name.Trim)
            fixStr(True, key.KeyType, key.KeyType.Trim)
        End If
        ' Make sure the keyType is valid
        chkCasing(key, validCmds, key.KeyType, True)
        Return True
    End Function

    ''' <summary> Checks the <c> Value </c> or the <c> KeyType </c> of an <c> iniKey </c> against a given array of expected cased values, attempts 
    ''' to repair casing errors if possible </summary>
    ''' <param name="key"> The <c> iniKey </c> whose casing will be audited </param>
    ''' <param name="casedArray"> The array of expected cased values </param>
    ''' <param name="strToChk"> A pointer to the value being audited </param>
    ''' <param name="chkType"> <c> True </c> to check <c> KeyTypes </c>, <c> False </c> to check <c> Values </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub chkCasing(ByRef key As iniKey, casedArray As String(), ByRef strToChk As String, chkType As Boolean)
        ' Get the properly cased string
        Dim casedString = strToChk
        For Each casedText In casedArray
            If strToChk.Equals(casedText, StringComparison.InvariantCultureIgnoreCase) Then casedString = casedText
        Next
        ' Determine if there's a casing error
        Dim hasCasingErr = Not casedString.Equals(strToChk, StringComparison.InvariantCulture) And casedArray.Contains(casedString)
        Dim replacementText = If(chkType, key.KeyType.Replace(key.KeyType, casedString), key.Value.Replace(key.Value, casedString))
        Dim validData = String.Join(", ", casedArray)
        fullKeyErr(key, $"{casedString} has a casing error.", hasCasingErr And lintCasing.ShouldScan, lintCasing.fixFormat, strToChk, replacementText)
        fixStr(chkType And hasCasingErr, key.Name, key.Name.Replace(key.KeyType, replacementText))
        fixStr(chkType And hasCasingErr, key.KeyType, replacementText)
        fullKeyErr(key, $"Invalid data provided: {strToChk} in {key.toString}{Environment.NewLine}Valid data: {validData}", Not casedArray.Contains(casedString) And lintInvalid.ShouldScan)
    End Sub

    ''' <summary> Processes a FileKey format winapp2.ini <c> iniKey </c> and checks it for errors, correcting them where possible </summary>
    ''' <param name="key"> A winapp2.ini FileKey format <c> iniKey </c> to be checked for correctness </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Public Function pFileKey(key As iniKey) As iniKey
        If key Is Nothing Then argIsNull(NameOf(key)) : Return key
        ' Pipe symbol checks
        Dim iteratorCheckerList = Split(key.Value, "|")
        fullKeyErr(key, "Missing pipe (|) in FileKey.", Not key.vHas("|"))
        ' The driveLtr check to allow entries that contain hard coded drive letters to contain colons. Since this is an edge case only likely to pop up in winapp3.ini (as far as official releases go)
        ' We'll assume that if the path contains a hard coded drive letter, any colon use is intentional and disable this check. 
        fullKeyErr(key, "Colon (:) found where there should be a semicolon (;)", key.Value.Contains(":") And Not driveLtrs.IsMatch(getFirstDir(key.Value)), lintSemis.fixFormat, key.Value, key.Value.Replace(":", ";"))
        ' Captures any incident of semi colons coming before the first pipe symbol
        fullKeyErr(key, "Semicolon (;) found before pipe (|).", lintSemis.ShouldScan And key.vHas(";") And (key.Value.IndexOf(";", StringComparison.InvariantCultureIgnoreCase) < key.Value.IndexOf("|", StringComparison.InvariantCultureIgnoreCase)))
        fullKeyErr(key, "Trailing semicolon (;) in parameters", lintSemis.ShouldScan And key.vHas(";|"), lintSemis.fixFormat, key.Value, key.Value.Replace(";|", "|"))
        ' Check for incorrect spellings of RECURSE or REMOVESELF
        If iteratorCheckerList.Length > 2 Then fullKeyErr(key, "RECURSE or REMOVESELF is incorrectly spelled, or there are too many pipe (|) symbols.", Not iteratorCheckerList(2).Contains("RECURSE") And Not iteratorCheckerList(2).Contains("REMOVESELF"))
        ' Check for missing pipe symbol on recurse and removeself, fix them if detected
        Dim flags As New List(Of String) From {"RECURSE", "REMOVESELF"}
        flags.ForEach(Sub(flagStr) fullKeyErr(key, $"Missing pipe (|) before {flagStr}.", lintFlags.ShouldScan And key.vHas(flagStr) And Not key.vHas($"|{flagStr}"), lintFlags.fixFormat, key.Value, key.Value.Replace(flagStr, $"|{flagStr}")))
        ' Make sure VirtualStore folders point to the correct place
        inputMismatchErr(key.LineNumber, "Incorrect VirtualStore location.", key.Value, "%LocalAppData%\VirtualStore\Program Files*\", key.vHas("\virtualStore\p", True) And Not key.vHasAny({"programdata", "program files*", "program*"}, True))
        ' Backslash checks, fix if detected
        fullKeyErr(key, "Backslash (\) found before pipe (|).", lintSlashes.ShouldScan And key.vHas("\|"), lintSlashes.fixFormat, key.Value, key.Value.Replace("\|", "|"))
        ' Get the parameters given to the file key and sort them 
        Dim keyParams As New winapp2KeyParameters(key)
        Dim argsStrings As New strList
        Dim dupeArgs As New strList
        ' Check for duplicate args
        For Each arg In keyParams.ArgsList
            If argsStrings.chkDupes(arg) And lintParams.ShouldScan Then
                customErr(key.LineNumber, $"{If(arg.Length = 0, "Empty", "Duplicate")} FileKey parameter found", {$"Command: {arg}"})
                dupeArgs.add(arg, lintParams.fixFormat)
            End If
        Next
        ' Remove any duplicate arguments from the key parameters and reconstruct keys we've modified above
        If lintParams.fixFormat Then
            dupeArgs.Items.ForEach(Sub(arg) keyParams.ArgsList.Remove(arg))
            keyParams.reconstructKey(key)
        End If
        If lintMutliDupe.ShouldScan Then cDuplicateKeysBetweenEntries(key)
        Return key
    End Function

    ''' <summary> Processes a DetectFile format <c> iniKey </c> and checks it for errors, correcting where possible </summary>
    ''' <param name="key"> A winapp2.ini DetectFile format <c> iniKey </c> to be checked for correctness </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Function pDetectFile(key As iniKey) As iniKey
        ' Trailing Backslashes & nested wildcards
        fullKeyErr(key, "Trailing backslash (\) found in DetectFile", lintSlashes.ShouldScan _
    And key.Value.Last = CChar("\"), lintSlashes.fixFormat, key.Value, key.Value.TrimEnd(CChar("\")))
        If key.vHas("*") Then
            Dim splitDir = key.Value.Split(CChar("\"))
            For i = 0 To splitDir.Length - 1
                fullKeyErr(key, "Nested wildcard found in DetectFile", splitDir(i).Contains("*") And i <> splitDir.Length - 1)
            Next
        End If
        ' Make sure that DetectFile paths point to a filesystem location
        chkPathFormatValidity(key, False)
        Return key
    End Function

    ''' <summary> Audits the syntax of file system and registry paths </summary>
    ''' <param name="key"> An <c> iniKey </c> containing a registry or filesystem path to have its syntax validated </param>
    ''' <param name="isRegistry"> Indicates that the given <c> <paramref name="key"/> </c> is expected to hold a registry path </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub chkPathFormatValidity(key As iniKey, isRegistry As Boolean)
        If Not lintPathValidity.ShouldScan Then Return
        ' Remove the flags from ExcludeKeys if we have them before getting the first directory portion
        Dim rootStr = If(key.KeyType <> "ExcludeKey", getFirstDir(key.Value), getFirstDir(pathFromExcludeKey(key)))
        ' Ensure that registry paths have a valid hive and file paths have either a variable or a drive letter
        fullKeyErr(key, "Invalid registry path detected.", isRegistry And Not longReg.IsMatch(rootStr) And Not shortReg.IsMatch(rootStr))
        fullKeyErr(key, "Invalid file system path detected.", Not isRegistry And Not driveLtrs.IsMatch(rootStr) And Not rootStr.StartsWith("%", StringComparison.InvariantCultureIgnoreCase))
    End Sub

    ''' <summary> Processes a list of ExcludeKey format <c> iniKeys </c> and checks them for errors, correcting where possible </summary>
    ''' <param name="key"> A winapp2.ini ExcludeKey format <c> iniKey </c> to be checked for correctness </param>
    ''' <param name="hasF"> Indicates whether the entry excludes any filesystem locations </param>
    ''' <param name="hasR"> Indicates whether the entry excludes any registry locations </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub pExcludeKey(ByRef key As iniKey, ByRef hasF As Boolean, ByRef hasR As Boolean)
        Select Case True
            Case key.vHasAny({"FILE|", "PATH|"})
                hasF = True
                If lintPathValidity.ShouldScan Then
                    chkPathFormatValidity(key, False)
                    fullKeyErr(key, "Missing backslash (\) before pipe (|) in ExcludeKey.", key.vHas("|") And Not key.vHas("\|"))
                End If
            Case key.vHas("REG|")
                hasR = True
                chkPathFormatValidity(key, True)
            Case Else
                If key.Value.StartsWith("FILE", StringComparison.InvariantCulture) Or
                        key.Value.StartsWith("PATH", StringComparison.InvariantCulture) Or
                        key.Value.StartsWith("REG", StringComparison.InvariantCulture) Then
                    fullKeyErr(key, "Missing pipe symbol after ExcludeKey flag)")
                    Return
                End If
                fullKeyErr(key, "No valid exclude flag (FILE, PATH, or REG) found in ExcludeKey.")
        End Select
    End Sub

    ''' <summary> Sorts a <c> keyList </c> alphabetically with winapp2.ini precedence applied to the key values </summary>
    ''' <param name="kl"> A <c> keyList </c> to be sorted alphabetically (with numbers having precedence) </param>
    ''' <param name="hadDuplicatesRemoved"> Indicates that keys have been removed from <c> <paramref name="kl"/> </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub sortKeys(ByRef kl As keyList, hadDuplicatesRemoved As Boolean)
        If Not lintAlpha.ShouldScan Or kl.KeyCount <= 1 Then Return
        Dim keyValues = kl.toStrLst(True)
        Dim sortedKeyValues = replaceAndSort(keyValues, "|", " \ \")
        ' Rewrite the alphabetized keys back into the keylist (also fixes numbering)
        Dim keysOutOfPlace = False
        findOutOfPlace(keyValues, sortedKeyValues, kl.KeyType, kl.lineNums, keysOutOfPlace)
        If (keysOutOfPlace Or hadDuplicatesRemoved) And (lintAlpha.fixFormat Or lintWrongNums.fixFormat Or lintExtraNums.fixFormat) Then
            kl.renumberKeys(sortedKeyValues)
        End If
    End Sub

    ''' <summary> Prints an error when data is received that does not match an expected value </summary>
    ''' <param name="linecount"> The line number on which the error was detected </param>
    ''' <param name="err"> A description of the error as it will be displayed to the user </param>
    ''' <param name="received"> The (erroneous) input data </param>
    ''' <param name="expected"> The expected data </param>
    ''' <param name="cond"> Indicates that the error condition is present <br/> Optional, Default: <c> True </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub inputMismatchErr(linecount As Integer, err As String, received As String, expected As String, Optional cond As Boolean = True)
        If cond Then customErr(linecount, err, {$"Expected: {expected}", $"Found: {received}"})
    End Sub

    ''' <summary> Prints an error followed by the [Full Name *] of the entry to which it belongs </summary>
    ''' <param name="cond"> Indicates that the error condition is present </param>
    ''' <param name="entry"> The <c> winapp2entry </c> containing an error </param>
    ''' <param name="errTxt"> A description of the error as it will be displayed to the user </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub fullNameErr(cond As Boolean, entry As winapp2entry, errTxt As String)
        If cond Then customErr(entry.LineNum, errTxt, {$"Entry Name: {entry.FullName}"})
    End Sub

    ''' <summary> Prints an error whose output text contains an <c> iniKey </c> string, optionally correcting that value with one that is provided </summary>
    ''' <param name="key"> The <c> iniKey </c> containing an error </param>
    ''' <param name="err"> A description of the error as it will be displayed to the user </param>
    ''' <param name="cond"> Indicates that the error condition(s) are present (including any <c> lintRule.shouldScans </c>) <br/> Optional, Default: <c> True </c> </param>
    ''' <param name="repCond"> Indicates that the repair function should run <br/> Optional, Default: <c> False </c> </param>
    ''' <param name="newVal"> The corrected value with which to replace the incorrect correct value held by <c> <paramref name="repairVal"/> </c> <br/> Optional, Default: <c> "" </c> </param>
    ''' <param name="repairVal"> The incorrect value <br/> Optional, Default: <c> "" </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub fullKeyErr(key As iniKey, err As String, Optional cond As Boolean = True, Optional repCond As Boolean = False, Optional ByRef repairVal As String = "", Optional newVal As String = "")
        If cond Then customErr(key.LineNumber, err, {$"Key: {key.toString}"})
        fixStr(cond And repCond, repairVal, newVal)
    End Sub

    ''' <summary> Prints arbitrarily defined errors without a precondition </summary>
    ''' <param name="lineCount"> The line number on which the error was detected </param>
    ''' <param name="err"> A description of the error as it will be displayed to the user </param>
    ''' <param name="lines"> Any additional error information to be printed alongside the description </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub customErr(lineCount As Integer, err As String, lines As String())
        gLog(err, ascend:=True)
        cwl($"Line: {lineCount} - Error: {err}")
        MostRecentLintLog += $"Line: {lineCount} - Error: {err}" & Environment.NewLine
        For Each errStr In lines
            cwl(errStr)
            gLog(errStr, indent:=True)
            MostRecentLintLog += errStr & Environment.NewLine
        Next
        gLog(descend:=True)
        cwl()
        MostRecentLintLog += Environment.NewLine
        ErrorsFound += 1
    End Sub

    ''' <summary> Replace a given string with a new value if the fix condition is met </summary>
    ''' <param name="param"> The condition under which the string should be replaced </param>
    ''' <param name="currentValue"> A pointer to the string to be replaced </param>
    ''' <param name="newValue"> The replacement value for <c> <paramref name="currentValue"/> </c> </param>
    ''' Docs last updated: 2021-11-13 | Code last updated: 2021-11-13
    Private Sub fixStr(param As Boolean, ByRef currentValue As String, newValue As String)
        If param Then
            gLog($"Changing '{currentValue}' to '{newValue}'", ascend:=True, descend:=True, indent:=True, buffr:=True)
            currentValue = newValue
        End If
    End Sub
End Module