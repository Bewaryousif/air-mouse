Imports System.Windows.Forms
Imports System.Drawing

Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    Friend WithEvents picPreview As PictureBox
    Friend WithEvents btnStart As Button
    Friend WithEvents btnStop As Button
    Friend WithEvents lblStatus As Label
    Friend WithEvents lblGesture As Label
    Friend WithEvents lblFps As Label
    Friend WithEvents cboCamera As ComboBox
    Friend WithEvents lblCamera As Label
    Friend WithEvents trkSmoothing As TrackBar
    Friend WithEvents lblSmoothing As Label
    Friend WithEvents trkDwell As TrackBar
    Friend WithEvents lblDwell As Label
    Friend WithEvents chkShowMask As CheckBox
    Friend WithEvents chkEnableControl As CheckBox
    Friend WithEvents grpSettings As GroupBox
    Friend WithEvents lblHelp As Label

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.picPreview = New PictureBox()
        Me.btnStart = New Button()
        Me.btnStop = New Button()
        Me.lblStatus = New Label()
        Me.lblGesture = New Label()
        Me.lblFps = New Label()
        Me.cboCamera = New ComboBox()
        Me.lblCamera = New Label()
        Me.trkSmoothing = New TrackBar()
        Me.lblSmoothing = New Label()
        Me.trkDwell = New TrackBar()
        Me.lblDwell = New Label()
        Me.chkShowMask = New CheckBox()
        Me.chkEnableControl = New CheckBox()
        Me.grpSettings = New GroupBox()
        Me.lblHelp = New Label()

        ' picPreview
        Me.picPreview.BackColor = Color.Black
        Me.picPreview.Location = New Point(12, 12)
        Me.picPreview.Name = "picPreview"
        Me.picPreview.Size = New Size(640, 480)
        Me.picPreview.SizeMode = PictureBoxSizeMode.StretchImage
        Me.picPreview.BorderStyle = BorderStyle.FixedSingle

        ' btnStart
        Me.btnStart.Location = New Point(670, 12)
        Me.btnStart.Name = "btnStart"
        Me.btnStart.Size = New Size(110, 40)
        Me.btnStart.Text = "▶ Start"
        Me.btnStart.BackColor = Color.FromArgb(76, 175, 80)
        Me.btnStart.ForeColor = Color.White
        Me.btnStart.FlatStyle = FlatStyle.Flat
        Me.btnStart.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)

        ' btnStop
        Me.btnStop.Location = New Point(786, 12)
        Me.btnStop.Name = "btnStop"
        Me.btnStop.Size = New Size(110, 40)
        Me.btnStop.Text = "■ Stop"
        Me.btnStop.BackColor = Color.FromArgb(198, 40, 40)
        Me.btnStop.ForeColor = Color.White
        Me.btnStop.FlatStyle = FlatStyle.Flat
        Me.btnStop.Font = New Font("Segoe UI", 10.0!, FontStyle.Bold)
        Me.btnStop.Enabled = False

        ' lblCamera
        Me.lblCamera.Location = New Point(670, 64)
        Me.lblCamera.Name = "lblCamera"
        Me.lblCamera.Size = New Size(60, 23)
        Me.lblCamera.Text = "Camera:"
        Me.lblCamera.TextAlign = ContentAlignment.MiddleLeft

        ' cboCamera
        Me.cboCamera.Location = New Point(732, 64)
        Me.cboCamera.Name = "cboCamera"
        Me.cboCamera.Size = New Size(164, 23)
        Me.cboCamera.DropDownStyle = ComboBoxStyle.DropDownList

        ' chkEnableControl
        Me.chkEnableControl.Location = New Point(670, 96)
        Me.chkEnableControl.Name = "chkEnableControl"
        Me.chkEnableControl.Size = New Size(226, 24)
        Me.chkEnableControl.Text = "Control the mouse (uncheck to preview)"
        Me.chkEnableControl.Checked = True

        ' chkShowMask
        Me.chkShowMask.Location = New Point(670, 124)
        Me.chkShowMask.Name = "chkShowMask"
        Me.chkShowMask.Size = New Size(226, 24)
        Me.chkShowMask.Text = "Show skin-detection mask"

        ' grpSettings
        Me.grpSettings.Location = New Point(670, 156)
        Me.grpSettings.Name = "grpSettings"
        Me.grpSettings.Size = New Size(226, 170)
        Me.grpSettings.Text = "Settings"
        Me.grpSettings.Controls.Add(Me.lblSmoothing)
        Me.grpSettings.Controls.Add(Me.trkSmoothing)
        Me.grpSettings.Controls.Add(Me.lblDwell)
        Me.grpSettings.Controls.Add(Me.trkDwell)

        ' lblSmoothing
        Me.lblSmoothing.Location = New Point(10, 22)
        Me.lblSmoothing.Name = "lblSmoothing"
        Me.lblSmoothing.Size = New Size(206, 20)
        Me.lblSmoothing.Text = "Cursor smoothing: 5"

        ' trkSmoothing
        Me.trkSmoothing.Location = New Point(8, 44)
        Me.trkSmoothing.Name = "trkSmoothing"
        Me.trkSmoothing.Size = New Size(210, 45)
        Me.trkSmoothing.Minimum = 1
        Me.trkSmoothing.Maximum = 15
        Me.trkSmoothing.Value = 5

        ' lblDwell
        Me.lblDwell.Location = New Point(10, 92)
        Me.lblDwell.Name = "lblDwell"
        Me.lblDwell.Size = New Size(206, 20)
        Me.lblDwell.Text = "Dwell-click time: 1.0 s"

        ' trkDwell
        Me.trkDwell.Location = New Point(8, 114)
        Me.trkDwell.Name = "trkDwell"
        Me.trkDwell.Size = New Size(210, 45)
        Me.trkDwell.Minimum = 5
        Me.trkDwell.Maximum = 30
        Me.trkDwell.Value = 10

        ' lblGesture
        Me.lblGesture.Location = New Point(670, 332)
        Me.lblGesture.Name = "lblGesture"
        Me.lblGesture.Size = New Size(226, 28)
        Me.lblGesture.Text = "Gesture: —"
        Me.lblGesture.Font = New Font("Segoe UI", 11.0!, FontStyle.Bold)
        Me.lblGesture.ForeColor = Color.FromArgb(0, 120, 215)

        ' lblFps
        Me.lblFps.Location = New Point(670, 362)
        Me.lblFps.Name = "lblFps"
        Me.lblFps.Size = New Size(226, 20)
        Me.lblFps.Text = "FPS: —"

        ' lblStatus
        Me.lblStatus.Location = New Point(12, 500)
        Me.lblStatus.Name = "lblStatus"
        Me.lblStatus.Size = New Size(640, 20)
        Me.lblStatus.Text = "Ready. Press Start to begin."

        ' lblHelp
        Me.lblHelp.Location = New Point(670, 388)
        Me.lblHelp.Name = "lblHelp"
        Me.lblHelp.Size = New Size(226, 130)
        Me.lblHelp.Font = New Font("Segoe UI", 8.25!)
        Me.lblHelp.Text = "Gestures:" & vbCrLf &
            "• 1 finger — move cursor" & vbCrLf &
            "• hold still 1s — left click" & vbCrLf &
            "• 2 fingers — move, no click" & vbCrLf &
            "• fist — right click" & vbCrLf &
            "• open palm — drag" & vbCrLf &
            "• 3 fingers up/down — scroll"

        ' MainForm
        Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(908, 530)
        Me.Controls.Add(Me.picPreview)
        Me.Controls.Add(Me.btnStart)
        Me.Controls.Add(Me.btnStop)
        Me.Controls.Add(Me.lblCamera)
        Me.Controls.Add(Me.cboCamera)
        Me.Controls.Add(Me.chkEnableControl)
        Me.Controls.Add(Me.chkShowMask)
        Me.Controls.Add(Me.grpSettings)
        Me.Controls.Add(Me.lblGesture)
        Me.Controls.Add(Me.lblFps)
        Me.Controls.Add(Me.lblStatus)
        Me.Controls.Add(Me.lblHelp)
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.Name = "MainForm"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Text = "Air Mouse (VB.NET) — webcam gesture control"
    End Sub

End Class
