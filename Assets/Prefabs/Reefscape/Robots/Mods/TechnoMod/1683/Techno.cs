using System;
using System.Collections;
using Games.Reefscape.Enums;
using Games.Reefscape.FieldScripts;
using Games.Reefscape.GamePieceSystem;
using Games.Reefscape.Robots;
using Games.Reefscape.Scoring.Scorers;
using MoSimCore.BaseClasses.GameManagement;
using MoSimCore.Enums;
using RobotFramework.Components;
using RobotFramework.Controllers.GamePieceSystem;
using RobotFramework.Controllers.PidSystems;
using RobotFramework.Enums;
using RobotFramework.GamePieceSystem;
using UnityEngine;

namespace Prefabs.Reefscape.Robots.Mods._1683Mod._1683
{
    public class Techno : ReefscapeRobotBase
    {
        [Header("Robot Components")] [SerializeField]
        private GenericElevator elevator;

        [SerializeField] private GenericJoint elevatorArm;
        [SerializeField] private GenericJoint intakeArm;
        [SerializeField] private GenericJoint stage1Joint;
        [SerializeField] private GenericJoint stage2Joint;

        [Header("Intake")] [SerializeField] private GenericAnimationJoint[] intakeWheels;
        [SerializeField] private float wheelIntakeSpeed = 10f;
        private bool _isScoring;
        private bool _alreadyPlaced;
        [SerializeField] private Transform algaeTarget;
        private bool _isAlgaeGroundMode;
        [SerializeField] private Transform coralTarget;
        [SerializeField] private Transform coralSlider;

        [Header("Game Piece Intakes")] [SerializeField]
        private ReefscapeGamePieceIntake coralIntake;

        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Climb")] [SerializeField] private ClimbScorer climbScorer;
        [SerializeField] private Rigidbody intakeRigidBody;

        [Header("PID Constants")] [SerializeField]
        private PidConstants elevatorArmPid;

        [SerializeField] private PidConstants intakeArmPid;

        [Header("Robot Setpoints")] [SerializeField]
        private TechnoSetpoint stow;

        [SerializeField] private TechnoSetpoint hpIntake;
        [SerializeField] private TechnoSetpoint groundAlgaeIntake;
        [SerializeField] private TechnoSetpoint l1;
        [SerializeField] private TechnoSetpoint l2;
        [SerializeField] private TechnoSetpoint l3;
        [SerializeField] private TechnoSetpoint l4;
        [SerializeField] private TechnoSetpoint lowAlgae;
        [SerializeField] private TechnoSetpoint highAlgae;
        [SerializeField] private TechnoSetpoint bargePrep;
        [SerializeField] private TechnoSetpoint bargePlace;
        [SerializeField] private TechnoSetpoint processor;
        [SerializeField] private TechnoSetpoint climb;
        [SerializeField] private TechnoSetpoint climbDown;

        [Header("Audio")] [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;
        [SerializeField] private AudioSource algaeAudioSource;
        [SerializeField] private AudioClip algaeClip;

        [Header("Game Piece States")] [SerializeField]
        private GamePieceState coralStowState;

        [SerializeField] private GamePieceState coralL1StowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Game Piece Controllers")]
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _coralController;

        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _algaeController;

        [Header("Target Setpoints")] private float _elevatorTargetHeight;
        private float _elevatorArmTargetAngle;
        private float _intakeArmTargetAngle;

        private ReefscapeAutoAlign _autoAlign;

        private readonly float _coralScoringZOffset = 7;

        private ReefscapeSetpoints _previousSetpoint = ReefscapeSetpoints.Stow;

        protected override void Start()
        {
            base.Start();

            climbScorer = gameObject.GetComponent<ClimbScorer>();
            elevatorArm.SetPid(elevatorArmPid);
            intakeArm.SetPid(intakeArmPid);

            _elevatorTargetHeight = stow.elevatorHeight;
            _elevatorArmTargetAngle = stow.elevatorAngle;
            _intakeArmTargetAngle = stow.intakeAngle;

            RobotGamePieceController.SetPreload(coralStowState);
            _coralController =
                RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Coral.ToString());
            _algaeController =
                RobotGamePieceController.GetPieceByName(ReefscapeGamePieceType.Algae.ToString());

            _coralController.gamePieceStates = new[]
            {
                coralStowState,
                coralL1StowState
            };
            _coralController.intakes.Add(coralIntake);

            _algaeController.gamePieceStates = new[] { algaeStowState };
            _algaeController.intakes.Add(algaeIntake);

            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.Stop();

            algaeAudioSource.clip = algaeClip;
            algaeAudioSource.loop = true;
            algaeAudioSource.Stop();

            _autoAlign = gameObject.GetComponent<ReefscapeAutoAlign>();
        }

        private void UpdateSetpoints()
        {
            elevator.SetTarget(_elevatorTargetHeight);
            elevatorArm.SetTargetAngle(_elevatorArmTargetAngle).withAxis(JointAxis.X);
            intakeArm.SetTargetAngle(_intakeArmTargetAngle).withAxis(JointAxis.X);
        }

        private void LateUpdate()
        {
            elevatorArm.UpdatePid(elevatorArmPid);
            intakeArm.UpdatePid(intakeArmPid);
        }

        private void FixedUpdate()
        {

            UpdateAudio();
            _algaeController.SetTargetState(algaeStowState);

            if (CurrentIntakeMode == ReefscapeIntakeMode.L1)
            {
                if (CurrentCoralStationMode.DropOrientation == DropOrientation.Vertical)
                {
                    CurrentCoralStationMode.DropOrientation = DropOrientation.Horizontal;
                    coralIntake.ChangeTarget(coralL1StowState.stateTarget);
                    _autoAlign.offset = new Vector3(0, 0, _coralScoringZOffset - 2);
                }

                _coralController.SetTargetState(coralL1StowState);
            }
            else
            {
                if (CurrentCoralStationMode.DropOrientation == DropOrientation.Horizontal)
                {
                    CurrentCoralStationMode.DropOrientation = DropOrientation.Vertical;
                    coralIntake.ChangeTarget(coralStowState.stateTarget);

                }

                _coralController.SetTargetState(coralStowState);
            }

            if (CurrentRobotMode == ReefscapeRobotMode.Algae && !_isAlgaeGroundMode)
            {
                algaeIntake.ChangeTarget(algaeStowState.stateTarget);
                _isAlgaeGroundMode = true;
            }
            else if (CurrentRobotMode == ReefscapeRobotMode.Coral && _isAlgaeGroundMode)
            {
                algaeIntake.ChangeTarget(algaeTarget);
                _isAlgaeGroundMode = false;
            }

            UpdateCoralPosition();
            UpdateAutoAlignOffset();

            var canIntake = _coralController.currentStateNum == 0 && _algaeController.currentStateNum == 0;

            if (!_isScoring)
            {
                bool isIntaking = IntakeAction.IsPressed();

                if (isIntaking)
                {
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(wheelIntakeSpeed).useAxis(JointAxis.Z);
                }
                else
                {
                    // Explicitly stop wheel animations
                    foreach (var wheel in intakeWheels)
                        wheel.VelocityRoller(0).useAxis(JointAxis.Z);
                }
            }

            //Climb Logic

            if (climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb)
            {
                intakeRigidBody.mass = 30;
                stage1Joint.lockAllAxis();
                stage2Joint.lockAllAxis();

            }
            else if (CurrentSetpoint != ReefscapeSetpoints.Climb && CurrentSetpoint != ReefscapeSetpoints.Climbed
                                                                 && intakeRigidBody.mass.Equals(30.0f))
            {
                intakeRigidBody.mass = 1;
                stage1Joint.freeLinearAxis(JointAxis.Y);
                stage2Joint.freeLinearAxis(JointAxis.Y);
            }

            if (_previousSetpoint == ReefscapeSetpoints.Place && CurrentSetpoint != ReefscapeSetpoints.Place)
            {
                _alreadyPlaced = false;
            }

            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    StartCoroutine(ControlledSetSetpoint(stow));

                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.Intake:
                    StartCoroutine(ControlledSetSetpoint
                        (CurrentRobotMode == ReefscapeRobotMode.Coral ? hpIntake : groundAlgaeIntake)
                    );

                    _algaeController.RequestIntake(algaeIntake,
                        canIntake && CurrentRobotMode == ReefscapeRobotMode.Algae);
                    _coralController.RequestIntake(coralIntake,
                        canIntake && CurrentRobotMode == ReefscapeRobotMode.Coral);
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Barge)
                    {
                        StartCoroutine(ControlledSetSetpoint(bargePlace));
                    }

                    StartCoroutine(PlaceCoroutine());
                    break;
                case ReefscapeSetpoints.L1:
                    StartCoroutine(ControlledSetSetpoint(l1));
                    break;
                case ReefscapeSetpoints.Stack:
                    StartCoroutine(ControlledSetSetpoint(hpIntake));
                    break;
                case ReefscapeSetpoints.L2:
                    StartCoroutine(ControlledSetSetpoint(l2));
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    StartCoroutine(ControlledSetSetpoint(lowAlgae));
                    _algaeController.RequestIntake(algaeIntake, canIntake);
                    break;
                case ReefscapeSetpoints.L3:
                    StartCoroutine(ControlledSetSetpoint(l3));
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    StartCoroutine(ControlledSetSetpoint(highAlgae));
                    _algaeController.RequestIntake(algaeIntake, canIntake);
                    break;
                case ReefscapeSetpoints.L4:
                    StartCoroutine(ControlledSetSetpoint(l4));
                    break;
                case ReefscapeSetpoints.Processor:
                    StartCoroutine(ControlledSetSetpoint(processor));
                    break;
                case ReefscapeSetpoints.Barge:
                    StartCoroutine(ControlledSetSetpoint(bargePrep));
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    StartCoroutine(ControlledSetSetpoint(climb));
                    break;
                case ReefscapeSetpoints.Climbed:
                    StartCoroutine(ControlledSetSetpoint(climbDown));
                    break;
            }

            _previousSetpoint = CurrentSetpoint;

            UpdateSetpoints();
        }
        
        private IEnumerator ControlledSetSetpoint(TechnoSetpoint setpoint)
        {
            if (setpoint.elevatorHeight > _elevatorTargetHeight)
            {
                _elevatorArmTargetAngle = setpoint.elevatorAngle;
                _intakeArmTargetAngle = setpoint.intakeAngle;
                yield return new WaitUntil(() =>
                    IsNear(elevatorArm.GetSingleAxisAngle(JointAxis.X), setpoint.elevatorAngle, 2f)
                    && IsNear(intakeArm.GetSingleAxisAngle(JointAxis.X), setpoint.intakeAngle, 5f)
                    && (setpoint == l4 ? (_autoAlign.enabled && _autoAlign.getDistance() < 0.2) : true )
                );
                _elevatorTargetHeight = setpoint.elevatorHeight;
            }
            else
            {
                _intakeArmTargetAngle = setpoint.intakeAngle;
                _elevatorTargetHeight = setpoint.elevatorHeight;
                yield return new WaitUntil(() =>
                    IsNear(intakeArm.GetSingleAxisAngle(JointAxis.X), setpoint.intakeAngle, 5f)
                    && IsNear(elevator.GetElevatorHeight(), setpoint.elevatorHeight, 10f)
                );
                _elevatorArmTargetAngle = setpoint.elevatorAngle;
            }
        }
        
        private IEnumerator PlaceCoroutine()
        {
            if (_alreadyPlaced)
                yield break;

            _isScoring = true;
            StartCoroutine(PlacePiece());
            
            float timer = 0;
            while (timer < 0.5f)
            {
                foreach (var wheel in intakeWheels) wheel.VelocityRoller(-wheelIntakeSpeed);
                timer += Time.deltaTime;
                yield return null;
            }

            foreach (var wheel in intakeWheels) wheel.VelocityRoller(0);

            _isScoring = false;
        }

        private IEnumerator PlacePiece()
        {
            if (_alreadyPlaced)
            {
                yield break;
            }

            if (_algaeController.HasPiece())
            {
                if (LastSetpoint == ReefscapeSetpoints.Barge)   
                {
                    yield return new WaitUntil(() => elevator.GetElevatorHeight() > 32.5);
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 6.5f, 4.5f));
                }
                else
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
                }

                _alreadyPlaced = true;
            }
            else
            {
                if (LastSetpoint == ReefscapeSetpoints.L4)
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 9));
                }
                else
                {
                    _coralController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
                }

                _alreadyPlaced = true;
            }
        }

        private void UpdateAutoAlignOffset()
        {
            if (coralIntake.GamePiece != null && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                var coralPositionMeters = coralTarget.transform
                    .InverseTransformPoint(coralIntake.GamePiece.transform.position).x;

                _autoAlign.offset = new Vector3(-coralPositionMeters * 39.3701f, 0, _coralScoringZOffset);
            }
        }

        private void UpdateCoralPosition()
        {
            if (coralIntake.GamePiece != null && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                var localSliderSpaceX =
                    coralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).x;
                coralSlider.localPosition = new Vector3(localSliderSpaceX, 0, 0);
            }
        }

        private void UpdateAudio()
        {
            if (BaseGameManager.Instance.RobotState == RobotState.Disabled)
            {
                if (intakeAudioSource.isPlaying || algaeAudioSource.isPlaying)
                {
                    intakeAudioSource.Stop();
                    algaeAudioSource.Stop();
                }

                return;
            }

            if ((IntakeAction.IsPressed() || OuttakeAction.IsPressed()) &&
                !intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Play();
            }
            else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() &&
                     intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Stop();
            }

            if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0 && !algaeAudioSource.isPlaying)
            {
                algaeAudioSource.Play();
            }
            else if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum == 0 &&
                     algaeAudioSource.isPlaying)
            {
                algaeAudioSource.Stop();
            }
        }

        private bool IsNear(float number1, float number2, float tolerance)
        {
            return Math.Abs(number1 - number2) <= tolerance;
        }
    }
}