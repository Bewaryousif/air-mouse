Imports System
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports OpenCvSharp
Imports OpenCvSharp.Extensions

''' <summary>
''' Main window. Captures webcam frames on a background thread, runs hand
''' detection + gesture logic, drives the mouse, and renders a live preview
''' with an overlay. Pure VB.NET + OpenCvSharp (no Python).
''' </summary>
Public Class MainForm

    Private _capture As VideoCapture
    Private _worker As Thread
    Private _running As Boolean = False

    Private ReadOnly _detector As New HandDetector()
    Private ReadOnly _gestures As New GestureEngine()
    Private ReadOnly _mouse As New MouseController()

    ' Active-region margin (pixels) inside the frame.
    Private Const MarginX As Integer = 120
    Private Const MarginY As Integer = 90

    Private _fps As Double = 0
    Private _lastTick As Long = 0

    ' ----------------------------------------------------------------------
    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Populate camera list (probe indices 0..3).
        For i = 0 To 3
            cboCamera.Items.Add("Camera " & i)
        Next
        If cboCamera.Items.Count > 0 Then cboCamera.SelectedIndex = 0

        lblStatus.Text = "Ready. Screen: " & _mouse.ScreenWidth & "x" & _mouse.ScreenHeight &
                         ". Press Start."
    End Sub

    ' ---- Start / Stop -----------------------------------------------------
    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        If _running Then Return

        Dim camIndex = Math.Max(0, cboCamera.SelectedIndex)
        _capture = New VideoCapture(camIndex)
        _capture.Set(VideoCaptureProperties.FrameWidth, 640)
        _capture.Set(VideoCaptureProperties.FrameHeight, 480)

        If Not _capture.IsOpened() Then
            MessageBox.Show("Could not open " & cboCamera.Text &
                            ". Is it connected and not in use by another app?",
                            "Camera error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            _capture.Dispose()
            _capture = Nothing
            Return
        End If

        _running = True
        btnStart.Enabled = False
        btnStop.Enabled = True
        cboCamera.Enabled = False

        _worker = New Thread(AddressOf CaptureLoop)
        _worker.IsBackground = True
        _worker.Start()

        lblStatus.Text = "Running. Show your hand to the camera."
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        StopCapture()
    End Sub

    Private Sub StopCapture()
        _running = False
        Try
            If _worker IsNot Nothing AndAlso _worker.IsAlive Then
                _worker.Join(500)
            End If
        Catch
        End Try

        _mouse.ReleaseAll()

        If _capture IsNot Nothing Then
            _capture.Release()
            _capture.Dispose()
            _capture = Nothing
        End If

        btnStart.Enabled = True
        btnStop.Enabled = False
        cboCamera.Enabled = True
        lblStatus.Text = "Stopped."
    End Sub

    ' ---- Capture / processing loop (background thread) --------------------
    Private Sub CaptureLoop()
        Dim frame As New Mat()
        Dim mask As New Mat()

        Try
            While _running
                If Not _capture.Read(frame) OrElse frame.Empty() Then
                    Thread.Sleep(5)
                    Continue While
                End If

                ' Mirror for a natural, selfie-view experience.
                Cv2.Flip(frame, frame, FlipMode.Y)

                Dim showMask = ReadCheck(chkShowMask)
                Dim hand = _detector.Detect(frame, If(showMask, mask, Nothing))

                ' Gesture decision.
                Dim act = _gestures.Update(hand, frame.Width, frame.Height, MarginX, MarginY)

                ' Apply to the OS mouse (unless control is disabled).
                If ReadCheck(chkEnableControl) Then
                    ApplyAction(act)
                End If

                ' Draw overlay on the frame.
                DrawOverlay(frame, hand, act)

                ' FPS.
                Dim now = DateTime.Now.Ticks
                If _lastTick <> 0 Then
                    Dim dt = (now - _lastTick) / TimeSpan.TicksPerSecond
                    If dt > 0 Then _fps = _fps * 0.9 + (1.0 / dt) * 0.1
                End If
                _lastTick = now

                ' Push the image to the UI thread.
                Dim display As Mat = If(showMask AndAlso Not mask.Empty(), mask, frame)
                Dim bmp = BitmapConverter.ToBitmap(display)
                UpdatePreview(bmp, act.Label, _fps)

                Thread.Sleep(1)
            End While
        Catch ex As Exception
            SafeStatus("Error: " & ex.Message)
        Finally
            frame.Dispose()
            mask.Dispose()
        End Try
    End Sub

    Private Sub ApplyAction(act As GestureEngine.GestureAction)
        Select Case act.Kind
            Case GestureEngine.ActionKind.Move
                If act.HasPoint Then _mouse.MoveToNormalized(act.NormX, act.NormY)
            Case GestureEngine.ActionKind.DragStart
                If act.HasPoint Then _mouse.MoveToNormalized(act.NormX, act.NormY)
                _mouse.BeginDrag()
            Case GestureEngine.ActionKind.DragMove
                If act.HasPoint Then _mouse.MoveToNormalized(act.NormX, act.NormY)
            Case GestureEngine.ActionKind.DragEnd
                _mouse.EndDrag()
            Case GestureEngine.ActionKind.LeftClick
                _mouse.LeftClick()
            Case GestureEngine.ActionKind.RightClick
                _mouse.RightClick()
            Case GestureEngine.ActionKind.Scroll
                _mouse.Scroll(act.ScrollAmount)
        End Select
    End Sub

    ' ---- Overlay drawing --------------------------------------------------
    Private Sub DrawOverlay(frame As Mat, hand As HandResult, act As GestureEngine.GestureAction)
        ' Active region rectangle.
        Dim regionColor = If(hand IsNot Nothing, New Scalar(90, 220, 120), New Scalar(0, 200, 255))
        Cv2.Rectangle(frame,
                      New OpenCvSharp.Point(MarginX, MarginY),
                      New OpenCvSharp.Point(frame.Width - MarginX, frame.Height - MarginY),
                      regionColor, 2)

        If hand IsNot Nothing Then
            ' Hand contour + centre + bounding box.
            Cv2.Circle(frame, New OpenCvSharp.Point(hand.CenterX, hand.CenterY), 8,
                       New Scalar(0, 0, 255), -1)
            Cv2.Rectangle(frame, hand.BoundingBox, New Scalar(255, 180, 0), 2)
            Cv2.PutText(frame, "fingers: " & hand.FingerCount,
                        New OpenCvSharp.Point(hand.BoundingBox.X, hand.BoundingBox.Y - 8),
                        HersheyFonts.HersheySimplex, 0.6, New Scalar(255, 180, 0), 2)
        End If

        ' Status bar.
        Cv2.Rectangle(frame, New OpenCvSharp.Point(0, 0),
                      New OpenCvSharp.Point(frame.Width, 30), New Scalar(32, 32, 32), -1)
        Cv2.PutText(frame, "Gesture: " & act.Label, New OpenCvSharp.Point(8, 21),
                    HersheyFonts.HersheySimplex, 0.6, New Scalar(240, 240, 240), 1)
    End Sub

    ' ---- Thread-safe UI helpers ------------------------------------------
    Private Sub UpdatePreview(bmp As Bitmap, label As String, fps As Double)
        If IsDisposed OrElse Not IsHandleCreated Then
            bmp.Dispose()
            Return
        End If
        Try
            picPreview.BeginInvoke(Sub()
                                       Dim old = picPreview.Image
                                       picPreview.Image = bmp
                                       If old IsNot Nothing Then old.Dispose()
                                       lblGesture.Text = "Gesture: " & label
                                       lblFps.Text = "FPS: " & fps.ToString("0.0")
                                   End Sub)
        Catch
            bmp.Dispose()
        End Try
    End Sub

    Private Function ReadCheck(cb As CheckBox) As Boolean
        If cb.InvokeRequired Then
            Return CBool(cb.Invoke(Function() cb.Checked))
        End If
        Return cb.Checked
    End Function

    Private Sub SafeStatus(text As String)
        If IsDisposed OrElse Not IsHandleCreated Then Return
        Try
            BeginInvoke(Sub() lblStatus.Text = text)
        Catch
        End Try
    End Sub

    ' ---- Settings sliders -------------------------------------------------
    Private Sub trkSmoothing_Scroll(sender As Object, e As EventArgs) Handles trkSmoothing.Scroll
        _mouse.Smoothing = trkSmoothing.Value
        lblSmoothing.Text = "Cursor smoothing: " & trkSmoothing.Value
    End Sub

    Private Sub trkDwell_Scroll(sender As Object, e As EventArgs) Handles trkDwell.Scroll
        Dim seconds = trkDwell.Value / 10.0
        _gestures.DwellClickSeconds = seconds
        lblDwell.Text = "Dwell-click time: " & seconds.ToString("0.0") & " s"
    End Sub

    ' ---- Shutdown ---------------------------------------------------------
    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) _
            Handles MyBase.FormClosing
        StopCapture()
    End Sub

End Class
