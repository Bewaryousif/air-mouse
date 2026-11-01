Imports System
Imports System.Runtime.InteropServices
Imports System.Windows.Forms

''' <summary>
''' Drives the real Windows cursor via the Win32 SendInput / SetCursorPos APIs.
''' Provides smoothed movement and left/right/double click + drag.
''' Pure .NET P/Invoke — no third-party dependency.
''' </summary>
Public Class MouseController

    ' ---- Win32 interop -----------------------------------------------------
    <StructLayout(LayoutKind.Sequential)>
    Private Structure INPUT
        Public type As UInteger
        Public mi As MOUSEINPUT
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure MOUSEINPUT
        Public dx As Integer
        Public dy As Integer
        Public mouseData As UInteger
        Public dwFlags As UInteger
        Public time As UInteger
        Public dwExtraInfo As IntPtr
    End Structure

    Private Const INPUT_MOUSE As UInteger = 0
    Private Const MOUSEEVENTF_LEFTDOWN As UInteger = &H2
    Private Const MOUSEEVENTF_LEFTUP As UInteger = &H4
    Private Const MOUSEEVENTF_RIGHTDOWN As UInteger = &H8
    Private Const MOUSEEVENTF_RIGHTUP As UInteger = &H10
    Private Const MOUSEEVENTF_WHEEL As UInteger = &H800

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SendInput(nInputs As UInteger, pInputs As INPUT(), cbSize As Integer) As UInteger
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetCursorPos(x As Integer, y As Integer) As Boolean
    End Function

    ' ---- Config ------------------------------------------------------------
    ''' <summary>Higher = smoother cursor but more lag. 1 = no smoothing.</summary>
    Public Property Smoothing As Double = 5.0

    Private _screenW As Integer
    Private _screenH As Integer
    Private _prevX As Double = -1
    Private _prevY As Double = -1
    Private _leftDown As Boolean = False

    Public Sub New()
        _screenW = Screen.PrimaryScreen.Bounds.Width
        _screenH = Screen.PrimaryScreen.Bounds.Height
    End Sub

    Public ReadOnly Property ScreenWidth As Integer
        Get
            Return _screenW
        End Get
    End Property

    Public ReadOnly Property ScreenHeight As Integer
        Get
            Return _screenH
        End Get
    End Property

    ''' <summary>
    ''' Move the cursor to a screen position given a normalised point
    ''' (nx, ny in 0..1) mapped from the camera's active region.
    ''' </summary>
    Public Sub MoveToNormalized(nx As Double, ny As Double)
        nx = Clamp01(nx)
        ny = Clamp01(ny)

        Dim targetX = nx * _screenW
        Dim targetY = ny * _screenH

        If _prevX < 0 Then
            _prevX = targetX
            _prevY = targetY
        End If

        Dim s = Math.Max(Smoothing, 1.0)
        Dim newX = _prevX + (targetX - _prevX) / s
        Dim newY = _prevY + (targetY - _prevY) / s
        _prevX = newX
        _prevY = newY

        Dim x = CInt(Math.Max(0, Math.Min(_screenW - 1, newX)))
        Dim y = CInt(Math.Max(0, Math.Min(_screenH - 1, newY)))
        SetCursorPos(x, y)
    End Sub

    Public Sub LeftClick()
        SendMouse(MOUSEEVENTF_LEFTDOWN)
        SendMouse(MOUSEEVENTF_LEFTUP)
    End Sub

    Public Sub DoubleClick()
        LeftClick()
        System.Threading.Thread.Sleep(40)
        LeftClick()
    End Sub

    Public Sub RightClick()
        SendMouse(MOUSEEVENTF_RIGHTDOWN)
        SendMouse(MOUSEEVENTF_RIGHTUP)
    End Sub

    Public Sub BeginDrag()
        If Not _leftDown Then
            SendMouse(MOUSEEVENTF_LEFTDOWN)
            _leftDown = True
        End If
    End Sub

    Public Sub EndDrag()
        If _leftDown Then
            SendMouse(MOUSEEVENTF_LEFTUP)
            _leftDown = False
        End If
    End Sub

    Public ReadOnly Property IsDragging As Boolean
        Get
            Return _leftDown
        End Get
    End Property

    Public Sub Scroll(amount As Integer)
        Dim inp(0) As INPUT
        inp(0).type = INPUT_MOUSE
        inp(0).mi.mouseData = CUInt(amount)
        inp(0).mi.dwFlags = MOUSEEVENTF_WHEEL
        SendInput(1UI, inp, Marshal.SizeOf(GetType(INPUT)))
    End Sub

    Public Sub ReleaseAll()
        EndDrag()
    End Sub

    ' ---- helpers -----------------------------------------------------------
    Private Shared Sub SendMouse(flags As UInteger)
        Dim inp(0) As INPUT
        inp(0).type = INPUT_MOUSE
        inp(0).mi.dwFlags = flags
        SendInput(1UI, inp, Marshal.SizeOf(GetType(INPUT)))
    End Sub

    Private Shared Function Clamp01(v As Double) As Double
        If v < 0 Then Return 0
        If v > 1 Then Return 1
        Return v
    End Function

End Class
