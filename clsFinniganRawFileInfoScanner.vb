Option Strict On

' Written by Matthew Monroe for the Department of Energy (PNNL, Richland, WA)
' Copyright 2005, Battelle Memorial Institute.  All Rights Reserved.
'
' Last modified September 17, 2005

Public Class clsFinniganRawFileInfoScanner
    Implements MSFileInfoScanner.iMSFileInfoProcessor

    ' Note: The extension must be in all caps
    Public Const FINNIGAN_RAW_FILE_EXTENSION As String = ".RAW"

    Public Function GetDatasetNameViaPath(ByVal strDataFilePath As String) As String Implements iMSFileInfoProcessor.GetDatasetNameViaPath
        ' The dataset name is simply the file name without .Raw
        Try
            Return System.IO.Path.GetFileNameWithoutExtension(strDataFilePath)
        Catch ex As System.Exception
            Return String.Empty
        End Try
    End Function

    Public Function ProcessDatafile(ByVal strDataFilePath As String, ByRef udtFileInfo As iMSFileInfoProcessor.udtFileInfoType) As Boolean Implements iMSFileInfoProcessor.ProcessDatafile
        ' Returns True if success, False if an error

        Dim objXcaliberAccessor As FinniganFileIO.FinniganFileReaderBaseClass
        Dim ioFileInfo As System.IO.FileInfo
        Dim udtScanHeaderInfo As MSFileInfoScanner.FinniganFileIO.XRawFileIO.udtScanHeaderInfoType

        Dim intDatasetID As Integer
        Dim intScanEnd As Integer

        Dim strStatusMessage As String
        Dim strDataFilePathLocal As String

        Dim blnReadError As Boolean
        Dim blnDeleteLocalFile As Boolean

        ' Obtain the full path to the file
        ioFileInfo = New System.IO.FileInfo(strDataFilePath)

        If Not ioFileInfo.Exists Then
            Return False
        End If

        ' Future, optional: Determine the DatasetID
        ' Unfortunately, this is not present in metadata.txt
        ' intDatasetID = LookupDatasetID(strDatasetName)
        intDatasetID = 0

        ' Record the file size and Dataset ID
        With udtFileInfo
            .FileSystemCreationTime = ioFileInfo.CreationTime
            .FileSystemModificationTime = ioFileInfo.LastWriteTime

            ' The acquisition times will get updated below to more accurate values
            .AcqTimeStart = .FileSystemModificationTime
            .AcqTimeEnd = .FileSystemModificationTime

            .DatasetID = intDatasetID
            .DatasetName = System.IO.Path.GetFileNameWithoutExtension(ioFileInfo.Name)
            .FileExtension = ioFileInfo.Extension
            .FileSizeBytes = ioFileInfo.Length

            .ScanCount = 0
        End With


        blnDeleteLocalFile = False
        blnReadError = False

        ' Use Xraw to read the .Raw file
        ' If reading from a SAMBA-mounted network share, and if the current user has 
        '  Read privileges but not Read&Execute privileges, then we will need to copy the file locally
        objXcaliberAccessor = New FinniganFileIO.XRawFileIO

        ' Open a handle to the data file
        If Not objXcaliberAccessor.OpenRawFile(ioFileInfo.FullName) Then
            ' File open failed
            Console.WriteLine("Call to .OpenRawFile failed for: " & ioFileInfo.FullName)
            blnReadError = True

            If clsMSFileScanner.GetAppFolderPath.Substring(0, 2).ToLower <> ioFileInfo.FullName.Substring(0, 2).ToLower Then
                ' Copy the file locally and try again

                Try
                    strDataFilePathLocal = System.IO.Path.Combine(clsMSFileScanner.GetAppFolderPath, System.IO.Path.GetFileName(strDataFilePath))

                    If strDataFilePathLocal.ToLower <> strDataFilePath.ToLower Then
                        Console.WriteLine("Copying file " & System.IO.Path.GetFileName(strDataFilePath) & " to the working folder")
                        System.IO.File.Copy(strDataFilePath, strDataFilePathLocal, True)

                        strDataFilePath = String.Copy(strDataFilePathLocal)
                        blnDeleteLocalFile = True

                        ' Update ioFileInfo then try to re-open
                        ioFileInfo = New System.IO.FileInfo(strDataFilePath)

                        If Not objXcaliberAccessor.OpenRawFile(ioFileInfo.FullName) Then
                            ' File open failed
                            Console.WriteLine("Call to .OpenRawFile failed for: " & ioFileInfo.FullName)
                            blnReadError = True
                        Else
                            blnReadError = False
                        End If
                    End If
                Catch ex As System.Exception
                    blnReadError = True
                End Try
            End If
        End If

        If Not blnReadError Then
            ' Read the file info
            Try
                udtFileInfo.AcqTimeStart = objXcaliberAccessor.FileInfo.CreationDate
            Catch ex As System.Exception
                ' Read error
                blnReadError = True
            End Try

            If Not blnReadError Then
                Try
                    ' Look up the end scan time then compute .AcqTimeEnd
                    intScanEnd = objXcaliberAccessor.FileInfo.ScanEnd
                    objXcaliberAccessor.GetScanInfo(intScanEnd, udtScanHeaderInfo)

                    With udtFileInfo
                        .AcqTimeEnd = .AcqTimeStart.AddMinutes(udtScanHeaderInfo.RetentionTime)
                        .ScanCount = objXcaliberAccessor.GetNumScans()
                    End With
                Catch ex As System.Exception
                    ' Error; use default values
                    With udtFileInfo
                        .AcqTimeEnd = .AcqTimeStart
                        .ScanCount = 0
                    End With
                End Try
            End If
        End If

        ' Close the handle to the data file
        objXcaliberAccessor.CloseRawFile()
        objXcaliberAccessor = Nothing

        ' Delete the local copy of the data file
        If blnDeleteLocalFile Then
            Try
                System.IO.File.Delete(strDataFilePathLocal)
            Catch ex As System.Exception
                ' Deletion failed
                Console.WriteLine("Deletion failed for: " & System.IO.Path.GetFileName(strDataFilePathLocal))
            End Try
        End If

        Return Not blnReadError

    End Function
End Class
