﻿Imports LibOptimization.Util
Imports LibOptimization.MathUtil

Namespace Optimization
    ''' <summary>
    ''' Basic Particle Swarm Optmization
    ''' </summary>
    ''' <remarks>
    ''' Features:
    '''  -Swarm Intelligence algorithm.
    '''  -Derivative free optimization algorithm.
    ''' 
    ''' Refference:
    ''' [1]James Kennedy and Russell Eberhart, "Particle Swarm Optimization", Proceedings of IEEE the International Conference on Neural Networks，1995
    ''' [2]Y. Shi and Russell Eberhart, "A Modified Particle Swarm Optimizer", Proceedings of Congress on Evolu-tionary Computation, 79-73., 1998
    ''' [3]R. C. Eberhart and Y. Shi, "Comparing inertia weights and constriction factors in particle swarm optimization", In Proceedings of the Congress on Evolutionary Computation, vol. 1, pp. 84–88, IEEE, La Jolla, Calif, USA, July 2000.
    ''' </remarks>
    Public Class clsOptPSO : Inherits absOptimization
#Region "Member"
        ''' <summary>Max iteration count(Default:20000)</summary>
        Public Property Iteration As Integer = 20000

        ''' <summary>Epsilon(Default:0.000001) for Criterion</summary>
        Public Property EPS As Double = 0.000001 '1e-6

        ''' <summary>Use criterion</summary>
        Public Property IsUseCriterion As Boolean = True

        ''' <summary>
        ''' higher N percentage particles are finished at the time of same evaluate value.
        ''' This parameter is valid is when IsUseCriterion is true.
        ''' </summary>
        Public Property HigherNPercent As Double = 0.8 'for IsCriterion()
        Private HigherNPercentIndex As Integer = 0 'for IsCriterion())

        'particles
        Private m_swarm As New List(Of clsParticle)
        Private m_globalBest As clsPoint = Nothing

        '-------------------------------------------------------------------
        'Coefficient of PSO
        '-------------------------------------------------------------------
        ''' <summary>Swarm Size(Default:100)</summary>
        Public Property SwarmSize As Integer = 100

        ''' <summary>Inertia weight. Weigth=1.0(orignal paper 1995), Weight=0.729(Default setting)</summary>
        Public Property Weight As Double = 0.729

        ''' <summary>velocity coefficient(affected by personal best). C1 = C2 = 2.0 (orignal paper 1995), C1 = C2 = 1.49445(Default setting)</summary>
        Public Property C1 As Double = 1.49445

        ''' <summary>velocity coefficient(affected by global best). C1 = C2 = 2.0 (orignal paper 1995), C1 = C2 = 1.49445(Default setting)</summary>
        Public Property C2 As Double = 1.49445
#End Region

#Region "Constructor"
        ''' <summary>
        ''' Default constructor
        ''' </summary>
        ''' <remarks></remarks>
        Public Sub New(ByVal ai_func As absObjectiveFunction)
            Me.m_func = ai_func
        End Sub
#End Region

#Region "Public"
        ''' <summary>
        ''' Initialize
        ''' </summary>
        ''' <remarks></remarks>
        Public Overrides Sub Init()
            Try
                'init meber varibles
                Me.m_iteration = 0
                Me.m_swarm.Clear()

                'Set initialize value
                For i As Integer = 0 To Me.SwarmSize - 1
                    Dim tempPosition = New clsPoint(Me.m_func)
                    Dim tempBestPosition = New clsPoint(Me.m_func)
                    Dim tempVelocity(Me.m_func.NumberOfVariable - 1) As Double
                    For j As Integer = 0 To Me.m_func.NumberOfVariable - 1
                        'position
                        Dim value As Double = clsUtil.GenRandomRange(Me.m_rand, -Me.InitialValueRange, Me.InitialValueRange)
                        If MyBase.InitialPosition IsNot Nothing AndAlso MyBase.InitialPosition.Length = Me.m_func.NumberOfVariable Then
                            value += Me.InitialPosition(j)
                        End If
                        tempPosition(j) = value
                        tempBestPosition(j) = tempPosition(j)

                        'velocity
                        tempVelocity(j) = clsUtil.GenRandomRange(Me.m_rand, -Me.InitialValueRange, Me.InitialValueRange)
                    Next
                    tempPosition.ReEvaluate()
                    tempBestPosition.ReEvaluate()
                    Me.m_swarm.Add(New clsParticle(tempPosition, tempVelocity, tempBestPosition))
                Next

                'Sort Evaluate
                Me.m_swarm.Sort()
                Me.m_globalBest = Me.m_swarm(0).BestPoint.Copy()

                'Detect HigherNPercentIndex
                Me.HigherNPercentIndex = CInt(Me.m_swarm.Count * Me.HigherNPercent)
                If Me.HigherNPercentIndex = Me.m_swarm.Count OrElse Me.HigherNPercentIndex >= Me.m_swarm.Count Then
                    Me.HigherNPercentIndex = Me.m_swarm.Count - 1
                End If

            Catch ex As Exception
                Me.m_error.SetError(True, Util.clsError.ErrorType.ERR_INIT)
            Finally
                System.GC.Collect()
            End Try
        End Sub

        ''' <summary>
        ''' Do optimize
        ''' </summary>
        ''' <param name="ai_iteration"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function DoIteration(Optional ai_iteration As Integer = 0) As Boolean
            'Check Last Error
            If Me.IsRecentError() = True Then
                Return True
            End If

            'do iterate
            ai_iteration = If(ai_iteration = 0, Me.Iteration - 1, ai_iteration - 1)
            For iterate As Integer = 0 To ai_iteration
                'check iteration count
                If Iteration <= m_iteration Then
                    Me.m_swarm.Sort()
                    Me.m_error.SetError(True, Util.clsError.ErrorType.ERR_OPT_MAXITERATION)
                    Return True
                End If
                m_iteration += 1

                'check criterion
                If Me.IsUseCriterion = True Then
                    'higher N percentage particles are finished at the time of same evaluate value.
                    If clsUtil.IsCriterion(Me.EPS, Me.m_swarm(0).BestPoint, Me.m_swarm(Me.HigherNPercentIndex).BestPoint) Then
                        Return True
                    End If
                End If

                'PSO process
                For Each particle In Me.m_swarm
                    'replace personal best
                    If particle.Point.Eval < particle.BestPoint.Eval Then
                        particle.BestPoint = particle.Point.Copy()

                        'replace global best
                        If particle.Point.Eval < Me.m_globalBest.Eval Then
                            Me.m_globalBest = particle.Point.Copy()
                        End If
                    End If

                    'update a velocity 
                    For i As Integer = 0 To Me.m_func.NumberOfVariable - 1
                        Dim r1 = Me.m_rand.NextDouble()
                        Dim r2 = Me.m_rand.NextDouble()
                        Dim newV = Me.Weight * particle.Velocity(i) + _
                                   C1 * r1 * (particle.BestPoint(i) - particle.Point(i)) + _
                                   C2 * r2 * (Me.m_globalBest(i) - particle.Point(i))
                        particle.Velocity(i) = newV

                        'update a position using velocity
                        Dim newPos = particle.Point(i) + particle.Velocity(i)
                        particle.Point(i) = newPos
                    Next
                    particle.Point.ReEvaluate()
                Next

                'sort by eval
                Me.m_swarm.Sort()
            Next

            Return False
        End Function

        ''' <summary>
        ''' Recent Error
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function IsRecentError() As Boolean
            Return Me.m_error.IsError()
        End Function

        ''' <summary>
        ''' Result
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Result As Optimization.clsPoint
            Get
                Return Me.m_swarm(0).BestPoint
            End Get
        End Property

        ''' <summary>
        ''' for Debug
        ''' </summary>
        ''' <value></value>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides ReadOnly Property Results As List(Of Optimization.clsPoint)
            Get
                Dim ret As New List(Of clsPoint)(Me.m_swarm.Count - 1)
                For Each p In Me.m_swarm
                    ret.Add(p.BestPoint.Copy())
                Next
                Return ret
            End Get
        End Property
#End Region

#Region "Private"
#End Region
    End Class
End Namespace
