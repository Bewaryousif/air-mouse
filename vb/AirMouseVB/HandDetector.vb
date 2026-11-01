Imports System
Imports System.Collections.Generic
Imports OpenCvSharp

''' <summary>
''' Detects a hand in a webcam frame using classic computer-vision techniques
''' (no machine-learning model, no Python):
'''
'''   1. Convert the frame to HSV and threshold for skin colour.
'''   2. Clean the mask with morphological open/close + blur.
'''   3. Find the largest contour -> assume it is the hand.
'''   4. Compute the convex hull and convexity defects to count fingers.
'''
''' The result (<see cref="HandResult"/>) gives the palm centre (to drive the
''' cursor) and the number of extended fingers (to drive gestures).
''' </summary>
Public Class HandDetector

    ' --- Skin-colour thresholds in HSV. Tunable from the UI. ---------------
    Public Property HueMin As Integer = 0
    Public Property HueMax As Integer = 25
    Public Property SatMin As Integer = 40
    Public Property SatMax As Integer = 255
    Public Property ValMin As Integer = 60
    Public Property ValMax As Integer = 255

    ' Ignore tiny blobs (noise). Area is in pixels.
    Public Property MinHandArea As Double = 5000

    ''' <summary>
    ''' Analyse one BGR frame. Returns Nothing if no hand is found.
    ''' The <paramref name="debugMask"/>, if supplied, receives the skin mask
    ''' for display.
    ''' </summary>
    Public Function Detect(frame As Mat, Optional debugMask As Mat = Nothing) As HandResult
        If frame Is Nothing OrElse frame.Empty() Then Return Nothing

        Using hsv As New Mat(), mask As New Mat()
            Cv2.CvtColor(frame, hsv, ColorConversionCodes.BGR2HSV)

            Dim lower = New Scalar(HueMin, SatMin, ValMin)
            Dim upper = New Scalar(HueMax, SatMax, ValMax)
            Cv2.InRange(hsv, lower, upper, mask)

            ' Clean up the mask.
            Dim kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, New OpenCvSharp.Size(5, 5))
            Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel)
            Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel)
            Cv2.GaussianBlur(mask, mask, New OpenCvSharp.Size(5, 5), 0)

            If debugMask IsNot Nothing Then
                mask.CopyTo(debugMask)
            End If

            ' Find contours.
            Dim contours As Point()()
            Dim hierarchy As HierarchyIndex()
            Cv2.FindContours(mask, contours, hierarchy,
                             RetrievalModes.External, ContourApproximationModes.ApproxSimple)

            If contours Is Nothing OrElse contours.Length = 0 Then Return Nothing

            ' Largest contour by area = the hand (assumption).
            Dim bestIdx As Integer = -1
            Dim bestArea As Double = MinHandArea
            For i = 0 To contours.Length - 1
                Dim a = Cv2.ContourArea(contours(i))
                If a > bestArea Then
                    bestArea = a
                    bestIdx = i
                End If
            Next
            If bestIdx < 0 Then Return Nothing

            Dim hand = contours(bestIdx)

            ' Palm centre via image moments.
            Dim m = Cv2.Moments(hand)
            If m.M00 = 0 Then Return Nothing
            Dim cx = CInt(m.M10 / m.M00)
            Dim cy = CInt(m.M01 / m.M00)

            ' Bounding box.
            Dim box = Cv2.BoundingRect(hand)

            ' Count fingers from convexity defects.
            Dim fingerCount = CountFingers(hand, box)

            Return New HandResult With {
                .CenterX = cx,
                .CenterY = cy,
                .BoundingBox = box,
                .FingerCount = fingerCount,
                .Contour = hand,
                .Area = bestArea
            }
        End Using
    End Function

    ''' <summary>
    ''' Count extended fingers using convex-hull convexity defects. Each deep
    ''' "valley" between two fingertips with a sharp angle counts as a gap, so
    ''' fingers = valid defects + 1 (clamped to 0..5).
    ''' </summary>
    Private Function CountFingers(contour As Point(), box As Rect) As Integer
        Try
            Dim hullIndices = Cv2.ConvexHullIndices(contour)
            If hullIndices Is Nothing OrElse hullIndices.Length <= 3 Then Return 0

            Dim defects = Cv2.ConvexityDefects(contour, hullIndices)
            If defects Is Nothing OrElse defects.Length = 0 Then Return 0

            Dim count As Integer = 0
            Dim minDepth = box.Height * 0.08   ' ignore shallow dents

            For Each d In defects
                ' d = (startIdx, endIdx, farIdx, depth*256)
                Dim startP = contour(d.Item0)
                Dim endP = contour(d.Item1)
                Dim farP = contour(d.Item2)
                Dim depth = d.Item3 / 256.0

                If depth < minDepth Then Continue For

                ' Angle at the far point between the two fingers.
                Dim a = Distance(endP, startP)
                Dim b = Distance(farP, startP)
                Dim c = Distance(endP, farP)
                If b = 0 OrElse c = 0 Then Continue For

                Dim angle = Math.Acos((b * b + c * c - a * a) / (2 * b * c))

                ' A real finger gap is a fairly sharp angle (< ~90 degrees).
                If angle <= Math.PI / 2 Then
                    count += 1
                End If
            Next

            Dim fingers = count + 1
            If fingers > 5 Then fingers = 5
            If fingers < 0 Then fingers = 0
            Return fingers
        Catch
            Return 0
        End Try
    End Function

    Private Shared Function Distance(p1 As Point, p2 As Point) As Double
        Dim dx = p1.X - p2.X
        Dim dy = p1.Y - p2.Y
        Return Math.Sqrt(dx * dx + dy * dy)
    End Function

End Class

''' <summary>Outcome of a single hand detection pass.</summary>
Public Class HandResult
    Public Property CenterX As Integer
    Public Property CenterY As Integer
    Public Property BoundingBox As Rect
    Public Property FingerCount As Integer
    Public Property Contour As Point()
    Public Property Area As Double
End Class
