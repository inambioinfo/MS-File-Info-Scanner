Option Strict On

' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Started in 2005
'
' Last modified September 17, 2005

Public Class clsMicromassRawFolderInfoScanner
    Inherits clsMSFileInfoProcessorBaseClass

    ' Note: The extension must be in all caps
    Public Const MICROMASS_RAW_FOLDER_EXTENSION As String = ".RAW"

    Private Const MINIMUM_ACCEPTABLE_ACQ_START_TIME As DateTime = #1/1/1975#

    Public Overrides Function GetDatasetNameViaPath(ByVal strDataFilePath As String) As String
        ' The dataset name is simply the folder name without .Raw
        Try
			Return Path.GetFileNameWithoutExtension(strDataFilePath)
		Catch ex As Exception
			Return String.Empty
		End Try
	End Function

	Private Function MinutesToTimeSpan(ByVal dblMinutes As Double) As TimeSpan

		Dim intMinutes As Integer
		Dim intSeconds As Integer
		Dim dtTimeSpan As TimeSpan

		Try
			intMinutes = CInt(Math.Floor(dblMinutes))
			intSeconds = CInt(Math.Round((dblMinutes - intMinutes) * 60, 0))

			dtTimeSpan = New TimeSpan(0, intMinutes, intSeconds)
		Catch ex As Exception
			dtTimeSpan = New TimeSpan(0, 0, 0)
		End Try

		Return dtTimeSpan

	End Function

	Public Overrides Function ProcessDataFile(ByVal strDataFilePath As String, ByRef udtFileInfo As iMSFileInfoProcessor.udtFileInfoType) As Boolean
		' Returns True if success, False if an error

		Dim blnSuccess As Boolean
		Dim ioFolderInfo As DirectoryInfo
		Dim ioFileInfo As FileInfo

		Dim intFileCount As Integer

		Dim objNativeFileIO As clsMassLynxNativeIO
		Dim udtHeaderInfo As clsMassLynxNativeIO.udtMSHeaderInfoType = New clsMassLynxNativeIO.udtMSHeaderInfoType
		Dim udtFunctionInfo As clsMassLynxNativeIO.udtMSFunctionInfoType = New clsMassLynxNativeIO.udtMSFunctionInfoType

		Dim intFunctionCount As Integer
		Dim intFunctionNumber As Integer
		Dim sngEndRT As Single

		Dim dtNewStartDate As DateTime

		Try
			ioFolderInfo = New DirectoryInfo(strDataFilePath)
			With udtFileInfo
				.FileSystemCreationTime = ioFolderInfo.CreationTime
				.FileSystemModificationTime = ioFolderInfo.LastWriteTime

				' The acquisition times will get updated below to more accurate values
				.AcqTimeStart = .FileSystemModificationTime
				.AcqTimeEnd = .FileSystemModificationTime

				.DatasetName = GetDatasetNameViaPath(ioFolderInfo.Name)
				.FileExtension = ioFolderInfo.Extension

				' Sum up the sizes of all of the files in this folder
				.FileSizeBytes = 0
				intFileCount = 0
				For Each ioFileInfo In ioFolderInfo.GetFiles()
					.FileSizeBytes += ioFileInfo.Length

					If intFileCount = 0 Then
						' Assign the first file's modification time to .AcqTimeStart and .AcqTimeEnd
						' Necessary in case _header.txt is missing
						.AcqTimeStart = ioFileInfo.LastWriteTime
						.AcqTimeEnd = ioFileInfo.LastWriteTime
					End If

					If ioFileInfo.Name.ToLower = "_header.txt" Then
						' Assign the file's modification time to .AcqTimeStart and .AcqTimeEnd
						' These will get updated below to more precise values
						.AcqTimeStart = ioFileInfo.LastWriteTime
						.AcqTimeEnd = ioFileInfo.LastWriteTime
					End If

					intFileCount += 1
				Next ioFileInfo

				.ScanCount = 0
			End With

			blnSuccess = True

			objNativeFileIO = New clsMassLynxNativeIO
			If objNativeFileIO.GetFileInfo(ioFolderInfo.FullName, udtHeaderInfo) Then

				dtNewStartDate = Date.Parse(udtHeaderInfo.AcquDate & " " & udtHeaderInfo.AcquTime)

				intFunctionCount = objNativeFileIO.GetFunctionCount(ioFolderInfo.FullName)
				If intFunctionCount > 0 Then

					' Sum up the scan count of all of the functions
					' Additionally, find the largest EndRT value in all of the functions
					sngEndRT = 0
					For intFunctionNumber = 1 To intFunctionCount
						If objNativeFileIO.GetFunctionInfo(ioFolderInfo.FullName, 1, udtFunctionInfo) Then
							udtFileInfo.ScanCount += udtFunctionInfo.ScanCount
							If udtFunctionInfo.EndRT > sngEndRT Then
								sngEndRT = udtFunctionInfo.EndRT
							End If
						End If
					Next intFunctionNumber

					With udtFileInfo
						If dtNewStartDate >= MINIMUM_ACCEPTABLE_ACQ_START_TIME Then
							udtFileInfo.AcqTimeStart = dtNewStartDate

							If sngEndRT > 0 Then
								.AcqTimeEnd = .AcqTimeStart.Add(MinutesToTimeSpan(sngEndRT))
							Else
								.AcqTimeEnd = .AcqTimeStart
							End If
						Else
							' Keep .AcqTimeEnd as the file modification date
							' Set .AcqTimeStart based on .AcqEndTime
							If sngEndRT > 0 Then
								.AcqTimeStart = .AcqTimeEnd.Subtract(MinutesToTimeSpan(sngEndRT))
							Else
								.AcqTimeEnd = .AcqTimeStart
							End If
						End If
					End With
				Else
					If dtNewStartDate >= MINIMUM_ACCEPTABLE_ACQ_START_TIME Then
						udtFileInfo.AcqTimeStart = dtNewStartDate
					End If
				End If

			Else
				' Error getting the header info using clsMassLynxNativeIO
				' Continue anyway since we've populated some of the values
			End If

			blnSuccess = True

		Catch ex As Exception
			blnSuccess = False
		End Try

		Return blnSuccess
	End Function

End Class
