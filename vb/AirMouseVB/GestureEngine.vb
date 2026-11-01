Imports System

''' <summary>
''' Turns a stream of <see cref="HandResult"/> into high-level actions.
'''
''' Because pure OpenCV finger detection is less precise than MediaPipe, this
''' engine uses robust, forgiving gestures based on the number of extended
''' fingers plus a "dwell" (hold still) mechanism:
'''
'''   * 1 finger  ............... move the cursor
'''   * Hold cursor still 1 s ... left click  (dwell click)
'''   * 2 fingers ............... move without clicking (safe reposition)
'''   * 5 fingers (open palm) ... start / continue drag
'''   * 0 fingers (fist) ........ right click  (debounced)
'''   * 3 fingers up/down move .. scroll
'''
''' All transitions are debounced so detection noise never spams actions.
''' </summary>
Public Class GestureEngine

    ' ---- Tunables ----------------------------------------------------------
    Public Property DwellClickSeconds As Double = 1.0      ' hold-still to click
    Public Property DwellRadiusPixels As Double = 25       ' "still" tolerance
    Public Property RightClickCooldown As Double = 1.0
    Public Property ScrollDeadzonePixels As Double = 20
    Public Property ScrollSpeed As Integer = 60

    ' ---- State -------------------------------------------------------------
    Private _dwellStart As DateTime = DateTime.MinValue
    Private _dwellAnchorX As Integer
    Private _dwellAnchorY As Integer
    Private _dwellFired As Boolean = False
    Private _lastRightClick As DateTime = DateTime.MinValue
    Private _scrollAnchorY As Integer = -1

    Public Enum ActionKind
        None
        Move
        LeftClick
        RightClick
        DragStart
        DragMove
        DragEnd
        Scroll
    End Enum

    Public Class GestureAction
        Public Property Kind As ActionKind = ActionKind.None
        ' Normalised cursor target (0..1) for Move / Drag.
        Public Property NormX As Double
        Public Property NormY As Double
        Public Property HasPoint As Boolean
        Public Property ScrollAmount As Integer
        Public Property Label As String = "Idle"
    End Class

    Private _wasDragging As Boolean = False

    ''' <summary>
    ''' Process one detection. <paramref name="frameW"/>/<paramref name="frameH"/>
    ''' and the active-region margin let us map hand position to a normalised
    ''' cursor target that covers the whole screen from a small hand movement.
    ''' </summary>
    Public Function Update(hand As HandResult, frameW As Integer, frameH As Integer,
                           marginX As Integer, marginY As Integer) As GestureAction

        Dim act As New GestureAction()

        If hand Is Nothing Then
            _dwellStart = DateTime.MinValue
            _scrollAnchorY = -1
            If _wasDragging Then
                _wasDragging = False
                act.Kind = ActionKind.DragEnd
                act.Label = "Drag end"
                Return act
            End If
            act.Kind = ActionKind.None
            act.Label = "No hand"
            Return act
        End If

        ' Map palm centre within the active region to a 0..1 cursor target.
        Dim usableW = Math.Max(1, frameW - 2 * marginX)
        Dim usableH = Math.Max(1, frameH - 2 * marginY)
        Dim nx = (hand.CenterX - marginX) / CDbl(usableW)
        Dim ny = (hand.CenterY - marginY) / CDbl(usableH)
        act.NormX = nx
        act.NormY = ny
        act.HasPoint = True

        Dim fingers = hand.FingerCount
        Dim now = DateTime.Now

        ' --- Drag: open palm (5 fingers) ----------------------------------
        If fingers >= 5 Then
            _scrollAnchorY = -1
            _dwellStart = DateTime.MinValue
            If Not _wasDragging Then
                _wasDragging = True
                act.Kind = ActionKind.DragStart
                act.Label = "Drag start (open palm)"
            Else
                act.Kind = ActionKind.DragMove
                act.Label = "Dragging"
            End If
            Return act
        ElseIf _wasDragging Then
            ' Fingers dropped below 5 -> release the drag.
            _wasDragging = False
            act.Kind = ActionKind.DragEnd
            act.Label = "Drop"
            Return act
        End If

        ' --- Right click: fist (0 fingers) --------------------------------
        If fingers = 0 Then
            _dwellStart = DateTime.MinValue
            _scrollAnchorY = -1
            If (now - _lastRightClick).TotalSeconds >= RightClickCooldown Then
                _lastRightClick = now
                act.Kind = ActionKind.RightClick
                act.Label = "Right click (fist)"
                Return act
            End If
            act.Kind = ActionKind.None
            act.Label = "Fist..."
            Return act
        End If

        ' --- Scroll: three fingers ----------------------------------------
        If fingers = 3 Then
            _dwellStart = DateTime.MinValue
            If _scrollAnchorY < 0 Then _scrollAnchorY = hand.CenterY
            Dim delta = _scrollAnchorY - hand.CenterY
            If Math.Abs(delta) > ScrollDeadzonePixels Then
                Dim dir = If(delta > 0, 1, -1)
                _scrollAnchorY = hand.CenterY
                act.Kind = ActionKind.Scroll
                act.ScrollAmount = dir * ScrollSpeed
                act.Label = "Scroll " & If(dir > 0, "up", "down")
                Return act
            End If
            act.Kind = ActionKind.None
            act.Label = "Scroll ready"
            Return act
        Else
            _scrollAnchorY = -1
        End If

        ' --- Two fingers: reposition only (no dwell click) ----------------
        If fingers = 2 Then
            _dwellStart = DateTime.MinValue
            act.Kind = ActionKind.Move
            act.Label = "Move (no click)"
            Return act
        End If

        ' --- One finger: move + dwell-to-click ----------------------------
        ' (Treat any remaining small count, i.e. 1, as the pointing gesture.)
        act.Kind = ActionKind.Move
        act.Label = "Move"

        If _dwellStart = DateTime.MinValue Then
            _dwellStart = now
            _dwellAnchorX = hand.CenterX
            _dwellAnchorY = hand.CenterY
            _dwellFired = False
        Else
            Dim moved = Math.Sqrt((hand.CenterX - _dwellAnchorX) ^ 2 +
                                  (hand.CenterY - _dwellAnchorY) ^ 2)
            If moved > DwellRadiusPixels Then
                ' Hand moved -> restart the dwell timer.
                _dwellStart = now
                _dwellAnchorX = hand.CenterX
                _dwellAnchorY = hand.CenterY
                _dwellFired = False
            ElseIf Not _dwellFired AndAlso
                   (now - _dwellStart).TotalSeconds >= DwellClickSeconds Then
                _dwellFired = True
                act.Kind = ActionKind.LeftClick
                act.Label = "Click (dwell)"
            Else
                Dim remaining = DwellClickSeconds - (now - _dwellStart).TotalSeconds
                If Not _dwellFired Then
                    act.Label = "Hold to click: " & remaining.ToString("0.0") & "s"
                End If
            End If
        End If

        Return act
    End Function

End Class
