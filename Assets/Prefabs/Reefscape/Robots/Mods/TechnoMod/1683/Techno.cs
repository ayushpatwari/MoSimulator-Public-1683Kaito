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
    public class Techno: ReefscapeRobotBase
    {
        [Header("Robot Components")]
        [SerializeField] private GenericElevator elevator;
        [SerializeField] private GenericJoint elevatorArm;
        [SerializeField] private GenericJoint intakeArm;

        [Header("Climb")] 
        [SerializeField] private ClimbScorer climbScorer;
        [SerializeField] private Rigidbody intakeRigidBody;
        
        [Header("PID Constants")]
        [SerializeField] private PidConstants elevatorArmPid;
        [SerializeField] private PidConstants intakeArmPid;

        [Header("Robot Setpoints")]
        [SerializeField] private TechnoSetpoint stow;
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
        
        [Header("Game Piece Intakes")]
        [SerializeField] private ReefscapeGamePieceIntake coralIntake;
        [SerializeField] private ReefscapeGamePieceIntake algaeIntake;

        [Header("Audio")] 
        [SerializeField] private AudioSource intakeAudioSource;
        [SerializeField] private AudioClip intakeClip;
        [SerializeField] private AudioSource algaeAudioSource;
        [SerializeField] private AudioClip algaeClip; 
        
        [Header("Coral Transform")]
        [SerializeField] private Transform coralTarget;
        [SerializeField] private Transform coralSlider;

        [Header("Game Piece States")]
        [SerializeField] private GamePieceState coralStowState;

        [SerializeField] private GamePieceState coralL1StowState;
        [SerializeField] private GamePieceState algaeStowState;

        [Header("Game Piece Controllers")]
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _coralController;
        private RobotGamePieceController<ReefscapeGamePiece, ReefscapeGamePieceData>.GamePieceControllerNode
            _algaeController;

        [Header("Target Setpoints")] 
        private float _elevatorTargetHeight;
        private float _elevatorArmTargetAngle;
        private float _intakeArmTargetAngle;

        private ReefscapeAutoAlign autoAlign;
        
        private readonly float CoralScoringZOffset = 7;
        
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
            
            _algaeController.gamePieceStates = new []{algaeStowState};
            _algaeController.intakes.Add(algaeIntake);

            intakeAudioSource.clip = intakeClip;
            intakeAudioSource.loop = true;
            intakeAudioSource.Stop();

            algaeAudioSource.clip = algaeClip;
            algaeAudioSource.loop = true;
            algaeAudioSource.Stop();
            
            autoAlign = gameObject.GetComponent<ReefscapeAutoAlign>();
        }

        private void SetSetpoint(TechnoSetpoint setpoint)
        {
            _elevatorTargetHeight = setpoint.elevatorHeight;
            _elevatorArmTargetAngle = setpoint.elevatorAngle;
            _intakeArmTargetAngle = setpoint.intakeAngle;
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
                    autoAlign.offset = new Vector3(0, 0, CoralScoringZOffset + 1);
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

            UpdateCoralPosition();
            UpdateAutoAlignOffset();

            var canIntake = _coralController.currentStateNum == 0 && _algaeController.currentStateNum == 0;
            
            //Climb Logic

            if (climbScorer.AutoClimbTriggered && CurrentSetpoint == ReefscapeSetpoints.Climb)
            {
                intakeRigidBody.mass = 30;
            }
            
            switch (CurrentSetpoint)
            {
                case ReefscapeSetpoints.Stow:
                    SetSetpoint(stow);
                    
                    _algaeController.RequestIntake(algaeIntake, false);
                    _coralController.RequestIntake(coralIntake, false);
                    break;
                case ReefscapeSetpoints.Intake:
                    SetSetpoint(CurrentRobotMode == ReefscapeRobotMode.Coral ? hpIntake : groundAlgaeIntake);
                    

                    _algaeController.RequestIntake(algaeIntake, canIntake && CurrentRobotMode == ReefscapeRobotMode.Algae);
                    _coralController.RequestIntake(coralIntake, canIntake && CurrentRobotMode == ReefscapeRobotMode.Coral);
                    break;
                case ReefscapeSetpoints.Place:
                    if (LastSetpoint == ReefscapeSetpoints.Barge)
                    {
                        SetSetpoint(bargePlace);
                    }
                    PlacePiece();
                    break;
                case ReefscapeSetpoints.L1:
                    SetSetpoint(l1);
                    break;
                case ReefscapeSetpoints.Stack:
                    SetSetpoint(hpIntake);
                    break;
                case ReefscapeSetpoints.L2:
                    SetSetpoint(l2);
                    break;
                case ReefscapeSetpoints.LowAlgae:
                    SetSetpoint(lowAlgae);
                    _algaeController.RequestIntake(algaeIntake, canIntake);
                    break;
                case ReefscapeSetpoints.L3:
                    SetSetpoint(l3);
                    break;
                case ReefscapeSetpoints.HighAlgae:
                    SetSetpoint(highAlgae);
                    _algaeController.RequestIntake(algaeIntake, canIntake);
                    break;
                case ReefscapeSetpoints.L4:
                    SetSetpoint(l4);
                    break;
                case ReefscapeSetpoints.Processor:
                    SetSetpoint(processor);
                    break;
                case ReefscapeSetpoints.Barge:
                    SetSetpoint(bargePrep);
                    break;
                case ReefscapeSetpoints.RobotSpecial:
                    SetState(ReefscapeSetpoints.Stow);
                    break;
                case ReefscapeSetpoints.Climb:
                    SetSetpoint(climb);
                    break;
                case ReefscapeSetpoints.Climbed:
                    SetSetpoint(climbDown);
                    break;
            }
            
            UpdateSetpoints();
        }

        private void UpdateAutoAlignOffset()
        {
            if (coralIntake.GamePiece != null && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                var coralPositionMeters = coralTarget.transform
                    .InverseTransformPoint(coralIntake.GamePiece.transform.position).x;

                autoAlign.offset = new Vector3(-coralPositionMeters * 39.3701f, 0, CoralScoringZOffset);
            }
        }

        private void PlacePiece()
        {
            if (_algaeController.HasPiece())
            {
                if (LastSetpoint == ReefscapeSetpoints.Barge)
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 5, 7));
                }
                else
                {
                    _algaeController.ReleaseGamePieceWithForce(new Vector3(0, 0, 5));
                }
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
            }
        }

        private void UpdateCoralPosition()
        {
            if (coralIntake.GamePiece != null && CurrentIntakeMode == ReefscapeIntakeMode.Normal)
            {
                var localSliderSpaceX = coralTarget.transform.InverseTransformPoint(coralIntake.GamePiece.transform.position).x;
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

            if ((IntakeAction.IsPressed() || OuttakeAction.IsPressed() || CurrentSetpoint is ReefscapeSetpoints.Climb) &&
                !intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Play();
            }
            else if (!IntakeAction.IsPressed() && !OuttakeAction.IsPressed() && CurrentSetpoint is not ReefscapeSetpoints.Climb &&
                     intakeAudioSource.isPlaying)
            {
                intakeAudioSource.Stop();
            }

            if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum > 0 && !algaeAudioSource.isPlaying)
            {
                algaeAudioSource.Play();
            }
            else if (RobotGamePieceController.GetPieceByName("Algae").currentStateNum == 0 && algaeAudioSource.isPlaying)
            {
                algaeAudioSource.Stop();
            }
        }
    }
}