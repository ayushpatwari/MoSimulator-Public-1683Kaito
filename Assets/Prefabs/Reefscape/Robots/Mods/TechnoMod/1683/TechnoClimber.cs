using RobotFramework.Components;
using UnityEngine;

namespace Robots.Climbing
{
    public class TechnoClimber : MonoBehaviour
    {
        [Header("Barb Joints")] 
        [SerializeField] private GenericAnimationJoint _barbTL;
        [SerializeField] private GenericAnimationJoint _barbTR;
        [SerializeField] private GenericAnimationJoint _barbBL;
        [SerializeField] private GenericAnimationJoint  _barbBR;

        [SerializeField] private float ClickerSpeed = 120f;

        private void Update()
        {
            _barbTL.SpringLoaded().AllowedDirection(-1).RotationSpeed(ClickerSpeed);
            _barbTR.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
            _barbBL.SpringLoaded().AllowedDirection(-1).RotationSpeed(ClickerSpeed);
            _barbBR.SpringLoaded().AllowedDirection(1).RotationSpeed(ClickerSpeed);
        }
    }
}