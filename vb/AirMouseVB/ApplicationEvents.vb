Imports Microsoft.VisualBasic.ApplicationServices

Namespace My

    ''' <summary>
    ''' Application-level startup. Uses the WindowsFormsApplicationBase so the
    ''' project behaves like a standard VB WinForms app with MainForm as the
    ''' startup window.
    ''' </summary>
    Partial Friend Class MyApplication
        Inherits WindowsFormsApplicationBase

        Public Sub New()
            MyBase.New(AuthenticationMode.Windows)
            Me.IsSingleInstance = False
            Me.EnableVisualStyles = True
            Me.SaveMySettingsOnExit = True
            Me.ShutdownStyle = ShutdownMode.AfterMainFormCloses
        End Sub

        Protected Overrides Sub OnCreateMainForm()
            Me.MainForm = New AirMouseVB.MainForm()
        End Sub

    End Class

End Namespace
